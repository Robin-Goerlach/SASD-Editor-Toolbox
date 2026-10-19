using Sasd.Editor.Commands;
using Sasd.Editor.Input;
using Xunit;

namespace Sasd.Editor.Core.Tests;

/// <summary>
/// Key translation is intentionally observational: the command dispatcher
/// owns edits, and the host owns missing prompt arguments.
/// </summary>
public sealed class FirstEdPrefixBoundaryTests
{
    [Theory]
    [InlineData(EditorKey.Escape)]
    [InlineData(EditorKey.Enter)]
    [InlineData(EditorKey.Backspace)]
    [InlineData(EditorKey.Delete)]
    [InlineData(EditorKey.ArrowLeft)]
    [InlineData(EditorKey.ArrowRight)]
    [InlineData(EditorKey.Home)]
    [InlineData(EditorKey.End)]
    public void AltModifiedSpecialKey_CannotExecuteEditorCommand(EditorKey key)
    {
        var map = new FirstEdKeyMap();
        var action = map.Translate(new EditorKeyStroke(key,
            modifiers: EditorKeyModifiers.Alt));

        Assert.Equal(EditorInputActionKind.Ignored, action.Kind);
        Assert.Equal(FirstEdCommandPrefix.None, map.PendingPrefix);
    }

    [Theory]
    [InlineData('K')]
    [InlineData('O')]
    [InlineData('Q')]
    public void PrefixFollowedByUnsupportedSpecialKey_ClearsStateWithoutUndo(char prefix)
    {
        var map = new FirstEdKeyMap();
        Assert.Equal(EditorInputActionKind.PrefixPending,
            map.Translate(EditorKeyStroke.Control(prefix)).Kind);

        var action = map.Translate(new EditorKeyStroke(EditorKey.Escape,
            modifiers: EditorKeyModifiers.Alt));

        Assert.Equal(EditorInputActionKind.Ignored, action.Kind);
        Assert.Equal(FirstEdCommandPrefix.None, map.PendingPrefix);
        Assert.Equal(EditorInputActionKind.InsertText,
            map.Translate(EditorKeyStroke.ForCharacter('x')).Kind);
    }

    [Theory]
    [InlineData('K', 'M', EditorCommandId.SetMarker, EditorCommandArgumentKind.Number)]
    [InlineData('K', 'R', EditorCommandId.ReadFile, EditorCommandArgumentKind.FilePath)]
    [InlineData('K', 'X', EditorCommandId.Exit, EditorCommandArgumentKind.Confirmation)]
    [InlineData('O', 'J', EditorCommandId.LinkWindow, EditorCommandArgumentKind.TwoNumbers)]
    [InlineData('O', 'N', EditorCommandId.GoToLine, EditorCommandArgumentKind.Number)]
    [InlineData('Q', 'F', EditorCommandId.FindNext, EditorCommandArgumentKind.Text)]
    [InlineData('Q', 'A', EditorCommandId.ReplaceNext, EditorCommandArgumentKind.FindReplace)]
    public void PrefixedCommandsExposePromptTypeWithoutTouchingHost(
        char prefix, char second, EditorCommandId command, EditorCommandArgumentKind argument)
    {
        var map = new FirstEdKeyMap();
        map.Translate(EditorKeyStroke.Control(prefix));

        var action = map.Translate(EditorKeyStroke.ForCharacter(second));

        Assert.Equal(EditorInputActionKind.Command, action.Kind);
        Assert.NotNull(action.Binding);
        Assert.Equal(command, action.Binding.CommandId);
        Assert.Equal(argument, action.Binding.ArgumentKind);
        Assert.Equal(FirstEdCommandPrefix.None, map.PendingPrefix);
    }

    [Theory]
    [InlineData('K', EditorCommandId.SetMarker)]
    [InlineData('O', EditorCommandId.GoToWindow)]
    [InlineData('Q', EditorCommandId.JumpMarker)]
    public void NumberedPrefixCarriesFixedNumberIntoHostRequest(
        char prefix, EditorCommandId expected)
    {
        var map = new FirstEdKeyMap();
        map.Translate(EditorKeyStroke.Control(prefix));
        var action = map.Translate(EditorKeyStroke.ForCharacter('7'));

        Assert.NotNull(action.Binding);
        Assert.Equal(expected, action.Binding.CommandId);
        Assert.Equal(EditorCommandArgumentKind.None, action.Binding.ArgumentKind);
        Assert.Equal(7, action.Binding.FixedNumber);
        var request = action.Binding.CreateRequest(number: 9, pageSize: 12);
        Assert.Equal(expected, request.Id);
        Assert.Equal(7, request.Number);
        Assert.Equal(12, request.PageSize);
    }

    [Fact]
    public void EscapeCancelsPendingPrefixButWithoutPrefixMeansUndo()
    {
        var map = new FirstEdKeyMap();
        map.Translate(EditorKeyStroke.Control('Q'));
        Assert.Equal(EditorInputActionKind.Ignored,
            map.Translate(new EditorKeyStroke(EditorKey.Escape)).Kind);
        Assert.Equal(FirstEdCommandPrefix.None, map.PendingPrefix);

        var unprefixed = map.Translate(new EditorKeyStroke(EditorKey.Escape));
        Assert.Equal(EditorCommandId.Undo, unprefixed.Binding?.CommandId);
    }

    [Fact]
    public void InvalidSecondKeyIsConsumedButDoesNotSwallowNextText()
    {
        var map = new FirstEdKeyMap();
        map.Translate(EditorKeyStroke.Control('O'));
        Assert.Equal(EditorInputActionKind.Ignored,
            map.Translate(EditorKeyStroke.ForCharacter('?')).Kind);
        var next = map.Translate(EditorKeyStroke.ForCharacter('z'));

        Assert.Equal(EditorInputActionKind.InsertText, next.Kind);
        Assert.Equal("z", next.Text);
        Assert.Equal(FirstEdCommandPrefix.None, map.PendingPrefix);
    }
}
