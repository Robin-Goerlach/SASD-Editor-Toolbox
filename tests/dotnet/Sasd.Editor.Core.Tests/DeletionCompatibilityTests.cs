using Sasd.Editor.Commands;
using Sasd.Editor.Editing;
using Sasd.Editor.Model;
using Xunit;

namespace Sasd.Editor.Core.Tests;

/// <summary>
/// Behavioral tests for FIRST-ED deletion commands whose line-boundary and word
/// classification rules intentionally differ from common modern-editor defaults.
/// </summary>
public sealed class DeletionCompatibilityTests
{
    [Fact]
    public async Task DeleteRightWord_DeletesPunctuationRunAndFollowingBlanks()
    {
        var session = new EditorSession();
        var window = session.CreateDocument("abc!!  def");
        window.Cursor = new TextPosition(0, 3);
        var dispatcher = new EditorCommandDispatcher(session);

        Assert.True(await dispatcher.ExecuteAsync(new(EditorCommandId.DeleteRightWord)));

        Assert.Equal("abcdef", window.Document.Buffer.GetLine(0).Text);
        Assert.Equal(new TextPosition(0, 3), window.Cursor);
        Assert.True(window.Document.IsDirty);
        Assert.Equal(1, session.Undo.Count);
    }

    [Fact]
    public async Task DeleteRightWord_AfterLastNonBlank_JoinsNextLineAndRealignsMarkers()
    {
        var session = new EditorSession();
        var window = session.CreateDocument("abc   \nnext\nlast");

        window.Cursor = new TextPosition(1, 0);
        session.SetMarker(1);
        window.Cursor = new TextPosition(2, 0);
        session.SetMarker(2);
        window.Cursor = new TextPosition(0, 3);

        var dispatcher = new EditorCommandDispatcher(session);
        Assert.True(await dispatcher.ExecuteAsync(new(EditorCommandId.DeleteRightWord)));

        Assert.Equal(new[] { "abc   next", "last" }, window.Document.Buffer.Snapshot().Select(line => line.Text));
        Assert.False(session.JumpToMarker(1));
        Assert.True(session.JumpToMarker(2));
        Assert.Equal(1, window.Cursor.Line);
        Assert.Equal(3, window.Cursor.Column);
    }

    [Fact]
    public async Task DeleteRightCharacter_InsideText_RemovesExactlyOneCharacter()
    {
        var session = new EditorSession();
        var window = session.CreateDocument("abcd");
        window.Cursor = new TextPosition(0, 1);
        var dispatcher = new EditorCommandDispatcher(session);

        Assert.True(await dispatcher.ExecuteAsync(new(EditorCommandId.DeleteRightCharacter)));

        Assert.Equal("acd", window.Document.Buffer.GetLine(0).Text);
        Assert.Equal(new TextPosition(0, 1), window.Cursor);
        Assert.True(window.Document.IsDirty);
        Assert.Equal(1, session.Undo.Count);
    }

    [Fact]
    public async Task DeleteRightCharacter_AfterLastNonBlank_JoinsAndRealignsLinkedWindow()
    {
        var session = new EditorSession();
        var source = session.CreateDocument("abc   \nnext\nlast");
        var linked = session.LinkWindow(source);
        linked.Cursor = new TextPosition(2, 8);
        linked.TopLine = 1;

        session.SetCurrentWindow(source.WindowId);
        source.Cursor = new TextPosition(0, 3);
        var dispatcher = new EditorCommandDispatcher(session);

        Assert.True(await dispatcher.ExecuteAsync(new(EditorCommandId.DeleteRightCharacter)));

        Assert.Equal(new[] { "abc   next", "last" }, source.Document.Buffer.Snapshot().Select(line => line.Text));
        Assert.Equal(new TextPosition(0, 3), source.Cursor);
        Assert.Equal(new TextPosition(1, 8), linked.Cursor);
        Assert.Equal(1, linked.TopLine);
    }
}
