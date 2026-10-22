namespace Sasd.Editor.FirstEd.Sample;

/// <summary>
/// Distinguishes movement (which only changes presentation) from a committed
/// editor action. This prevents an arrow key from ever dispatching a command.
/// </summary>
internal enum MicroStarMenuResultKind
{
    Ignored,
    SelectionChanged,
    Accept,
    Cancel
}

internal readonly record struct MicroStarMenuResult(
    MicroStarMenuResultKind Kind, char Choice = '\0');

/// <summary>
/// Pure navigation state for the MicroStar sample F10 popup. Console input is
/// normalized by the host; this class neither accesses Console nor mutates
/// EditorSession. It is intentionally a policy, not an editor key map.
/// </summary>
/// <remarks>
/// The handbook documents a MicroStar menu but not this precise modern arrow
/// navigation order. We preserve the already documented SASD shortcuts and
/// offer Left/Up, Right/Down, Home/End, Enter, Escape and Ctrl-U.
/// </remarks>
internal sealed class ConsoleMicroStarMenuNavigation
{
    // Visual reading order, across the two-column menu. Q cancels rather
    // than dispatching a command. These letters match the existing host.
    private const string Choices = "FRGU SWIN BECM DTLQ";
    private static readonly char[] Ordered =
        Choices.Where(c => c != ' ').ToArray();

    private int _selected;

    internal char SelectedChoice => Ordered[_selected];

    internal MicroStarMenuResult Handle(
        ConsoleKey key, char character = '\0',
        ConsoleModifiers modifiers = 0)
    {
        if (key == ConsoleKey.Escape
            || (modifiers.HasFlag(ConsoleModifiers.Control)
                && key == ConsoleKey.U))
            return new(MicroStarMenuResultKind.Cancel);

        // Treat Alt/Ctrl combinations as host/terminal shortcuts, not as
        // printable commands. This avoids accidental destructive commands
        // from a terminal-specific modified key sequence.
        if ((modifiers & (ConsoleModifiers.Alt | ConsoleModifiers.Control)) != 0)
            return new(MicroStarMenuResultKind.Ignored);

        switch (key)
        {
            case ConsoleKey.LeftArrow:
            case ConsoleKey.UpArrow:
                _selected = (_selected + Ordered.Length - 1) % Ordered.Length;
                return new(MicroStarMenuResultKind.SelectionChanged);
            case ConsoleKey.RightArrow:
            case ConsoleKey.DownArrow:
                _selected = (_selected + 1) % Ordered.Length;
                return new(MicroStarMenuResultKind.SelectionChanged);
            case ConsoleKey.Home:
                _selected = 0;
                return new(MicroStarMenuResultKind.SelectionChanged);
            case ConsoleKey.End:
                _selected = Ordered.Length - 1;
                return new(MicroStarMenuResultKind.SelectionChanged);
            case ConsoleKey.Enter:
                return SelectedChoice == 'Q'
                    ? new(MicroStarMenuResultKind.Cancel)
                    : new(MicroStarMenuResultKind.Accept, SelectedChoice);
        }

        // Retain the existing one-keystroke menu shortcuts. Invalid letters
        // remain inert; they never fall through to ordinary editor insertion.
        var letter = char.ToUpperInvariant(character);
        if (!ConsoleMicroStarPopupLayout.IsMenuChoice(letter))
            return new(MicroStarMenuResultKind.Ignored);
        return letter == 'Q'
            ? new(MicroStarMenuResultKind.Cancel)
            : new(MicroStarMenuResultKind.Accept, letter);
    }
}
