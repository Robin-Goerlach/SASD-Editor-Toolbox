using Sasd.Editor.Commands;
using Sasd.Editor.Editing;
using Sasd.Editor.Hooks;
using Sasd.Editor.IO;
using Sasd.Editor.Model;
using Xunit;

namespace Sasd.Editor.Core.Tests;

/// <summary>
/// The editor must treat decoded provider data as untrusted input: a malformed
/// later line may not insert earlier lines or change topology/Undo state.
/// </summary>
public sealed class FileReadProviderValidationTests
{
    [Theory]
    [InlineData("bad\rline")]
    [InlineData("bad\nline")]
    [InlineData("bad\u008Dline")]
    public async Task ReadFile_EmbeddedStructuralSeparatorFailsWithoutPartialImport(string invalid)
    {
        await AssertMalformedImport(
        [
            new EditorLineSnapshot("valid"),
            new EditorLineSnapshot(invalid)
        ]);
    }

    [Fact]
    public async Task ReadFile_UnknownLineFlagFailsWithoutPartialImport()
    {
        await AssertMalformedImport(
        [
            new EditorLineSnapshot("first"),
            new EditorLineSnapshot("bad flags", (EditorLineFlags)(1 << 10))
        ]);
    }

    [Fact]
    public async Task ReadFile_NullProviderEntryFailsWithoutPartialImport()
    {
        await AssertMalformedImport(
        [
            new EditorLineSnapshot("first"),
            null!
        ]);
    }

    [Fact]
    public async Task ReadFile_NullLineTextFailsWithoutPartialImport()
    {
        await AssertMalformedImport(
        [
            new EditorLineSnapshot("first"),
            new EditorLineSnapshot(null!)
        ]);
    }

    [Fact]
    public async Task ReadFile_ValidWrappedAndColorFlagsStillImportWithBlockBitStripped()
    {
        var codec = new FixedReadCodec(
        [
            new EditorLineSnapshot("soft", EditorLineFlags.Wrapped | EditorLineFlags.InBlock),
            new EditorLineSnapshot("end", EditorLineFlags.UserColored)
        ]);
        var session = new EditorSession(fileCodec: codec);
        var window = session.CreateDocument("before\nafter");
        var dispatcher = new EditorCommandDispatcher(session);

        Assert.True(await dispatcher.ExecuteAsync(new(EditorCommandId.ReadFile, Text: "fixture")));
        var lines = window.Document.Buffer.Snapshot();
        Assert.Equal(new[] { "before", "soft", "end", "after" }, lines.Select(x => x.Text));
        Assert.Equal(EditorLineFlags.Wrapped, lines[1].Flags);
        Assert.Equal(EditorLineFlags.UserColored, lines[2].Flags);
        Assert.Equal(1, session.Undo.Count);

        Assert.True(session.Engine.Undo());
        Assert.Equal(new[] { "before", "after" },
            window.Document.Buffer.Snapshot().Select(x => x.Text));
    }

    [Fact]
    public async Task ReadFile_CanCancelDuringLargeProviderValidationWithoutMutation()
    {
        // The built-in interruption bridge has already performed two
        // checkpoints before the service validates returned codec lines.
        // At the checkpoint after 128 records the host cancels the request.
        using var cancellation = new CancellationTokenSource();
        var hooks = new CancelDuringValidationHooks(cancellation);
        var session = new EditorSession(hooks, new FixedReadCodec(
            Enumerable.Range(0, 256).Select(i => new EditorLineSnapshot($"line {i}")).ToArray()));
        var window = session.CreateDocument("before\nafter");

        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            async () => await session.Files.ReadIntoCurrentWindowAsync(
                "fixture", cancellation.Token));

        Assert.Equal(new[] { "before", "after" },
            window.Document.Buffer.Snapshot().Select(x => x.Text));
        Assert.Equal(new TextPosition(0, 0), window.Cursor);
        Assert.False(window.Document.IsDirty);
        Assert.Equal(0, session.Undo.Count);
        Assert.True(hooks.Checkpoints >= 3);
    }

    private static async Task AssertMalformedImport(IReadOnlyList<EditorLineSnapshot> lines)
    {
        var hooks = new RecordingErrors();
        var session = new EditorSession(hooks, new FixedReadCodec(lines));
        var window = session.CreateDocument("before\nafter");
        var linked = session.LinkWindow(window);
        session.SetCurrentWindow(window.WindowId);
        window.Cursor = new TextPosition(0, 4);

        // Existing block, marker, and linked views must survive the rejection.
        session.BeginBlock();
        window.Cursor = new TextPosition(1, 2);
        session.EndBlock();
        session.SetMarker(1);
        window.Cursor = new TextPosition(0, 4);
        var before = window.Document.Buffer.Snapshot();
        var block = session.BlockState;

        var succeeded = await new EditorCommandDispatcher(session).ExecuteAsync(
            new(EditorCommandId.ReadFile, Text: "fixture"));

        Assert.False(succeeded);
        Assert.Equal(before, window.Document.Buffer.Snapshot());
        Assert.Equal(block, session.BlockState);
        Assert.Equal(new TextPosition(0, 4), window.Cursor);
        Assert.Equal(0, linked.Cursor.Line);
        Assert.True(session.JumpToMarker(1));
        Assert.Equal(new TextPosition(1, 2).Line, session.CurrentWindow.Cursor.Line);
        Assert.False(window.Document.IsDirty);
        Assert.Equal(0, window.Document.Version);
        Assert.Equal(0, session.Undo.Count);
        Assert.Equal(EditorErrorId.FileIoFailed, Assert.Single(hooks.Errors).Id);
    }

    private sealed class FixedReadCodec(IReadOnlyList<EditorLineSnapshot> lines) : IEditorFileCodec
    {
        public ValueTask<IReadOnlyList<EditorLineSnapshot>> ReadAsync(
            string path, CancellationToken cancellationToken = default) =>
            ValueTask.FromResult(lines);

        public ValueTask WriteAsync(
            string path, IReadOnlyList<EditorLineSnapshot> data,
            CancellationToken cancellationToken = default) => ValueTask.CompletedTask;
    }

    private sealed class RecordingErrors : IEditorHooks
    {
        public List<EditorError> Errors { get; } = [];
        public ValueTask OnErrorAsync(
            EditorSession session, EditorError error, CancellationToken cancellationToken)
        {
            Errors.Add(error);
            return ValueTask.CompletedTask;
        }
    }

    private sealed class CancelDuringValidationHooks(CancellationTokenSource cancellation) : IEditorHooks
    {
        public int Checkpoints { get; private set; }

        public ValueTask PollInputDuringOperationAsync(
            EditorSession session, CancellationToken cancellationToken)
        {
            Checkpoints++;
            if (Checkpoints == 3)
            {
                cancellation.Cancel();
            }

            return ValueTask.CompletedTask;
        }
    }
}
