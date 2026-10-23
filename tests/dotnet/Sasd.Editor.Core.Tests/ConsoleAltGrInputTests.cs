using Sasd.Editor.Editing;
using Sasd.Editor.FirstEd.Sample;
using Sasd.Editor.Input;
using Xunit;

namespace Sasd.Editor.Core.Tests;

/// <summary>
/// Guards the boundary between a physical Windows keyboard layout and the
/// platform-neutral FIRST-ED command mapper. No actual keyboard hardware is
/// required: ConsoleKeyInfo is the public input contract of this adapter.
/// </summary>
public sealed class ConsoleAltGrInputTests
{
    [Theory]
    [InlineData('[', ConsoleKey.D8)]
    [InlineData('{', ConsoleKey.D7)]
    [InlineData('@', ConsoleKey.Q)]
    [InlineData('\\', ConsoleKey.Oem5)]
    public void PrintableCtrlAltSymbolsAreTextNotEditorCommands(
        char character, ConsoleKey physicalKey)
    {
        // Windows can represent AltGr as Ctrl+Alt. Verify the entire route:
        // physical console event -> normalized keystroke -> FIRST-ED action.
        var physical = new ConsoleKeyInfo(
            character, physicalKey, false, true, true);
        var stroke = ConsoleKeyTranslator.Translate(physical);
        Assert.NotNull(stroke);
        Assert.Equal(EditorKey.Character, stroke.Value.Key);
        Assert.Equal(character, stroke.Value.Character);
        Assert.Equal(EditorKeyModifiers.None, stroke.Value.Modifiers);

        var action = new FirstEdKeyMap().Translate(stroke.Value);
        Assert.Equal(EditorInputActionKind.InsertText, action.Kind);
        Assert.Equal(character.ToString(), action.Text);
    }

    [Fact]
    public void ControlLetterStillBeginsItsHistoricalPrefix()
    {
        var physical = new ConsoleKeyInfo(
            '\u000b', ConsoleKey.K, false, false, true);
        var stroke = ConsoleKeyTranslator.Translate(physical);
        Assert.NotNull(stroke);

        var keyMap = new FirstEdKeyMap();
        keyMap.Translate(stroke.Value);
        Assert.Equal(FirstEdCommandPrefix.CtrlK, keyMap.PendingPrefix);
    }

    [Fact]
    public void ActualCtrlAltLetterIsNotReinterpretedAsOrdinaryText()
    {
        // Real Ctrl+Alt letter shortcuts are not necessarily AltGr symbols.
        // Only printable punctuation/symbol output receives the exception.
        var physical = new ConsoleKeyInfo(
            'q', ConsoleKey.Q, false, true, true);
        var stroke = ConsoleKeyTranslator.Translate(physical);
        Assert.NotNull(stroke);
        Assert.NotEqual(EditorKeyModifiers.None, stroke.Value.Modifiers);

        var action = new FirstEdKeyMap().Translate(stroke.Value);
        Assert.Equal(EditorInputActionKind.Ignored, action.Kind);
    }

    [Fact]
    public void NonPrintableCtrlAltInputCannotBecomeText()
    {
        var physical = new ConsoleKeyInfo(
            '\0', ConsoleKey.D8, false, true, true);
        var stroke = ConsoleKeyTranslator.Translate(physical);
        Assert.Null(stroke);
    }
}
