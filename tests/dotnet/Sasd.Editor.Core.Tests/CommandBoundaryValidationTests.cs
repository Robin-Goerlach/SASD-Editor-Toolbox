using Sasd.Editor.Commands;
using Sasd.Editor.Editing;
using Sasd.Editor.Hooks;
using Sasd.Editor.Model;
using Xunit;

namespace Sasd.Editor.Core.Tests;

/// <summary>
/// Regression coverage for the semantic command boundary. Invalid host input
/// must be rejected before state mutation or file I/O begins.
/// </summary>
public sealed class CommandBoundaryValidationTests
{
    [Fact]
    public async Task MissingNumericArgument_IsRejectedThroughHostNeutralErrorHook()
    {
        var hooks = new RecordingHooks();
        var session = new EditorSession(hooks);
        session.CreateDocument("alpha");
        var dispatcher = new EditorCommandDispatcher(session);

        var executed = await dispatcher.ExecuteAsync(new(EditorCommandId.GoToLine));

        Assert.False(executed);
        var error = Assert.Single(hooks.Errors);
        Assert.Equal("EDITOR_COMMAND_INVALID_ARGUMENT", error.Code);
        Assert.Equal(new TextPosition(0, 0), session.CurrentWindow.Cursor);
    }

    [Theory]
    [InlineData(EditorCommandId.GoToLine, 0)]
    [InlineData(EditorCommandId.GoToColumn, 0)]
    [InlineData(EditorCommandId.GoToWindow, 0)]
    [InlineData(EditorCommandId.DeleteWindow, 0)]
    [InlineData(EditorCommandId.SetLeftMargin, 0)]
    [InlineData(EditorCommandId.SetRightMargin, 0)]
    [InlineData(EditorCommandId.SetTabWidth, 0)]
    [InlineData(EditorCommandId.SetUndoLimit, 0)]
    [InlineData(EditorCommandId.SetMarker, 0)]
    [InlineData(EditorCommandId.SetMarker, 21)]
    [InlineData(EditorCommandId.JumpMarker, 21)]
    public async Task InvalidStableNumericRange_IsRejectedBeforeDispatch(
        EditorCommandId commandId,
        int value)
    {
        var hooks = new RecordingHooks();
        var session = new EditorSession(hooks);
        session.CreateDocument("alpha");
        var dispatcher = new EditorCommandDispatcher(session);

        Assert.False(await dispatcher.ExecuteAsync(new(commandId, Number: value)));
        Assert.Single(hooks.Errors);
    }

    [Fact]
    public async Task InvalidPageSize_IsRejectedBeforeViewportMovement()
    {
        var hooks = new RecordingHooks();
        var session = new EditorSession(hooks);
        session.CreateDocument("one\ntwo");
        var dispatcher = new EditorCommandDispatcher(session);

        Assert.False(await dispatcher.ExecuteAsync(
            new(EditorCommandId.CursorDown, PageSize: 0)));

        Assert.Equal(new TextPosition(0, 0), session.CurrentWindow.Cursor);
        Assert.Single(hooks.Errors);
    }

    [Fact]
    public async Task CreateAndLinkWindow_RequireBothValidatedNumbers()
    {
        var hooks = new RecordingHooks();
        var session = new EditorSession(hooks);
        session.CreateDocument("alpha");
        var dispatcher = new EditorCommandDispatcher(session);

        Assert.False(await dispatcher.ExecuteAsync(
            new(EditorCommandId.CreateWindow, Number: 2, Number2: 1)));
        Assert.False(await dispatcher.ExecuteAsync(
            new(EditorCommandId.CreateWindow, Number: 3)));
        Assert.False(await dispatcher.ExecuteAsync(
            new(EditorCommandId.LinkWindow, Number: 1, Number2: 0)));

        Assert.Equal(3, hooks.Errors.Count);
        Assert.Single(session.Windows);
    }

    [Fact]
    public async Task ReadAndWriteRejectBlankPathsBeforeIo()
    {
        var hooks = new RecordingHooks();
        var session = new EditorSession(hooks);
        session.CreateDocument("alpha");
        var dispatcher = new EditorCommandDispatcher(session);

        Assert.False(await dispatcher.ExecuteAsync(
            new(EditorCommandId.ReadFile, Text: "   ")));
        Assert.False(await dispatcher.ExecuteAsync(
            new(EditorCommandId.WriteFile, Text: "\t")));

        Assert.Equal(2, hooks.Errors.Count);
    }

    [Fact]
    public async Task ReplaceNext_UsesSeparatePortableTextArguments_AndAllowsEmptyReplacement()
    {
        var hooks = new RecordingHooks();
        var session = new EditorSession(hooks);
        session.CreateDocument("alpha beta");
        var dispatcher = new EditorCommandDispatcher(session);

        Assert.True(await dispatcher.ExecuteAsync(
            new(EditorCommandId.ReplaceNext, Text: "alpha", Text2: string.Empty)));

        Assert.Equal(" beta", session.CurrentWindow.Document.Buffer.GetLine(0).Text);
        Assert.Empty(hooks.Errors);
    }

    [Fact]
    public async Task ReplaceNext_LegacyPackedPayload_RemainsAcceptedDuringTransition()
    {
        var session = new EditorSession();
        session.CreateDocument("alpha");
        var dispatcher = new EditorCommandDispatcher(session);

        Assert.True(await dispatcher.ExecuteAsync(
            new(EditorCommandId.ReplaceNext, Text: "alpha\0A")));

        Assert.Equal("A", session.CurrentWindow.Document.Buffer.GetLine(0).Text);
    }

    [Fact]
    public async Task ReplaceNext_MissingReplacement_IsRejectedWithoutMutation()
    {
        var hooks = new RecordingHooks();
        var session = new EditorSession(hooks);
        session.CreateDocument("alpha");
        var dispatcher = new EditorCommandDispatcher(session);

        Assert.False(await dispatcher.ExecuteAsync(
            new(EditorCommandId.ReplaceNext, Text: "alpha")));

        Assert.Equal("alpha", session.CurrentWindow.Document.Buffer.GetLine(0).Text);
        Assert.Single(hooks.Errors);
    }

    [Fact]
    public async Task UndoLimitCompatibilityCommand_RejectsZeroButAcceptsOne()
    {
        var hooks = new RecordingHooks();
        var session = new EditorSession(hooks);
        session.CreateDocument("alpha");
        var dispatcher = new EditorCommandDispatcher(session);

        Assert.Equal(100, session.Undo.Limit);
        Assert.False(await dispatcher.ExecuteAsync(
            new(EditorCommandId.SetUndoLimit, Number: 0)));
        Assert.Equal(100, session.Undo.Limit);

        Assert.True(await dispatcher.ExecuteAsync(
            new(EditorCommandId.SetUndoLimit, Number: 1)));
        Assert.Equal(1, session.Undo.Limit);
    }

    [Fact]
    public async Task InsertControlCharacter_RequiresExactlyOneCharacter()
    {
        var hooks = new RecordingHooks();
        var session = new EditorSession(hooks);
        session.CreateDocument("alpha");
        var dispatcher = new EditorCommandDispatcher(session);

        Assert.False(await dispatcher.ExecuteAsync(
            new(EditorCommandId.InsertControlCharacter, Text: string.Empty)));
        Assert.False(await dispatcher.ExecuteAsync(
            new(EditorCommandId.InsertControlCharacter, Text: "ab")));

        Assert.Equal("alpha", session.CurrentWindow.Document.Buffer.GetLine(0).Text);
        Assert.Equal(2, hooks.Errors.Count);
    }

    private sealed class RecordingHooks : IEditorHooks
    {
        public List<EditorError> Errors { get; } = [];

        public ValueTask OnErrorAsync(
            EditorSession session,
            EditorError error,
            CancellationToken cancellationToken)
        {
            Errors.Add(error);
            return ValueTask.CompletedTask;
        }
    }
}
