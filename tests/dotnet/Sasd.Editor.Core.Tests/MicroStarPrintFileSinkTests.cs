using Sasd.Editor.Editing;
using Sasd.Editor.FirstEd.Sample;
using Xunit;

namespace Sasd.Editor.Core.Tests;

/// <summary>
/// Verify real print-file output using a temporary directory. A destination
/// is created exclusively and never overwrites existing bytes.
/// </summary>
public sealed class MicroStarPrintFileSinkTests
{
    [Fact]
    public async Task SchedulerWritesUtf8LinesThenReleasesFile()
    {
        var root = Path.Combine(Path.GetTempPath(), "sasd-print-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        try
        {
            var file = Path.Combine(root, "out.txt");
            var session = new EditorSession();
            var queue = new MicroStarPrintQueue(session);
            Assert.True(queue.TryStart(["one", "two"], new MicroStarPrintFileSink(file)));
            await session.Scheduler.RunIdleCycleAsync();
            Assert.Equal(1, queue.Active!.CompletedLines);
            await session.Scheduler.RunIdleCycleAsync();
            Assert.True(queue.Active.IsFinished);
            Assert.Equal(["one", "two"], await File.ReadAllLinesAsync(file));
        }
        finally { Directory.Delete(root, true); }
    }

    [Fact]
    public void ExistingDestinationIsNeverTruncated()
    {
        var root = Path.Combine(Path.GetTempPath(), "sasd-print-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        try
        {
            var file = Path.Combine(root, "keep.txt");
            File.WriteAllText(file, "original");
            Assert.Throws<IOException>(() => new MicroStarPrintFileSink(file));
            Assert.Equal("original", File.ReadAllText(file));
        }
        finally { Directory.Delete(root, true); }
    }

    [Fact]
    public async Task CancellationClosesPartialOutputAndPreventsFurtherLines()
    {
        var root = Path.Combine(Path.GetTempPath(), "sasd-print-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        try
        {
            var file = Path.Combine(root, "partial.txt");
            var session = new EditorSession();
            var queue = new MicroStarPrintQueue(session);
            Assert.True(queue.TryStart(["first", "second"], new MicroStarPrintFileSink(file)));
            await session.Scheduler.RunIdleCycleAsync();
            queue.Cancel();
            await session.Scheduler.RunIdleCycleAsync();
            Assert.Equal(["first"], await File.ReadAllLinesAsync(file));
        }
        finally { Directory.Delete(root, true); }
    }
}
