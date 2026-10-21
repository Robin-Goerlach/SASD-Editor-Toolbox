using Sasd.Editor.Commands;
using Sasd.Editor.Editing;
using Sasd.Editor.FirstEd.Sample;
using Sasd.Editor.Input;
using Sasd.Editor.Model;
using Xunit;

namespace Sasd.Editor.Core.Tests;

/// <summary>
/// Headless regression coverage for the historic EditCptabdef wrapper's
/// deliberate empty-string special case. This does not simulate DOS dialogs:
/// only the portable numeric command and sample's host-input policy are used.
/// </summary>
public sealed class ConsoleFirstEdTabWidthPromptTests
{
    [Fact]
    public async Task ExplicitEmptyEnter_UsesZeroBasedCursorColumnForTabInterval()
    {
        var session = new EditorSession();
        var window = session.CreateDocument("unchanged");
        window.Cursor = new TextPosition(0, 5);
        var originalTabSize = window.Options.TabSize;
        var request = ConsoleFirstEdTabWidthPromptPolicy.Resolve(
            Binding(), visibleRows: 11, cursorColumn: window.Cursor.Column,
            answer: string.Empty);

        Assert.NotNull(request);
        Assert.Equal(EditorCommandId.SetTabWidth, request.Id);
        Assert.Equal(5, request.Number);
        Assert.Equal(11, request.PageSize);
        Assert.True(await new EditorCommandDispatcher(session).ExecuteAsync(request));

        // Tab-stop configuration is not a text edit. No Undo record, Dirty
        // state change or cursor displacement is allowed.
        Assert.Equal(5, window.Options.TabSize);
        Assert.Equal(new TextPosition(0, 5), window.Cursor);
        Assert.Equal("unchanged", window.Document.Buffer.GetLine(0).Text);
        Assert.False(window.Document.IsDirty);
        Assert.Equal(0, session.Undo.Count);
        Assert.True(originalTabSize > 0);
    }

    [Fact]
    public void ExplicitEmptyEnterAtFirstColumn_DoesNotInstallInvalidZeroInterval()
    {
        var session = new EditorSession();
        var view = session.CreateDocument("text");
        var previous = view.Options.TabSize;

        // The handbook's one-based column-minus-one formula yields zero.
        // The modern editor requires width >= 1; suppress an invalid request
        // rather than accidentally changing window configuration.
        Assert.Null(ConsoleFirstEdTabWidthPromptPolicy.Resolve(
            Binding(), 8, cursorColumn: 0, answer: ""));
        Assert.Equal(previous, view.Options.TabSize);
    }

    [Theory]
    [InlineData(null)]
    [InlineData(" ")]
    [InlineData("invalid")]
    [InlineData("2147483648")]
    public void CancelledOrInvalidResponse_DoesNotTurnIntoColumnDefault(string? answer)
    {
        // Only a deliberately submitted *empty* string uses Cursor.Column.
        // Escape/null, spaces, non-numbers and overflow cancel normally.
        Assert.Null(ConsoleFirstEdTabWidthPromptPolicy.Resolve(
            Binding(), visibleRows: 9, cursorColumn: 7, answer: answer));
    }

    [Fact]
    public async Task ExplicitNumericWidth_OverridesCurrentCursorBasedDefault()
    {
        var session = new EditorSession();
        var window = session.CreateDocument("text");
        window.Cursor = new TextPosition(0, 19);
        var request = ConsoleFirstEdTabWidthPromptPolicy.Resolve(
            Binding(), visibleRows: 12, cursorColumn: window.Cursor.Column,
            answer: "  4  ");

        Assert.NotNull(request);
        Assert.Equal(4, request.Number);
        Assert.True(await new EditorCommandDispatcher(session).ExecuteAsync(request));
        Assert.Equal(4, window.Options.TabSize);
        Assert.Equal(new TextPosition(0, 19), window.Cursor);
        Assert.Equal(0, session.Undo.Count);
    }

    [Fact]
    public async Task EmptyTabPrompt_ChangesOnlySelectedWindowOption()
    {
        var session = new EditorSession();
        var first = session.CreateDocument("first");
        var second = session.CreateDocument("second");
        var oldFirstWidth = first.Options.TabSize;
        session.SetCurrentWindow(second.WindowId);
        second.Cursor = new TextPosition(0, 6);

        var request = ConsoleFirstEdTabWidthPromptPolicy.Resolve(
            Binding(), 9, second.Cursor.Column, "");
        Assert.NotNull(request);
        Assert.True(await new EditorCommandDispatcher(session).ExecuteAsync(request));

        // The original 1985 Tabsize was global. SASD's per-window TabSize
        // is an intentional, already documented modern divergence, retained
        // by this compatibility wrapper.
        Assert.Equal(oldFirstWidth, first.Options.TabSize);
        Assert.Equal(6, second.Options.TabSize);
        Assert.Equal(0, session.Undo.Count);
    }

    [Fact]
    public void WrongBindingIsRejectedBeforeConstructingACommand()
    {
        Assert.Throws<ArgumentException>(() =>
            ConsoleFirstEdTabWidthPromptPolicy.Resolve(
                new EditorCommandBinding(
                    EditorCommandId.SetUndoLimit, EditorCommandArgumentKind.Number),
                9, 4, ""));
    }

    private static EditorCommandBinding Binding()
    {
        var map = new FirstEdKeyMap();
        Assert.Equal(EditorInputActionKind.PrefixPending,
            map.Translate(EditorKeyStroke.Control('K')).Kind);
        var action = map.Translate(EditorKeyStroke.ForCharacter('T'));
        Assert.NotNull(action.Binding);
        Assert.Equal(EditorInputActionKind.Command, action.Kind);
        Assert.Equal(EditorCommandId.SetTabWidth, action.Binding.CommandId);
        Assert.Equal(EditorCommandArgumentKind.Number, action.Binding.ArgumentKind);
        return action.Binding;
    }
}
