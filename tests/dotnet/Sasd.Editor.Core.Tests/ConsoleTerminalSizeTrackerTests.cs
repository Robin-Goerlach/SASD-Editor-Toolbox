using Sasd.Editor.FirstEd.Sample;
using Xunit;

namespace Sasd.Editor.Core.Tests;

/// <summary>
/// Pure host-side tests for the resize trigger. Interactive validation under
/// Windows Terminal and a native Linux PTY remains a separate release gate.
/// </summary>
public sealed class ConsoleTerminalSizeTrackerTests
{
    [Fact]
    public void FirstObservationSetsBaselineWithoutRepainting()
    {
        var tracker = new ConsoleTerminalSizeTracker();
        Assert.False(tracker.Observe(100, 35));
        Assert.False(tracker.Observe(100, 35));
    }

    [Fact]
    public void BothDimensionsCauseExactlyOneChangePerNewGeometry()
    {
        var tracker = new ConsoleTerminalSizeTracker();
        tracker.Observe(100, 35);

        Assert.True(tracker.Observe(120, 35));
        Assert.False(tracker.Observe(120, 35));
        Assert.True(tracker.Observe(120, 40));
        Assert.False(tracker.Observe(120, 40));
        Assert.True(tracker.Observe(80, 25));
    }

    [Fact]
    public void InvalidTransientDimensionsAreNormalized()
    {
        var tracker = new ConsoleTerminalSizeTracker();
        Assert.False(tracker.Observe(0, 0));
        Assert.False(tracker.Observe(-1, 1));
        Assert.True(tracker.Observe(2, 1));
    }
}
