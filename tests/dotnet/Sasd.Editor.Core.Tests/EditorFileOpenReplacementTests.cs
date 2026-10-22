using Sasd.Editor.Editing;
using Sasd.Editor.IO;
using Sasd.Editor.Model;
using Xunit;

namespace Sasd.Editor.Core.Tests;

/// <summary>
/// Replacement and insertion have different document-lifecycle contracts.
/// These tests ensure that opening never inserts into or destroys a linked
/// source document when decoding or validation fails.
/// </summary>
public sealed class EditorFileOpenReplacementTests
{
    [Fact]
    public async Task OpenCreatesIndependentDocumentAndKeepsLinkedView()
    {
        var session = new EditorSession(fileCodec: new FixtureCodec(
            new EditorLineSnapshot("new", EditorLineFlags.Wrapped),
            new EditorLineSnapshot("second")));
        var source = session.CreateDocument("original");
        var sibling = session.LinkWindow(source);
        var oldDocument = source.Document;

        Assert.True(await session.Files.OpenReplacingCurrentWindowAsync("new.txt"));
        Assert.NotSame(oldDocument, sibling.Document);
        Assert.Same(oldDocument, source.Document);
        Assert.Equal(new[] { "new", "second" },
            sibling.Document.Buffer.Snapshot().Select(line => line.Text).ToArray());
        Assert.Equal("original", source.Document.Buffer.GetLine(0).Text);
        Assert.Equal("new.txt", sibling.Document.DisplayName);
        Assert.False(sibling.Document.IsDirty);
        Assert.Equal(0, session.Undo.Count);
    }

    [Fact]
    public async Task DirtySourceCannotBeSilentlyDiscarded()
    {
        var session = new EditorSession(fileCodec: new FixtureCodec(
            new EditorLineSnapshot("replacement")));
        var window = session.CreateDocument("keep");
        window.Document.MarkChanged();
        Assert.False(await session.Files.OpenReplacingCurrentWindowAsync("next.txt"));
        Assert.Equal("keep", window.Document.Buffer.GetLine(0).Text);
        Assert.True(window.Document.IsDirty);
        Assert.True(await session.Files.OpenReplacingCurrentWindowAsync(
            "next.txt", allowDiscardDirty: true));
        Assert.Equal("replacement", window.Document.Buffer.GetLine(0).Text);
    }

    [Fact]
    public async Task MalformedCodecDoesNotDetachSource()
    {
        var session = new EditorSession(fileCodec: new FixtureCodec(
            new EditorLineSnapshot("valid"), new EditorLineSnapshot("bad\nline")));
        var window = session.CreateDocument("preserve");
        var doc = window.Document;
        await Assert.ThrowsAsync<IOException>(async () =>
            await session.Files.OpenReplacingCurrentWindowAsync("bad.txt"));
        Assert.Same(doc, window.Document);
        Assert.Equal("preserve", doc.Buffer.GetLine(0).Text);
    }

    [Fact]
    public async Task EmptyFileOpensAsOneBlankEditableLine()
    {
        var session = new EditorSession(fileCodec: new FixtureCodec());
        var window = session.CreateDocument("old");
        Assert.True(await session.Files.OpenReplacingCurrentWindowAsync("empty.txt"));
        Assert.Equal(1, window.Document.Buffer.LineCount);
        Assert.Equal("", window.Document.Buffer.GetLine(0).Text);
    }

    private sealed class FixtureCodec(params EditorLineSnapshot[] lines) : IEditorFileCodec
    {
        public ValueTask<IReadOnlyList<EditorLineSnapshot>> ReadAsync(
            string path, CancellationToken token = default) =>
            ValueTask.FromResult<IReadOnlyList<EditorLineSnapshot>>(lines);

        public ValueTask WriteAsync(
            string path, IReadOnlyList<EditorLineSnapshot> lines,
            CancellationToken token = default) => ValueTask.CompletedTask;
    }
}
