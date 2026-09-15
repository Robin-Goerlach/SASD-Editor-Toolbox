using Sasd.Editor.Commands;
using Sasd.Editor.Editing;
using Sasd.Editor.Model;
using Xunit;

namespace Sasd.Editor.Core.Tests;

public sealed class FirstEdCommandProcessorTests
{
    [Fact]
    public async Task BeginningEndLine_UsesHistoricalLastNonBlankSemantics()
    {
        var session = new EditorSession();
        session.CreateDocument("abc   ");
        var dispatcher = new EditorCommandDispatcher(session);

        Assert.True(await dispatcher.ExecuteAsync(new(EditorCommandId.BeginningOrEndOfLine)));
        Assert.Equal(new TextPosition(0, 3), session.CurrentWindow.Cursor);

        Assert.True(await dispatcher.ExecuteAsync(new(EditorCommandId.BeginningOrEndOfLine)));
        Assert.Equal(new TextPosition(0, 0), session.CurrentWindow.Cursor);
    }

    [Fact]
    public async Task GoToLine_ClampsPastEndButPreservesVirtualColumn()
    {
        var session = new EditorSession();
        session.CreateDocument("one\ntwo\nthree");
        session.CurrentWindow.Cursor = new TextPosition(0, 12);
        var dispatcher = new EditorCommandDispatcher(session);

        Assert.True(await dispatcher.ExecuteAsync(new(EditorCommandId.GoToLine, Number: 99)));

        Assert.Equal(new TextPosition(2, 12), session.CurrentWindow.Cursor);
    }

    [Fact]
    public async Task GoToColumn_UsesOneBasedUserVisibleNumber()
    {
        var session = new EditorSession();
        session.CreateDocument("short");
        var dispatcher = new EditorCommandDispatcher(session);

        Assert.True(await dispatcher.ExecuteAsync(new(EditorCommandId.GoToColumn, Number: 10)));

        Assert.Equal(9, session.CurrentWindow.Cursor.Column);
    }

    [Fact]
    public async Task PreviousWindow_WrapsFromFirstToLast()
    {
        var session = new EditorSession();
        var first = session.CreateDocument("first");
        var second = session.CreateDocument("second");
        session.SetCurrentWindow(first.WindowId);
        var dispatcher = new EditorCommandDispatcher(session);

        Assert.True(await dispatcher.ExecuteAsync(new(EditorCommandId.PreviousWindow)));

        Assert.Equal(second.WindowId, session.CurrentWindow.WindowId);
    }

    [Fact]
    public async Task GoToWindow_UsesModuloWindowNumbering()
    {
        var session = new EditorSession();
        var first = session.CreateDocument("first");
        var second = session.CreateDocument("second");
        var dispatcher = new EditorCommandDispatcher(session);

        Assert.True(await dispatcher.ExecuteAsync(new(EditorCommandId.GoToWindow, Number: 3)));

        Assert.Equal(first.WindowId, session.CurrentWindow.WindowId);
        Assert.NotEqual(second.WindowId, session.CurrentWindow.WindowId);
    }

    [Fact]
    public async Task LinkWindow_ReattachesDestinationAndKeepsIndependentCursor()
    {
        var session = new EditorSession();
        var destination = session.CreateDocument("destination");
        var source = session.CreateDocument("alpha\nbeta");
        destination.Cursor = new TextPosition(0, 2);
        source.Cursor = new TextPosition(1, 1);
        var dispatcher = new EditorCommandDispatcher(session);

        Assert.True(await dispatcher.ExecuteAsync(new(EditorCommandId.LinkWindow, Number: 1, Number2: 2)));

        Assert.Same(source.Document, destination.Document);
        Assert.Equal(new TextPosition(0, 2), destination.Cursor);
        Assert.Equal(new TextPosition(1, 1), source.Cursor);

        session.SetCurrentWindow(source.WindowId);
        session.Engine.InsertText("X");
        Assert.Equal("bXeta", destination.Document.Buffer.GetLine(1).Text);
    }

    [Fact]
    public async Task OptionCommands_TranslateOneBasedMarginsAndSetLimits()
    {
        var session = new EditorSession();
        session.CreateDocument("text");
        var dispatcher = new EditorCommandDispatcher(session);

        Assert.True(await dispatcher.ExecuteAsync(new(EditorCommandId.SetLeftMargin, Number: 5)));
        Assert.True(await dispatcher.ExecuteAsync(new(EditorCommandId.SetRightMargin, Number: 70)));
        Assert.True(await dispatcher.ExecuteAsync(new(EditorCommandId.SetTabWidth, Number: 8)));
        Assert.True(await dispatcher.ExecuteAsync(new(EditorCommandId.SetUndoLimit, Number: 12)));

        Assert.Equal(4, session.CurrentWindow.Options.LeftMargin);
        Assert.Equal(69, session.CurrentWindow.Options.RightMargin);
        Assert.Equal(8, session.CurrentWindow.Options.TabSize);
        Assert.Equal(12, session.Undo.Limit);
    }

    [Fact]
    public async Task BlockBoundaryCommand_SwitchesToWindowContainingBlock()
    {
        var session = new EditorSession();
        var blockWindow = session.CreateDocument("one\ntwo\nthree");
        blockWindow.Cursor = new TextPosition(1, 2);
        session.BeginBlock();
        blockWindow.Cursor = new TextPosition(2, 2);
        session.EndBlock();
        var other = session.CreateDocument("other");
        other.Cursor = new TextPosition(0, 1);
        var dispatcher = new EditorCommandDispatcher(session);

        Assert.True(await dispatcher.ExecuteAsync(new(EditorCommandId.TopOfBlock)));

        Assert.Equal(blockWindow.WindowId, session.CurrentWindow.WindowId);
        Assert.Equal(new TextPosition(1, 2), session.CurrentWindow.Cursor);
    }
}
