using Sasd.Editor.Editing;
using Sasd.Editor.IO;
using Sasd.Editor.Model;
using Xunit;

namespace Sasd.Editor.Core.Tests;

/// <summary>
/// Read Block acceptance tests: structural insertion, block metadata and Undo
/// must agree even with linked views, malformed input, and an empty file.
/// </summary>
public sealed class MicroStarBlockReadTests
{
    [Fact]
    public async Task ImportedLinesBecomeSingleUndoableMarkedBlock()
    {
        var session = new EditorSession(fileCodec: new FixtureCodec(
            new EditorLineSnapshot("soft", EditorLineFlags.Wrapped),
            new EditorLineSnapshot("hard")));
        var originalView = session.CreateDocument("before\nafter");
        var linkedView = session.LinkWindow(originalView);
        linkedView.Cursor = new TextPosition(0, 2);
        var original = originalView.Document.Buffer.Snapshot();

        Assert.Equal(2, await session.Files.ReadBlockIntoCurrentWindowAsync("block.txt"));
        Assert.Equal(new[] { "before", "soft", "hard", "after" },
            originalView.Document.Buffer.Snapshot().Select(x => x.Text).ToArray());
        Assert.Equal(1, session.Block?.FirstLine);
        Assert.Equal(2, session.Block?.LastLine);
        Assert.Equal(originalView.Document.DocumentId, session.Block?.DocumentId);
        Assert.True(originalView.Document.Buffer.GetLine(1).Flags.HasFlag(EditorLineFlags.Wrapped));
        Assert.True(originalView.Document.Buffer.GetLine(1).Flags.HasFlag(EditorLineFlags.InBlock));
        Assert.Equal(new TextPosition(0, 2), linkedView.Cursor);
        Assert.Equal(1, session.Undo.Count);

        Assert.True(session.Engine.Undo());
        Assert.Equal(original, originalView.Document.Buffer.Snapshot());
        Assert.Null(session.Block);
        Assert.Equal(0, session.Undo.Count);
    }

    [Fact]
    public async Task EmptyInputRetainsExistingBlockAndDoesNotPushUndo()
    {
        var session = new EditorSession(fileCodec: new FixtureCodec());
        var view = session.CreateDocument("one\ntwo");
        view.Cursor = new TextPosition(0, 0);
        session.BeginBlock();
        view.Cursor = new TextPosition(1, 0);
        session.EndBlock();
        var original = session.BlockState;

        Assert.Equal(0, await session.Files.ReadBlockIntoCurrentWindowAsync("empty.txt"));
        Assert.Equal(original, session.BlockState);
        Assert.Equal(0, session.Undo.Count);
        Assert.False(view.Document.IsDirty);
    }

    [Fact]
    public async Task MalformedDecodedLineRejectsWholeOperation()
    {
        var session = new EditorSession(fileCodec: new FixtureCodec(
            new EditorLineSnapshot("valid"),
            new EditorLineSnapshot("invalid\nline")));
        var view = session.CreateDocument("before\nafter");
        var original = view.Document.Buffer.Snapshot();

        await Assert.ThrowsAsync<IOException>(async () =>
            await session.Files.ReadBlockIntoCurrentWindowAsync("bad.txt"));
        Assert.Equal(original, view.Document.Buffer.Snapshot());
        Assert.Null(session.Block);
        Assert.Equal(0, session.Undo.Count);
        Assert.False(view.Document.IsDirty);
    }

    [Fact]
    public async Task OrdinaryReadStillDoesNotCreateBlock()
    {
        var session = new EditorSession(fileCodec: new FixtureCodec(
            new EditorLineSnapshot("ordinary")));
        session.CreateDocument("original");
        Assert.Equal(1, await session.Files.ReadIntoCurrentWindowAsync("file.txt"));
        Assert.Null(session.Block);
    }

    private sealed class FixtureCodec(params EditorLineSnapshot[] lines) : IEditorFileCodec
    {
        public ValueTask<IReadOnlyList<EditorLineSnapshot>> ReadAsync(
            string path, CancellationToken cancellationToken = default) =>
            ValueTask.FromResult<IReadOnlyList<EditorLineSnapshot>>(lines);
        public ValueTask WriteAsync(string path,
            IReadOnlyList<EditorLineSnapshot> lines,
            CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();
    }
}
