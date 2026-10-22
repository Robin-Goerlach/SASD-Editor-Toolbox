using Sasd.Editor.FirstEd.Sample;
using Xunit;

namespace Sasd.Editor.Core.Tests;

/// <summary>Headless list navigation and viewport invariants.</summary>
public sealed class MicroStarSelectionListTests
{
    private static MicroStarSelectionList Create(int rows = 3) => new(
        Enumerable.Range(0, 10).Select(i =>
            new MicroStarListItem($"id{i}", $"Entry {i}", Enabled: i != 4)), rows);

    [Fact]
    public void DownAndPageDownRevealSelectionWithoutLosingIdentity()
    {
        var list = Create();
        Assert.Equal("id0", list.Selected?.Id);
        Assert.Equal(MicroStarListResult.SelectionChanged, list.Handle(ConsoleKey.PageDown));
        Assert.Equal("id3", list.Selected?.Id);
        Assert.Equal(1, list.FirstVisible);
        list.Handle(ConsoleKey.DownArrow);
        Assert.Equal("id5", list.Selected?.Id); // disabled id4 is skipped
        Assert.True(list.FirstVisible <= 5);
        Assert.True(list.FirstVisible + list.VisibleRows > 5);
        Assert.Equal(3, list.Visible.Count);
    }

    [Fact]
    public void HomeAndEndRespectEnabledEntries()
    {
        var list = Create();
        list.Handle(ConsoleKey.End);
        Assert.Equal("id9", list.Selected?.Id);
        list.Handle(ConsoleKey.Home);
        Assert.Equal("id0", list.Selected?.Id);
    }

    [Fact]
    public void AcceptAndCancelAreDistinctFromNavigation()
    {
        var list = Create();
        Assert.Equal(MicroStarListResult.Accepted, list.Handle(ConsoleKey.Enter));
        Assert.Equal(MicroStarListResult.Cancelled, list.Handle(ConsoleKey.Escape));
        Assert.Equal(MicroStarListResult.Cancelled,
            list.Handle(ConsoleKey.U, ConsoleModifiers.Control));
        Assert.Equal("id0", list.Selected?.Id);
    }

    [Fact]
    public void EmptyAndDisabledOnlyListsCannotAccept()
    {
        var empty = new MicroStarSelectionList([], 4);
        var disabled = new MicroStarSelectionList([
            new("disabled", "Unavailable", false)
        ], 4);
        Assert.Null(empty.Selected);
        Assert.Null(disabled.Selected);
        Assert.Equal(MicroStarListResult.Ignored, empty.Handle(ConsoleKey.Enter));
        Assert.Equal(MicroStarListResult.Ignored, disabled.Handle(ConsoleKey.DownArrow));
    }

    [Fact]
    public void RejectsDuplicateKeysAndInvalidViewport()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new MicroStarSelectionList([], 0));
        Assert.Throws<ArgumentException>(() => new MicroStarSelectionList([
            new("same", "A"), new("same", "B")
        ], 2));
    }

    [Fact]
    public void ModifiedNavigationAndUnrelatedKeysAreIgnored()
    {
        var list = Create();
        Assert.Equal(MicroStarListResult.Ignored,
            list.Handle(ConsoleKey.DownArrow, ConsoleModifiers.Alt));
        Assert.Equal(MicroStarListResult.Ignored, list.Handle(ConsoleKey.A));
        Assert.Equal("id0", list.Selected?.Id);
    }
}
