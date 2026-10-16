using Sasd.Editor.Commands;
using Sasd.Editor.Editing;
using Sasd.Editor.Hooks;
using Sasd.Editor.Model;
using Xunit;

namespace Sasd.Editor.Core.Tests;

/// <summary>
/// Exercises editor-host interleavings at actual asynchronous search and
/// replacement awaits. TaskCompletionSource gates give tests deterministic
/// control over those awaits without timing or OS input dependencies.
/// </summary>
public sealed class AsyncSearchStateTests
{
    [Fact]
    public async Task FindNext_WindowSwitchDuringCheckpoint_UpdatesOnlyOriginalView()
    {
        var hooks = new ControlledHooks();
        var session = new EditorSession(hooks);
        var source = session.CreateDocument("start\nneedle");
        var other = session.CreateDocument("needle\nother");
        other.Cursor = new TextPosition(0, 4);
        session.SetCurrentWindow(source.WindowId);
        var gate = Gate();
        hooks.Checkpoint = _ => gate.WaitAsync();

        var pending = new EditorCommandDispatcher(session)
            .ExecuteAsync(new(EditorCommandId.FindNext, Text: "needle")).AsTask();
        await gate.Started;
        session.SetCurrentWindow(other.WindowId);
        gate.Continue();

        Assert.True(await pending);
        Assert.Same(other, session.CurrentWindow);
        Assert.Equal(new TextPosition(1, 0), source.Cursor);
        Assert.Equal(new TextPosition(0, 4), other.Cursor);
        Assert.Equal("needle", session.Search.LastPattern);
        Assert.Empty(hooks.Errors);
        Assert.Equal(0, session.Undo.Count);
    }

    [Fact]
    public async Task FindNext_ChangedCursorDuringCheckpoint_DoesNotOverwriteNavigation()
    {
        var hooks = new ControlledHooks();
        var session = new EditorSession(hooks);
        var window = session.CreateDocument("start\nneedle");
        var gate = Gate();
        hooks.Checkpoint = _ => gate.WaitAsync();

        var pending = new EditorCommandDispatcher(session)
            .ExecuteAsync(new(EditorCommandId.FindNext, Text: "needle")).AsTask();
        await gate.Started;
        window.Cursor = new TextPosition(0, 3);
        gate.Continue();

        Assert.False(await pending);
        Assert.Equal(new TextPosition(0, 3), window.Cursor);
        Assert.Null(session.Search.LastPattern);
        Assert.Equal(EditorErrorId.OperationFailed, Assert.Single(hooks.Errors).Id);
        Assert.Equal(0, session.Undo.Count);
    }

    [Fact]
    public async Task FindNext_DirectBufferChangeDuringCheckpoint_RejectsStaleResult()
    {
        var hooks = new ControlledHooks();
        var session = new EditorSession(hooks);
        var window = session.CreateDocument("start\nneedle");
        var gate = Gate();
        hooks.Checkpoint = _ => gate.WaitAsync();

        var pending = new EditorCommandDispatcher(session)
            .ExecuteAsync(new(EditorCommandId.FindNext, Text: "needle")).AsTask();
        await gate.Started;
        window.Document.Buffer.ReplaceLine(1, "changed");
        Assert.Equal(0, window.Document.Version);
        gate.Continue();

        Assert.False(await pending);
        Assert.Equal("changed", window.Document.Buffer.GetLine(1).Text);
        Assert.Equal(new TextPosition(0, 0), window.Cursor);
        Assert.Null(session.Search.LastPattern);
        Assert.Equal(EditorErrorId.OperationFailed, Assert.Single(hooks.Errors).Id);
    }

    [Fact]
    public async Task FindNext_ClosedOriginDuringCheckpoint_DoesNotSearchRemainingWindow()
    {
        var hooks = new ControlledHooks();
        var session = new EditorSession(hooks);
        var source = session.CreateDocument("start\nneedle");
        var other = session.CreateDocument("untouched");
        session.SetCurrentWindow(source.WindowId);
        var gate = Gate();
        hooks.Checkpoint = _ => gate.WaitAsync();

        var pending = new EditorCommandDispatcher(session)
            .ExecuteAsync(new(EditorCommandId.FindNext, Text: "needle")).AsTask();
        await gate.Started;
        Assert.True(session.CloseWindow(source.WindowId));
        gate.Continue();

        Assert.False(await pending);
        Assert.Same(other, session.CurrentWindow);
        Assert.Equal("untouched", other.Document.Buffer.GetLine(0).Text);
        Assert.Null(session.Search.LastPattern);
        Assert.Equal(EditorErrorId.OperationFailed, Assert.Single(hooks.Errors).Id);
    }

    [Fact]
    public async Task FindAgain_ConcurrentNewSearch_DoesNotOverwriteNewerRememberedPattern()
    {
        var hooks = new ControlledHooks();
        var session = new EditorSession(hooks);
        session.CreateDocument("alpha beta alpha");
        Assert.Equal(new TextPosition(0, 0), session.Search.FindNext("alpha"));
        var gate = Gate();
        hooks.Checkpoint = _ => gate.WaitAsync();

        var pending = new EditorCommandDispatcher(session)
            .ExecuteAsync(new(EditorCommandId.FindAgain)).AsTask();
        await gate.Started;

        // A synchronous host search may complete while the prior async
        // Find Again is suspended; the older result must not win afterward.
        Assert.Equal(new TextPosition(0, 6), session.Search.FindNext("beta"));
        gate.Continue();

        Assert.False(await pending);
        Assert.Equal("beta", session.Search.LastPattern);
        Assert.Equal(new TextPosition(0, 6), session.CurrentWindow.Cursor);
        Assert.Equal(EditorErrorId.OperationFailed, Assert.Single(hooks.Errors).Id);
    }

    [Fact]
    public async Task ReplaceNext_WindowSwitchDuringScan_DoesNotPromptOrMutateOtherView()
    {
        var hooks = new ControlledHooks();
        var session = new EditorSession(hooks);
        var source = session.CreateDocument("alpha beta");
        var other = session.CreateDocument("other");
        session.SetCurrentWindow(source.WindowId);
        var gate = Gate();
        hooks.Checkpoint = _ => gate.WaitAsync();

        var pending = new EditorCommandDispatcher(session)
            .ExecuteAsync(new(EditorCommandId.ReplaceNext, Text: "alpha", Text2: "X")).AsTask();
        await gate.Started;
        session.SetCurrentWindow(other.WindowId);
        gate.Continue();

        Assert.False(await pending);
        Assert.Equal("alpha beta", source.Document.Buffer.GetLine(0).Text);
        Assert.Equal("other", other.Document.Buffer.GetLine(0).Text);
        Assert.Same(other, session.CurrentWindow);
        Assert.Equal(0, hooks.ReplacePrompts);
        Assert.Equal(0, session.Undo.Count);
        Assert.Equal(EditorErrorId.OperationFailed, Assert.Single(hooks.Errors).Id);
    }

    [Fact]
    public async Task ReplaceNext_WindowSwitchDuringConfirmation_RejectsInsteadOfEditingNewView()
    {
        var hooks = new ControlledHooks();
        var session = new EditorSession(hooks);
        var source = session.CreateDocument("alpha beta");
        var other = session.CreateDocument("unrelated");
        session.SetCurrentWindow(source.WindowId);
        var gate = Gate();
        hooks.Confirm = _ => gate.DecideAsync(ReplaceDecision.Replace);

        var pending = new EditorCommandDispatcher(session)
            .ExecuteAsync(new(EditorCommandId.ReplaceNext, Text: "alpha", Text2: "X")).AsTask();
        await gate.Started;
        session.SetCurrentWindow(other.WindowId);
        gate.Continue();

        Assert.False(await pending);
        Assert.Equal("alpha beta", source.Document.Buffer.GetLine(0).Text);
        Assert.Equal("unrelated", other.Document.Buffer.GetLine(0).Text);
        Assert.Same(other, session.CurrentWindow);
        Assert.Equal(0, session.Undo.Count);
        Assert.Equal(EditorErrorId.OperationFailed, Assert.Single(hooks.Errors).Id);
    }

    [Fact]
    public async Task ReplaceNext_ChangedTextDuringConfirmation_DoesNotApplyStaleMatch()
    {
        var hooks = new ControlledHooks();
        var session = new EditorSession(hooks);
        var source = session.CreateDocument("alpha beta");
        var linked = session.LinkWindow(source);
        session.SetCurrentWindow(source.WindowId);
        var gate = Gate();
        hooks.Confirm = _ => gate.DecideAsync(ReplaceDecision.Replace);

        var pending = new EditorCommandDispatcher(session)
            .ExecuteAsync(new(EditorCommandId.ReplaceNext, Text: "alpha", Text2: "X")).AsTask();
        await gate.Started;
        session.SetCurrentWindow(linked.WindowId);
        session.Engine.InsertText("changed ");
        var expected = source.Document.Buffer.Snapshot();
        gate.Continue();

        Assert.False(await pending);
        Assert.Equal(expected, source.Document.Buffer.Snapshot());
        Assert.Equal(1, source.Document.Version);
        Assert.Equal(1, session.Undo.Count); // Only the explicit user edit.
        Assert.Equal(EditorErrorId.OperationFailed, Assert.Single(hooks.Errors).Id);
    }

    [Fact]
    public async Task ReplaceNext_ConfirmedUnchanged_CreatesOneUndoableMutation()
    {
        var hooks = new ControlledHooks();
        var session = new EditorSession(hooks);
        var source = session.CreateDocument("alpha beta");
        var gate = Gate();
        hooks.Confirm = _ => gate.DecideAsync(ReplaceDecision.Replace);

        var pending = new EditorCommandDispatcher(session)
            .ExecuteAsync(new(EditorCommandId.ReplaceNext, Text: "alpha", Text2: "X")).AsTask();
        await gate.Started;
        gate.Continue();

        Assert.True(await pending);
        Assert.Equal("X beta", source.Document.Buffer.GetLine(0).Text);
        Assert.Equal(new TextPosition(0, 1), source.Cursor);
        Assert.True(source.Document.IsDirty);
        Assert.Equal(1, session.Undo.Count);
        Assert.True(session.Engine.Undo());
        Assert.Equal("alpha beta", source.Document.Buffer.GetLine(0).Text);
    }

    [Fact]
    public async Task ReplaceNext_ConfirmationSkip_AdvancesWithoutChangingDocument()
    {
        var hooks = new ControlledHooks();
        var session = new EditorSession(hooks);
        var window = session.CreateDocument("alpha beta alpha");
        hooks.Confirm = _ => Task.FromResult(ReplaceDecision.Skip);

        Assert.False(await new EditorCommandDispatcher(session).ExecuteAsync(
            new(EditorCommandId.ReplaceNext, Text: "alpha", Text2: "X")));

        Assert.Equal(new TextPosition(0, 5), window.Cursor);
        Assert.Equal("alpha beta alpha", window.Document.Buffer.GetLine(0).Text);
        Assert.False(window.Document.IsDirty);
        Assert.Equal(0, session.Undo.Count);
        Assert.Empty(hooks.Errors);
    }

    [Fact]
    public async Task ReplaceNext_DirectServiceRejectsEmptyPatternBeforeEditing()
    {
        var session = new EditorSession();
        var window = session.CreateDocument("alpha");
        await Assert.ThrowsAsync<ArgumentException>(
            async () => await session.Search.ReplaceNextAsync("", "new"));
        Assert.Equal("alpha", window.Document.Buffer.GetLine(0).Text);
        Assert.Equal(0, session.Undo.Count);
    }

    private static AsyncGate Gate() => new();

    private sealed class AsyncGate
    {
        private readonly TaskCompletionSource _started =
            new(TaskCreationOptions.RunContinuationsAsynchronously);
        private readonly TaskCompletionSource _released =
            new(TaskCreationOptions.RunContinuationsAsynchronously);

        public Task Started => _started.Task;

        public async Task WaitAsync()
        {
            _started.TrySetResult();
            await _released.Task;
        }

        public async Task<ReplaceDecision> DecideAsync(ReplaceDecision decision)
        {
            await WaitAsync();
            return decision;
        }

        public void Continue() => _released.TrySetResult();
    }

    private sealed class ControlledHooks : IEditorHooks
    {
        public Func<EditorSession, Task>? Checkpoint { get; set; }
        public Func<EditorSession, Task<ReplaceDecision>>? Confirm { get; set; }
        public List<EditorError> Errors { get; } = [];
        public int ReplacePrompts { get; private set; }

        public ValueTask PollInputDuringOperationAsync(
            EditorSession session, CancellationToken cancellationToken) =>
            new(Checkpoint?.Invoke(session) ?? Task.CompletedTask);

        public ValueTask<ReplaceDecision> BeforeReplaceAsync(
            EditorSession session, ReplaceCandidate candidate, CancellationToken cancellationToken)
        {
            ReplacePrompts++;
            return new(Confirm?.Invoke(session) ?? Task.FromResult(ReplaceDecision.Replace));
        }

        public ValueTask OnErrorAsync(
            EditorSession session, EditorError error, CancellationToken cancellationToken)
        {
            Errors.Add(error);
            return ValueTask.CompletedTask;
        }
    }
}
