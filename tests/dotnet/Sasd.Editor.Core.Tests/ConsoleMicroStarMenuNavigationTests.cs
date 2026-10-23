using Sasd.Editor.FirstEd.Sample;
using Xunit;

namespace Sasd.Editor.Core.Tests;

/// <summary>
/// Menu keyboard tests run without a terminal and therefore exercise exactly
/// the same policy on Windows and Linux, independent of Console.ReadKey.
/// </summary>
public sealed class ConsoleMicroStarMenuNavigationTests
{
    [Fact]
    public void ArrowNavigationOnlyChangesSelectionUntilEnter()
    {
        var menu = new ConsoleMicroStarMenuNavigation();
        Assert.Equal('F', menu.SelectedChoice);
        Assert.Equal(MicroStarMenuResultKind.SelectionChanged,
            menu.Handle(ConsoleKey.DownArrow).Kind);
        Assert.Equal('R', menu.SelectedChoice);
        var accepted = menu.Handle(ConsoleKey.Enter);
        Assert.Equal(MicroStarMenuResultKind.Accept, accepted.Kind);
        Assert.Equal('R', accepted.Choice);
    }

    [Fact]
    public void PreviousFromFirstWrapsToCancel()
    {
        var menu = new ConsoleMicroStarMenuNavigation();
        menu.Handle(ConsoleKey.UpArrow);
        Assert.Equal('Q', menu.SelectedChoice);
        Assert.Equal(MicroStarMenuResultKind.Cancel,
            menu.Handle(ConsoleKey.Enter).Kind);
    }

    [Theory]
    [InlineData(ConsoleKey.Escape, '\0', ConsoleModifiers.None)]
    [InlineData(ConsoleKey.U, '\u0015', ConsoleModifiers.Control)]
    [InlineData(ConsoleKey.Q, 'q', ConsoleModifiers.None)]
    public void CancelIsNeverAnEditorCommand(
        ConsoleKey key, char ch, ConsoleModifiers modifiers)
    {
        var menu = new ConsoleMicroStarMenuNavigation();
        Assert.Equal(MicroStarMenuResultKind.Cancel,
            menu.Handle(key, ch, modifiers).Kind);
    }

    [Theory]
    [InlineData(ConsoleKey.NoName, '\u001b', ConsoleModifiers.None)]
    [InlineData(ConsoleKey.NoName, '\u0015', ConsoleModifiers.None)]
    [InlineData(ConsoleKey.NoName, '\u0015', ConsoleModifiers.Control)]
    public void RawTerminalControlCodesCancelTheMenuWithoutDispatch(
        ConsoleKey key, char character, ConsoleModifiers modifiers)
    {
        // A PTY may expose the control byte while losing the virtual key.
        // This should behave exactly like the named Escape/Ctrl-U gestures.
        var menu = new ConsoleMicroStarMenuNavigation();
        menu.Handle(ConsoleKey.DownArrow);
        var result = menu.Handle(key, character, modifiers);
        Assert.Equal(MicroStarMenuResultKind.Cancel, result.Kind);
        Assert.Equal('R', menu.SelectedChoice);
    }

    [Fact]
    public void AltModifiedCtrlUIsNotAccidentallyTreatedAsCancellation()
    {
        // AltGr is often represented by Ctrl+Alt. In particular a modified
        // key must not turn into a destructive or cancellation menu action.
        var menu = new ConsoleMicroStarMenuNavigation();
        var result = menu.Handle(ConsoleKey.U, 'u',
            ConsoleModifiers.Control | ConsoleModifiers.Alt);
        Assert.Equal(MicroStarMenuResultKind.Ignored, result.Kind);
        Assert.Equal('F', menu.SelectedChoice);
    }

    [Fact]
    public void ExistingLetterShortcutsStillWork()
    {
        var menu = new ConsoleMicroStarMenuNavigation();
        var result = menu.Handle(ConsoleKey.T, 't');
        Assert.Equal(MicroStarMenuResultKind.Accept, result.Kind);
        Assert.Equal('T', result.Choice);
    }

    [Fact]
    public void ModifiedAndUnsupportedKeysAreIgnored()
    {
        var menu = new ConsoleMicroStarMenuNavigation();
        Assert.Equal(MicroStarMenuResultKind.Ignored,
            menu.Handle(ConsoleKey.F, 'f', ConsoleModifiers.Alt).Kind);
        Assert.Equal(MicroStarMenuResultKind.Ignored,
            menu.Handle(ConsoleKey.Z, 'z').Kind);
        Assert.Equal('F', menu.SelectedChoice);
    }

    [Fact]
    public void HighlightIsVisualOnlyAndDoesNotChangeMenuContents()
    {
        var popup = ConsoleMicroStarPopupLayout.Create(
            100, 30, ConsoleMicroStarPopupLayout.MenuLines)!;
        var original = popup.GetRow(2);
        var selected = popup.GetRow(2, 'F');
        Assert.Equal(original.Length, selected.Length);
        Assert.Contains("> Find", selected);
        Assert.Equal(original, popup.GetRow(2));
    }
}
