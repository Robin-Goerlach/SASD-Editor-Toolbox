using Sasd.Editor.Commands;
using Sasd.Editor.Editing;
using Sasd.Editor.Input;
using Sasd.Editor.Model;
using Xunit;

namespace Sasd.Editor.Core.Tests;

/// <summary>
/// Host prompt completion is an explicit boundary: cancellation (null)
/// cannot be coerced to an intentional empty edit argument.
/// </summary>
public sealed class HostPromptResolutionTests
{
    private static EditorCommandBinding Binding(
        EditorCommandId command, EditorCommandArgumentKind kind) => new(command, kind);

    [Fact]
    public void CancelledSecondReplacePromptNeverDeletesTextOrCreatesUndo()
    {
        var session = new EditorSession();
        var view = session.CreateDocument("Alpha beta");
        var keyMap = new FirstEdKeyMap();
        keyMap.Translate(EditorKeyStroke.Control('Q'));
        var action = keyMap.Translate(EditorKeyStroke.ForCharacter('A'));

        // The first answer exists; only the second prompt was abandoned.
        // The host must not create/dispatch any command in this case.
        Assert.Null(EditorCommandPromptResolver.Resolve(
            action.Binding!, firstResponse: "Alpha", secondResponse: null));
        Assert.Equal("Alpha beta", view.Document.Buffer.GetLine(0).Text);
        Assert.Equal(new TextPosition(0, 0), view.Cursor);
        Assert.False(view.Document.IsDirty);
        Assert.Equal(0, session.Undo.Count);
    }

    [Fact]
    public async Task ExplicitlyEmptyReplacementRemainsAValidUndoableDeletion()
    {
        var session = new EditorSession();
        var view = session.CreateDocument("Alpha beta");
        var request = EditorCommandPromptResolver.Resolve(
            Binding(EditorCommandId.ReplaceNext, EditorCommandArgumentKind.FindReplace),
            firstResponse: "Alpha", secondResponse: string.Empty, pageSize: 15);

        Assert.NotNull(request);
        Assert.Equal("", request.Text2);
        Assert.Equal(15, request.PageSize);
        Assert.True(await new EditorCommandDispatcher(session).ExecuteAsync(request));
        Assert.Equal(" beta", view.Document.Buffer.GetLine(0).Text);
        Assert.Equal(1, session.Undo.Count);
        Assert.True(session.Engine.Undo());
        Assert.Equal("Alpha beta", view.Document.Buffer.GetLine(0).Text);
    }

    [Fact]
    public void CancelledFindDoesNotBecomeFindAgain_ButSubmittedEmptyFindDoes()
    {
        var find = Binding(EditorCommandId.FindNext, EditorCommandArgumentKind.Text);
        Assert.Null(EditorCommandPromptResolver.Resolve(find, firstResponse: null));

        var repeat = EditorCommandPromptResolver.Resolve(
            find, firstResponse: string.Empty, pageSize: 9);
        Assert.NotNull(repeat);
        Assert.Equal(EditorCommandId.FindAgain, repeat.Id);
        Assert.Equal(9, repeat.PageSize);

        var specific = EditorCommandPromptResolver.Resolve(
            find, firstResponse: "needle");
        Assert.NotNull(specific);
        Assert.Equal(EditorCommandId.FindNext, specific.Id);
        Assert.Equal("needle", specific.Text);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData("garbage")]
    [InlineData("999999999999999999999")]
    public void InvalidOrCancelledFirstNumberDoesNotCreateRequest(string? value)
    {
        Assert.Null(EditorCommandPromptResolver.Resolve(
            Binding(EditorCommandId.GoToLine, EditorCommandArgumentKind.Number),
            firstResponse: value));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("bad")]
    public void CancelledOrInvalidSecondNumberDoesNotCreateRequest(string? second)
    {
        Assert.Null(EditorCommandPromptResolver.Resolve(
            Binding(EditorCommandId.LinkWindow, EditorCommandArgumentKind.TwoNumbers),
            firstResponse: "2", secondResponse: second));
    }

    [Fact]
    public void TwoNumbersResolveWithoutConflatingTheirPositions()
    {
        var command = EditorCommandPromptResolver.Resolve(
            Binding(EditorCommandId.CreateWindow, EditorCommandArgumentKind.TwoNumbers),
            firstResponse: " 5 ", secondResponse: "2", pageSize: 24);
        Assert.NotNull(command);
        Assert.Equal(5, command.Number);
        Assert.Equal(2, command.Number2);
        Assert.Equal(24, command.PageSize);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void FilePathCancelledOrBlankIsNotDispatched(string? path)
    {
        Assert.Null(EditorCommandPromptResolver.Resolve(
            Binding(EditorCommandId.WriteFile, EditorCommandArgumentKind.FilePath),
            firstResponse: path));
    }

    [Fact]
    public void FilePathIsTrimmedButNeverOpenedByResolver()
    {
        var command = EditorCommandPromptResolver.Resolve(
            Binding(EditorCommandId.ReadFile, EditorCommandArgumentKind.FilePath),
            firstResponse: "  test.dat  ");
        Assert.NotNull(command);
        Assert.Equal("test.dat", command.Text);
    }

    [Theory]
    [InlineData(null, false)]
    [InlineData("", false)]
    [InlineData("no", false)]
    [InlineData(" YES ", true)]
    [InlineData("yes", true)]
    public void ExplicitExitConfirmationRequired(string? answer, bool expected)
    {
        var command = EditorCommandPromptResolver.Resolve(
            Binding(EditorCommandId.Exit, EditorCommandArgumentKind.Confirmation),
            firstResponse: answer);
        Assert.Equal(expected, command is not null);
    }

    [Fact]
    public void CharacterPromptCancellationCannotInsertControlCharacter()
    {
        var binding = Binding(EditorCommandId.InsertControlCharacter,
            EditorCommandArgumentKind.Character);
        Assert.Null(EditorCommandPromptResolver.Resolve(binding));
        var request = EditorCommandPromptResolver.Resolve(binding,
            characterResponse: 'A');
        Assert.NotNull(request);
        Assert.Equal("A", request.Text);
    }

    [Fact]
    public void NoPromptBindingPreservesFixedMarkerAndPageSize()
    {
        var binding = new EditorCommandBinding(
            EditorCommandId.SetMarker, EditorCommandArgumentKind.None, FixedNumber: 3);
        var result = EditorCommandPromptResolver.Resolve(binding, pageSize: 20);
        Assert.NotNull(result);
        Assert.Equal(3, result.Number);
        Assert.Equal(20, result.PageSize);
    }

    [Fact]
    public void UnsupportedPromptKindDoesNotGuessAnEdit()
    {
        Assert.Null(EditorCommandPromptResolver.Resolve(new EditorCommandBinding(
            EditorCommandId.InsertText, (EditorCommandArgumentKind)999)));
    }
}
