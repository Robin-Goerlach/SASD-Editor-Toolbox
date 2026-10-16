using Sasd.Editor.Commands;
using Sasd.Editor.Editing;
using Sasd.Editor.Hooks;
using Sasd.Editor.IO;
using Sasd.Editor.Model;
using Xunit;

namespace Sasd.Editor.Core.Tests;

/// <summary>
/// Deterministic tests for the modern asynchronous Read File destination
/// boundary. A delayed codec stands in for slow I/O without sleep or polling.
/// </summary>
public sealed class FileReadDestinationConsistencyTests
{
    [Fact]
    public async Task ReadFile_SwitchingWindowDuringIo_InsertsIntoOriginWithoutChangingSelection()
    {
        var codec = new PausingReadCodec(new EditorLineSnapshot("import"));
        var session = new EditorSession(fileCodec: codec);
        var source = session.CreateDocument("source\ntail");
        source.Cursor = new TextPosition(0, 4);
        var other = session.CreateDocument("unrelated");
        session.SetCurrentWindow(source.WindowId);
        var dispatcher = new EditorCommandDispatcher(session);

        var pending = dispatcher.ExecuteAsync(new(EditorCommandId.ReadFile, Text: "fixture")).AsTask();
        await codec.Started;
        session.SetCurrentWindow(other.WindowId);
        codec.Complete();

        Assert.True(await pending);
        Assert.Equal(new[] { "source", "import", "tail" },
            source.Document.Buffer.Snapshot().Select(static line => line.Text));
        Assert.Equal("unrelated", other.Document.Buffer.GetLine(0).Text);
        Assert.Equal(new TextPosition(0, 4), source.Cursor);
        Assert.Same(other, session.CurrentWindow);
        Assert.True(source.Document.IsDirty);
        Assert.False(other.Document.IsDirty);
        Assert.Equal(1, session.Undo.Count);

        // The import is one structural edit even though focus changed during
        // I/O. One Undo restores the source without touching the other stream.
        Assert.True(session.Engine.Undo());
        Assert.Equal(new[] { "source", "tail" },
            source.Document.Buffer.Snapshot().Select(static line => line.Text));
        Assert.Equal("unrelated", other.Document.Buffer.GetLine(0).Text);
        Assert.Same(other, session.CurrentWindow);
    }

    [Fact]
    public async Task ReadFile_NavigatingOriginDuringIo_PreservesTheLaterCursorAnchor()
    {
        var codec = new PausingReadCodec(new EditorLineSnapshot("import"));
        var session = new EditorSession(fileCodec: codec);
        var source = session.CreateDocument("source\ntail");
        source.Cursor = new TextPosition(0, 2);

        var pending = session.Files.ReadIntoCurrentWindowAsync("fixture").AsTask();
        await codec.Started;
        source.Cursor = new TextPosition(1, 9);
        codec.Complete();

        Assert.Equal(1, await pending);
        Assert.Equal(new[] { "source", "import", "tail" },
            source.Document.Buffer.Snapshot().Select(static line => line.Text));
        Assert.Equal(new TextPosition(2, 9), source.Cursor);
        Assert.Equal(1, session.Undo.Count);
    }

    [Fact]
    public async Task ReadFile_ChangingTargetTextDuringIo_RejectsWithoutSecondMutation()
    {
        var codec = new PausingReadCodec(new EditorLineSnapshot("import"));
        var hooks = new ErrorHooks();
        var session = new EditorSession(hooks, codec);
        var source = session.CreateDocument("source\ntail");
        source.Cursor = new TextPosition(0, 2);
        var linked = session.LinkWindow(source);
        session.SetCurrentWindow(source.WindowId);
        var pending = new EditorCommandDispatcher(session)
            .ExecuteAsync(new(EditorCommandId.ReadFile, Text: "fixture")).AsTask();
        await codec.Started;

        // A linked view may still edit the same stream while the read is
        // pending. Importing at the saved numeric line index would be unsafe.
        session.SetCurrentWindow(linked.WindowId);
        session.Engine.InsertText("changed ");
        var afterEdit = source.Document.Buffer.Snapshot();
        codec.Complete();

        Assert.False(await pending);
        Assert.Equal(afterEdit, source.Document.Buffer.Snapshot());
        Assert.Equal(1, source.Document.Version);
        Assert.Equal(1, session.Undo.Count); // Only the explicit user edit.
        Assert.True(source.Document.IsDirty);
        Assert.Same(linked, session.CurrentWindow);
        Assert.Equal(EditorErrorId.OperationFailed, Assert.Single(hooks.Errors).Id);
    }

    [Fact]
    public async Task ReadFile_ClosingOriginDuringIo_DoesNotImportIntoReplacementWindow()
    {
        var codec = new PausingReadCodec(new EditorLineSnapshot("import"));
        var hooks = new ErrorHooks();
        var session = new EditorSession(hooks, codec);
        var source = session.CreateDocument("source\ntail");
        var other = session.CreateDocument("other");
        session.SetCurrentWindow(source.WindowId);

        var pending = new EditorCommandDispatcher(session)
            .ExecuteAsync(new(EditorCommandId.ReadFile, Text: "fixture")).AsTask();
        await codec.Started;
        Assert.True(session.CloseWindow(source.WindowId));
        codec.Complete();

        Assert.False(await pending);
        Assert.Same(other, session.CurrentWindow);
        Assert.Equal("other", other.Document.Buffer.GetLine(0).Text);
        Assert.Equal("source", source.Document.Buffer.GetLine(0).Text);
        Assert.Equal(0, session.Undo.Count);
        Assert.Equal(EditorErrorId.OperationFailed, Assert.Single(hooks.Errors).Id);
    }

    private sealed class ErrorHooks : IEditorHooks
    {
        public List<EditorError> Errors { get; } = [];

        public ValueTask OnErrorAsync(
            EditorSession session, EditorError error, CancellationToken cancellationToken)
        {
            Errors.Add(error);
            return ValueTask.CompletedTask;
        }
    }

    private sealed class PausingReadCodec(params EditorLineSnapshot[] lines) : IEditorFileCodec
    {
        private readonly TaskCompletionSource _started =
            new(TaskCreationOptions.RunContinuationsAsynchronously);
        private readonly TaskCompletionSource _complete =
            new(TaskCreationOptions.RunContinuationsAsynchronously);

        public Task Started => _started.Task;

        public void Complete() => _complete.TrySetResult();

        public async ValueTask<IReadOnlyList<EditorLineSnapshot>> ReadAsync(
            string path, CancellationToken cancellationToken = default)
        {
            _started.TrySetResult();
            await _complete.Task.WaitAsync(cancellationToken);
            return lines;
        }

        public ValueTask WriteAsync(
            string path,
            IReadOnlyList<EditorLineSnapshot> snapshots,
            CancellationToken cancellationToken = default) => ValueTask.CompletedTask;
    }
}
