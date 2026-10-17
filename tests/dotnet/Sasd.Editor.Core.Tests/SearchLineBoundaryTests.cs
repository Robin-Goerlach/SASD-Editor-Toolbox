using Sasd.Editor.Commands;
using Sasd.Editor.Editing;
using Sasd.Editor.Hooks;
using Sasd.Editor.Model;
using Xunit;

namespace Sasd.Editor.Core.Tests;

/// <summary>
/// A logical line must never contain newline delimiter characters merely
/// because a single-line find/replace command supplied a multiline argument.
/// Future multiline editing requires its own structural topology operation.
/// </summary>
public sealed class SearchLineBoundaryTests
{
    [Theory]
    [InlineData("a\nb")]
    [InlineData("a\rb")]
    public async Task FindNext_LineBreakPatternIsRejectedBeforeRememberedStateChanges(string pattern)
    {
        var hooks = new ErrorHooks();
        var session = new EditorSession(hooks);
        var window = session.CreateDocument("a b");
        var dispatcher = new EditorCommandDispatcher(session);

        Assert.False(await dispatcher.ExecuteAsync(
            new(EditorCommandId.FindNext, Text: pattern)));
        Assert.Null(session.Search.LastPattern);
        Assert.Equal(new TextPosition(0, 0), window.Cursor);
        Assert.Equal("a b", window.Document.Buffer.GetLine(0).Text);
        Assert.False(window.Document.IsDirty);
        Assert.Equal(0, session.Undo.Count);
        Assert.Equal(EditorErrorId.InvalidCommandArgument, Assert.Single(hooks.Errors).Id);
    }

    [Theory]
    [InlineData("a\nb")]
    [InlineData("a\rb")]
    public async Task ReplaceNext_LineBreakReplacementDoesNotMutateOrRememberFind(string replacement)
    {
        var hooks = new ErrorHooks();
        var session = new EditorSession(hooks);
        var window = session.CreateDocument("a a");
        var dispatcher = new EditorCommandDispatcher(session);

        Assert.False(await dispatcher.ExecuteAsync(
            new(EditorCommandId.ReplaceNext, Text: "a", Text2: replacement)));
        Assert.Null(session.Search.LastPattern);
        Assert.Equal("a a", window.Document.Buffer.GetLine(0).Text);
        Assert.Equal(new TextPosition(0, 0), window.Cursor);
        Assert.Equal(0, session.Undo.Count);
        Assert.Equal(EditorErrorId.InvalidCommandArgument, Assert.Single(hooks.Errors).Id);
    }

    [Theory]
    [InlineData("x\ny")]
    [InlineData("x\ry")]
    public void ReplaceAll_LineBreakReplacementIsRejectedBeforeAnyMutation(string replacement)
    {
        var session = new EditorSession();
        var window = session.CreateDocument("a\na");
        var before = window.Document.Buffer.Snapshot();

        Assert.Throws<ArgumentException>(() => session.Search.ReplaceAll("a", replacement));

        Assert.Equal(before, window.Document.Buffer.Snapshot());
        Assert.False(window.Document.IsDirty);
        Assert.Equal(0, session.Undo.Count);
    }

    [Fact]
    public void ReplaceAll_EmptyReplacementStillDeletesMatchedTextOnEachLine()
    {
        var session = new EditorSession();
        var window = session.CreateDocument("a\na");

        Assert.Equal(2, session.Search.ReplaceAll("a", string.Empty));
        Assert.Equal(2, window.Document.Buffer.LineCount);
        Assert.Equal("", window.Document.Buffer.GetLine(0).Text);
        Assert.Equal("", window.Document.Buffer.GetLine(1).Text);
        Assert.True(window.Document.IsDirty);
        Assert.Equal(1, session.Undo.Count);
        Assert.True(session.Engine.Undo());
        Assert.Equal("a", window.Document.Buffer.GetLine(0).Text);
        Assert.Equal("a", window.Document.Buffer.GetLine(1).Text);
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
}
