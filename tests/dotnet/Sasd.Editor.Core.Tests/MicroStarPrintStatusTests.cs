using Sasd.Editor.Editing;
using Sasd.Editor.FirstEd.Sample;
using Xunit;

namespace Sasd.Editor.Core.Tests;

/// <summary>
/// A status query must be observational: the test advances the existing
/// cooperative scheduler explicitly and never relies on real printer I/O.
/// </summary>
public sealed class MicroStarPrintStatusTests
{
    [Fact]
    public void NoJobHasAnExplicitMessage()
    {
        Assert.Equal("No print job has been started.", MicroStarPrintStatus.Describe(null));
    }

    [Fact]
    public async Task ProgressCompletionAndCancellationRemainDistinct()
    {
        var session = new EditorSession();
        var queue = new MicroStarPrintQueue(session);
        Assert.True(queue.TryStart(["first", "second"], new Sink()));
        Assert.Equal("Printing: 0/2 records.", MicroStarPrintStatus.Describe(queue.Active));
        await session.Scheduler.RunIdleCycleAsync();
        Assert.Equal("Printing: 1/2 records.", MicroStarPrintStatus.Describe(queue.Active));
        await session.Scheduler.RunIdleCycleAsync();
        Assert.Equal("Print complete: 2/2 records.", MicroStarPrintStatus.Describe(queue.Active));

        Assert.True(queue.TryStart(["next"], new Sink()));
        queue.Cancel();
        Assert.Equal("Print cancelled after 0/1 records.", MicroStarPrintStatus.Describe(queue.Active));
    }

    [Fact]
    public async Task OutputFailureCannotBeReportedAsSuccess()
    {
        var session = new EditorSession();
        var queue = new MicroStarPrintQueue(session);
        Assert.True(queue.TryStart(["first"], new Sink(fail: true)));
        await session.Scheduler.RunIdleCycleAsync();
        Assert.Equal("Print failed after 0/1 records.", MicroStarPrintStatus.Describe(queue.Active));
    }

    private sealed class Sink(bool fail = false) : IMicroStarPrintSink
    {
        public ValueTask WriteLineAsync(string text, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (fail) throw new IOException("Simulated printer failure.");
            return ValueTask.CompletedTask;
        }
    }
}
