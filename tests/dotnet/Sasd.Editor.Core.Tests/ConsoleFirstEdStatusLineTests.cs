using Sasd.Editor.Commands;
using Sasd.Editor.Editing;
using Sasd.Editor.FirstEd.Sample;
using Sasd.Editor.Rendering;
using Sasd.Editor.Model;
using Xunit;

namespace Sasd.Editor.Core.Tests;

/// <summary>
/// Status-header integration tests without a physical console. Two linked
/// views share a document but MUST preserve distinct cursor positions and
/// one unambiguous active marker after switching keyboard focus.
/// </summary>
public sealed class ConsoleFirstEdStatusLineTests
{
    [Fact]
    public void LinkedWindowsHaveDistinctCursorLabelsAndOnlyOneActiveMarker()
    {
        var session = new EditorSession();
        var first = session.CreateDocument(string.Join((char)10, "alpha", "beta"));
        var second = session.LinkWindow(first);
        first.Cursor = new TextPosition(0, 0);
        second.Cursor = new TextPosition(1, 2);
        var builder = new EditorViewportBuilder(session);

        session.SetCurrentWindow(first.WindowId);
        var firstLabel = ConsoleFirstEdStatusLinePolicy.Format(
            builder.Build(first, 3, 40).Status, 1, true);
        var secondLabel = ConsoleFirstEdStatusLinePolicy.Format(
            builder.Build(second, 3, 40).Status, 2, false);
        Assert.StartsWith(">W1 ", firstLabel);
        Assert.StartsWith(" W2 ", secondLabel);
        Assert.Contains("Ln 1  Col 1", firstLabel);
        Assert.Contains("Ln 2  Col 3", secondLabel);

        // Switching focus must not transpose the windows' own status
        // values. The view-specific numbers remain stable; only the two
        // active markers invert.
        session.SetCurrentWindow(second.WindowId);
        var firstAfter = ConsoleFirstEdStatusLinePolicy.Format(
            builder.Build(first, 3, 40).Status, 1, false);
        var secondAfter = ConsoleFirstEdStatusLinePolicy.Format(
            builder.Build(second, 3, 40).Status, 2, true);
        Assert.Equal(" " + firstLabel[1..], firstAfter);
        Assert.Equal(">" + secondLabel[1..], secondAfter);
        Assert.Same(first.Document, second.Document);
    }

    [Fact]
    public async Task WindowResizeAndDeletionRebuildHeadersFromSurvivingFrames()
    {
        var session = new EditorSession();
        var first = session.CreateDocument("first");
        Assert.True(session.WindowLayout.Configure(18));
        var dispatcher = new EditorCommandDispatcher(session);

        // The historical Create Window operation splits an existing screen
        // rectangle, unlike Link Window which can share a document. Use
        // semantic commands here so the test includes real window ownership.
        Assert.True(await dispatcher.ExecuteAsync(
            new EditorCommandRequest(EditorCommandId.CreateWindow,
                Number: 6, Number2: 1)));
        var second = session.Windows[1];
        second.Cursor = new TextPosition(0, 2);
        var builder = new EditorViewportBuilder(session);

        // Resizing changes only geometry. Rebuild each header from the
        // surviving frame identity and that window's own viewport status.
        Assert.True(session.WindowLayout.ResizeWorkspace(14));
        var frames = session.WindowLayout.Frames;
        Assert.Equal(2, frames.Count);
        Assert.Equal(14, frames.Sum(frame => frame.Height));
        Assert.Equal(first.WindowId, frames[0].WindowId);
        Assert.Equal(second.WindowId, frames[1].WindowId);

        var firstHeader = ConsoleFirstEdStatusLinePolicy.Format(
            builder.Build(first, frames[0].TextRows, 30).Status,
            1, first.WindowId == session.CurrentWindow.WindowId);
        var secondHeader = ConsoleFirstEdStatusLinePolicy.Format(
            builder.Build(second, frames[1].TextRows, 30).Status,
            2, second.WindowId == session.CurrentWindow.WindowId);
        Assert.StartsWith(" W1 ", firstHeader);
        Assert.StartsWith(">W2 ", secondHeader);
        Assert.Contains("Col 3", secondHeader);

        // Removing the first window renumbers the remaining display row,
        // not its document or cursor. A stale W2 header would misidentify
        // the only editable window after the layout has reclaimed its rows.
        Assert.True(await dispatcher.ExecuteAsync(
            new EditorCommandRequest(EditorCommandId.DeleteWindow, Number: 1)));
        var survivingFrame = Assert.Single(session.WindowLayout.Frames);
        Assert.Equal(second.WindowId, survivingFrame.WindowId);
        Assert.Equal(14, survivingFrame.Height);
        var onlyHeader = ConsoleFirstEdStatusLinePolicy.Format(
            builder.Build(second, survivingFrame.TextRows, 30).Status,
            1, second.WindowId == session.CurrentWindow.WindowId);
        Assert.StartsWith(">W1 ", onlyHeader);
        Assert.Contains("Col 3", onlyHeader);
        Assert.DoesNotContain("W2", onlyHeader);
    }

    [Fact]
    public void RejectedTerminalResizePreservesFramesAndDisplayedStatus()
    {
        var session = new EditorSession();
        var first = session.CreateDocument("first document");
        var second = session.CreateDocument("second document");
        Assert.True(session.WindowLayout.Configure(12));
        session.SetCurrentWindow(second.WindowId);
        second.Cursor = new TextPosition(0, 4);

        var builder = new EditorViewportBuilder(session);
        var beforeFrames = session.WindowLayout.Frames.ToArray();
        var beforeHeaders = beforeFrames.Select((frame, index) =>
        {
            var window = session.Windows[index];
            return ConsoleFirstEdStatusLinePolicy.Format(
                builder.Build(window, frame.TextRows, 30).Status,
                index + 1, window.WindowId == session.CurrentWindow.WindowId);
        }).ToArray();
        var originalUndo = session.Undo.Count;

        // Each window needs at least three terminal rows. A six-row workspace
        // is legal for two windows, but five rows cannot accommodate both.
        // Rejection must be atomic: if a partial shrink escaped, the host
        // could repaint a header over another window's document area.
        Assert.False(session.WindowLayout.ResizeWorkspace(5));
        Assert.Equal(12, session.WindowLayout.TotalRows);
        Assert.Equal(beforeFrames, session.WindowLayout.Frames);

        var afterHeaders = session.WindowLayout.Frames.Select((frame, index) =>
        {
            var window = session.Windows[index];
            return ConsoleFirstEdStatusLinePolicy.Format(
                builder.Build(window, frame.TextRows, 30).Status,
                index + 1, window.WindowId == session.CurrentWindow.WindowId);
        }).ToArray();

        // Preserve both the exact physical header strings and the distinct
        // view/document identities; a refused host resize must never become
        // an editor command, cursor edit, dirty change, or Undo transaction.
        Assert.Equal(beforeHeaders, afterHeaders);
        Assert.StartsWith(" W1 ", afterHeaders[0]);
        Assert.StartsWith(">W2 ", afterHeaders[1]);
        Assert.Contains("Col 5", afterHeaders[1]);
        Assert.Same(first, session.Windows[0]);
        Assert.Same(second, session.CurrentWindow);
        Assert.False(first.Document.IsDirty);
        Assert.False(second.Document.IsDirty);
        Assert.Equal(originalUndo, session.Undo.Count);
    }

    [Fact]
    public void DirtyMarkerIsSharedByLinkedViewsWithoutChangingUndo()
    {
        var session = new EditorSession();
        var first = session.CreateDocument("unchanged");
        var second = session.LinkWindow(first);
        session.SetCurrentWindow(first.WindowId);
        session.Engine.InsertText("edited ");
        var undo = session.Undo.Count;
        var builder = new EditorViewportBuilder(session);

        var firstLabel = ConsoleFirstEdStatusLinePolicy.Format(
            builder.Build(first, 3, 40).Status, 1, true);
        var secondLabel = ConsoleFirstEdStatusLinePolicy.Format(
            builder.Build(second, 3, 40).Status, 2, false);
        Assert.EndsWith(" *", firstLabel);
        Assert.EndsWith(" *", secondLabel);
        Assert.Equal(undo, session.Undo.Count);
        Assert.True(first.Document.IsDirty);
    }
}
