namespace Sasd.Editor.FirstEd.Sample;

/// <summary>
/// Final presentation-only sanitization for directory names and paths.
/// Filesystem names are data, never trusted terminal instruction sequences.
/// </summary>
internal static class MicroStarBrowserDisplay
{
    /// <summary>
    /// Restrict this first ASCII-framed popup to printable ASCII and keep a
    /// stable cell width. Control characters (notably ESC), ANSI sequences,
    /// tabs and wide Unicode glyphs must never pass through Console.Write
    /// in a popup, where they could reposition the cursor or erase the screen.
    /// The original full path remains untouched in the browser model.
    /// </summary>
    internal static string SafeLabel(string text, int maxCells)
    {
        ArgumentNullException.ThrowIfNull(text);
        if (maxCells < 4) throw new ArgumentOutOfRangeException(nameof(maxCells));

        var safe = new string(text.Select(character =>
            character >= ' ' && character <= '~' ? character : '?').ToArray());
        return safe.Length <= maxCells
            ? safe : safe[..(maxCells - 3)] + "...";
    }
}
