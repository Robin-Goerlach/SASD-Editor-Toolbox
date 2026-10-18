using Sasd.Editor.Commands;
using Sasd.Editor.Editing;
using Sasd.Editor.Model;
using Xunit;

namespace Sasd.Editor.Core.Tests;

/// <summary>
/// Undo records actual text mutations, not commands that only encountered
/// virtual space or characters without any invariant case transformation.
/// </summary>
public sealed class InlineNoOpEditingTests
{
    [Theory]
    [InlineData(3)]
    [InlineData(100)]
    [InlineData(int.MaxValue)]
    public async Task ChangeCase_AtOrPastEndOfLinePreservesVirtualCursorAndUndo(int column)
    {
        var session = new EditorSession();
        var first = session.CreateDocument("abc");
        var linked = session.LinkWindow(first);
        first.Cursor = new TextPosition(0, column);
        linked.Cursor = new TextPosition(0, 1);
        session.SetCurrentWindow(first.WindowId);

        Assert.True(await new EditorCommandDispatcher(session)
            .ExecuteAsync(new(EditorCommandId.ChangeCase)));

        Assert.Equal("abc", first.Document.Buffer.GetLine(0).Text);
        Assert.Equal(new TextPosition(0, column), first.Cursor);
        Assert.Equal(new TextPosition(0, 1), linked.Cursor);
        Assert.False(first.Document.IsDirty);
        Assert.Equal(0, first.Document.Version);
        Assert.Equal(0, session.Undo.Count);
    }

    [Theory]
    [InlineData("a1c")]
    [InlineData("a!c")]
    [InlineData("a c")]
    public async Task ChangeCase_CharactersWithoutCaseMappingDoNotDirtyText(string content)
    {
        var session = new EditorSession();
        var view = session.CreateDocument(content);
        view.Cursor = new TextPosition(0, 1);

        Assert.True(await new EditorCommandDispatcher(session)
            .ExecuteAsync(new(EditorCommandId.ChangeCase)));

        Assert.Equal(content, view.Document.Buffer.GetLine(0).Text);
        Assert.False(view.Document.IsDirty);
        Assert.Equal(0, view.Document.Version);
        Assert.Equal(0, session.Undo.Count);
    }

    [Theory]
    [InlineData(3)]
    [InlineData(100)]
    [InlineData(int.MaxValue)]
    public async Task DeleteToEndOfLine_WhenNoStoredCharactersRemainDoesNotDirty(int column)
    {
        var session = new EditorSession();
        var view = session.CreateDocument("abc");
        view.Cursor = new TextPosition(0, column);

        Assert.True(await new EditorCommandDispatcher(session)
            .ExecuteAsync(new(EditorCommandId.DeleteToEndOfLine)));

        Assert.Equal("abc", view.Document.Buffer.GetLine(0).Text);
        Assert.Equal(new TextPosition(0, column), view.Cursor);
        Assert.False(view.Document.IsDirty);
        Assert.Equal(0, view.Document.Version);
        Assert.Equal(0, session.Undo.Count);
    }

    [Fact]
    public async Task ActualChangeCaseAndUndoRestoreCleanDocument()
    {
        var session = new EditorSession();
        var view = session.CreateDocument("alpha");
        var dispatcher = new EditorCommandDispatcher(session);

        Assert.True(await dispatcher.ExecuteAsync(new(EditorCommandId.ChangeCase)));
        Assert.Equal("Alpha", view.Document.Buffer.GetLine(0).Text);
        Assert.True(view.Document.IsDirty);
        Assert.Equal(1, view.Document.Version);
        Assert.Equal(1, session.Undo.Count);

        Assert.True(await dispatcher.ExecuteAsync(new(EditorCommandId.Undo)));
        Assert.Equal("alpha", view.Document.Buffer.GetLine(0).Text);
        Assert.False(view.Document.IsDirty);
        Assert.Equal(0, view.Document.Version);
        Assert.Equal(0, session.Undo.Count);
    }

    [Fact]
    public async Task ActualDeleteToEndAndUndoRestoreLinkedViewState()
    {
        var session = new EditorSession();
        var first = session.CreateDocument("alpha beta");
        var linked = session.LinkWindow(first);
        linked.Cursor = new TextPosition(0, 3);
        first.Cursor = new TextPosition(0, 5);
        session.SetCurrentWindow(first.WindowId);
        var dispatcher = new EditorCommandDispatcher(session);

        Assert.True(await dispatcher.ExecuteAsync(new(EditorCommandId.DeleteToEndOfLine)));
        Assert.Equal("alpha", first.Document.Buffer.GetLine(0).Text);
        Assert.Equal(1, session.Undo.Count);

        Assert.True(await dispatcher.ExecuteAsync(new(EditorCommandId.Undo)));
        Assert.Equal("alpha beta", first.Document.Buffer.GetLine(0).Text);
        Assert.Equal(new TextPosition(0, 3), linked.Cursor);
        Assert.Equal(0, session.Undo.Count);
    }
}
