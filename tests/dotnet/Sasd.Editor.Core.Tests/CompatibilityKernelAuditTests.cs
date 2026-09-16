using Sasd.Editor.Commands;
using Sasd.Editor.Editing;
using Sasd.Editor.Model;
using Xunit;

namespace Sasd.Editor.Core.Tests;

/// <summary>
/// Regression tests added while auditing low-level FIRST-ED command semantics
/// against the Turbo Editor Toolbox 1.0 handbook.
/// </summary>
public sealed class CompatibilityKernelAuditTests
{
    [Fact]
    public async Task LeftChar_FromColumnZero_UsesPreviousLastNonBlankEnd()
    {
        var session = new EditorSession();
        session.CreateDocument("abc   \nnext");
        session.CurrentWindow.Cursor = new TextPosition(1, 0);
        var dispatcher = new EditorCommandDispatcher(session);

        Assert.True(await dispatcher.ExecuteAsync(new(EditorCommandId.CursorLeft)));

        Assert.Equal(new TextPosition(0, 3), session.CurrentWindow.Cursor);
    }

    [Fact]
    public async Task RightChar_AtEndOfLine_AdvancesVirtualColumnWithoutChangingLine()
    {
        var session = new EditorSession();
        session.CreateDocument("abc\nnext");
        session.CurrentWindow.Cursor = new TextPosition(0, 3);
        var dispatcher = new EditorCommandDispatcher(session);

        Assert.True(await dispatcher.ExecuteAsync(new(EditorCommandId.CursorRight)));

        Assert.Equal(new TextPosition(0, 4), session.CurrentWindow.Cursor);
        Assert.False(session.CurrentWindow.Document.IsDirty);
    }

    [Fact]
    public async Task Tab_InOvertypeMode_MovesCursorWithoutChangingDocument()
    {
        var session = new EditorSession();
        session.CreateDocument("abc");
        session.CurrentWindow.Options.InsertMode = false;
        session.CurrentWindow.Options.TabSize = 4;
        session.CurrentWindow.Cursor = new TextPosition(0, 1);
        var dispatcher = new EditorCommandDispatcher(session);

        Assert.True(await dispatcher.ExecuteAsync(new(EditorCommandId.Tab)));

        Assert.Equal(new TextPosition(0, 4), session.CurrentWindow.Cursor);
        Assert.Equal("abc", session.CurrentWindow.Document.Buffer.GetLine(0).Text);
        Assert.False(session.CurrentWindow.Document.IsDirty);
        Assert.Equal(0, session.Undo.Count);
    }

    [Fact]
    public async Task Tab_InInsertMode_InsertsPaddingToNextStop()
    {
        var session = new EditorSession();
        session.CreateDocument("ab");
        session.CurrentWindow.Options.InsertMode = true;
        session.CurrentWindow.Options.TabSize = 4;
        session.CurrentWindow.Cursor = new TextPosition(0, 1);
        var dispatcher = new EditorCommandDispatcher(session);

        Assert.True(await dispatcher.ExecuteAsync(new(EditorCommandId.Tab)));

        Assert.Equal(new TextPosition(0, 4), session.CurrentWindow.Cursor);
        Assert.Equal("a   b", session.CurrentWindow.Document.Buffer.GetLine(0).Text);
        Assert.True(session.CurrentWindow.Document.IsDirty);
        Assert.Equal(1, session.Undo.Count);
    }

    [Fact]
    public void MarkerJump_UsesRecordedLineButPreservesTargetWindowColumn()
    {
        var session = new EditorSession();
        var markedWindow = session.CreateDocument("zero\none\ntwo");
        markedWindow.Cursor = new TextPosition(2, 1);
        session.SetMarker(1);

        // The marker deliberately does not remember this later column value.
        markedWindow.Cursor = new TextPosition(0, 7);
        session.CreateDocument("other");

        Assert.True(session.JumpToMarker(1));

        Assert.Equal(markedWindow.WindowId, session.CurrentWindow.WindowId);
        Assert.Equal(new TextPosition(2, 7), markedWindow.Cursor);
    }

    [Fact]
    public void OffblockStyleClear_PreservesBlockDefinitionAndClearsEveryStream()
    {
        var session = new EditorSession();
        var blockWindow = session.CreateDocument("one\ntwo\nthree");
        blockWindow.Cursor = new TextPosition(0, 0);
        session.BeginBlock();
        blockWindow.Cursor = new TextPosition(1, 0);
        session.EndBlock();
        var block = session.Block;
        Assert.NotNull(block);

        var otherWindow = session.CreateDocument("other");
        var otherLine = otherWindow.Document.Buffer.GetLine(0);
        otherWindow.Document.Buffer.SetFlags(0, otherLine.Flags | EditorLineFlags.InBlock);

        session.ClearBlockHighlights();

        Assert.Same(block, session.Block);
        Assert.DoesNotContain(blockWindow.Document.Buffer.Snapshot(), line => line.Flags.HasFlag(EditorLineFlags.InBlock));
        Assert.DoesNotContain(otherWindow.Document.Buffer.Snapshot(), line => line.Flags.HasFlag(EditorLineFlags.InBlock));

        session.MarkBlockHighlights();

        Assert.True(blockWindow.Document.Buffer.GetLine(0).Flags.HasFlag(EditorLineFlags.InBlock));
        Assert.True(blockWindow.Document.Buffer.GetLine(1).Flags.HasFlag(EditorLineFlags.InBlock));
        Assert.False(blockWindow.Document.Buffer.GetLine(2).Flags.HasFlag(EditorLineFlags.InBlock));
        Assert.False(otherWindow.Document.Buffer.GetLine(0).Flags.HasFlag(EditorLineFlags.InBlock));
    }
}
