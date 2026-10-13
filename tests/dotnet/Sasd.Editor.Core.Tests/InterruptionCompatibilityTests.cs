using Sasd.Editor.Commands;
using Sasd.Editor.Editing;
using Sasd.Editor.Hooks;
using Sasd.Editor.Input;
using Sasd.Editor.IO;
using Sasd.Editor.Model;
using Xunit;

namespace Sasd.Editor.Core.Tests;

/// <summary>
/// Exercises the clean-room EditAbort/EditBreathe modernization boundary.
/// Tests intentionally distinguish user-originated Ctrl-U from external
/// CancellationToken cancellation.
/// </summary>
public sealed class InterruptionCompatibilityTests
{
    [Fact]
    public async Task ReadFile_CtrlUAbortsInFlightIoBeforeEditorMutation()
    {
        var codec = new BlockingReadCodec();
        var hooks = new PollingHooks(abortOnPoll: 2);
        var session = new EditorSession(hooks, codec);
        var window = session.CreateDocument("before\nafter");
        window.Cursor = new TextPosition(0, 3);
        var before = window.Document.Buffer.Snapshot();
        var dispatcher = new EditorCommandDispatcher(session);

        var executed = await dispatcher.ExecuteAsync(
            new(EditorCommandId.ReadFile, Text: "ignored-by-test-codec"));

        Assert.False(executed);
        Assert.True(codec.CancellationObserved);
        Assert.Equal(before, window.Document.Buffer.Snapshot());
        Assert.Equal(new TextPosition(0, 3), window.Cursor);
        Assert.False(window.Document.IsDirty);
        Assert.Equal(0, session.Undo.Count);
        Assert.False(session.Typeahead.AbortRequested);

        var error = Assert.Single(hooks.Errors);
        Assert.Equal(EditorErrorId.OperationAborted, error.Id);
        Assert.Equal("EDITOR_OPERATION_ABORTED", error.Code);
    }

    [Fact]
    public async Task Reformat_CtrlUAtPlanningCheckpointLeavesParagraphAtomic()
    {
        var hooks = new PollingHooks(abortOnPoll: 2);
        var session = new EditorSession(hooks);
        var window = session.CreateDocument("one two\nthree four\nfive six");
        window.Document.Buffer.SetFlags(0, EditorLineFlags.Wrapped);
        window.Document.Buffer.SetFlags(1, EditorLineFlags.Wrapped);
        window.Options.RightMargin = 8;
        window.Cursor = new TextPosition(0, 2);
        var before = window.Document.Buffer.Snapshot();
        var dispatcher = new EditorCommandDispatcher(session);

        var executed = await dispatcher.ExecuteAsync(new(EditorCommandId.ReformatParagraph));

        Assert.False(executed);
        Assert.Equal(before, window.Document.Buffer.Snapshot());
        Assert.Equal(new TextPosition(0, 2), window.Cursor);
        Assert.False(window.Document.IsDirty);
        Assert.Equal(0, session.Undo.Count);
        Assert.Equal(EditorErrorId.OperationAborted, Assert.Single(hooks.Errors).Id);
    }

    [Fact]
    public async Task Find_CtrlUDoesNotCommitCursorOrRememberedSearchState()
    {
        var hooks = new PollingHooks(abortOnPoll: 2);
        var session = new EditorSession(hooks);
        var window = session.CreateDocument("alpha\nbeta\ngamma");
        window.Cursor = new TextPosition(0, 4);
        var dispatcher = new EditorCommandDispatcher(session);

        var executed = await dispatcher.ExecuteAsync(
            new(EditorCommandId.FindNext, Text: "not-present"));

        Assert.False(executed);
        Assert.Equal(new TextPosition(0, 4), window.Cursor);
        Assert.Null(session.Search.LastPattern);
        Assert.Equal(EditorErrorId.OperationAborted, Assert.Single(hooks.Errors).Id);
    }

    [Fact]
    public async Task BreatheCheckpointQueuesOrdinaryInputForLaterCommandProcessing()
    {
        var hooks = new PollingHooks(keyOnPoll: EditorKeyStroke.ForCharacter('x'));
        var session = new EditorSession(hooks);
        session.CreateDocument("alpha");

        await session.Interruption.CheckpointAsync();

        Assert.True(session.Typeahead.TryRead(out var queued));
        Assert.Equal('x', queued.Character);
        Assert.False(session.Typeahead.AbortRequested);
        Assert.Empty(hooks.Errors);
    }

    [Fact]
    public async Task ExternalCancellationRemainsPlatformControlFlow_NotEditorAbort()
    {
        var hooks = new PollingHooks();
        var session = new EditorSession(hooks, new BlockingReadCodec());
        var window = session.CreateDocument("unchanged");
        var before = window.Document.Buffer.Snapshot();
        var dispatcher = new EditorCommandDispatcher(session);
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            async () => await dispatcher.ExecuteAsync(
                new(EditorCommandId.ReadFile, Text: "ignored-by-test-codec"),
                cancellation.Token));

        Assert.Equal(before, window.Document.Buffer.Snapshot());
        Assert.False(window.Document.IsDirty);
        Assert.Empty(hooks.Errors);
    }

    private sealed class PollingHooks(
        int? abortOnPoll = null,
        EditorKeyStroke? keyOnPoll = null) : IEditorHooks
    {
        private int _pollCount;

        public List<EditorError> Errors { get; } = [];

        public ValueTask PollInputDuringOperationAsync(
            EditorSession session,
            CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            _pollCount++;

            if (abortOnPoll == _pollCount)
            {
                Assert.Equal(
                    EditorTypeaheadWriteResult.Aborted,
                    session.Typeahead.EnqueueFromHost(EditorKeyStroke.Control('U')));
            }
            else if (keyOnPoll.HasValue && _pollCount == 1)
            {
                Assert.Equal(
                    EditorTypeaheadWriteResult.Queued,
                    session.Typeahead.EnqueueFromHost(keyOnPoll.Value));
            }

            return ValueTask.CompletedTask;
        }

        public ValueTask OnErrorAsync(
            EditorSession session,
            EditorError error,
            CancellationToken cancellationToken)
        {
            Errors.Add(error);
            return ValueTask.CompletedTask;
        }
    }

    /// <summary>
    /// Deterministic cancellable stand-in for a slow codec. It allows the test
    /// to prove that Ctrl-U cancels an operation that is already awaiting I/O,
    /// rather than only rejecting a command before it starts.
    /// </summary>
    private sealed class BlockingReadCodec : IEditorFileCodec
    {
        public bool CancellationObserved { get; private set; }

        public async ValueTask<IReadOnlyList<EditorLineSnapshot>> ReadAsync(
            string path,
            CancellationToken cancellationToken = default)
        {
            try
            {
                await Task.Delay(Timeout.InfiniteTimeSpan, cancellationToken);
                return Array.Empty<EditorLineSnapshot>();
            }
            catch (OperationCanceledException)
            {
                CancellationObserved = true;
                throw;
            }
        }

        public ValueTask WriteAsync(
            string path,
            IReadOnlyList<EditorLineSnapshot> lines,
            CancellationToken cancellationToken = default) =>
            ValueTask.CompletedTask;
    }
}
