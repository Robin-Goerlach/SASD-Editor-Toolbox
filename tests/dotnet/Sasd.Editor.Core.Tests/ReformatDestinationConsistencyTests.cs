using Sasd.Editor.Commands;
using Sasd.Editor.Editing;
using Sasd.Editor.Hooks;
using Sasd.Editor.Model;
using Xunit;

namespace Sasd.Editor.Core.Tests;

/// <summary>
/// The FIRST-ED reformat plan is built across cooperative checkpoints. These
/// tests inject host edits at known checkpoints, not through wall-clock sleeps.
/// A stale plan must never overwrite text or navigation made while planning.
/// </summary>
public sealed class ReformatDestinationConsistencyTests
{
    [Fact]
    public async Task LinkedWindowEditDuringPlanning_RejectsStaleParagraph()
    {
        var hooks = new OnCheckpointHooks();
        var session = new EditorSession(hooks);
        var source = session.CreateDocument("one    two three");
        source.Options.RightMargin = 8;
        var linked = session.LinkWindow(source);
        linked.Cursor = new TextPosition(0, 0);
        session.SetCurrentWindow(source.WindowId);
        hooks.AtCheckpoint = (number, editor) =>
        {
            if (number != 2) return;
            editor.SetCurrentWindow(linked.WindowId);
            editor.Engine.InsertText("X");
        };

        var ok = await new EditorCommandDispatcher(session)
            .ExecuteAsync(new(EditorCommandId.ReformatParagraph));

        Assert.False(ok);
        Assert.Equal("Xone    two three", source.Document.Buffer.GetLine(0).Text);
        Assert.Equal(1, source.Document.Buffer.LineCount);
        Assert.Equal(1, source.Document.Version);
        Assert.Equal(1, session.Undo.Count); // Only the intervening user edit.
        Assert.Equal(EditorErrorId.OperationFailed, Assert.Single(hooks.Errors).Id);
    }

    [Fact]
    public async Task DirectBufferMutationWithoutVersionIncrementIsAlsoRejected()
    {
        var hooks = new OnCheckpointHooks();
        var session = new EditorSession(hooks);
        var view = session.CreateDocument("one    two three");
        view.Options.RightMargin = 8;
        hooks.AtCheckpoint = (number, _) =>
        {
            if (number == 2)
                view.Document.Buffer.ReplaceLine(0, "external document edit");
        };

        var ok = await new EditorCommandDispatcher(session)
            .ExecuteAsync(new(EditorCommandId.ReformatParagraph));

        Assert.False(ok);
        Assert.Equal("external document edit", view.Document.Buffer.GetLine(0).Text);
        Assert.Equal(0, view.Document.Version);
        Assert.Equal(0, session.Undo.Count);
        Assert.Equal(EditorErrorId.OperationFailed, Assert.Single(hooks.Errors).Id);
    }

    [Fact]
    public async Task HostNavigationDuringPlanningIsNotOverwritten()
    {
        var hooks = new OnCheckpointHooks();
        var session = new EditorSession(hooks);
        var view = session.CreateDocument("one    two three");
        view.Options.RightMargin = 8;
        hooks.AtCheckpoint = (number, _) =>
        {
            if (number == 2) view.Cursor = new TextPosition(0, 99);
        };

        var ok = await new EditorCommandDispatcher(session)
            .ExecuteAsync(new(EditorCommandId.ReformatParagraph));

        Assert.False(ok);
        Assert.Equal(new TextPosition(0, 99), view.Cursor);
        Assert.Equal("one    two three", view.Document.Buffer.GetLine(0).Text);
        Assert.False(view.Document.IsDirty);
        Assert.Equal(0, session.Undo.Count);
        Assert.Equal(EditorErrorId.OperationFailed, Assert.Single(hooks.Errors).Id);
    }

    [Fact]
    public async Task MarginChangeDuringPlanningDoesNotPublishOldLayout()
    {
        var hooks = new OnCheckpointHooks();
        var session = new EditorSession(hooks);
        var view = session.CreateDocument("one    two three");
        view.Options.RightMargin = 8;
        hooks.AtCheckpoint = (number, _) =>
        {
            if (number == 2) view.Options.RightMargin = 20;
        };

        Assert.False(await new EditorCommandDispatcher(session)
            .ExecuteAsync(new(EditorCommandId.ReformatParagraph)));
        Assert.Equal(20, view.Options.RightMargin);
        Assert.Equal("one    two three", view.Document.Buffer.GetLine(0).Text);
        Assert.Equal(0, session.Undo.Count);
        Assert.Equal(EditorErrorId.OperationFailed, Assert.Single(hooks.Errors).Id);
    }

    [Fact]
    public async Task SwitchingOnlyActiveWindowDoesNotCorruptOriginatingReformat()
    {
        var hooks = new OnCheckpointHooks();
        var session = new EditorSession(hooks);
        var source = session.CreateDocument("one    two three");
        source.Options.RightMargin = 8;
        var other = session.CreateDocument("do not edit");
        session.SetCurrentWindow(source.WindowId);
        hooks.AtCheckpoint = (number, editor) =>
        {
            if (number == 2) editor.SetCurrentWindow(other.WindowId);
        };

        Assert.True(await new EditorCommandDispatcher(session)
            .ExecuteAsync(new(EditorCommandId.ReformatParagraph)));
        Assert.Equal(new[] { "one two", "three" },
            source.Document.Buffer.Snapshot().Select(x => x.Text));
        Assert.Equal("do not edit", other.Document.Buffer.GetLine(0).Text);
        Assert.Equal(other.WindowId, session.CurrentWindow.WindowId);
        Assert.Equal(1, session.Undo.Count);
        Assert.Empty(hooks.Errors);
    }

    [Fact]
    public async Task ReattachingInitiatingWindowDuringPlanningRejectsOldParagraph()
    {
        var hooks = new OnCheckpointHooks();
        var session = new EditorSession(hooks);
        var source = session.CreateDocument("one    two three");
        var originalDocumentId = source.Document.DocumentId;
        source.Options.RightMargin = 8;
        var other = session.CreateDocument("other");
        session.SetCurrentWindow(source.WindowId);
        hooks.AtCheckpoint = (number, editor) =>
        {
            if (number == 2)
                editor.AttachWindowDocument(source, other.Document);
        };

        Assert.False(await new EditorCommandDispatcher(session)
            .ExecuteAsync(new(EditorCommandId.ReformatParagraph)));
        Assert.Same(other.Document, source.Document);
        Assert.Null(session.FindDocument(originalDocumentId));
        Assert.Equal("other", other.Document.Buffer.GetLine(0).Text);
        Assert.Equal(0, session.Undo.Count);
        Assert.Equal(EditorErrorId.OperationFailed, Assert.Single(hooks.Errors).Id);
    }

    private sealed class OnCheckpointHooks : IEditorHooks
    {
        private int _checkpoints;
        public Action<int, EditorSession>? AtCheckpoint { get; set; }
        public List<EditorError> Errors { get; } = [];

        public ValueTask PollInputDuringOperationAsync(
            EditorSession session, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            AtCheckpoint?.Invoke(++_checkpoints, session);
            return ValueTask.CompletedTask;
        }

        public ValueTask OnErrorAsync(
            EditorSession session, EditorError error, CancellationToken cancellationToken)
        {
            Errors.Add(error);
            return ValueTask.CompletedTask;
        }
    }
}
