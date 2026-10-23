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

    [Theory]
    [InlineData(ConsoleKey.NoName, '\u001b', ConsoleModifiers.None, true)]
    [InlineData(ConsoleKey.NoName, '\u0015', ConsoleModifiers.None, true)]
    [InlineData(ConsoleKey.U, '\u0015', ConsoleModifiers.Control, true)]
    [InlineData(ConsoleKey.U, 'u', ConsoleModifiers.Control | ConsoleModifiers.Alt, false)]
    [InlineData(ConsoleKey.NoName, '\u0015', ConsoleModifiers.Alt, false)]
    [InlineData(ConsoleKey.F, 'f', ConsoleModifiers.None, false)]
    public void SharedPromptAndMenuCancellationPoliciesAgree(
        ConsoleKey key, char character, ConsoleModifiers modifiers, bool expected)
    {
        // The same physical event passes through different UI surfaces.
        // All must agree about aborting; otherwise a host-specific terminal
        // event could close a file prompt but accidentally accept a menu item.
        var physical = new ConsoleKeyInfo(character, key,
            (modifiers & ConsoleModifiers.Shift) != 0,
            (modifiers & ConsoleModifiers.Alt) != 0,
            (modifiers & ConsoleModifiers.Control) != 0);
        Assert.Equal(expected, ConsolePromptKeyPolicy.IsCancellation(physical));
        Assert.Equal(expected,
            ConsolePromptKeyPolicy.IsCancellation(key, character, modifiers));

        var navigation = new ConsoleMicroStarMenuNavigation();
        Assert.Equal(expected,
            navigation.Handle(key, character, modifiers).Kind
                == MicroStarMenuResultKind.Cancel);
    }

    [Fact]
    public void ExistingLetterShortcutsStillWork()
    {
        var menu = new ConsoleMicroStarMenuNavigation();
        var result = menu.Handle(ConsoleKey.T, 't');
        Assert.Equal(MicroStarMenuResultKind.Accept, result.Kind);
        Assert.Equal('T', result.Choice);
    }

    [Theory]
    [InlineData('\u017F')] // long s: Unicode uppercase is ASCII S
    [InlineData('\u212A')] // Kelvin sign: visually resembles Latin K
    [InlineData('\u0131')] // dotless i: not an ASCII I
    [InlineData('\u00E9')] // ordinary accented input is not a shortcut
    public void NonAsciiTextCannotSelectOrExecuteMenuCommands(char character)
    {
        // F10 is a command menu, not a case-insensitive Unicode search.
        // No Unicode lookalike or locale-specific uppercase conversion may
        // silently trigger Save, Write, Delete, or any other operation.
        var menu = new ConsoleMicroStarMenuNavigation();
        Assert.False(ConsoleMicroStarPopupLayout.IsMenuChoice(character));
        Assert.Equal(MicroStarMenuResultKind.Ignored,
            menu.Handle(ConsoleKey.NoName, character).Kind);
        Assert.Equal('F', menu.SelectedChoice);
    }

    [Theory]
    [InlineData('s', 'S')]
    [InlineData('w', 'W')]
    [InlineData('q', 'Q')]
    [InlineData('F', 'F')]
    public void AsciiShortcutNormalizationRemainsStable(char input, char expected)
    {
        Assert.True(ConsoleMicroStarPopupLayout.TryNormalizeMenuChoice(input, out var choice));
        Assert.Equal(expected, choice);
        var result = new ConsoleMicroStarMenuNavigation().Handle(ConsoleKey.NoName, input);
        Assert.Equal(expected == 'Q'
            ? MicroStarMenuResultKind.Cancel
            : MicroStarMenuResultKind.Accept, result.Kind);
        if (expected != 'Q')
            Assert.Equal(expected, result.Choice);
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
