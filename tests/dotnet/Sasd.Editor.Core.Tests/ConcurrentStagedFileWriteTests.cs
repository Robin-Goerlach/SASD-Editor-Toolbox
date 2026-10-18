using Sasd.Editor.IO;
using Xunit;

namespace Sasd.Editor.Core.Tests;

/// <summary>
/// Tests the ordering contract for built-in file publication with explicit
/// TaskCompletionSource gates. No test depends on a large file or wall-clock
/// scheduling of disk operations.
/// </summary>
public sealed class ConcurrentStagedFileWriteTests
{
    [Fact]
    public async Task SameTarget_SecondWriterCannotStageOrPublishBeforeFirstFinishes()
    {
        var directory = CreateDirectory();
        try
        {
            var target = Path.Combine(directory, "shared.txt");
            await File.WriteAllTextAsync(target, "original");
            var first = new PausedStage();
            var secondEntered = false;

            var earlierWrite = StagedFileWriter.WriteAsync(target, first.WriteAsync).AsTask();
            await first.Started;

            // The later write is invoked while the first stage is suspended.
            // If the second writer overtook, the first could publish old bytes
            // *after* the later save had already acknowledged the new bytes.
            var laterWrite = StagedFileWriter.WriteAsync(target,
                async (stage, token) =>
                {
                    secondEntered = true;
                    await File.WriteAllTextAsync(stage, "latest", token);
                }).AsTask();

            Assert.False(secondEntered);
            Assert.False(laterWrite.IsCompleted);
            Assert.Equal("original", await File.ReadAllTextAsync(target));

            first.Continue();
            await earlierWrite;
            await laterWrite;

            Assert.True(secondEntered);
            Assert.Equal("latest", await File.ReadAllTextAsync(target));
            Assert.Equal(new[] { "shared.txt" }, Directory.GetFiles(directory).Select(Path.GetFileName));
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }

    [Fact]
    public async Task DifferentTargets_AreNotBlockedByAnUnrelatedStalledWriter()
    {
        var directory = CreateDirectory();
        try
        {
            var firstTarget = Path.Combine(directory, "first.txt");
            var otherTarget = Path.Combine(directory, "second.txt");
            var paused = new PausedStage();
            var earlierWrite = StagedFileWriter.WriteAsync(firstTarget, paused.WriteAsync).AsTask();
            await paused.Started;

            // Another file has a different target gate and must be able to
            // complete even though the first callback has not been released.
            await StagedFileWriter.WriteAsync(otherTarget,
                (stage, token) => File.WriteAllTextAsync(stage, "independent", token));

            Assert.False(earlierWrite.IsCompleted);
            Assert.Equal("independent", await File.ReadAllTextAsync(otherTarget));
            paused.Continue();
            await earlierWrite;
            Assert.Equal("earlier", await File.ReadAllTextAsync(firstTarget));
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }

    [Fact]
    public async Task CancelledWaiter_DoesNotWriteOrPublishAndLeavesGateReusable()
    {
        var directory = CreateDirectory();
        try
        {
            var target = Path.Combine(directory, "shared.txt");
            await File.WriteAllTextAsync(target, "previous");
            var paused = new PausedStage();
            var writing = StagedFileWriter.WriteAsync(target, paused.WriteAsync).AsTask();
            await paused.Started;
            using var cancellation = new CancellationTokenSource();
            var touchedStage = false;

            var rejected = StagedFileWriter.WriteAsync(target,
                (stage, _) =>
                {
                    touchedStage = true;
                    return Task.CompletedTask;
                }, cancellation.Token).AsTask();

            cancellation.Cancel();
            await Assert.ThrowsAnyAsync<OperationCanceledException>(() => rejected);
            Assert.False(touchedStage);
            Assert.Equal("previous", await File.ReadAllTextAsync(target));

            paused.Continue();
            await writing;
            Assert.Equal("earlier", await File.ReadAllTextAsync(target));

            // A cancelled waiter must not poison the shared gate or retain
            // an accidental reservation that blocks future saves.
            await StagedFileWriter.WriteAsync(target,
                (stage, token) => File.WriteAllTextAsync(stage, "successor", token));
            Assert.Equal("successor", await File.ReadAllTextAsync(target));
            Assert.Equal(new[] { "shared.txt" }, Directory.GetFiles(directory).Select(Path.GetFileName));
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }

    [Fact]
    public async Task StageFailure_ReleasesGateForWaitingWriterAndPreservesOldBytesUntilPublish()
    {
        var directory = CreateDirectory();
        try
        {
            var target = Path.Combine(directory, "shared.txt");
            await File.WriteAllTextAsync(target, "original");
            var stageCreated = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            var continueFailedStage = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            var exception = new IOException("simulated stage failure");

            var failing = StagedFileWriter.WriteAsync(target,
                async (stage, token) =>
                {
                    await File.WriteAllTextAsync(stage, "incomplete", token);
                    stageCreated.TrySetResult();
                    await continueFailedStage.Task.WaitAsync(token);
                    throw exception;
                }).AsTask();
            await stageCreated.Task;

            var successor = StagedFileWriter.WriteAsync(target,
                (stage, token) => File.WriteAllTextAsync(stage, "complete", token)).AsTask();

            Assert.False(successor.IsCompleted);
            Assert.Equal("original", await File.ReadAllTextAsync(target));
            continueFailedStage.TrySetResult();
            Assert.Same(exception, await Assert.ThrowsAsync<IOException>(() => failing));
            await successor;

            Assert.Equal("complete", await File.ReadAllTextAsync(target));
            Assert.Equal(new[] { "shared.txt" }, Directory.GetFiles(directory).Select(Path.GetFileName));
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }

    [Fact]
    public async Task EquivalentNormalizedPaths_CannotBypassTargetGate()
    {
        var directory = CreateDirectory();
        try
        {
            var subdir = Path.Combine(directory, "sub");
            Directory.CreateDirectory(subdir);
            var target = Path.Combine(directory, "file.txt");
            var alternativeSpelling = Path.Combine(subdir, "..", "file.txt");
            var paused = new PausedStage();
            var first = StagedFileWriter.WriteAsync(target, paused.WriteAsync).AsTask();
            await paused.Started;

            var enteredOther = false;
            var second = StagedFileWriter.WriteAsync(alternativeSpelling,
                async (stage, token) =>
                {
                    enteredOther = true;
                    await File.WriteAllTextAsync(stage, "new", token);
                }).AsTask();

            Assert.False(enteredOther);
            paused.Continue();
            await Task.WhenAll(first, second);
            Assert.True(enteredOther);
            Assert.Equal("new", await File.ReadAllTextAsync(target));
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }

    private static string CreateDirectory()
    {
        var directory = Path.Combine(Path.GetTempPath(), $"sasd-ordered-write-{Guid.NewGuid():N}");
        Directory.CreateDirectory(directory);
        return directory;
    }

    private sealed class PausedStage
    {
        private readonly TaskCompletionSource _started =
            new(TaskCreationOptions.RunContinuationsAsynchronously);
        private readonly TaskCompletionSource _released =
            new(TaskCreationOptions.RunContinuationsAsynchronously);

        public Task Started => _started.Task;
        public void Continue() => _released.TrySetResult();

        public async Task WriteAsync(string stage, CancellationToken token)
        {
            await File.WriteAllTextAsync(stage, "earlier", token);
            _started.TrySetResult();
            await _released.Task.WaitAsync(token);
        }
    }
}
