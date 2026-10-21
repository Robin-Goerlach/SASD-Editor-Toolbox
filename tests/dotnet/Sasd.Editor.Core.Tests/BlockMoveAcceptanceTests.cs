using Sasd.Editor.Commands;
using Sasd.Editor.Editing;
using Sasd.Editor.Model;
using Xunit;

namespace Sasd.Editor.Core.Tests;

/// <summary>
/// Targeted 1985 block-family acceptance: Move/Copy/Delete must keep logical
/// block boundaries, line descriptors, linked views and Undo mutually coherent.
/// </summary>
/// <remarks>
/// The handbook defines whole-line insertion positions and rejects a move into
/// the moved block itself, but it does not specify modern document versions,
/// Dirty accounting or snapshot Undo for an adjacent no-op move. That edge case
/// follows SASD's explicit modern nonmutating-command policy.
/// </remarks>
public sealed class BlockMoveAcceptanceTests
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task MoveBeforeImmediatelyFollowingLine_IsSuccessfulNoOp(bool hidden)
    {
        var session = new EditorSession();
        var source = session.CreateDocument("alpha\nbeta\ngamma\ndelta");
        source.Cursor = new TextPosition(1, 0);
        session.BeginBlock();
        source.Cursor = new TextPosition(2, 0);
        session.EndBlock();

        // A hidden block still has usable logical endpoints even when
        // InBlock highlights are cleared from its line descriptors.
        if (hidden)
            session.ToggleBlockHidden();

        var linked = session.LinkWindow(source);
        linked.Cursor = new TextPosition(3, 17);
        linked.TopLine = 2;

        session.SetCurrentWindow(source.WindowId);
        source.Cursor = new TextPosition(3, 9);
        session.SetMarker(1);
        var beforeLines = source.Document.Buffer.Snapshot();
        var beforeBlock = session.BlockState;
        var beforeVersion = source.Document.Version;

        // A remove followed by insert BEFORE "delta" would put beta/gamma
        // back where they already are. Do not manufacture a text edit.
        var dispatcher = new EditorCommandDispatcher(session);
        Assert.True(await dispatcher.ExecuteAsync(new(EditorCommandId.MoveBlock)));
        Assert.Equal(beforeLines, source.Document.Buffer.Snapshot());
        Assert.Equal(beforeBlock, session.BlockState);
        Assert.Equal(new TextPosition(3, 9), source.Cursor);
        Assert.Equal(new TextPosition(3, 17), linked.Cursor);
        Assert.Equal(2, linked.TopLine);
        Assert.Equal(beforeVersion, source.Document.Version);
        Assert.False(source.Document.IsDirty);
        Assert.Equal(0, session.Undo.Count);
        Assert.True(session.JumpToMarker(1));
        Assert.Equal(3, source.Cursor.Line);
    }

    [Fact]
    public async Task MoveBeforeEarlierLine_StillMovesTextAndUndoesAllTopology()
    {
        var session = new EditorSession();
        var source = session.CreateDocument("a\nb\nc\nd\ne");
        source.Cursor = new TextPosition(2, 0);
        session.BeginBlock();
        source.Cursor = new TextPosition(3, 0);
        session.EndBlock();

        var linked = session.LinkWindow(source);
        linked.Cursor = new TextPosition(4, 11);
        linked.TopLine = 3;
        session.SetCurrentWindow(source.WindowId);
        source.Cursor = new TextPosition(0, 6);
        var original = source.Document.Buffer.Snapshot();
        var dispatcher = new EditorCommandDispatcher(session);

        Assert.True(await dispatcher.ExecuteAsync(new(EditorCommandId.MoveBlock)));
        Assert.Equal(new[] { "c", "d", "a", "b", "e" },
            source.Document.Buffer.Snapshot().Select(x => x.Text));
        Assert.Equal(0, session.Block!.FirstLine);
        Assert.Equal(1, session.Block.LastLine);
        Assert.Equal(new TextPosition(2, 6), source.Cursor);
        Assert.Equal(new TextPosition(4, 11), linked.Cursor);
        // The linked cursor still refers to surviving "e". Its TopLine
        // originally referred to the *removed* "d" descriptor, so the
        // central deletion policy reattaches it to the next surviving line
        // before the move inserts the block at the beginning. This places
        // the resulting top-of-view on the final logical line, index 4.
        Assert.Equal(4, linked.TopLine);
        Assert.True(source.Document.IsDirty);
        Assert.Equal(1, session.Undo.Count);

        Assert.True(await dispatcher.ExecuteAsync(new(EditorCommandId.Undo)));
        Assert.Equal(original, source.Document.Buffer.Snapshot());
        Assert.Equal(new TextPosition(0, 6), source.Cursor);
        Assert.Equal(new TextPosition(4, 11), linked.Cursor);
        Assert.Equal(2, session.Block!.FirstLine);
        Assert.Equal(3, session.Block.LastLine);
        Assert.False(source.Document.IsDirty);
        Assert.Equal(0, session.Undo.Count);
    }

    [Fact]
    public async Task MoveWholeSourceToAnotherDocument_LeavesBlankDescriptorAndUndoesAtomically()
    {
        var session = new EditorSession();
        var source = session.CreateDocument("first\nsecond");
        source.Cursor = new TextPosition(0, 0);
        session.BeginBlock();
        source.Cursor = new TextPosition(1, 0);
        session.EndBlock();
        source.Document.Buffer.SetFlags(
            0, EditorLineFlags.InBlock | EditorLineFlags.UserColored | EditorLineFlags.Wrapped);

        var linkedSource = session.LinkWindow(source);
        linkedSource.Cursor = new TextPosition(1, 14);
        session.SetCurrentWindow(source.WindowId);
        source.Cursor = new TextPosition(1, 0);
        session.SetMarker(1);

        var destination = session.CreateDocument("x\ny");
        destination.Cursor = new TextPosition(1, 5);
        var beforeSource = source.Document.Buffer.Snapshot();
        var beforeTarget = destination.Document.Buffer.Snapshot();
        var dispatcher = new EditorCommandDispatcher(session);

        Assert.True(await dispatcher.ExecuteAsync(new(EditorCommandId.MoveBlock)));
        Assert.Equal(new[] { "" }, source.Document.Buffer.Snapshot().Select(x => x.Text));
        Assert.Equal(EditorLineFlags.None, source.Document.Buffer.GetLine(0).Flags);
        Assert.Equal(new[] { "x", "first", "second", "y" },
            destination.Document.Buffer.Snapshot().Select(x => x.Text));
        Assert.Equal(EditorLineFlags.Wrapped | EditorLineFlags.UserColored | EditorLineFlags.InBlock,
            destination.Document.Buffer.GetLine(1).Flags);
        Assert.Equal(destination.Document.DocumentId, session.Block!.DocumentId);
        Assert.Equal(1, session.Block.FirstLine);
        Assert.Equal(2, session.Block.LastLine);
        Assert.Equal(1, session.Undo.Count);
        Assert.True(source.Document.IsDirty);
        Assert.True(destination.Document.IsDirty);

        // One Undo restores both streams. In particular, source must not
        // retain a blank sentinel *and* the moved original descriptors.
        Assert.True(await dispatcher.ExecuteAsync(new(EditorCommandId.Undo)));
        Assert.Equal(beforeSource, source.Document.Buffer.Snapshot());
        Assert.Equal(beforeTarget, destination.Document.Buffer.Snapshot());
        Assert.Equal(source.Document.DocumentId, session.Block!.DocumentId);
        Assert.Equal(0, session.Block.FirstLine);
        Assert.Equal(1, session.Block.LastLine);
        Assert.Equal(new TextPosition(1, 14), linkedSource.Cursor);
        Assert.True(session.JumpToMarker(1));
        Assert.Equal(1, session.CurrentWindow.Cursor.Line);
        Assert.False(source.Document.IsDirty);
        Assert.False(destination.Document.IsDirty);
        Assert.Equal(0, session.Undo.Count);
    }

    [Fact]
    public async Task CopyIntoOwnBlock_UsesContiguousEndpointRealignmentAndUndo()
    {
        var session = new EditorSession();
        var source = session.CreateDocument("a\nb\nc\nd\ne");
        source.Cursor = new TextPosition(1, 0);
        session.BeginBlock();
        source.Cursor = new TextPosition(3, 0);
        session.EndBlock();
        source.Cursor = new TextPosition(2, 4);
        var original = source.Document.Buffer.Snapshot();

        // The handbook does not prohibit copying into the same block.
        // In a whole-line, contiguous endpoint model, insertion *inside*
        // the source span expands that span: both original endpoints
        // continue to identify their original logical lines.
        var dispatcher = new EditorCommandDispatcher(session);
        Assert.True(await dispatcher.ExecuteAsync(new(EditorCommandId.CopyBlock)));
        Assert.Equal(new[] { "a", "b", "b", "c", "d", "c", "d", "e" },
            source.Document.Buffer.Snapshot().Select(x => x.Text));
        Assert.Equal(1, session.Block!.FirstLine);
        Assert.Equal(6, session.Block.LastLine);
        Assert.Equal(new TextPosition(5, 4), source.Cursor);
        Assert.All(source.Document.Buffer.Snapshot().Skip(1).Take(6),
            line => Assert.True(line.Flags.HasFlag(EditorLineFlags.InBlock)));
        Assert.True(source.Document.IsDirty);
        Assert.Equal(1, session.Undo.Count);

        Assert.True(await dispatcher.ExecuteAsync(new(EditorCommandId.Undo)));
        Assert.Equal(original, source.Document.Buffer.Snapshot());
        Assert.Equal(1, session.Block!.FirstLine);
        Assert.Equal(3, session.Block.LastLine);
        Assert.False(source.Document.IsDirty);
        Assert.Equal(0, session.Undo.Count);
    }
}
