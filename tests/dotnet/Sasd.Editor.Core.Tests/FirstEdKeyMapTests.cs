using Sasd.Editor.Commands;
using Sasd.Editor.Input;
using Xunit;

namespace Sasd.Editor.Core.Tests;

public sealed class FirstEdKeyMapTests
{
    [Fact]
    public void PrintableCharacter_IsTextInput()
    {
        var keyMap = new FirstEdKeyMap();

        var action = keyMap.Translate(EditorKeyStroke.ForCharacter('x'));

        Assert.Equal(EditorInputActionKind.InsertText, action.Kind);
        Assert.Equal("x", action.Text);
    }

    [Fact]
    public void Escape_MapsToUndo()
    {
        var keyMap = new FirstEdKeyMap();

        var action = keyMap.Translate(new EditorKeyStroke(EditorKey.Escape));

        AssertCommand(action, EditorCommandId.Undo);
    }

    [Fact]
    public void CtrlKThenB_MapsToBeginBlockAndClearsPrefix()
    {
        var keyMap = new FirstEdKeyMap();

        var prefix = keyMap.Translate(EditorKeyStroke.Control('K'));
        Assert.Equal(EditorInputActionKind.PrefixPending, prefix.Kind);
        Assert.Equal(FirstEdCommandPrefix.CtrlK, keyMap.PendingPrefix);

        var action = keyMap.Translate(EditorKeyStroke.ForCharacter('b'));

        AssertCommand(action, EditorCommandId.BeginBlock);
        Assert.Equal(FirstEdCommandPrefix.None, keyMap.PendingPrefix);
    }

    [Fact]
    public void CtrlQThenDigit_MapsDirectlyToNumberedMarker()
    {
        var keyMap = new FirstEdKeyMap();
        keyMap.Translate(EditorKeyStroke.Control('Q'));

        var action = keyMap.Translate(EditorKeyStroke.ForCharacter('3'));

        AssertCommand(action, EditorCommandId.JumpMarker);
        Assert.Equal(3, action.Binding!.FixedNumber);
        Assert.Equal(EditorCommandArgumentKind.None, action.Binding.ArgumentKind);
    }

    [Fact]
    public void CtrlOThenN_DeclaresNumericPromptForGoToLine()
    {
        var keyMap = new FirstEdKeyMap();
        keyMap.Translate(EditorKeyStroke.Control('O'));

        var action = keyMap.Translate(EditorKeyStroke.ForCharacter('N'));

        AssertCommand(action, EditorCommandId.GoToLine);
        Assert.Equal(EditorCommandArgumentKind.Number, action.Binding!.ArgumentKind);
    }

    [Fact]
    public void CtrlKThenR_DeclaresFilePathPrompt()
    {
        var keyMap = new FirstEdKeyMap();
        keyMap.Translate(EditorKeyStroke.Control('K'));

        var action = keyMap.Translate(EditorKeyStroke.ForCharacter('R'));

        AssertCommand(action, EditorCommandId.ReadFile);
        Assert.Equal(EditorCommandArgumentKind.FilePath, action.Binding!.ArgumentKind);
    }

    [Fact]
    public void EscapeWhilePrefixPending_CancelsPrefixWithoutUndo()
    {
        var keyMap = new FirstEdKeyMap();
        keyMap.Translate(EditorKeyStroke.Control('K'));

        var cancel = keyMap.Translate(new EditorKeyStroke(EditorKey.Escape));
        var text = keyMap.Translate(EditorKeyStroke.ForCharacter('a'));

        Assert.Equal(EditorInputActionKind.Ignored, cancel.Kind);
        Assert.Equal(FirstEdCommandPrefix.None, keyMap.PendingPrefix);
        Assert.Equal(EditorInputActionKind.InsertText, text.Kind);
        Assert.Equal("a", text.Text);
    }

    [Fact]
    public void PrefixSecondKey_AcceptsControlLetterForm()
    {
        var keyMap = new FirstEdKeyMap();
        keyMap.Translate(EditorKeyStroke.Control('K'));

        var action = keyMap.Translate(EditorKeyStroke.Control('C'));

        AssertCommand(action, EditorCommandId.CopyBlock);
    }

    private static void AssertCommand(EditorInputAction action, EditorCommandId expected)
    {
        Assert.Equal(EditorInputActionKind.Command, action.Kind);
        Assert.NotNull(action.Binding);
        Assert.Equal(expected, action.Binding!.CommandId);
    }
}
