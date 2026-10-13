using Sasd.Editor.Commands;
using Sasd.Editor.Editing;
using Sasd.Editor.Model;
using Xunit;

namespace Sasd.Editor.Core.Tests;

/// <summary>
/// Regression coverage for the correctness-first compound undo transaction
/// model. These tests protect the modern atomicity policy without pretending
/// that the historical line-stack Undo implementation exposed the same model.
/// </summary>
public sealed class CompoundUndoCompatibilityTests
{
    [Fact]
    public async Task CrossDocumentBlockMove_IsOneUndoItemAndRestoresBothDocuments()
    {
        var session = new EditorSession();

        var source = session.CreateDocument("a\nb\nc\nd");
        source.Cursor = new TextPosition(1, 0);
        session.BeginBlock();
        source.Cursor = new TextPosition(2, 0);
        session.EndBlock();

        source.Cursor = new TextPosition(3, 2);
        session.SetMarker(1);

        var sourceLinked = session.LinkWindow(source);
        sourceLinked.Cursor = new TextPosition(3, 6);
        sourceLinked.TopLine = 3;
        sourceLinked.LeftColumn = 2;

        var target = session.CreateDocument("x\ny");
        target.Cursor = new TextPosition(1, 4);
        target.TopLine = 1;
        session.SetMarker(2);

        var dispatcher = new EditorCommandDispatcher(session);
        Assert.True(await dispatcher.ExecuteAsync(new(EditorCommandId.MoveBlock)));

        Assert.Equal(1, session.Undo.Count);
        Assert.Equal(new[] { "a", "d" }, source.Document.Buffer.Snapshot().Select(line => line.Text));
        Assert.Equal(new[] { "x", "b", "c", "y" }, target.Document.Buffer.Snapshot().Select(line => line.Text));
        Assert.Equal(target.Document.DocumentId, session.Block!.DocumentId);

        Assert.True(session.Engine.Undo());

        // A move is one logical command. One Undo must restore both streams;
        // otherwise the target would retain a duplicate of the moved lines.
        Assert.Equal(new[] { "a", "b", "c", "d" }, source.Document.Buffer.Snapshot().Select(line => line.Text));
        Assert.Equal(new[] { "x", "y" }, target.Document.Buffer.Snapshot().Select(line => line.Text));
        Assert.Equal(0, session.Undo.Count);

        // Topology-adjusted view state is restored to the pre-command anchors.
        Assert.Same(target, session.CurrentWindow);
        Assert.Equal(new TextPosition(1, 4), target.Cursor);
        Assert.Equal(1, target.TopLine);
        Assert.Equal(new TextPosition(3, 6), sourceLinked.Cursor);
        Assert.Equal(3, sourceLinked.TopLine);
        Assert.Equal(2, sourceLinked.LeftColumn);

        // The logical block also returns to its source range and is re-projected
        // into line flags after both document snapshots have been restored.
        Assert.Equal(source.Document.DocumentId, session.Block!.DocumentId);
        Assert.Equal(1, session.Block.FirstLine);
        Assert.Equal(2, session.Block.LastLine);
        Assert.True(source.Document.Buffer.GetLine(1).Flags.HasFlag(EditorLineFlags.InBlock));
        Assert.True(source.Document.Buffer.GetLine(2).Flags.HasFlag(EditorLineFlags.InBlock));
        Assert.DoesNotContain(
            target.Document.Buffer.Snapshot(),
            line => line.Flags.HasFlag(EditorLineFlags.InBlock));

        // Markers shifted by source deletion / target insertion are restored too.
        Assert.True(session.JumpToMarker(1));
        Assert.Same(source, session.CurrentWindow);
        Assert.Equal(3, source.Cursor.Line);

        Assert.True(session.JumpToMarker(2));
        Assert.Same(target, session.CurrentWindow);
        Assert.Equal(1, target.Cursor.Line);
    }

    [Fact]
    public async Task DeleteLineUndo_RestoresMarkerBlockAndLinkedViewState()
    {
        var session = new EditorSession();
        var source = session.CreateDocument("a\nb\nc\nd");

        source.Cursor = new TextPosition(1, 0);
        session.BeginBlock();
        session.SetMarker(1);
        source.Cursor = new TextPosition(2, 0);
        session.EndBlock();

        var linked = session.LinkWindow(source);
        linked.Cursor = new TextPosition(3, 8);
        linked.TopLine = 3;

        session.SetCurrentWindow(source.WindowId);
        source.Cursor = new TextPosition(1, 5);

        var dispatcher = new EditorCommandDispatcher(session);
        Assert.True(await dispatcher.ExecuteAsync(new(EditorCommandId.DeleteLine)));

        Assert.False(session.JumpToMarker(1));
        Assert.Null(session.Block);
        Assert.Equal(new TextPosition(2, 8), linked.Cursor);

        // JumpToMarker above is a no-op because the deleted marker is undefined,
        // so the post-command session state still matches the undo transaction.
        Assert.True(session.Engine.Undo());

        Assert.Equal(new[] { "a", "b", "c", "d" }, source.Document.Buffer.Snapshot().Select(line => line.Text));
        Assert.Equal(new TextPosition(1, 5), source.Cursor);
        Assert.Equal(new TextPosition(3, 8), linked.Cursor);
        Assert.Equal(3, linked.TopLine);
        Assert.NotNull(session.Block);
        Assert.Equal(1, session.Block!.FirstLine);
        Assert.Equal(2, session.Block.LastLine);

        Assert.True(session.JumpToMarker(1));
        Assert.Equal(1, session.CurrentWindow.Cursor.Line);
    }

    [Fact]
    public async Task Undo_PreservesExplicitNavigationAndMarkerChangesMadeAfterEdit()
    {
        var session = new EditorSession();
        var window = session.CreateDocument("a\nb\nc");
        window.Cursor = new TextPosition(0, 0);
        var dispatcher = new EditorCommandDispatcher(session);

        Assert.True(await dispatcher.ExecuteAsync(new(EditorCommandId.InsertLine)));

        // These changes happen after the text transaction and are intentionally
        // not undoable text state. The post-state comparison in the undo manager
        // prevents the earlier transaction from blindly overwriting them.
        window.Cursor = new TextPosition(2, 7);
        window.TopLine = 1;
        session.SetMarker(1);

        Assert.True(session.Engine.Undo());

        Assert.Equal(new[] { "a", "b", "c" }, window.Document.Buffer.Snapshot().Select(line => line.Text));
        Assert.Equal(new TextPosition(2, 7), window.Cursor);
        Assert.Equal(1, window.TopLine);

        Assert.True(session.JumpToMarker(1));
        Assert.Equal(2, window.Cursor.Line);
    }

    [Fact]
    public async Task DestroyingOneDocument_DiscardsWholeCompoundUndoItem()
    {
        var session = new EditorSession();
        var source = session.CreateDocument("a\nb\nc");
        source.Cursor = new TextPosition(1, 0);
        session.BeginBlock();
        session.EndBlock();

        var target = session.CreateDocument("x\ny");
        target.Cursor = new TextPosition(1, 0);
        var dispatcher = new EditorCommandDispatcher(session);

        Assert.True(await dispatcher.ExecuteAsync(new(EditorCommandId.MoveBlock)));
        Assert.Equal(1, session.Undo.Count);

        session.SetCurrentWindow(source.WindowId);
        Assert.True(await dispatcher.ExecuteAsync(new(EditorCommandId.DeleteWindowText)));

        // DeleteWindowText is deliberately destructive and non-undoable. Because
        // the preceding move transaction also owns target state, retaining only
        // the target half would violate compound atomicity.
        Assert.Equal(0, session.Undo.Count);
        Assert.False(session.Engine.Undo());
    }
}
