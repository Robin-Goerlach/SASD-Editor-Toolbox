using Sasd.Editor.Commands;
using Sasd.Editor.Editing;
using Sasd.Editor.Model;
using Xunit;

namespace Sasd.Editor.Core.Tests;

/// <summary>
/// Regression evidence for selective marker restoration after compound Undo.
/// Markers automatically shifted by block topology must be restored, while an
/// explicit marker reassignment made after the edit belongs to the user.
/// </summary>
public sealed class UndoMarkerReassignmentTests
{
    [Fact]
    public async Task CrossDocumentBlockMoveUndo_RestoresImplicitMarkersButKeepsLaterExplicitMarker()
    {
        var session = new EditorSession();
        var source = session.CreateDocument("a\nb\nc\nd");

        // Source marker 1 identifies a surviving line *after* the block:
        // moving the block will shift it from line 3 to line 1.
        source.Cursor = new TextPosition(3, 6);
        session.SetMarker(1);

        // Source marker 2 is *inside* the moved block. The ordinary
        // deletion policy invalidates it; Undo must restore it.
        source.Cursor = new TextPosition(2, 4);
        session.SetMarker(2);
        source.Cursor = new TextPosition(1, 0);
        session.BeginBlock();
        source.Cursor = new TextPosition(2, 0);
        session.EndBlock();

        var target = session.CreateDocument("x\ny");
        target.Cursor = new TextPosition(1, 8);
        session.SetMarker(3);

        var dispatcher = new EditorCommandDispatcher(session);
        Assert.True(await dispatcher.ExecuteAsync(new(EditorCommandId.MoveBlock)));
        Assert.Equal(new[] { "a", "d" },
            source.Document.Buffer.Snapshot().Select(x => x.Text));
        Assert.Equal(new[] { "x", "b", "c", "y" },
            target.Document.Buffer.Snapshot().Select(x => x.Text));
        Assert.False(session.JumpToMarker(2));
        Assert.Equal(1, session.Undo.Count);

        // A later *explicit* Set Marker is not part of the earlier move's
        // transaction. The user now reassigns marker 3 to target line 0,
        // replacing the topology-shifted post-move marker at target line 3.
        session.SetCurrentWindow(target.WindowId);
        target.Cursor = new TextPosition(0, 12);
        session.SetMarker(3);

        Assert.True(await dispatcher.ExecuteAsync(new(EditorCommandId.Undo)));

        // One Undo restores BOTH documents and the two markers affected by
        // the command itself, but must not overwrite marker 3's later value.
        Assert.Equal(new[] { "a", "b", "c", "d" },
            source.Document.Buffer.Snapshot().Select(x => x.Text));
        Assert.Equal(new[] { "x", "y" },
            target.Document.Buffer.Snapshot().Select(x => x.Text));
        Assert.Equal(source.Document.DocumentId, session.Block!.DocumentId);
        Assert.Equal(1, session.Block.FirstLine);
        Assert.Equal(2, session.Block.LastLine);
        Assert.Equal(0, session.Undo.Count);

        Assert.True(session.JumpToMarker(1));
        Assert.Same(source, session.CurrentWindow);
        Assert.Equal(3, source.Cursor.Line);

        Assert.True(session.JumpToMarker(2));
        Assert.Same(source, session.CurrentWindow);
        Assert.Equal(2, source.Cursor.Line);

        Assert.True(session.JumpToMarker(3));
        Assert.Same(target, session.CurrentWindow);
        Assert.Equal(0, target.Cursor.Line);
        Assert.Equal(12, target.Cursor.Column);
        Assert.False(source.Document.IsDirty);
        Assert.False(target.Document.IsDirty);
    }
}
