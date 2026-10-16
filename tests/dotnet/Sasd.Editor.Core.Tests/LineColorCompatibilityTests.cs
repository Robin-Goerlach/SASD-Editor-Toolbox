using Sasd.Editor.Editing;
using Sasd.Editor.Model;
using Xunit;

namespace Sasd.Editor.Core.Tests;

/// <summary>
/// Behavioral clean-room coverage for EditColorLine and EditColorFile.
/// The historical operations set the line's Colored bit; the modern core
/// models that bit with UserColored, without tying it to terminal rendering.
/// </summary>
public sealed class LineColorCompatibilityTests
{
    [Fact]
    public void ColorCurrentLine_SetsOnlySelectedLineAndPreservesOtherFlags()
    {
        var session = new EditorSession();
        var view = session.CreateDocument("first\nsecond\nthird");
        var document = view.Document;
        document.Buffer.SetFlags(1, EditorLineFlags.Wrapped | EditorLineFlags.InBlock);
        view.Cursor = new TextPosition(1, 9);

        session.ColorCurrentLine();

        Assert.Equal(EditorLineFlags.None, document.Buffer.GetLine(0).Flags);
        Assert.Equal(
            EditorLineFlags.Wrapped | EditorLineFlags.InBlock | EditorLineFlags.UserColored,
            document.Buffer.GetLine(1).Flags);
        Assert.Equal(EditorLineFlags.None, document.Buffer.GetLine(2).Flags);
        Assert.Equal(new TextPosition(1, 9), view.Cursor);
        Assert.False(document.IsDirty);
        Assert.Equal(0, document.Version);
        Assert.Equal(0, session.Undo.Count);
    }

    [Fact]
    public void ColorCurrentDocument_ColorsAllLinesIdempotentlyWithoutTextMutation()
    {
        var session = new EditorSession();
        var view = session.CreateDocument("one\ntwo\nthree");
        var document = view.Document;
        document.Buffer.SetFlags(0, EditorLineFlags.Wrapped);
        document.Buffer.SetFlags(1, EditorLineFlags.UserColored);
        var initialTexts = document.Buffer.Snapshot().Select(static line => line.Text).ToArray();

        session.ColorCurrentDocument();
        session.ColorCurrentDocument();

        foreach (var line in document.Buffer.Snapshot())
        {
            Assert.True(line.Flags.HasFlag(EditorLineFlags.UserColored));
        }
        Assert.True(document.Buffer.GetLine(0).Flags.HasFlag(EditorLineFlags.Wrapped));
        Assert.Equal(initialTexts, document.Buffer.Snapshot().Select(static line => line.Text));
        Assert.False(document.IsDirty);
        Assert.Equal(0, document.Version);
        Assert.Equal(0, session.Undo.Count);
    }

    [Fact]
    public void ColorThroughLinkedWindow_IsVisibleToSiblingButNotOtherDocuments()
    {
        var session = new EditorSession();
        var firstView = session.CreateDocument("shared one\nshared two");
        var secondView = session.LinkWindow(firstView);
        secondView.Cursor = new TextPosition(1, 0);
        var other = session.CreateDocument("unrelated");
        session.SetCurrentWindow(secondView.WindowId);

        session.ColorCurrentLine();

        Assert.Same(firstView.Document, secondView.Document);
        Assert.True(firstView.Document.Buffer.GetLine(1).Flags.HasFlag(EditorLineFlags.UserColored));
        Assert.False(firstView.Document.Buffer.GetLine(0).Flags.HasFlag(EditorLineFlags.UserColored));
        Assert.False(other.Document.Buffer.GetLine(0).Flags.HasFlag(EditorLineFlags.UserColored));
        Assert.Equal(secondView.WindowId, session.CurrentWindow.WindowId);
    }
}
