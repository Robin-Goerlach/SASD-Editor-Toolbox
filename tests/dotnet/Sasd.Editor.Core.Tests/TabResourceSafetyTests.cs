using Sasd.Editor.Commands;
using Sasd.Editor.Editing;
using Sasd.Editor.Model;
using Xunit;

namespace Sasd.Editor.Core.Tests;

/// <summary>
/// Tabs near large virtual columns and host-configured widths must not
/// allocate unbounded blank padding or corrupt the cursor/Undo journal.
/// </summary>
public sealed class TabResourceSafetyTests
{
    [Fact]
    public void DirectEngineTabAtOverflowingVirtualColumnDoesNotMutate()
    {
        var session = new EditorSession();
        var view = session.CreateDocument("abc");
        view.Cursor = new TextPosition(0, int.MaxValue - 2);

        Assert.Throws<ArgumentOutOfRangeException>(() => session.Engine.Tab());

        Assert.Equal("abc", view.Document.Buffer.GetLine(0).Text);
        Assert.Equal(int.MaxValue - 2, view.Cursor.Column);
        Assert.Equal(0, view.Document.Version);
        Assert.False(view.Document.IsDirty);
        Assert.Equal(0, session.Undo.Count);
    }

    [Fact]
    public void DirectEngineTabRejectsUnreasonableWidthBeforeAllocation()
    {
        var session = new EditorSession();
        var view = session.CreateDocument("abc");
        view.Options.TabSize = int.MaxValue;
        view.Cursor = new TextPosition(0, 0);

        Assert.Throws<ArgumentOutOfRangeException>(() => session.Engine.Tab());
        Assert.Equal("abc", view.Document.Buffer.GetLine(0).Text);
        Assert.Equal(0, view.Cursor.Column);
        Assert.False(view.Document.IsDirty);
        Assert.Equal(0, session.Undo.Count);
    }

    [Fact]
    public void DirectEngineTabRejectsLargeVirtualGapBeforePadding()
    {
        var session = new EditorSession();
        var view = session.CreateDocument("abc");
        view.Cursor = new TextPosition(0, 50_000);

        Assert.Throws<ArgumentOutOfRangeException>(() => session.Engine.Tab());
        Assert.Equal(50_000, view.Cursor.Column);
        Assert.Equal("abc", view.Document.Buffer.GetLine(0).Text);
        Assert.Equal(0, session.Undo.Count);
    }

    [Fact]
    public async Task FirstEdInsertModeTabRejectsExtremeWidthWithTypedError()
    {
        var session = new EditorSession();
        var view = session.CreateDocument("abc");
        view.Options.TabSize = int.MaxValue;

        Assert.False(await new EditorCommandDispatcher(session)
            .ExecuteAsync(new(EditorCommandId.Tab)));
        Assert.Equal("abc", view.Document.Buffer.GetLine(0).Text);
        Assert.Equal(0, view.Cursor.Column);
        Assert.Equal(0, session.Undo.Count);
    }

    [Fact]
    public async Task FirstEdOvertypeTabMovesOnlyVirtualCursorWithoutPadding()
    {
        var session = new EditorSession();
        var view = session.CreateDocument("abc");
        view.Options.InsertMode = false;
        view.Cursor = new TextPosition(0, 100_000);

        Assert.True(await new EditorCommandDispatcher(session)
            .ExecuteAsync(new(EditorCommandId.Tab)));

        Assert.Equal(100_004, view.Cursor.Column);
        Assert.Equal("abc", view.Document.Buffer.GetLine(0).Text);
        Assert.False(view.Document.IsDirty);
        Assert.Equal(0, view.Document.Version);
        Assert.Equal(0, session.Undo.Count);
    }

    [Fact]
    public async Task NormalInsertModeTabStillEditsAndUndoesAsBefore()
    {
        var session = new EditorSession();
        var view = session.CreateDocument("abc");
        view.Cursor = new TextPosition(0, 1);
        var dispatcher = new EditorCommandDispatcher(session);

        Assert.True(await dispatcher.ExecuteAsync(new(EditorCommandId.Tab)));
        Assert.Equal("a   bc", view.Document.Buffer.GetLine(0).Text);
        Assert.Equal(4, view.Cursor.Column);
        Assert.Equal(1, session.Undo.Count);

        Assert.True(await dispatcher.ExecuteAsync(new(EditorCommandId.Undo)));
        Assert.Equal("abc", view.Document.Buffer.GetLine(0).Text);
        Assert.False(view.Document.IsDirty);
    }
}
