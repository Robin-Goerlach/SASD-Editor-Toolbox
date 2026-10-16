using Sasd.Editor.Commands;
using Sasd.Editor.Editing;
using Sasd.Editor.Model;
using Xunit;

namespace Sasd.Editor.Core.Tests;

/// <summary>
/// Host-reported viewport dimensions can be much larger than the document.
/// These regressions prevent unchecked arithmetic from turning a perfectly
/// valid PageSize into negative logical line coordinates.
/// </summary>
public sealed class ViewportNavigationOverflowTests
{
    [Fact]
    public async Task PageDown_WithMaximumVisibleRows_SaturatesAtDocumentEnd()
    {
        var session = FiveLineSession();
        var window = session.CurrentWindow;
        window.TopLine = 2;
        window.Cursor = new TextPosition(3, 12);

        Assert.True(await new EditorCommandDispatcher(session).ExecuteAsync(
            new(EditorCommandId.PageDown, PageSize: int.MaxValue)));

        Assert.Equal(4, window.TopLine);
        Assert.Equal(new TextPosition(4, 12), window.Cursor);
        Assert.False(window.Document.IsDirty);
        Assert.Equal(0, session.Undo.Count);
    }

    [Fact]
    public async Task ScrollUp_WithMaximumVisibleRows_RecognizesLastDocumentLine()
    {
        var session = FiveLineSession();
        var window = session.CurrentWindow;
        window.TopLine = 2;
        window.Cursor = new TextPosition(4, 8);

        Assert.True(await new EditorCommandDispatcher(session).ExecuteAsync(
            new(EditorCommandId.ScrollUp, PageSize: int.MaxValue)));

        // A tall viewport displays the final document line. Under the
        // historical bottom-edge rule, the cursor moves upward with the view.
        Assert.Equal(1, window.TopLine);
        Assert.Equal(new TextPosition(3, 8), window.Cursor);
        Assert.False(window.Document.IsDirty);
        Assert.Equal(0, session.Undo.Count);
    }

    [Fact]
    public async Task PageUp_WithMaximumVisibleRows_KeepsTheRelativeCursorRow()
    {
        var session = FiveLineSession();
        var window = session.CurrentWindow;
        window.TopLine = 2;
        window.Cursor = new TextPosition(3, 6);

        Assert.True(await new EditorCommandDispatcher(session).ExecuteAsync(
            new(EditorCommandId.PageUp, PageSize: int.MaxValue)));

        Assert.Equal(0, window.TopLine);
        Assert.Equal(new TextPosition(1, 6), window.Cursor);
        Assert.False(window.Document.IsDirty);
        Assert.Equal(0, session.Undo.Count);
    }

    [Fact]
    public async Task CursorDown_WithMaximumVisibleRows_LeavesTopLineUnchanged()
    {
        var session = FiveLineSession();
        var window = session.CurrentWindow;
        window.TopLine = 2;
        window.Cursor = new TextPosition(3, 7);

        Assert.True(await new EditorCommandDispatcher(session).ExecuteAsync(
            new(EditorCommandId.CursorDown, PageSize: int.MaxValue)));

        Assert.Equal(2, window.TopLine);
        Assert.Equal(new TextPosition(4, 7), window.Cursor);
        Assert.False(window.Document.IsDirty);
        Assert.Equal(0, session.Undo.Count);
    }

    private static EditorSession FiveLineSession()
    {
        var session = new EditorSession();
        session.CreateDocument("zero\none\ntwo\nthree\nfour");
        return session;
    }
}
