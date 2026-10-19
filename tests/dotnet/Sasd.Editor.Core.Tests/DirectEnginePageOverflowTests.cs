using Sasd.Editor.Editing;
using Sasd.Editor.Model;
using Xunit;

namespace Sasd.Editor.Core.Tests;

/// <summary>
/// The direct engine's page-size parameter is host-provided and can be a
/// valid int even when its displacement does not fit in an int line sum.
/// </summary>
public sealed class DirectEnginePageOverflowTests
{
    [Fact]
    public void HugePageDownFromNonzeroLineClampsToLastRatherThanWrappingToFirst()
    {
        var session = new EditorSession();
        var view = session.CreateDocument("first\nsecond\nthird");
        var linked = session.LinkWindow(view);
        linked.Cursor = new TextPosition(0, 90);
        session.SetCurrentWindow(view.WindowId);
        view.Cursor = new TextPosition(1, 5);

        session.Engine.MovePageDown(int.MaxValue);

        Assert.Equal(new TextPosition(2, 5), view.Cursor);
        Assert.Equal(new TextPosition(0, 90), linked.Cursor);
        Assert.False(view.Document.IsDirty);
        Assert.Equal(0, view.Document.Version);
        Assert.Equal(0, session.Undo.Count);
    }

    [Fact]
    public void HugePageUpFromNonzeroLineClampsToFirst()
    {
        var session = new EditorSession();
        var view = session.CreateDocument("a\nsecond\nthird");
        view.Cursor = new TextPosition(2, 5);

        session.Engine.MovePageUp(int.MaxValue);

        Assert.Equal(new TextPosition(0, 1), view.Cursor);
        Assert.Equal(0, session.Undo.Count);
    }

    [Fact]
    public void PageDownAtDocumentBottomRemainsAtBottom()
    {
        var session = new EditorSession();
        var view = session.CreateDocument("a\nb\nc");
        view.Cursor = new TextPosition(2, 1);

        session.Engine.MovePageDown(int.MaxValue);

        Assert.Equal(new TextPosition(2, 1), view.Cursor);
        Assert.False(view.Document.IsDirty);
        Assert.Equal(0, session.Undo.Count);
    }

    [Fact]
    public void PageSizeOneStillMovesOneLogicalLineInDirectEngine()
    {
        // Direct engine paging is an API convenience, not the historical
        // displayed-viewport command that displaces by visibleRows - 1.
        var session = new EditorSession();
        var view = session.CreateDocument("a\nb\nc");
        view.Cursor = new TextPosition(0, 1);

        session.Engine.MovePageDown(1);

        Assert.Equal(new TextPosition(1, 1), view.Cursor);
    }
}
