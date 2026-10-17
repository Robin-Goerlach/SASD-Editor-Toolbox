using Sasd.Editor.Editing;
using Sasd.Editor.Model;
using Sasd.Editor.Search;
using Xunit;

namespace Sasd.Editor.Core.Tests;

/// <summary>
/// A wrapped search partitions match STARTS at the original cursor, not match
/// spans. All tests run against the ordinary UI-free editor session.
/// </summary>
public sealed class SearchWrapBoundaryTests
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task WrapAround_ReturnsMatchStartingBeforeCursorEvenWhenItsEndCrossesCursor(
        bool interruptible)
    {
        var session = new EditorSession();
        var view = session.CreateDocument("xxABCDEF");
        view.Cursor = new TextPosition(0, 4);
        var options = new SearchOptions(CaseSensitive: true, WrapAround: true);

        var result = interruptible
            ? await session.Search.FindNextAsync("ABCDEF", options)
            : session.Search.FindNext("ABCDEF", options);

        // Original cursor = 4, valid match starts at 2 and ends at 8.
        // The previous limit-minus-pattern-length loop missed this match.
        Assert.Equal(new TextPosition(0, 2), result);
        Assert.Equal(result, view.Cursor);
        Assert.False(view.Document.IsDirty);
        Assert.Equal(0, session.Undo.Count);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task WrapAround_AlsoFindsMatchStartingAtZeroAcrossOriginalCursor(
        bool interruptible)
    {
        var session = new EditorSession();
        var view = session.CreateDocument("abcde");
        view.Cursor = new TextPosition(0, 2);

        var result = interruptible
            ? await session.Search.FindNextAsync("abcde", new SearchOptions(WrapAround: true))
            : session.Search.FindNext("abcde", new SearchOptions(WrapAround: true));

        Assert.Equal(new TextPosition(0, 0), result);
        Assert.Equal(0, session.Undo.Count);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task WithoutWrapAround_DoesNotReturnOverlappingEarlierMatch(bool interruptible)
    {
        var session = new EditorSession();
        var view = session.CreateDocument("xxABCDEF");
        view.Cursor = new TextPosition(0, 4);

        var result = interruptible
            ? await session.Search.FindNextAsync("ABCDEF")
            : session.Search.FindNext("ABCDEF");

        Assert.Null(result);
        Assert.Equal(new TextPosition(0, 4), view.Cursor);
        Assert.Equal("ABCDEF", session.Search.LastPattern);
        Assert.Equal(0, session.Undo.Count);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task WrapAround_DoesNotIgnoreWholeWordBoundaries(bool interruptible)
    {
        var session = new EditorSession();
        var view = session.CreateDocument("xabc");
        view.Cursor = new TextPosition(0, 2);
        var options = new SearchOptions(WholeWord: true, WrapAround: true);

        var result = interruptible
            ? await session.Search.FindNextAsync("abc", options)
            : session.Search.FindNext("abc", options);

        // "abc" starts before column 2, but is part of "xabc".
        Assert.Null(result);
        Assert.Equal(new TextPosition(0, 2), view.Cursor);
        Assert.False(view.Document.IsDirty);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task WrapAround_VisitsEarlierLinesBeforeCurrentLinePrefix(bool interruptible)
    {
        var session = new EditorSession();
        var view = session.CreateDocument("target\nxx target");
        view.Cursor = new TextPosition(1, 4);
        var options = new SearchOptions(WrapAround: true);

        var result = interruptible
            ? await session.Search.FindNextAsync("target", options)
            : session.Search.FindNext("target", options);

        Assert.Equal(new TextPosition(0, 0), result);
        Assert.Equal(new TextPosition(0, 0), view.Cursor);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task FindAgain_ReusesWrappedOptionAfterCursorMovesIntoPreviousMatch(
        bool interruptible)
    {
        var session = new EditorSession();
        var view = session.CreateDocument("xxABCDEF");
        Assert.Equal(new TextPosition(0, 2),
            session.Search.FindNext("ABCDEF", new SearchOptions(WrapAround: true)));

        view.Cursor = new TextPosition(0, 4);
        var result = interruptible
            ? await session.Search.FindAgainAsync()
            : session.Search.FindAgain();

        Assert.Equal(new TextPosition(0, 2), result);
        Assert.Equal(new TextPosition(0, 2), view.Cursor);
    }
}
