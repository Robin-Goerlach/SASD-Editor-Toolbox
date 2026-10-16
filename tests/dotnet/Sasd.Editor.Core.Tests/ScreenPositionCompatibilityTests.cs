using Sasd.Editor.Editing;
using Sasd.Editor.Model;
using Sasd.Editor.Rendering;
using Xunit;

namespace Sasd.Editor.Core.Tests;

/// <summary>
/// Clean-room regression coverage for the observable responsibilities of
/// EditHscroll and EditGenlineno (handbook pp. 179 and 176). These are view
/// concerns: neither operation may change document text or cursor identity.
/// </summary>
public sealed class ScreenPositionCompatibilityTests
{
    [Fact]
    public void HorizontalVisibility_AdjustsOnlyWhenCursorIsOutsideVisibleColumns()
    {
        var session = new EditorSession();
        var window = session.CreateDocument("abcdef");
        window.Cursor = new TextPosition(0, 4);

        window.EnsureCursorHorizontallyVisible(5);
        Assert.Equal(0, window.LeftColumn); // The last visible column is inclusive.

        window.Cursor = new TextPosition(0, 5);
        window.EnsureCursorHorizontallyVisible(5);
        Assert.Equal(1, window.LeftColumn); // Minimal right shift.

        window.Cursor = new TextPosition(0, 2);
        window.EnsureCursorHorizontallyVisible(5);
        Assert.Equal(1, window.LeftColumn); // Already visible: no unnecessary jump.

        window.Cursor = new TextPosition(0, 0);
        window.EnsureCursorHorizontallyVisible(5);
        Assert.Equal(0, window.LeftColumn); // Cursor becomes the left edge.

        Assert.False(window.Document.IsDirty);
        Assert.Equal(0, session.Undo.Count);
    }

    [Fact]
    public void AllWindowVisibility_AdjustsEveryViewAndPreservesIndependentState()
    {
        var session = new EditorSession();
        var original = session.CreateDocument("first\nsecond");
        var linked = session.LinkWindow(original);
        var unrelated = session.CreateDocument("third");

        original.Cursor = new TextPosition(0, 2);
        original.LeftColumn = 7;
        original.TopLine = 0;
        linked.Cursor = new TextPosition(1, 14); // Valid virtual column.
        linked.LeftColumn = 0;
        unrelated.Cursor = new TextPosition(0, 4);
        unrelated.LeftColumn = 0;

        var currentId = session.CurrentWindow.WindowId;
        session.EnsureAllWindowCursorsHorizontallyVisible(5);

        Assert.Equal(2, original.LeftColumn);
        Assert.Equal(10, linked.LeftColumn);
        Assert.Equal(0, unrelated.LeftColumn);
        Assert.Equal(new TextPosition(0, 2), original.Cursor);
        Assert.Equal(new TextPosition(1, 14), linked.Cursor);
        Assert.Equal(0, original.TopLine);
        Assert.Equal(currentId, session.CurrentWindow.WindowId);
        Assert.False(original.Document.IsDirty);
        Assert.False(unrelated.Document.IsDirty);
        Assert.Equal(0, session.Undo.Count);

        // Running the bounded compatibility pass twice does not drift offsets.
        session.EnsureAllWindowCursorsHorizontallyVisible(5);
        Assert.Equal(2, original.LeftColumn);
        Assert.Equal(10, linked.LeftColumn);
    }

    [Fact]
    public void InvalidWidth_ThrowsBeforeAnyWindowCanBeChanged()
    {
        var session = new EditorSession();
        var first = session.CreateDocument("text");
        var second = session.CreateDocument("other");
        first.LeftColumn = 9;
        second.LeftColumn = 4;

        Assert.Throws<ArgumentOutOfRangeException>(
            () => session.EnsureAllWindowCursorsHorizontallyVisible(0));
        Assert.Throws<ArgumentOutOfRangeException>(
            () => session.EnsureAllWindowCursorsHorizontallyVisible(-1));

        Assert.Equal(9, first.LeftColumn);
        Assert.Equal(4, second.LeftColumn);
    }

    [Fact]
    public void VirtualColumnNearIntMaximum_DoesNotOverflow()
    {
        var session = new EditorSession();
        var window = session.CreateDocument("short");
        window.Cursor = new TextPosition(0, int.MaxValue);
        window.EnsureCursorHorizontallyVisible(7);
        Assert.Equal(int.MaxValue - 6, window.LeftColumn);

        window.EnsureCursorHorizontallyVisible(7);
        Assert.Equal(int.MaxValue - 6, window.LeftColumn);
        Assert.Equal(int.MaxValue, window.Cursor.Column);
    }

    [Fact]
    public void ViewportStatus_ReportsAbsoluteLinePerWindowWithoutTraversingTheDocument()
    {
        var session = new EditorSession();
        var text = string.Join("\n", Enumerable.Range(0, 50).Select(static i => $"line {i}"));
        var first = session.CreateDocument(text);
        var linked = session.LinkWindow(first);
        var separate = session.CreateDocument("other\nsecond");

        first.Cursor = new TextPosition(23, 3);
        first.TopLine = 22;
        linked.Cursor = new TextPosition(45, 2);
        linked.TopLine = 42;
        separate.Cursor = new TextPosition(1, 0);

        var builder = new EditorViewportBuilder(session);
        Assert.Equal(24, builder.Build(first, 2, 12).Status.Line);
        Assert.Equal(46, builder.Build(linked, 2, 12).Status.Line);
        Assert.Equal(2, builder.Build(separate, 2, 12).Status.Line);
        Assert.Equal(new TextPosition(23, 3), first.Cursor);
        Assert.Equal(new TextPosition(45, 2), linked.Cursor);
        Assert.False(first.Document.IsDirty);
        Assert.Equal(0, session.Undo.Count);
    }

    [Fact]
    public void HorizontalShift_IsReflectedInViewportText()
    {
        var session = new EditorSession();
        var window = session.CreateDocument("abcdefghijk");
        window.Cursor = new TextPosition(0, 8);
        session.EnsureAllWindowCursorsHorizontallyVisible(4);

        Assert.Equal(5, window.LeftColumn);
        Assert.Equal("fghi", new EditorViewportBuilder(session).Build(window, 1, 4).Lines[0].Text);
    }
}
