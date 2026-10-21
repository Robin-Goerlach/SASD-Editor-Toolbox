using Sasd.Editor.Commands;
using Sasd.Editor.Editing;
using Sasd.Editor.Model;
using Xunit;

namespace Sasd.Editor.Core.Tests;

/// <summary>
/// Regression tests for Undo's separation of document identity from view
/// identity. FIRST-ED allows several windows to display one text stream and
/// later reconnects those windows to different streams.
/// </summary>
/// <remarks>
/// SASD stores snapshot text by DocumentId, while cursor state belongs to a
/// particular WindowId AND its attached document. Treating WindowId alone as
/// sufficient after Link Window would restore a valid edit to the wrong text
/// stream's view and unexpectedly change the user's current window.
/// </remarks>
public sealed class UndoWindowIdentityTests
{
    [Fact]
    public async Task LegacyCapture_UndoAfterRelinkingOriginalWindow_DoesNotStealFocus()
    {
        var session = new EditorSession();
        var capturedView = session.CreateDocument("original");
        capturedView.Cursor = new TextPosition(0, 3);

        // The older public capture API is supported for integrations that
        // record a pre-edit snapshot themselves. It is deliberately tested
        // separately from the preferred atomic BeginTransaction API.
        session.Undo.Capture(capturedView, "legacy manual edit");
        var originalDocument = capturedView.Document;
        originalDocument.Buffer.ReplaceLine(0, "modified");
        originalDocument.MarkChanged();
        var sibling = session.LinkWindow(capturedView);

        // An entirely different document is displayed in another window.
        // Reattach the captured window to it while a linked sibling keeps
        // the edited original document (and its Undo snapshot) alive.
        var other = session.CreateDocument("other");
        other.Cursor = new TextPosition(0, 8);
        capturedView.Cursor = new TextPosition(0, 11);
        var dispatcher = new EditorCommandDispatcher(session);
        Assert.True(await dispatcher.ExecuteAsync(
            new(EditorCommandId.LinkWindow, Number: 1, Number2: 3)));
        Assert.Same(other.Document, capturedView.Document);
        Assert.Same(originalDocument, sibling.Document);
        Assert.Equal(1, session.Undo.Count);

        session.SetCurrentWindow(other.WindowId);
        Assert.True(await dispatcher.ExecuteAsync(new(EditorCommandId.Undo)));

        // Text is restored in its ORIGINAL document, not the new attachment.
        // Undo must not move the relinked view's cursor or activate it.
        Assert.Equal("original", sibling.Document.Buffer.GetLine(0).Text);
        Assert.Equal("other", other.Document.Buffer.GetLine(0).Text);
        Assert.Equal(new TextPosition(0, 11), capturedView.Cursor);
        Assert.Equal(new TextPosition(0, 8), other.Cursor);
        Assert.Same(other, session.CurrentWindow);
        Assert.False(sibling.Document.IsDirty);
        Assert.Equal(0, session.Undo.Count);
    }

    [Fact]
    public void LegacyCapture_UndoStillRestoresCursorOnItsOriginalWindow()
    {
        var session = new EditorSession();
        var window = session.CreateDocument("initial");
        window.Cursor = new TextPosition(0, 2);
        session.Undo.Capture(window, "legacy edit");

        window.Document.Buffer.ReplaceLine(0, "changed");
        window.Document.MarkChanged();
        window.Cursor = new TextPosition(0, 7);

        Assert.True(session.Engine.Undo());
        Assert.Equal("initial", window.Document.Buffer.GetLine(0).Text);
        Assert.Equal(new TextPosition(0, 2), window.Cursor);
        Assert.False(window.Document.IsDirty);
        Assert.Equal(0, session.Undo.Count);
    }

    [Fact]
    public void LegacyCapture_ClosedViewFallsBackToSurvivingLinkedDocumentView()
    {
        var session = new EditorSession();
        var oldView = session.CreateDocument("before");
        oldView.Cursor = new TextPosition(0, 4);
        session.Undo.Capture(oldView, "legacy edit");
        oldView.Document.Buffer.ReplaceLine(0, "after");
        oldView.Document.MarkChanged();

        var sibling = session.LinkWindow(oldView);
        sibling.Cursor = new TextPosition(0, 12);
        var unrelated = session.CreateDocument("independent");

        // Closing the captured view is different from relinking it: there
        // is no live window whose identity must be protected from a cursor
        // restore. The established fallback can use a sibling of that stream.
        Assert.True(session.CloseWindow(oldView.WindowId));
        Assert.Equal(1, session.Undo.Count);
        session.SetCurrentWindow(unrelated.WindowId);
        Assert.True(session.Engine.Undo());

        Assert.Equal("before", sibling.Document.Buffer.GetLine(0).Text);
        Assert.Same(sibling, session.CurrentWindow);
        Assert.Equal(new TextPosition(0, 4), sibling.Cursor);
        Assert.Equal("independent", unrelated.Document.Buffer.GetLine(0).Text);
    }

    [Fact]
    public async Task TransactionUndo_AfterRelinkRestoresTextButPreservesNewViewNavigation()
    {
        var session = new EditorSession();
        var captured = session.CreateDocument("first");
        captured.Cursor = new TextPosition(0, 2);
        session.Engine.InsertText("X");
        var sibling = session.LinkWindow(captured);
        var other = session.CreateDocument("second");
        other.Cursor = new TextPosition(0, 14);

        var dispatcher = new EditorCommandDispatcher(session);
        Assert.True(await dispatcher.ExecuteAsync(
            new(EditorCommandId.LinkWindow, Number: 1, Number2: 3)));
        captured.Cursor = new TextPosition(0, 9);
        session.SetCurrentWindow(other.WindowId);

        // The newer transaction API already validates both view and
        // document identity. This test prevents the legacy repair from
        // accidentally weakening its independent protection.
        Assert.True(await dispatcher.ExecuteAsync(new(EditorCommandId.Undo)));
        Assert.Equal("first", sibling.Document.Buffer.GetLine(0).Text);
        Assert.Equal("second", captured.Document.Buffer.GetLine(0).Text);
        Assert.Equal(new TextPosition(0, 9), captured.Cursor);
        Assert.Equal(new TextPosition(0, 14), other.Cursor);
        Assert.Same(other, session.CurrentWindow);
    }
}
