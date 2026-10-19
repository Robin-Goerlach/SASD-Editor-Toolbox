using Sasd.Editor.Editing;
using Sasd.Editor.Model;
using Xunit;

namespace Sasd.Editor.Core.Tests;

/// <summary>
/// Direct engine consumers can bypass the historical command dispatcher.
/// That must not bypass basic no-op, virtual-column, and Undo invariants.
/// </summary>
public sealed class DirectEngineMutationSafetyTests
{
    [Theory]
    [InlineData("")]
    [InlineData("\r")]
    [InlineData("\r\r\r")]
    public void IgnoredTextDoesNotCreateUndoOrMarkDirty(string text)
    {
        var session = new EditorSession();
        var view = session.CreateDocument("content");
        view.Cursor = new TextPosition(0, 12);
        session.Engine.InsertText(text);

        Assert.Equal("content", view.Document.Buffer.GetLine(0).Text);
        Assert.Equal(new TextPosition(0, 12), view.Cursor);
        Assert.False(view.Document.IsDirty);
        Assert.Equal(0, view.Document.Version);
        Assert.Equal(0, session.Undo.Count);
    }

    [Theory]
    // Column 3 is the physical EOL of "abc": Backspace there *deletes*
    // 'c'. Virtual whitespace begins at the next (zero-based) column.
    [InlineData(4)]
    [InlineData(40)]
    [InlineData(int.MaxValue)]
    public void DirectBackspaceThroughVirtualSpaceDoesNotModifySharedText(int column)
    {
        var session = new EditorSession();
        var view = session.CreateDocument("abc");
        var linked = session.LinkWindow(view);
        linked.Cursor = new TextPosition(0, 91);
        view.Cursor = new TextPosition(0, column);
        session.SetCurrentWindow(view.WindowId);

        session.Engine.DeleteLeftCharacter();

        Assert.Equal("abc", view.Document.Buffer.GetLine(0).Text);
        Assert.Equal(new TextPosition(0, column - 1), view.Cursor);
        Assert.Equal(new TextPosition(0, 91), linked.Cursor);
        Assert.False(view.Document.IsDirty);
        Assert.Equal(0, view.Document.Version);
        Assert.Equal(0, session.Undo.Count);
    }

    [Theory]
    [InlineData(3)]
    [InlineData(int.MaxValue)]
    public void DirectDeleteRightCharacterAtFinalLineIsNoOp(int column)
    {
        var session = new EditorSession();
        var view = session.CreateDocument("abc");
        view.Cursor = new TextPosition(0, column);
        session.Engine.DeleteRightCharacter();
        Assert.Equal("abc", view.Document.Buffer.GetLine(0).Text);
        Assert.Equal(new TextPosition(0, column), view.Cursor);
        Assert.False(view.Document.IsDirty);
        Assert.Equal(0, view.Document.Version);
        Assert.Equal(0, session.Undo.Count);
    }

    [Theory]
    [InlineData(3)]
    [InlineData(int.MaxValue)]
    public void DirectDeleteRightWordAtFinalLineIsNoOp(int column)
    {
        var session = new EditorSession();
        var view = session.CreateDocument("abc");
        view.Cursor = new TextPosition(0, column);
        session.Engine.DeleteRightWord();
        Assert.Equal("abc", view.Document.Buffer.GetLine(0).Text);
        Assert.Equal(new TextPosition(0, column), view.Cursor);
        Assert.False(view.Document.IsDirty);
        Assert.Equal(0, view.Document.Version);
        Assert.Equal(0, session.Undo.Count);
    }

    [Fact]
    public void PhysicalBackspaceRemainsUndoable()
    {
        var session = new EditorSession();
        var view = session.CreateDocument("abc");
        view.Cursor = new TextPosition(0, 3);
        session.Engine.DeleteLeftCharacter();
        Assert.Equal("ab", view.Document.Buffer.GetLine(0).Text);
        Assert.True(view.Document.IsDirty);
        Assert.Equal(1, session.Undo.Count);
        Assert.True(session.Engine.Undo());
        Assert.Equal("abc", view.Document.Buffer.GetLine(0).Text);
        Assert.False(view.Document.IsDirty);
    }

    [Fact]
    public void PhysicalDeleteRightCharacterRemainsUndoable()
    {
        var session = new EditorSession();
        var view = session.CreateDocument("abc");
        view.Cursor = new TextPosition(0, 1);
        session.Engine.DeleteRightCharacter();
        Assert.Equal("ac", view.Document.Buffer.GetLine(0).Text);
        Assert.Equal(1, session.Undo.Count);
        Assert.True(session.Engine.Undo());
        Assert.Equal("abc", view.Document.Buffer.GetLine(0).Text);
    }

    [Fact]
    public void MixedCarriageReturnsStillInsertOnlyRealCharactersOnce()
    {
        var session = new EditorSession();
        var view = session.CreateDocument("abc");
        session.Engine.InsertText("\rX\r");
        Assert.Equal("Xabc", view.Document.Buffer.GetLine(0).Text);
        Assert.Equal(1, session.Undo.Count);
        Assert.True(session.Engine.Undo());
        Assert.Equal("abc", view.Document.Buffer.GetLine(0).Text);
    }
}
