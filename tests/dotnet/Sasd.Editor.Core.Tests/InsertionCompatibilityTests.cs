using Sasd.Editor.Commands;
using Sasd.Editor.Editing;
using Sasd.Editor.Input;
using Sasd.Editor.Model;
using Xunit;

namespace Sasd.Editor.Core.Tests;

/// <summary>
/// Behavioral coverage for EditInsertLine, EditNewLine and structural word-wrap
/// insertion. The tests deliberately assert the distinction between Ctrl-N and
/// Return because the historical editor assigns different cursor/mode semantics.
/// </summary>
public sealed class InsertionCompatibilityTests
{
    [Fact]
    public void KeyMap_DistinguishesReturnFromCtrlN()
    {
        var keyMap = new FirstEdKeyMap();

        var enter = keyMap.Translate(new EditorKeyStroke(EditorKey.Enter));
        var ctrlN = keyMap.Translate(EditorKeyStroke.Control('N'));

        Assert.Equal(EditorCommandId.NewLine, enter.Binding!.CommandId);
        Assert.Equal(EditorCommandId.InsertLine, ctrlN.Binding!.CommandId);
    }

    [Fact]
    public async Task InsertLine_AtColumnZero_LeavesCursorOnBlankUpperLine()
    {
        var session = new EditorSession();
        var window = session.CreateDocument("alpha\nbeta");
        window.Cursor = new TextPosition(0, 0);
        var dispatcher = new EditorCommandDispatcher(session);

        Assert.True(await dispatcher.ExecuteAsync(new(EditorCommandId.InsertLine)));

        Assert.Equal(new[] { string.Empty, "alpha", "beta" }, window.Document.Buffer.Snapshot().Select(line => line.Text));
        Assert.Equal(new TextPosition(0, 0), window.Cursor);
        Assert.True(window.Document.IsDirty);
        Assert.Equal(1, session.Undo.Count);
    }

    [Fact]
    public async Task InsertLine_InMiddle_SplitsWithoutMovingCurrentCursor()
    {
        var session = new EditorSession();
        var window = session.CreateDocument("abcdef\ntail");
        window.Cursor = new TextPosition(0, 3);
        var dispatcher = new EditorCommandDispatcher(session);

        Assert.True(await dispatcher.ExecuteAsync(new(EditorCommandId.InsertLine)));

        Assert.Equal(new[] { "abc", "def", "tail" }, window.Document.Buffer.Snapshot().Select(line => line.Text));
        Assert.Equal(new TextPosition(0, 3), window.Cursor);
    }

    [Fact]
    public async Task InsertLine_BeyondLastNonBlank_InsertsBlankBelowAndRealignsLaterReferences()
    {
        var session = new EditorSession();
        var source = session.CreateDocument("abc   \ntail");
        source.Cursor = new TextPosition(1, 0);
        session.SetMarker(1);
        var linked = session.LinkWindow(source);
        linked.Cursor = new TextPosition(1, 7);
        linked.TopLine = 1;

        session.SetCurrentWindow(source.WindowId);
        source.Cursor = new TextPosition(0, 3);
        var dispatcher = new EditorCommandDispatcher(session);

        Assert.True(await dispatcher.ExecuteAsync(new(EditorCommandId.InsertLine)));

        Assert.Equal(new[] { "abc   ", string.Empty, "tail" }, source.Document.Buffer.Snapshot().Select(line => line.Text));
        Assert.Equal(new TextPosition(0, 3), source.Cursor);
        Assert.Equal(new TextPosition(2, 7), linked.Cursor);
        Assert.Equal(2, linked.TopLine);
        Assert.True(session.JumpToMarker(1));
        Assert.Equal(2, source.Cursor.Line);
        Assert.Equal(3, source.Cursor.Column);
    }

    [Fact]
    public async Task NewLine_InInsertMode_MovesToLowerLineAndAppliesAutoIndent()
    {
        var session = new EditorSession();
        var window = session.CreateDocument("    alpha");
        window.Options.AutoIndent = true;
        window.Cursor = new TextPosition(0, 9);
        window.Document.Buffer.SetFlags(0, EditorLineFlags.Wrapped);
        var dispatcher = new EditorCommandDispatcher(session);

        Assert.True(await dispatcher.ExecuteAsync(new(EditorCommandId.NewLine)));

        Assert.Equal(new[] { "    alpha", string.Empty }, window.Document.Buffer.Snapshot().Select(line => line.Text));
        Assert.Equal(new TextPosition(1, 4), window.Cursor);
        Assert.False(window.Document.Buffer.GetLine(0).Flags.HasFlag(EditorLineFlags.Wrapped));
        Assert.True(window.Document.IsDirty);
        Assert.Equal(1, session.Undo.Count);
    }

    [Fact]
    public async Task NewLine_InOvertypeMode_MovesDownWithoutSplittingExistingText()
    {
        var session = new EditorSession();
        var window = session.CreateDocument("  alpha\nbeta");
        window.Options.InsertMode = false;
        window.Options.AutoIndent = true;
        window.Cursor = new TextPosition(0, 5);
        var dispatcher = new EditorCommandDispatcher(session);

        Assert.True(await dispatcher.ExecuteAsync(new(EditorCommandId.NewLine)));

        Assert.Equal(new[] { "  alpha", "beta" }, window.Document.Buffer.Snapshot().Select(line => line.Text));
        Assert.Equal(new TextPosition(1, 2), window.Cursor);
        Assert.False(window.Document.IsDirty);
        Assert.Equal(0, session.Undo.Count);
    }

    [Fact]
    public async Task NewLine_InOvertypeModeAtEnd_AppendsBlankLine()
    {
        var session = new EditorSession();
        var window = session.CreateDocument("alpha");
        window.Options.InsertMode = false;
        window.Cursor = new TextPosition(0, 5);
        var dispatcher = new EditorCommandDispatcher(session);

        Assert.True(await dispatcher.ExecuteAsync(new(EditorCommandId.NewLine)));

        Assert.Equal(new[] { "alpha", string.Empty }, window.Document.Buffer.Snapshot().Select(line => line.Text));
        Assert.Equal(new TextPosition(1, 0), window.Cursor);
        Assert.True(window.Document.IsDirty);
        Assert.Equal(1, session.Undo.Count);
    }

    [Fact]
    public void WordWrapInsertion_RealignsLinkedWindowAndMarker()
    {
        var session = new EditorSession();
        var source = session.CreateDocument("abcd\ntail");
        source.Cursor = new TextPosition(1, 0);
        session.SetMarker(1);
        var linked = session.LinkWindow(source);
        linked.Cursor = new TextPosition(1, 6);
        linked.TopLine = 1;

        session.SetCurrentWindow(source.WindowId);
        source.Options.WordWrap = true;
        source.Options.LeftMargin = 0;
        source.Options.RightMargin = 3;
        source.Cursor = new TextPosition(0, 4);

        session.Engine.InsertText("e");

        Assert.Equal(new[] { "abcd", "e", "tail" }, source.Document.Buffer.Snapshot().Select(line => line.Text));
        Assert.True(source.Document.Buffer.GetLine(0).Flags.HasFlag(EditorLineFlags.Wrapped));
        Assert.False(source.Document.Buffer.GetLine(1).Flags.HasFlag(EditorLineFlags.Wrapped));
        Assert.Equal(new TextPosition(2, 6), linked.Cursor);
        Assert.Equal(2, linked.TopLine);
        Assert.True(session.JumpToMarker(1));
        Assert.Equal(2, source.Cursor.Line);
    }

    [Fact]
    public void TextInsertion_OnWrappedLinePreservesSoftBoundary()
    {
        var session = new EditorSession();
        var window = session.CreateDocument("alpha\nbeta");
        window.Document.Buffer.SetFlags(0, EditorLineFlags.Wrapped);
        window.Cursor = new TextPosition(0, 2);

        session.Engine.InsertText("X");

        Assert.Equal("alXpha", window.Document.Buffer.GetLine(0).Text);
        Assert.True(window.Document.Buffer.GetLine(0).Flags.HasFlag(EditorLineFlags.Wrapped));
    }
}
