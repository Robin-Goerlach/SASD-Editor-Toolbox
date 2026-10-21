using System.Text;
using Sasd.Editor.Commands;
using Sasd.Editor.Editing;
using Sasd.Editor.IO;
using Sasd.Editor.Model;
using Xunit;

namespace Sasd.Editor.Core.Tests;

/// <summary>
/// Regression evidence for EditInsertLine, EditNewLine and the final-line
/// EditDeleteLine exception from the 1985 FIRST-ED handbook.
/// </summary>
/// <remarks>
/// A soft Wrapped marker is an outgoing boundary, NOT an attribute of the
/// upper text characters. Splitting one line replaces its old boundary
/// (upper -> original successor) with two boundaries: a newly explicit hard
/// upper -> lower boundary and the *original* lower -> successor boundary.
/// Check actual legacy bytes so an apparently correct visual split cannot
/// accidentally rewrite a soft boundary (0x8D) as hard CR/LF on Save.
/// </remarks>
public sealed class LineSplitOutgoingBoundaryTests
{
    [Theory]
    [InlineData(EditorCommandId.InsertLine, 0, "", "abcdef", 0, 0)]
    [InlineData(EditorCommandId.InsertLine, 3, "abc", "def", 0, 3)]
    [InlineData(EditorCommandId.InsertLine, 6, "abcdef", "", 0, 6)]
    [InlineData(EditorCommandId.NewLine, 0, "", "abcdef", 1, 0)]
    [InlineData(EditorCommandId.NewLine, 3, "abc", "def", 1, 0)]
    [InlineData(EditorCommandId.NewLine, 6, "abcdef", "", 1, 0)]
    public async Task SplitTransfersOldWrappedBoundaryToLowerLineAndPreservesUndo(
        EditorCommandId command, int column, string upper, string lower,
        int resultingLine, int resultingColumn)
    {
        var dir = MakeDirectory();
        try
        {
            var path = Path.Combine(dir, "paragraph.txt");
            var session = new EditorSession();
            var source = session.CreateDocument("abcdef\ntail");
            var oldFlags = EditorLineFlags.Wrapped | EditorLineFlags.UserColored;
            source.Document.Buffer.SetFlags(0, oldFlags);

            // A linked window and marker on the original successor line
            // must keep identifying that line after the structural split.
            source.Cursor = new TextPosition(1, 0);
            session.SetMarker(1);
            var linked = session.LinkWindow(source);
            linked.Cursor = new TextPosition(1, 8);
            linked.TopLine = 1;

            session.SetCurrentWindow(source.WindowId);
            source.Cursor = new TextPosition(0, column);
            var preSplit = source.Document.Buffer.Snapshot();
            var dispatch = new EditorCommandDispatcher(session);
            Assert.True(await dispatch.ExecuteAsync(new(command)));

            var lines = source.Document.Buffer.Snapshot();
            Assert.Equal(3, lines.Count);
            Assert.Equal(upper, lines[0].Text);
            Assert.Equal(EditorLineFlags.UserColored, lines[0].Flags);
            Assert.Equal(lower, lines[1].Text);
            Assert.Equal(EditorLineFlags.Wrapped, lines[1].Flags);
            Assert.Equal("tail", lines[2].Text);
            Assert.Equal(new TextPosition(resultingLine, resultingColumn), source.Cursor);
            Assert.Equal(new TextPosition(2, 8), linked.Cursor);
            Assert.Equal(2, linked.TopLine);
            Assert.True(source.Document.IsDirty);
            Assert.Equal(1, source.Document.Version);
            Assert.Equal(1, session.Undo.Count);

            await new FirstEdLegacyFileCodec().WriteAsync(path, lines);
            var written = await File.ReadAllBytesAsync(path);
            var expected = Encoding.Latin1.GetBytes(
                upper + "\r\n" + lower + "\u008D" + "tail");
            Assert.Equal(expected, written);

            // One Undo step must restore both original boundaries and the
            // independent linked view. Original marker follows "tail".
            Assert.True(await dispatch.ExecuteAsync(new(EditorCommandId.Undo)));
            Assert.Equal(preSplit, source.Document.Buffer.Snapshot());
            Assert.Equal(new TextPosition(1, 8), linked.Cursor);
            Assert.Equal(1, linked.TopLine);
            Assert.True(session.JumpToMarker(1));
            Assert.Equal(1, source.Cursor.Line);
            Assert.False(source.Document.IsDirty);
            Assert.Equal(0, source.Document.Version);
            Assert.Equal(0, session.Undo.Count);
        }
        finally
        {
            Directory.Delete(dir, recursive: true);
        }
    }

    [Fact]
    public async Task InsertModeNewLineWithAutoindentKeepsOldBoundaryBelowNewSplit()
    {
        var session = new EditorSession();
        var source = session.CreateDocument("    abcdef\ntail");
        source.Document.Buffer.SetFlags(
            0, EditorLineFlags.UserColored | EditorLineFlags.Wrapped);
        source.Options.AutoIndent = true;
        source.Cursor = new TextPosition(0, 6);

        Assert.True(await new EditorCommandDispatcher(session)
            .ExecuteAsync(new(EditorCommandId.NewLine)));

        Assert.Equal(new[] { "    ab", "cdef", "tail" },
            source.Document.Buffer.Snapshot().Select(x => x.Text));
        Assert.Equal(new TextPosition(1, 4), source.Cursor);
        Assert.Equal(EditorLineFlags.UserColored,
            source.Document.Buffer.GetLine(0).Flags);
        Assert.Equal(EditorLineFlags.Wrapped,
            source.Document.Buffer.GetLine(1).Flags);
        Assert.Equal(1, session.Undo.Count);
    }

    [Fact]
    public async Task OvertypeReturnRemovesSoftBoundaryWithoutSplittingText()
    {
        var session = new EditorSession();
        var source = session.CreateDocument("abc\ntail");
        source.Document.Buffer.SetFlags(
            0, EditorLineFlags.UserColored | EditorLineFlags.Wrapped);
        source.Options.InsertMode = false;
        source.Cursor = new TextPosition(0, 2);
        var dispatch = new EditorCommandDispatcher(session);

        Assert.True(await dispatch.ExecuteAsync(new(EditorCommandId.NewLine)));
        Assert.Equal(new[] { "abc", "tail" },
            source.Document.Buffer.Snapshot().Select(x => x.Text));
        Assert.Equal(new TextPosition(1, 0), source.Cursor);
        Assert.Equal(EditorLineFlags.UserColored,
            source.Document.Buffer.GetLine(0).Flags);
        Assert.True(source.Document.IsDirty);
        Assert.Equal(1, session.Undo.Count);

        Assert.True(await dispatch.ExecuteAsync(new(EditorCommandId.Undo)));
        Assert.Equal(EditorLineFlags.UserColored | EditorLineFlags.Wrapped,
            source.Document.Buffer.GetLine(0).Flags);
        Assert.False(source.Document.IsDirty);
        Assert.Equal(0, session.Undo.Count);
    }

    [Fact]
    public async Task DeleteSoleLineBlanksCharactersAndClearsOutgoingSoftBoundary()
    {
        var session = new EditorSession();
        var source = session.CreateDocument("important");
        var originalFlags = EditorLineFlags.UserColored | EditorLineFlags.Wrapped;
        source.Document.Buffer.SetFlags(0, originalFlags);
        session.SetMarker(1);

        var linked = session.LinkWindow(source);
        linked.Cursor = new TextPosition(0, 12);
        session.SetCurrentWindow(source.WindowId);
        var dispatch = new EditorCommandDispatcher(session);

        Assert.True(await dispatch.ExecuteAsync(new(EditorCommandId.DeleteLine)));
        Assert.Equal(1, source.Document.Buffer.LineCount);
        Assert.Equal("         ", source.Document.Buffer.GetLine(0).Text);
        Assert.Equal(EditorLineFlags.UserColored,
            source.Document.Buffer.GetLine(0).Flags);
        Assert.Equal(new TextPosition(0, 12), linked.Cursor);
        Assert.False(session.JumpToMarker(1));
        Assert.Equal(1, session.Undo.Count);

        Assert.True(await dispatch.ExecuteAsync(new(EditorCommandId.Undo)));
        Assert.Equal("important", source.Document.Buffer.GetLine(0).Text);
        Assert.Equal(originalFlags, source.Document.Buffer.GetLine(0).Flags);
        Assert.False(source.Document.IsDirty);
        Assert.Equal(0, session.Undo.Count);
    }

    private static string MakeDirectory()
    {
        var dir = Path.Combine(
            Path.GetTempPath(), $"sasd-split-flags-{Guid.NewGuid():N}");
        Directory.CreateDirectory(dir);
        return dir;
    }
}
