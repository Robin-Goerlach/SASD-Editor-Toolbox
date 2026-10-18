using Sasd.Editor.Commands;
using Sasd.Editor.Editing;
using Sasd.Editor.Model;
using Xunit;

namespace Sasd.Editor.Core.Tests;

/// <summary>
/// Relinking or closing the last view of a document must not leave an Undo
/// entry or marker targeting a text stream that is no longer reachable.
/// Linked sibling views are explicitly the opposite case: they retain history.
/// </summary>
public sealed class WindowDocumentLifetimeTests
{
    [Fact]
    public async Task LinkWindow_OrphansDestinationStream_DiscardsItsUndoAndMarkers()
    {
        var session = new EditorSession();
        var old = session.CreateDocument("orphan");
        session.Engine.InsertText("X");
        old.Cursor = new TextPosition(0, 1);
        session.SetMarker(1);

        var source = session.CreateDocument("live");
        session.Engine.InsertText("Y");
        Assert.Equal(2, session.Undo.Count);

        var commands = new EditorCommandDispatcher(session);
        Assert.True(await commands.ExecuteAsync(
            new(EditorCommandId.LinkWindow, Number: 1, Number2: 2)));
        Assert.Same(source.Document, old.Document);
        Assert.Equal(1, session.Undo.Count);
        Assert.False(session.JumpToMarker(1));

        // Undo must reach the remaining live stream in one invocation;
        // no orphaned snapshot is allowed to consume the command.
        Assert.True(await commands.ExecuteAsync(new(EditorCommandId.Undo)));
        Assert.Equal("live", source.Document.Buffer.GetLine(0).Text);
        Assert.Equal(0, session.Undo.Count);
    }

    [Fact]
    public async Task LinkWindow_PreservesHistoryWhenOtherViewStillDisplaysOldStream()
    {
        var session = new EditorSession();
        var old = session.CreateDocument("old");
        session.Engine.InsertText("X");
        old.Cursor = new TextPosition(0, 1);
        session.SetMarker(1);
        var sibling = session.LinkWindow(old);
        var source = session.CreateDocument("new");
        var commands = new EditorCommandDispatcher(session);

        Assert.True(await commands.ExecuteAsync(
            new(EditorCommandId.LinkWindow, Number: 1, Number2: 3)));
        Assert.Same(source.Document, old.Document);
        Assert.NotSame(source.Document, sibling.Document);
        Assert.Equal(1, session.Undo.Count);
        Assert.True(session.JumpToMarker(1));
        Assert.Same(sibling.Document, session.CurrentWindow.Document);

        Assert.True(await commands.ExecuteAsync(new(EditorCommandId.Undo)));
        Assert.Equal("old", sibling.Document.Buffer.GetLine(0).Text);
        Assert.Equal("new", source.Document.Buffer.GetLine(0).Text);
        Assert.Equal(0, session.Undo.Count);
    }

    [Fact]
    public void CloseWindow_RemovesOnlyOrphanedHistoryAndPreservesOtherDocumentEdits()
    {
        var session = new EditorSession();
        var former = session.CreateDocument("first");
        session.Engine.InsertText("X");
        former.Cursor = new TextPosition(0, 0);
        session.SetMarker(2);
        var live = session.CreateDocument("second");
        session.Engine.InsertText("Y");

        Assert.Equal(2, session.Undo.Count);
        Assert.True(session.CloseWindow(former.WindowId));
        Assert.Equal(1, session.Undo.Count);
        Assert.False(session.JumpToMarker(2));
        Assert.True(session.Engine.Undo());
        Assert.Equal("second", live.Document.Buffer.GetLine(0).Text);
    }

    [Fact]
    public void CloseOneLinkedWindow_DoesNotDestroySharedHistoryOrMarker()
    {
        var session = new EditorSession();
        var first = session.CreateDocument("value");
        session.Engine.InsertText("X");
        first.Cursor = new TextPosition(0, 2);
        session.SetMarker(1);
        var linked = session.LinkWindow(first);

        Assert.True(session.CloseWindow(first.WindowId));
        Assert.Equal(1, session.Undo.Count);
        Assert.True(session.JumpToMarker(1));
        Assert.Same(linked.Document, session.CurrentWindow.Document);
        Assert.True(session.Engine.Undo());
        Assert.Equal("value", linked.Document.Buffer.GetLine(0).Text);
    }

    [Fact]
    public async Task LinkWindow_OrphansBlockDocument_ResetsIncompleteEndpoints()
    {
        var session = new EditorSession();
        var orphan = session.CreateDocument("a\nb");
        orphan.Cursor = new TextPosition(0, 0);
        session.BeginBlock();
        Assert.NotNull(session.BlockState.Start);

        session.CreateDocument("other");
        Assert.True(await new EditorCommandDispatcher(session).ExecuteAsync(
            new(EditorCommandId.LinkWindow, Number: 1, Number2: 2)));
        Assert.Null(session.BlockState.Start);
        Assert.Null(session.BlockState.End);
        Assert.Null(session.Block);
    }

    [Fact]
    public async Task LinkWindow_OrphanedDocumentDiscardRemovesCrossDocumentUndoAtomically()
    {
        var session = new EditorSession();
        var source = session.CreateDocument("a\nb");
        source.Cursor = new TextPosition(0, 0);
        session.BeginBlock();
        session.EndBlock();

        var target = session.CreateDocument("x\ny");
        target.Cursor = new TextPosition(1, 0);
        var commands = new EditorCommandDispatcher(session);
        Assert.True(await commands.ExecuteAsync(new(EditorCommandId.MoveBlock)));
        Assert.Equal(1, session.Undo.Count);

        // Replace the sole source view. The compound transaction touches
        // both source and target, so preserving only its target half would
        // make a later Undo restore inconsistent streams.
        Assert.True(await commands.ExecuteAsync(
            new(EditorCommandId.LinkWindow, Number: 1, Number2: 2)));
        Assert.Equal(0, session.Undo.Count);
        Assert.False(await commands.ExecuteAsync(new(EditorCommandId.Undo)));
    }
}
