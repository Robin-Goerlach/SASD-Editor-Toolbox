namespace Sasd.Editor.FirstEd.Sample;

/// <summary>
/// Immutable rectangle and ASCII row projection for a transient MicroStar
/// application popup. Kept apart from Console I/O so geometry and command
/// eligibility are testable without a terminal or an editor session.
/// </summary>
/// <remarks>
/// Historical MakeWindow/RestoreWindow saved text-mode video cells. Modern
/// terminals do not promise portable cell readback, especially after VT
/// control characters or wide Unicode glyphs. We instead redraw from the
/// editor model on dismissal. This preserves the observable overlay/recovery
/// behavior without carrying any DOS display-memory implementation into Core.
/// </remarks>
internal sealed class ConsoleMicroStarPopupLayout
{
    internal static readonly string[] MenuLines =
    [
        "MICROSTAR   F10 MENU",
        "F Find        R Replace",
        "G Go to line  U Undo",
        "S Save        W Write file",
        "I Read file   N Next window",
        "B Block begin E Block end",
        "C Copy block  M Move block",
        "D Delete      T Write block",
        "L Read block  Q Cancel",
        "ESC cancels"
    ];

    private readonly string[] _lines;

    private ConsoleMicroStarPopupLayout(
        int left, int top, int width, int height, string[] lines)
    {
        Left = left;
        Top = top;
        Width = width;
        Height = height;
        _lines = lines;
    }

    internal int Left { get; }
    internal int Top { get; }
    internal int Width { get; }
    internal int Height { get; }

    /// <summary>
    /// Return null when a popup would obscure the whole editing workspace or
    /// overrun the physical console. The caller can use a compact fallback.
    /// The final terminal column is reserved to avoid automatic VT wrapping.
    /// </summary>
    internal static ConsoleMicroStarPopupLayout? Create(
        int terminalWidth, int terminalHeight, IReadOnlyList<string> contents)
    {
        ArgumentNullException.ThrowIfNull(contents);
        if (contents.Count == 0) return null;

        var width = contents.Max(s => s.Length) + 4;
        var height = contents.Count + 2;

        // At least two columns of surrounding editor must remain. The
        // top/bottom margins prevent overwriting the command/status rows.
        if (terminalWidth < width + 4 || terminalHeight < height + 3)
            return null;

        return new ConsoleMicroStarPopupLayout(
            left: (terminalWidth - width) / 2,
            top: (terminalHeight - height) / 2,
            width, height, contents.ToArray());
    }

    /// <summary>
    /// Render one complete row; padding is deterministic and cannot leak any
    /// previous popup's content into the terminal. The renderer never writes
    /// beyond the exclusive Right boundary.
    /// </summary>
    internal string GetRow(int row, char? selectedChoice = null)
    {
        if (row < 0 || row >= Height)
            throw new ArgumentOutOfRangeException(nameof(row));

        if (row == 0 || row == Height - 1)
            return "+" + new string('-', Width - 2) + "+";

        var content = _lines[row - 1];

        // The selected command is indicated by replacing its shortcut with
        // a '>' cursor. Selection never changes the original menu definition
        // or the fixed popup width; no leftover glyphs survive repainting.
        // A token must begin at the start of a menu item (line start or after
        // whitespace), avoiding accidental highlighting inside descriptions.
        if (selectedChoice.HasValue)
        {
            var letter = char.ToUpperInvariant(selectedChoice.Value);
            for (var i = 0; i < content.Length; i++)
            {
                if (content[i] == letter
                    && (i == 0 || content[i - 1] == ' ')
                    && i + 1 < content.Length && content[i + 1] == ' ')
                {
                    content = content[..i] + ">" + content[(i + 1)..];
                    break;
                }
            }
        }
        return "| " + content.PadRight(Width - 4) + " |";
    }

    internal static bool IsMenuChoice(char letter) =>
        "FRGUSWINBECMDTLQ".Contains(char.ToUpperInvariant(letter));
}
