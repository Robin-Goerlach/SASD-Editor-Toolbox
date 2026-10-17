using System.Text;
using Sasd.Editor.Editing;
using Sasd.Editor.IO;
using Sasd.Editor.Model;
using Xunit;

namespace Sasd.Editor.Core.Tests;

/// <summary>
/// Verifies safe publication rather than relying on the timing of a large
/// filesystem write. The injected staging callback simulates a partial write
/// and failure deterministically before the destination is replaced.
/// </summary>
public sealed class StagedFileWriteTests
{
    [Fact]
    public async Task CancelDuringStaging_LeavesPreviousBytesAndRemovesPartialFile()
    {
        var directory = CreateDirectory();
        try
        {
            var target = Path.Combine(directory, "existing.txt");
            var oldBytes = new byte[] { 0x41, 0x8D, 0x42, 0x0D, 0x0A, 0x43 };
            await File.WriteAllBytesAsync(target, oldBytes);
            using var cancellation = new CancellationTokenSource();
            var started = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            var staged = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);

            var writing = StagedFileWriter.WriteAsync(
                target,
                async (temporary, token) =>
                {
                    await File.WriteAllBytesAsync(temporary, [0x58, 0x8D], token);
                    started.TrySetResult();
                    await staged.Task.WaitAsync(token);
                },
                cancellation.Token).AsTask();

            await started.Task;
            cancellation.Cancel();

            await Assert.ThrowsAnyAsync<OperationCanceledException>(() => writing);
            Assert.Equal(oldBytes, await File.ReadAllBytesAsync(target));
            Assert.Equal(new[] { "existing.txt" }, Directory.GetFiles(directory).Select(Path.GetFileName));
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }

    [Fact]
    public async Task StagingFailure_DoesNotTruncateDestinationOrLeaveTemporaryFile()
    {
        var directory = CreateDirectory();
        try
        {
            var target = Path.Combine(directory, "existing.txt");
            await File.WriteAllTextAsync(target, "important");
            var problem = new IOException("simulated staged output failure");

            var caught = await Assert.ThrowsAsync<IOException>(
                async () => await StagedFileWriter.WriteAsync(
                    target,
                    async (temporary, token) =>
                    {
                        await File.WriteAllTextAsync(temporary, "partial", token);
                        throw problem;
                    }));

            Assert.Same(problem, caught);
            Assert.Equal("important", await File.ReadAllTextAsync(target));
            Assert.Equal(new[] { "existing.txt" }, Directory.GetFiles(directory).Select(Path.GetFileName));
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }

    [Fact]
    public async Task SuccessfulStaging_ReplacesDestinationAndLeavesNoTemporaryFile()
    {
        var directory = CreateDirectory();
        try
        {
            var target = Path.Combine(directory, "existing.txt");
            await File.WriteAllTextAsync(target, "old");
            await StagedFileWriter.WriteAsync(
                target,
                (temporary, token) => File.WriteAllTextAsync(temporary, "new", token));

            Assert.Equal("new", await File.ReadAllTextAsync(target));
            Assert.Equal(new[] { "existing.txt" }, Directory.GetFiles(directory).Select(Path.GetFileName));
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }

    [Fact]
    public async Task LegacyCodec_OverwritePreservesWrappedBytesAndHasNoStagingArtifact()
    {
        var directory = CreateDirectory();
        try
        {
            var path = Path.Combine(directory, "legacy.txt");
            await File.WriteAllTextAsync(path, "previous");
            var codec = new FirstEdLegacyFileCodec();
            await codec.WriteAsync(path,
            [
                new EditorLineSnapshot("eins", EditorLineFlags.Wrapped),
                new EditorLineSnapshot("zwei")
            ]);

            Assert.Equal(new byte[] { 0x65, 0x69, 0x6E, 0x73, 0x8D, 0x7A, 0x77, 0x65, 0x69 },
                await File.ReadAllBytesAsync(path));
            var lines = await codec.ReadAsync(path);
            Assert.Equal(2, lines.Count);
            Assert.Equal(EditorLineFlags.Wrapped, lines[0].Flags);
            Assert.Equal(new[] { "legacy.txt" }, Directory.GetFiles(directory).Select(Path.GetFileName));
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }

    [Fact]
    public async Task ModernStorage_PreCancelledSavePreservesExistingBytesAndDirtyState()
    {
        var directory = CreateDirectory();
        try
        {
            var target = Path.Combine(directory, "modern.txt");
            await File.WriteAllTextAsync(target, "unchanged");
            var session = new EditorSession();
            var view = session.CreateDocument("changed");
            session.Engine.InsertText("new ");
            using var cancellation = new CancellationTokenSource();
            cancellation.Cancel();

            await Assert.ThrowsAnyAsync<OperationCanceledException>(
                async () => await new FileTextStorage()
                    .SaveAsync(view.Document, target, cancellation.Token));

            Assert.Equal("unchanged", await File.ReadAllTextAsync(target));
            Assert.True(view.Document.IsDirty);
            Assert.Null(view.Document.FilePath);
            Assert.Equal(1, session.Undo.Count);
            Assert.Equal(new[] { "modern.txt" }, Directory.GetFiles(directory).Select(Path.GetFileName));
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }

    [Fact]
    public async Task ModernStorage_ReplacesPreviousUtf8FileWithOriginalLineEndingStyle()
    {
        var directory = CreateDirectory();
        try
        {
            var target = Path.Combine(directory, "modern.txt");
            await File.WriteAllTextAsync(target, "old\r\n", new UTF8Encoding(false));
            var storage = new FileTextStorage();
            var document = await storage.LoadAsync(target);
            document.Buffer.ReplaceLine(0, "grün");
            document.MarkChanged();

            await storage.SaveAsync(document);

            Assert.Equal("grün\r\n", await File.ReadAllTextAsync(target, Encoding.UTF8));
            Assert.False(document.IsDirty);
            Assert.Equal(Path.GetFullPath(target), document.FilePath);
            Assert.Equal(new[] { "modern.txt" }, Directory.GetFiles(directory).Select(Path.GetFileName));
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }

    private static string CreateDirectory()
    {
        var directory = Path.Combine(Path.GetTempPath(), $"sasd-staged-{Guid.NewGuid():N}");
        Directory.CreateDirectory(directory);
        return directory;
    }
}
