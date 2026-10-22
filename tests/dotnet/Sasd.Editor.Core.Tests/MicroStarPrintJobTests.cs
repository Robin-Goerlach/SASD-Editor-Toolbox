using Sasd.Editor.Editing;
using Sasd.Editor.FirstEd.Sample;
using Xunit;

namespace Sasd.Editor.Core.Tests;

/// <summary>
/// Background printing tests do not require a physical printer or sleeps.
/// The existing input-first scheduler chooses when slices may run.
/// </summary>
public sealed class MicroStarPrintJobTests
{
    [Fact]
    public async Task EachIdleCyclePrintsAtMostOneLine()
    {
        var session = new EditorSession();
        var sink = new RecordingSink();
        var queue = new MicroStarPrintQueue(session);
        Assert.True(queue.TryStart(["alpha", "beta", "gamma"], sink));
        await session.Scheduler.RunIdleCycleAsync();
        Assert.Equal(["alpha"], sink.Lines);
        Assert.Equal(1, queue.Active!.CompletedLines);
        await session.Scheduler.RunIdleCycleAsync();
        await session.Scheduler.RunIdleCycleAsync();
        Assert.Equal(["alpha", "beta", "gamma"], sink.Lines);
        Assert.True(queue.Active.IsFinished);
        await session.Scheduler.RunIdleCycleAsync();
        Assert.Equal(3, sink.Lines.Count);
    }

    [Fact]
    public async Task SnapshotIsUnaffectedByLaterSourceMutation()
    {
        var session = new EditorSession();
        var source = new List<string> { "old" };
        var sink = new RecordingSink();
        var queue = new MicroStarPrintQueue(session);
        Assert.True(queue.TryStart(source, sink));
        source[0] = "new";
        await session.Scheduler.RunIdleCycleAsync();
        Assert.Equal(["old"], sink.Lines);
    }

    [Fact]
    public async Task CancelledJobNeverPrintsNextLine()
    {
        var session = new EditorSession();
        var sink = new RecordingSink();
        var queue = new MicroStarPrintQueue(session);
        Assert.True(queue.TryStart(["first", "second"], sink));
        await session.Scheduler.RunIdleCycleAsync();
        queue.Cancel();
        await session.Scheduler.RunIdleCycleAsync();
        Assert.Equal(["first"], sink.Lines);
        Assert.True(queue.Active!.IsCancelled);
    }

    [Fact]
    public async Task PrinterErrorIsRecordedWithoutRepeatingUnknownOutput()
    {
        var session = new EditorSession();
        var sink = new RecordingSink { Fail = true };
        var queue = new MicroStarPrintQueue(session);
        Assert.True(queue.TryStart(["a", "b"], sink));
        await session.Scheduler.RunIdleCycleAsync();
        await session.Scheduler.RunIdleCycleAsync();
        Assert.IsType<IOException>(queue.Active!.Failure);
        Assert.Equal(0, queue.Active.CompletedLines);
        Assert.Equal(1, sink.Attempts);
    }

    [Fact]
    public void SecondJobCannotReplaceUnfinishedJob()
    {
        var session = new EditorSession();
        var queue = new MicroStarPrintQueue(session);
        var sink = new RecordingSink();
        Assert.True(queue.TryStart(["a"], sink));
        Assert.False(queue.TryStart(["b"], sink));
        Assert.Single(session.Scheduler.Tasks);
    }

    [Fact]
    public void EmptyPrintJobDoesNotRegisterSchedulerWork()
    {
        var session = new EditorSession();
        var queue = new MicroStarPrintQueue(session);
        Assert.True(queue.TryStart([], new RecordingSink()));
        Assert.True(queue.Active!.IsFinished);
        Assert.Empty(session.Scheduler.Tasks);
    }

    [Fact]
    public async Task CancellationInsideOutputSliceClosesOwnedSinkAndPreservesCancellation()
    {
        var session = new EditorSession();
        using var cancellation = new CancellationTokenSource();
        var sink = new CancelledOutputSink();
        var queue = new MicroStarPrintQueue(session);
        Assert.True(queue.TryStart(["first", "second"], sink));

        // Simulate cancellation while an output adapter has already been
        // invoked. This differs from cancellation before scheduler dispatch:
        // the adapter is now owned by the job and must be released.
        cancellation.Cancel();
        // The scheduler normally refuses a pre-cancelled token, so invoke
        // the slice with a fresh token that the sink itself cancels.
        using var duringWrite = new CancellationTokenSource();
        sink.CancelDuringWrite = duringWrite;
        await Assert.ThrowsAnyAsync<OperationCanceledException>(async () =>
            await queue.Active!.ExecuteSliceAsync(session, duringWrite.Token));

        Assert.True(queue.Active!.IsCancelled);
        Assert.True(sink.Disposed);
        Assert.Equal(0, queue.Active.CompletedLines);
        await session.Scheduler.RunIdleCycleAsync();
        Assert.Equal(1, sink.Attempts);
    }

    private sealed class CancelledOutputSink : IMicroStarPrintSink, IDisposable
    {
        internal CancellationTokenSource? CancelDuringWrite { get; set; }
        internal int Attempts { get; private set; }
        internal bool Disposed { get; private set; }

        public ValueTask WriteLineAsync(string text, CancellationToken cancellationToken)
        {
            Attempts++;
            CancelDuringWrite!.Cancel();
            cancellationToken.ThrowIfCancellationRequested();
            return ValueTask.CompletedTask;
        }

        public void Dispose() => Disposed = true;
    }

    private sealed class RecordingSink : IMicroStarPrintSink
    {
        internal List<string> Lines { get; } = [];
        internal int Attempts { get; private set; }
        internal bool Fail { get; init; }

        public ValueTask WriteLineAsync(string text, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            Attempts++;
            if (Fail) throw new IOException("Simulated printer failure.");
            Lines.Add(text);
            return ValueTask.CompletedTask;
        }
    }
}
