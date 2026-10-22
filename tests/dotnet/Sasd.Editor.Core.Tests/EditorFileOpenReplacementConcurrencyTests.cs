using Sasd.Editor.Editing;
using Sasd.Editor.IO;
using Sasd.Editor.Model;
using Xunit;

namespace Sasd.Editor.Core.Tests;

/// <summary>
/// Deterministic interleaving tests for replacing an editor document.
///
/// An asynchronous codec is held at a controlled checkpoint. This allows
/// tests to reproduce changes that real terminal input or another linked
/// window could make while disk I/O is underway. Delays and sleeps would
/// make these tests platform-dependent and much less useful.
/// </summary>
public sealed class EditorFileOpenReplacementConcurrencyTests
{
    [Fact]
    public async Task NewEditDuringDecodeRejectsReplacementWithoutLosingText()
    {
        var codec = new PausedCodec();
        var session = new EditorSession(fileCodec: codec);
        var source = session.CreateDocument("original");
        var oldDocument = source.Document;

        var opening = session.Files.OpenReplacingCurrentWindowAsync("new.txt").AsTask();
        await codec.Started.Task;

        // Simulate an edit in the shared document before disk I/O completes.
        oldDocument.Buffer.ReplaceLine(0, "newer work");
        oldDocument.MarkChanged();
        codec.Complete(new EditorLineSnapshot("incoming"));

        Assert.False(await opening);
        Assert.Same(oldDocument, source.Document);
        Assert.Equal("newer work", oldDocument.Buffer.GetLine(0).Text);
        Assert.True(oldDocument.IsDirty);
    }

    [Fact]
    public async Task DetachedInitiatingViewCannotBeResurrected()
    {
        var codec = new PausedCodec();
        var session = new EditorSession(fileCodec: codec);
        var source = session.CreateDocument("keep");
        var remaining = session.LinkWindow(source);
        session.SetCurrentWindow(source.WindowId);

        var opening = session.Files.OpenReplacingCurrentWindowAsync("replacement.txt").AsTask();
        await codec.Started.Task;
        Assert.True(session.CloseWindow(source.WindowId));

        codec.Complete(new EditorLineSnapshot("incoming"));
        Assert.False(await opening);
        Assert.Same(remaining, session.CurrentWindow);
        Assert.Equal("keep", remaining.Document.Buffer.GetLine(0).Text);
    }

    [Fact]
    public async Task SwitchingFocusDoesNotRedirectReplacementToAnotherWindow()
    {
        var codec = new PausedCodec();
        var session = new EditorSession(fileCodec: codec);
        var first = session.CreateDocument("first");
        var second = session.CreateDocument("second");
        session.SetCurrentWindow(first.WindowId);

        var opening = session.Files.OpenReplacingCurrentWindowAsync("replacement.txt").AsTask();
        await codec.Started.Task;
        session.SetCurrentWindow(second.WindowId);

        codec.Complete(new EditorLineSnapshot("opened"));
        Assert.True(await opening);
        Assert.Same(second, session.CurrentWindow);
        Assert.Equal("opened", first.Document.Buffer.GetLine(0).Text);
        Assert.Equal("second", second.Document.Buffer.GetLine(0).Text);
    }

    [Fact]
    public async Task CancellationAfterDecodingLeavesOriginalAttached()
    {
        using var cancellation = new CancellationTokenSource();
        var codec = new PausedCodec();
        var session = new EditorSession(fileCodec: codec);
        var window = session.CreateDocument("preserve");
        var original = window.Document;

        var opening = session.Files.OpenReplacingCurrentWindowAsync(
            "replacement.txt", cancellationToken: cancellation.Token).AsTask();
        await codec.Started.Task;

        // The mock decoder ignores cancellation, just as a custom provider
        // may complete normally after the user cancels. The final commit
        // gate must still reject the replacement.
        cancellation.Cancel();
        codec.Complete(new EditorLineSnapshot("incoming"));

        await Assert.ThrowsAnyAsync<OperationCanceledException>(async () => await opening);
        Assert.Same(original, window.Document);
        Assert.Equal("preserve", original.Buffer.GetLine(0).Text);
    }

    [Fact]
    public async Task DirectBufferMutationWithoutVersionChangeIsDetected()
    {
        var codec = new PausedCodec();
        var session = new EditorSession(fileCodec: codec);
        var window = session.CreateDocument("original");
        var version = window.Document.Version;

        var opening = session.Files.OpenReplacingCurrentWindowAsync("replacement.txt").AsTask();
        await codec.Started.Task;
        window.Document.Buffer.ReplaceLine(0, "quiet mutation");
        Assert.Equal(version, window.Document.Version);
        codec.Complete(new EditorLineSnapshot("incoming"));

        Assert.False(await opening);
        Assert.Equal("quiet mutation", window.Document.Buffer.GetLine(0).Text);
    }

    private sealed class PausedCodec : IEditorFileCodec
    {
        private readonly TaskCompletionSource<IReadOnlyList<EditorLineSnapshot>> _result =
            new(TaskCreationOptions.RunContinuationsAsynchronously);

        internal TaskCompletionSource<bool> Started { get; } =
            new(TaskCreationOptions.RunContinuationsAsynchronously);

        public async ValueTask<IReadOnlyList<EditorLineSnapshot>> ReadAsync(
            string path, CancellationToken cancellationToken = default)
        {
            Started.TrySetResult(true);
            return await _result.Task.ConfigureAwait(false);
        }

        internal void Complete(params EditorLineSnapshot[] lines) =>
            _result.TrySetResult(lines);

        public ValueTask WriteAsync(string path,
            IReadOnlyList<EditorLineSnapshot> lines,
            CancellationToken cancellationToken = default) => ValueTask.CompletedTask;
    }
}
