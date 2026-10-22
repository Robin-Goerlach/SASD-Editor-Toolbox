using Sasd.Editor.Editing;
using Sasd.Editor.FirstEd.Sample;
using Sasd.Editor.IO;
using Sasd.Editor.Model;
using Xunit;

namespace Sasd.Editor.Core.Tests;

/// <summary>Headless acceptance tests for MicroStar block-only file export.</summary>
public sealed class ConsoleMicroStarBlockFileTests
{
    [Fact]
    public async Task WritesOnlyMarkedWholeLinesAndPreservesEditorState()
    {
        var session = new EditorSession();
        var view = session.CreateDocument("before\nselected 1\nselected 2\nafter");
        view.Cursor = new TextPosition(1, 0);
        session.BeginBlock();
        view.Cursor = new TextPosition(2, 0);
        session.EndBlock();
        var before = view.Document.Buffer.Snapshot();
        var dirty = view.Document.IsDirty;
        var undo = session.Undo.Count;
        var previousCursor = view.Cursor;
        var codec = new CapturingCodec();

        Assert.True(ConsoleMicroStarBlockFileService.HasExportableBlock(session));
        Assert.True(await ConsoleMicroStarBlockFileService.ExportAsync(
            session, "virtual.block", codec: codec));

        Assert.Equal("virtual.block", codec.Path);
        Assert.Equal(new[] { "selected 1", "selected 2" },
            codec.Lines.Select(line => line.Text).ToArray());
        Assert.All(codec.Lines, line =>
            Assert.Equal(EditorLineFlags.None, line.Flags & EditorLineFlags.InBlock));
        Assert.Equal(before, view.Document.Buffer.Snapshot());
        Assert.Equal(dirty, view.Document.IsDirty);
        Assert.Equal(undo, session.Undo.Count);
        Assert.Equal(previousCursor, view.Cursor);
        Assert.Null(view.Document.FilePath);
    }

    [Fact]
    public async Task UndefinedBlockDoesNotInvokeCodec()
    {
        var session = new EditorSession();
        session.CreateDocument("one\ntwo");
        var codec = new CapturingCodec();
        Assert.False(ConsoleMicroStarBlockFileService.HasExportableBlock(session));
        Assert.False(await ConsoleMicroStarBlockFileService.ExportAsync(
            session, "virtual.block", codec: codec));
        Assert.Null(codec.Path);
    }

    [Fact]
    public async Task ExportUsesSourceBlockEvenWhenOtherWindowIsCurrent()
    {
        var session = new EditorSession();
        var source = session.CreateDocument("alpha\nbeta");
        source.Cursor = new TextPosition(0, 0);
        session.BeginBlock();
        source.Cursor = new TextPosition(1, 0);
        session.EndBlock();
        session.CreateDocument("unrelated");
        var codec = new CapturingCodec();
        Assert.True(await ConsoleMicroStarBlockFileService.ExportAsync(
            session, "another.block", codec: codec));
        Assert.Equal(new[] { "alpha", "beta" },
            codec.Lines.Select(line => line.Text).ToArray());
    }

    [Fact]
    public async Task LegacyCodecPreservesSoftLineEndOnDisk()
    {
        var session = new EditorSession();
        var view = session.CreateDocument("soft\nhard\ntrailer");
        view.Document.Buffer.SetFlags(0, EditorLineFlags.Wrapped);
        view.Cursor = new TextPosition(0, 0);
        session.BeginBlock();
        view.Cursor = new TextPosition(1, 0);
        session.EndBlock();
        var path = Path.GetTempFileName();
        try
        {
            Assert.True(await ConsoleMicroStarBlockFileService.ExportAsync(
                session, path));
            var contents = await new FirstEdLegacyFileCodec().ReadAsync(path);
            Assert.Equal(new[] { "soft", "hard" },
                contents.Select(line => line.Text).ToArray());
            Assert.True(contents[0].Flags.HasFlag(EditorLineFlags.Wrapped));
            Assert.False(contents[1].Flags.HasFlag(EditorLineFlags.Wrapped));
            Assert.False(view.Document.IsDirty);
        }
        finally
        {
            File.Delete(path);
        }
    }

    private sealed class CapturingCodec : IEditorFileCodec
    {
        public string? Path { get; private set; }
        public IReadOnlyList<EditorLineSnapshot> Lines { get; private set; } =
            Array.Empty<EditorLineSnapshot>();

        public ValueTask<IReadOnlyList<EditorLineSnapshot>> ReadAsync(
            string path, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public ValueTask WriteAsync(string path,
            IReadOnlyList<EditorLineSnapshot> lines,
            CancellationToken cancellationToken = default)
        {
            Path = path;
            Lines = lines.ToArray();
            return ValueTask.CompletedTask;
        }
    }
}
