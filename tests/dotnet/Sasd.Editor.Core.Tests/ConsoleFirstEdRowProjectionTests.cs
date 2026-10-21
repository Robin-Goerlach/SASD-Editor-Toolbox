using Sasd.Editor.Commands;
using Sasd.Editor.Editing;
using Sasd.Editor.FirstEd.Sample;
using Sasd.Editor.Model;
using Xunit;

namespace Sasd.Editor.Core.Tests;

/// <summary>
/// Headless acceptance tests for the only boundary that is allowed to
/// translate stored text into physical FIRST-ED terminal output.
/// </summary>
/// <remarks>
/// The editor intentionally stores ASCII controls. In particular Ctrl-P+[
/// inserts a literal ESC. Feeding a line such as ESC[2J directly to Console
/// would invoke the terminal's clear-screen instruction instead of merely
/// showing the document. These tests therefore assert emitted display data,
/// not only the editor's underlying (unchanged) text.
/// </remarks>
public sealed class ConsoleFirstEdRowProjectionTests
{
    [Theory]
    [InlineData("\u0000", "^@")]
    [InlineData("\u0001", "^A")]
    [InlineData("\u0009", "^I")]
    [InlineData("\u000a", "^J")]
    [InlineData("\u000d", "^M")]
    [InlineData("\u001b", "^[")]
    [InlineData("\u001f", "^_")]
    [InlineData("\u007f", "^?")]
    [InlineData("\u009b", "[9B]")]
    public void TerminalControlCharacterIsPrintedAsInertAscii(
        string raw, string safe)
    {
        var projected = ConsoleFirstEdRowProjection.Project(raw, 0, 8);

        Assert.StartsWith(safe, projected.Text);
        Assert.Equal(8, projected.Text.Length);
        Assert.DoesNotContain(projected.Text, char.IsControl);
    }

    [Fact]
    public void EscapeSequenceInsideDocumentCannotClearPhysicalTerminal()
    {
        const string documentLine = "safe\u001b[2J\u001b[Htail";
        var projected = ConsoleFirstEdRowProjection.Project(
            documentLine, 0, 30);

        // ^[ is visible but it contains no ASCII ESC. Both CSI sequences
        // are inert printable text; no newline, backspace or tab is emitted.
        Assert.StartsWith("safe^[[2J^[[Htail", projected.Text);
        Assert.DoesNotContain('\u001b', projected.Text);
        Assert.DoesNotContain(projected.Text, char.IsControl);
    }

    [Fact]
    public async Task StoredCtrlPControlIsEscapedForDisplayButRemainsUndoable()
    {
        var session = new EditorSession();
        var view = session.CreateDocument("abc");
        view.Cursor = new TextPosition(0, 1);

        var dispatcher = new EditorCommandDispatcher(session);
        Assert.True(await dispatcher.ExecuteAsync(
            new(EditorCommandId.InsertControlCharacter, Text: "\u001b")));
        var stored = view.Document.Buffer.GetLine(0).Text;
        Assert.Equal("a\u001bbc", stored);

        var display = ConsoleFirstEdRowProjection.Project(stored, 0, 8, 2);
        Assert.Equal("a^[bc   ", display.Text);
        Assert.Equal(3, display.CursorColumn);
        Assert.Equal("a\u001bbc", view.Document.Buffer.GetLine(0).Text);

        Assert.True(await dispatcher.ExecuteAsync(new(EditorCommandId.Undo)));
        Assert.Equal("abc", view.Document.Buffer.GetLine(0).Text);
        Assert.Equal(0, session.Undo.Count);
    }

    [Theory]
    [InlineData("界", 2)]
    [InlineData("\ud83d\ude00", 2)]
    [InlineData("e\u0301", 1)]
    [InlineData("é", 1)]
    public void UnicodeGraphemeHasConservativeTerminalCellWidth(
        string glyph, int expectedCells)
    {
        var text = "a" + glyph + "z";
        var cursor = 1 + glyph.Length;
        var projected = ConsoleFirstEdRowProjection.Project(text, 0, 10, cursor);

        Assert.Equal("a" + glyph + "z", projected.Text.TrimEnd());
        Assert.Equal(1 + expectedCells, projected.CursorColumn);
        Assert.DoesNotContain(projected.Text, char.IsControl);
    }

    [Fact]
    public void ProjectionNeverBreaksWideGlyphOrSurrogateAtRightEdge()
    {
        Assert.Equal("a ", ConsoleFirstEdRowProjection.Project("a界b", 0, 2).Text);
        Assert.Equal("a ", ConsoleFirstEdRowProjection.Project(
            "a\ud83d\ude00b", 0, 2).Text);
        Assert.Equal("a界", ConsoleFirstEdRowProjection.Project("a界b", 0, 3).Text);
        Assert.Equal("a\ud83d\ude00", ConsoleFirstEdRowProjection.Project(
            "a\ud83d\ude00b", 0, 3).Text);
        Assert.Equal("e\u0301", ConsoleFirstEdRowProjection.Project(
            "e\u0301b", 0, 1).Text);
    }

    [Fact]
    public void HorizontalOffsetInsideGraphemeSkipsInsteadOfPrintingHalf()
    {
        var text = "a\ud83d\ude00b";
        var projected = ConsoleFirstEdRowProjection.Project(text, 2, 4, 3);
        Assert.Equal("b   ", projected.Text);
        Assert.Equal(0, projected.CursorColumn);

        var accent = ConsoleFirstEdRowProjection.Project("ae\u0301b", 2, 4);
        Assert.Equal("b   ", accent.Text);
    }

    [Fact]
    public void VirtualSpaceCursorDoesNotInsertStoredPadding()
    {
        var projected = ConsoleFirstEdRowProjection.Project("abc", 0, 10, 7);
        Assert.Equal("abc       ", projected.Text);
        Assert.Equal(7, projected.CursorColumn);
    }

    [Fact]
    public void MalformedUnpairedSurrogateIsNeverWrittenToTerminal()
    {
        var projected = ConsoleFirstEdRowProjection.Project("a\ud83db", 0, 8);
        Assert.Equal("a?b     ", projected.Text);
        Assert.DoesNotContain('\ud83d', projected.Text);
    }

    [Fact]
    public void NarrowRowsCannotEmitHalfOfEscapedControl()
    {
        Assert.Equal(" ", ConsoleFirstEdRowProjection.Project("\u001b", 0, 1).Text);
        Assert.Equal("^[", ConsoleFirstEdRowProjection.Project("\u001b", 0, 2).Text);
        Assert.Equal(" ", ConsoleFirstEdRowProjection.Project("\u009b", 0, 1).Text);
    }

    [Theory]
    [InlineData(-1, 5, 0)]
    [InlineData(0, 0, 0)]
    [InlineData(0, 5, -1)]
    public void InvalidGeometryIsRejectedBeforePainting(
        int firstCodeUnit, int width, int cursorCodeUnit)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            ConsoleFirstEdRowProjection.Project(
                "safe", firstCodeUnit, width, cursorCodeUnit));
    }
}
