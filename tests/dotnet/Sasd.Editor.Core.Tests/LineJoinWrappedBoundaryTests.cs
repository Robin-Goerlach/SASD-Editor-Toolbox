using System.Text;
using Sasd.Editor.Commands;
using Sasd.Editor.Editing;
using Sasd.Editor.IO;
using Sasd.Editor.Model;
using Xunit;

namespace Sasd.Editor.Core.Tests;

/// <summary>
/// Byte-level regression tests for the historical line-join commands and the
/// 0x8D FIRST-ED soft-boundary convention.
/// </summary>
/// <remarks>
/// A Wrapped bit belongs to the separator AFTER its line, not the line's
/// characters. When two lines are joined, their shared separator disappears.
/// Therefore the surviving line must inherit the REMOVED LOWER line's outgoing
/// flag, even though its text/colour remain attached to the upper descriptor.
/// Testing the actual legacy codec catches regressions that purely visual
/// line comparisons (e.g. checking only "abcdef") could silently miss.
/// </remarks>
public sealed class LineJoinWrappedBoundaryTests
{
    [Theory]
    [InlineData(EditorCommandId.DeleteRightCharacter, true, false)]
    [InlineData(EditorCommandId.DeleteRightCharacter, false, true)]
    [InlineData(EditorCommandId.DeleteRightWord, true, false)]
    [InlineData(EditorCommandId.DeleteRightWord, false, true)]
    [InlineData(EditorCommandId.DeleteLeftCharacter, true, false)]
    [InlineData(EditorCommandId.DeleteLeftCharacter, false, true)]
    public async Task JoinedLineCarriesTheRemainingOutgoingBoundaryIntoLegacyFile(
        EditorCommandId command, bool upperWrapped, bool lowerWrapped)
    {
        var directory = CreateDirectory();
        try
        {
            var path = Path.Combine(directory, "joined.txt");
            var session = new EditorSession();
            var first = session.CreateDocument("abc   \ndef\ntail");

            // The leading line's UserColored flag describes its display and
            // must remain on the surviving descriptor. Wrapped describes
            // the separator to be *removed*: it must come from the lower line.
            first.Document.Buffer.SetFlags(
                0, EditorLineFlags.UserColored
                    | (upperWrapped ? EditorLineFlags.Wrapped : EditorLineFlags.None));
            first.Document.Buffer.SetFlags(
                1, lowerWrapped ? EditorLineFlags.Wrapped : EditorLineFlags.None);

            var sibling = session.LinkWindow(first);
            sibling.Cursor = new TextPosition(2, 9);
            sibling.TopLine = 1;

            session.SetCurrentWindow(first.WindowId);
            first.Cursor = command == EditorCommandId.DeleteLeftCharacter
                ? new TextPosition(1, 0)
                : new TextPosition(0, 3);

            Assert.True(await new EditorCommandDispatcher(session)
                .ExecuteAsync(new(command)));

            var lines = first.Document.Buffer.Snapshot();
            Assert.Equal(2, lines.Count);
            Assert.Equal("abcdef", lines[0].Text);
            Assert.Equal("tail", lines[1].Text);
            Assert.Equal(
                EditorLineFlags.UserColored |
                (lowerWrapped ? EditorLineFlags.Wrapped : EditorLineFlags.None),
                lines[0].Flags);
            Assert.Equal(EditorLineFlags.None, lines[1].Flags);
            Assert.Equal(new TextPosition(1, 9), sibling.Cursor);
            Assert.Equal(1, sibling.TopLine);
            Assert.True(first.Document.IsDirty);
            Assert.Equal(1, session.Undo.Count);

            // A hard separator is CR/LF; a soft one is the single byte 0x8D.
            // User-colored presentation metadata is NOT part of the byte
            // format, so comparing written bytes checks only meaningful text
            // and line-boundary behavior.
            await new FirstEdLegacyFileCodec().WriteAsync(path, lines);
            var bytes = await File.ReadAllBytesAsync(path);
            var expected = Encoding.Latin1.GetBytes(
                "abcdef" + (lowerWrapped ? "\u008D" : "\r\n") + "tail");
            Assert.Equal(expected, bytes);
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }

    [Theory]
    [InlineData(EditorCommandId.DeleteRightCharacter)]
    [InlineData(EditorCommandId.DeleteRightWord)]
    [InlineData(EditorCommandId.DeleteLeftCharacter)]
    public async Task UndoRestoresBothOriginalOutgoingBoundaries(
        EditorCommandId command)
    {
        var session = new EditorSession();
        var first = session.CreateDocument("abc   \ndef\ntail");
        first.Document.Buffer.SetFlags(
            0, EditorLineFlags.UserColored | EditorLineFlags.Wrapped);
        first.Document.Buffer.SetFlags(1, EditorLineFlags.None);

        var sibling = session.LinkWindow(first);
        sibling.Cursor = new TextPosition(2, 8);
        sibling.TopLine = 1;

        session.SetCurrentWindow(first.WindowId);
        first.Cursor = command == EditorCommandId.DeleteLeftCharacter
            ? new TextPosition(1, 0)
            : new TextPosition(0, 3);
        var original = first.Document.Buffer.Snapshot();

        var dispatcher = new EditorCommandDispatcher(session);
        Assert.True(await dispatcher.ExecuteAsync(new(command)));
        Assert.Equal(EditorLineFlags.UserColored,
            first.Document.Buffer.GetLine(0).Flags);

        // Undo is one logical operation: restore both descriptors, their
        // outgoing flags and the independent linked view coordinates.
        Assert.True(await dispatcher.ExecuteAsync(new(EditorCommandId.Undo)));
        Assert.Equal(original, first.Document.Buffer.Snapshot());
        Assert.Equal(new TextPosition(2, 8), sibling.Cursor);
        Assert.Equal(1, sibling.TopLine);
        Assert.False(first.Document.IsDirty);
        Assert.Equal(0, first.Document.Version);
        Assert.Equal(0, session.Undo.Count);
    }

    private static string CreateDirectory()
    {
        var directory = Path.Combine(
            Path.GetTempPath(), $"sasd-join-boundaries-{Guid.NewGuid():N}");
        Directory.CreateDirectory(directory);
        return directory;
    }
}
