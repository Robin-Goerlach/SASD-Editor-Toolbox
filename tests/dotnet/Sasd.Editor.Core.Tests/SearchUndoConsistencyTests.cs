using Sasd.Editor.Commands;
using Sasd.Editor.Editing;
using Sasd.Editor.Hooks;
using Sasd.Editor.Model;
using Sasd.Editor.Search;
using Xunit;

namespace Sasd.Editor.Core.Tests;

/// <summary>
/// Search's remembered pattern must survive Undo, but a remembered coordinate
/// must not skip a newly exposed match after a structural or textual edit.
/// </summary>
public sealed class SearchUndoConsistencyTests
{
    [Fact]
    public void FindAgain_AfterDeletingMatchedTextAtSameColumn_FindsNewlyExposedHit()
    {
        var session = new EditorSession();
        var window = session.CreateDocument("aa");
        Assert.Equal(new TextPosition(0, 0), session.Search.FindNext("a"));

        // Empty replacement leaves the cursor at the old match column, but
        // the first remaining 'a' is now a different occurrence.
        Assert.True(session.Search.ReplaceNextAsync("a", "").AsTask().GetAwaiter().GetResult());
        Assert.Equal("a", window.Document.Buffer.GetLine(0).Text);
        Assert.Equal(new TextPosition(0, 0), window.Cursor);

        Assert.Equal(new TextPosition(0, 0), session.Search.FindAgain());
        Assert.Equal("a", session.Search.LastPattern);
    }

    [Fact]
    public async Task FindAgain_AfterUndoStartsAtRestoredCursorRatherThanSkippingIt()
    {
        var session = new EditorSession();
        var window = session.CreateDocument("alpha alpha");
        var dispatcher = new EditorCommandDispatcher(session);
        Assert.True(await dispatcher.ExecuteAsync(
            new(EditorCommandId.ReplaceNext, Text: "alpha", Text2: "X")));
        Assert.Equal("X alpha", window.Document.Buffer.GetLine(0).Text);

        Assert.True(session.Engine.Undo());
        Assert.Equal("alpha alpha", window.Document.Buffer.GetLine(0).Text);
        Assert.Equal(new TextPosition(0, 0), window.Cursor);

        // Undo does not remove the user's saved search expression, but
        // restoring the original stream invalidates the pre-Undo hit anchor.
        Assert.Equal("alpha", session.Search.LastPattern);
        Assert.True(await dispatcher.ExecuteAsync(new(EditorCommandId.FindAgain)));
        Assert.Equal(new TextPosition(0, 0), window.Cursor);
        Assert.Equal(0, session.Undo.Count);
    }

    [Fact]
    public void FindAgain_ChangingOnlyUserColorFlagDoesNotInvalidatePreviousMatch()
    {
        var session = new EditorSession();
        var window = session.CreateDocument("a a");
        Assert.Equal(new TextPosition(0, 0), session.Search.FindNext("a"));

        // User coloring is a view annotation, not a different text match.
        session.ColorCurrentLine();
        Assert.Equal(new TextPosition(0, 2), session.Search.FindAgain());
        Assert.False(window.Document.IsDirty);
        Assert.Equal(0, session.Undo.Count);
    }

    [Fact]
    public void FindAgain_AfterLinkedWindowDeletesPrecedingLine_RechecksAnchorText()
    {
        var session = new EditorSession();
        var first = session.CreateDocument("a\na");
        first.Cursor = new TextPosition(1, 0);
        Assert.Equal(new TextPosition(1, 0), session.Search.FindNext("a"));
        var linked = session.LinkWindow(first);
        session.SetCurrentWindow(linked.WindowId);
        linked.Cursor = new TextPosition(0, 0);
        session.Engine.DeleteLine();

        session.SetCurrentWindow(first.WindowId);
        first.Cursor = new TextPosition(0, 0);
        Assert.Equal(new TextPosition(0, 0), session.Search.FindAgain());
        Assert.Equal("a", session.Search.LastPattern);
    }

    [Fact]
    public async Task FindAgain_UndoOfUnrelatedDocumentPreservesCurrentMatch()
    {
        var session = new EditorSession();
        var first = session.CreateDocument("a a");
        Assert.Equal(new TextPosition(0, 0), session.Search.FindNext("a"));
        var second = session.CreateDocument("b");
        session.Engine.InsertText("c");
        Assert.True(session.Engine.Undo());

        session.SetCurrentWindow(first.WindowId);
        Assert.Equal(new TextPosition(0, 2), session.Search.FindAgain());
        Assert.Equal("a", session.Search.LastPattern);
        Assert.Equal(0, session.Undo.Count);
        Assert.False(second.Document.IsDirty);
    }

    [Fact]
    public async Task FindAgain_RestorationPreservesRememberedCaseSensitiveOption()
    {
        var session = new EditorSession();
        var window = session.CreateDocument("Alpha alpha");
        Assert.Equal(new TextPosition(0, 6),
            session.Search.FindNext("alpha", new SearchOptions(CaseSensitive: true)));
        session.Engine.InsertText("!");
        Assert.True(session.Engine.Undo());

        window.Cursor = new TextPosition(0, 0);
        Assert.Equal(new TextPosition(0, 6), session.Search.FindAgain());
        Assert.Equal("alpha", session.Search.LastPattern);
    }

    [Fact]
    public async Task UndoDuringPendingFind_InvalidatesSnapshotEvenIfRestoredTextMatches()
    {
        var hooks = new PausingHooks();
        var session = new EditorSession(hooks);
        var window = session.CreateDocument("alpha\nbeta");
        session.Engine.InsertText("X");
        Assert.Equal("Xalpha", window.Document.Buffer.GetLine(0).Text);
        var dispatcher = new EditorCommandDispatcher(session);

        // The pending scan is suspended at its first EditBreathe checkpoint.
        var searching = dispatcher.ExecuteAsync(
            new(EditorCommandId.FindNext, Text: "beta")).AsTask();
        await hooks.Started;
        Assert.True(session.Engine.Undo());
        hooks.Release();

        Assert.False(await searching);
        Assert.Equal("alpha", window.Document.Buffer.GetLine(0).Text);
        Assert.Null(session.Search.LastPattern);
        Assert.Equal(EditorErrorId.OperationFailed, Assert.Single(hooks.Errors).Id);
        Assert.Equal(0, session.Undo.Count);
    }

    private sealed class PausingHooks : IEditorHooks
    {
        private readonly TaskCompletionSource _started =
            new(TaskCreationOptions.RunContinuationsAsynchronously);
        private readonly TaskCompletionSource _released =
            new(TaskCreationOptions.RunContinuationsAsynchronously);

        public Task Started => _started.Task;
        public List<EditorError> Errors { get; } = [];

        public void Release() => _released.TrySetResult();

        public async ValueTask PollInputDuringOperationAsync(
            EditorSession session, CancellationToken cancellationToken)
        {
            _started.TrySetResult();
            await _released.Task.WaitAsync(cancellationToken);
        }

        public ValueTask OnErrorAsync(
            EditorSession session, EditorError error, CancellationToken cancellationToken)
        {
            Errors.Add(error);
            return ValueTask.CompletedTask;
        }
    }
}
