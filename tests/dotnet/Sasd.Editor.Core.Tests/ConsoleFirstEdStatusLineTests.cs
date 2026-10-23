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
