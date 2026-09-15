using Sasd.Editor.Editing;
using Xunit;

namespace Sasd.Editor.Core.Tests;

public sealed class EditorEngineTests
{
    [Fact]
    public void InsertAndUndo_RestoresOriginalText()
    {
        var session = new EditorSession();
        session.CreateDocument("hello");
        session.Engine.MoveEndOfLine();
        session.Engine.InsertText(" world");
        Assert.Equal("hello world", session.CurrentWindow.Document.Buffer.GetLine(0).Text);
        Assert.True(session.CurrentWindow.Document.IsDirty);
        Assert.True(session.Engine.Undo());
        Assert.Equal("hello", session.CurrentWindow.Document.Buffer.GetLine(0).Text);
    }

    [Fact]
    public void AutoIndent_CopiesLeadingWhitespace()
    {
        var session = new EditorSession();
        session.CreateDocument("    item");
        session.CurrentWindow.Options.AutoIndent = true;
        session.Engine.MoveEndOfLine();
        session.Engine.InsertNewLine();
        Assert.Equal(2, session.CurrentWindow.Document.Buffer.LineCount);
        Assert.Equal("    ", session.CurrentWindow.Document.Buffer.GetLine(1).Text);
        Assert.Equal(4, session.CurrentWindow.Cursor.Column);
    }

    [Fact]
    public void LinkedWindows_ShareDocumentButKeepCursorState()
    {
        var session = new EditorSession();
        var first = session.CreateDocument("one\ntwo");
        var second = session.LinkWindow(first);
        second.Cursor = new(1, 0);
        session.SetCurrentWindow(first.WindowId);
        session.Engine.InsertText("X");
        Assert.Same(first.Document, second.Document);
        Assert.Equal("Xone", second.Document.Buffer.GetLine(0).Text);
        Assert.Equal(1, second.Cursor.Line);
    }
}
