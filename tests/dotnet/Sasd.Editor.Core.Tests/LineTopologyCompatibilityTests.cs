using Sasd.Editor.Commands;
using Sasd.Editor.Editing;
using Sasd.Editor.IO;
using Sasd.Editor.Model;
using Xunit;

namespace Sasd.Editor.Core.Tests;

/// <summary>
/// Regression coverage for the modern EditDelline/EditRealign equivalent. The
/// tests focus on observable reference behavior rather than Pascal pointer layout.
/// </summary>
public sealed class LineTopologyCompatibilityTests
{
    [Fact]
    public async Task DeleteLine_InvalidatesMarkerOnDeletedLine_AndShiftsLaterMarker()
    {
        var session = new EditorSession();
        var window = session.CreateDocument("zero\none\ntwo");
        var dispatcher = new EditorCommandDispatcher(session);

        window.Cursor = new TextPosition(1, 0);
        session.SetMarker(1);
        window.Cursor = new TextPosition(2, 0);
        session.SetMarker(2);

        window.Cursor = new TextPosition(1, 5);
        Assert.True(await dispatcher.ExecuteAsync(new(EditorCommandId.DeleteLine)));

        Assert.False(session.JumpToMarker(1));
        Assert.True(session.JumpToMarker(2));
        Assert.Equal(1, window.Cursor.Line);
        Assert.Equal(5, window.Cursor.Column);
        Assert.Equal(new[] { "zero", "two" }, window.Document.Buffer.Snapshot().Select(line => line.Text));
    }

    [Fact]
    public async Task DeleteLine_RealignsEveryLinkedWindowAndTopLine()
    {
        var session = new EditorSession();
        var source = session.CreateDocument("zero\none\ntwo\nthree");
        var linked = session.LinkWindow(source);
        linked.Cursor = new TextPosition(3, 7);
        linked.TopLine = 2;

        session.SetCurrentWindow(source.WindowId);
        source.Cursor = new TextPosition(1, 3);
        source.TopLine = 1;

        var dispatcher = new EditorCommandDispatcher(session);
        Assert.True(await dispatcher.ExecuteAsync(new(EditorCommandId.DeleteLine)));

        Assert.Equal(new TextPosition(1, 3), source.Cursor);
        Assert.Equal(1, source.TopLine);
        Assert.Equal(new TextPosition(2, 7), linked.Cursor);
        Assert.Equal(1, linked.TopLine);
    }

    [Fact]
    public async Task DeleteLine_AtBlockBoundary_ClearsBlockAndHighlighting()
    {
        var session = new EditorSession();
        var window = session.CreateDocument("zero\none\ntwo\nthree");
        window.Cursor = new TextPosition(1, 0);
        session.BeginBlock();
        window.Cursor = new TextPosition(2, 0);
        session.EndBlock();

        window.Cursor = new TextPosition(1, 0);
        var dispatcher = new EditorCommandDispatcher(session);
        Assert.True(await dispatcher.ExecuteAsync(new(EditorCommandId.DeleteLine)));

        Assert.Null(session.Block);
        Assert.DoesNotContain(
            window.Document.Buffer.Snapshot(),
            line => line.Flags.HasFlag(EditorLineFlags.InBlock));
    }

    [Fact]
    public async Task DeleteLine_InsideBlock_ShiftsLaterBoundaryAndKeepsBlock()
    {
        var session = new EditorSession();
        var window = session.CreateDocument("zero\none\ntwo\nthree\nfour");
        window.Cursor = new TextPosition(0, 0);
        session.BeginBlock();
        window.Cursor = new TextPosition(3, 0);
        session.EndBlock();

        window.Cursor = new TextPosition(1, 0);
        var dispatcher = new EditorCommandDispatcher(session);
        Assert.True(await dispatcher.ExecuteAsync(new(EditorCommandId.DeleteLine)));

        Assert.NotNull(session.Block);
        Assert.Equal(0, session.Block!.FirstLine);
        Assert.Equal(2, session.Block.LastLine);
        Assert.All(
            window.Document.Buffer.Snapshot().Take(3),
            line => Assert.True(line.Flags.HasFlag(EditorLineFlags.InBlock)));
        Assert.False(window.Document.Buffer.GetLine(3).Flags.HasFlag(EditorLineFlags.InBlock));
    }

    [Fact]
    public async Task DeleteOnlyLine_KeepsDescriptorButInvalidatesPointerLikeMetadata()
    {
        var session = new EditorSession();
        var window = session.CreateDocument("abc");
        session.SetMarker(1);
        session.BeginBlock();
        session.EndBlock();

        var dispatcher = new EditorCommandDispatcher(session);
        Assert.True(await dispatcher.ExecuteAsync(new(EditorCommandId.DeleteLine)));

        Assert.Equal(1, window.Document.Buffer.LineCount);
        Assert.True(string.IsNullOrWhiteSpace(window.Document.Buffer.GetLine(0).Text));
        Assert.False(session.JumpToMarker(1));
        Assert.Null(session.Block);
        Assert.True(window.Document.IsDirty);
        Assert.Equal(1, session.Undo.Count);
    }

    [Fact]
    public async Task FileReadInsertion_RealignsWindowsMarkersAndBlockLimits()
    {
        var codec = new StubCodec(
            new EditorLineSnapshot("insert-a"),
            new EditorLineSnapshot("insert-b"));
        var session = new EditorSession(fileCodec: codec);
        var source = session.CreateDocument("zero\none\ntwo");

        source.Cursor = new TextPosition(2, 0);
        session.SetMarker(1);
        source.Cursor = new TextPosition(1, 0);
        session.BeginBlock();
        source.Cursor = new TextPosition(2, 0);
        session.EndBlock();

        var linked = session.LinkWindow(source);
        linked.Cursor = new TextPosition(2, 6);
        linked.TopLine = 1;

        session.SetCurrentWindow(source.WindowId);
        source.Cursor = new TextPosition(0, 4);

        Assert.Equal(2, await session.Files.ReadIntoCurrentWindowAsync("ignored"));

        Assert.Equal(new TextPosition(0, 4), source.Cursor);
        Assert.Equal(new TextPosition(4, 6), linked.Cursor);
        Assert.Equal(3, linked.TopLine);
        Assert.NotNull(session.Block);
        Assert.Equal(3, session.Block!.FirstLine);
        Assert.Equal(4, session.Block.LastLine);
        Assert.True(session.JumpToMarker(1));
        Assert.Equal(4, source.Cursor.Line);
        Assert.Equal(
            new[] { "zero", "insert-a", "insert-b", "one", "two" },
            source.Document.Buffer.Snapshot().Select(line => line.Text));
    }

    private sealed class StubCodec(params EditorLineSnapshot[] lines) : IEditorFileCodec
    {
        public ValueTask<IReadOnlyList<EditorLineSnapshot>> ReadAsync(
            string path,
            CancellationToken cancellationToken = default) =>
            ValueTask.FromResult<IReadOnlyList<EditorLineSnapshot>>(lines);

        public ValueTask WriteAsync(
            string path,
            IReadOnlyList<EditorLineSnapshot> linesToWrite,
            CancellationToken cancellationToken = default) =>
            ValueTask.CompletedTask;
    }
}
