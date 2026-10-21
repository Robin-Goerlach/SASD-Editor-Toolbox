using System.Globalization;
using System.Text;

namespace Sasd.Editor.FirstEd.Sample;

/// <summary>
/// Converts an editor/status/prompt string into a safe, bounded terminal row.
/// This is strictly a FIRST-ED console-host concern, not document conversion.
/// </summary>
/// <remarks>
/// The editor intentionally stores ASCII control bytes (EditInsertCtrlChar),
/// so writing a document directly to Console.Write is unsafe: ESC[2J could
/// clear the terminal, CR could overwrite another status field, and LF could
/// move the physical cursor to an unrelated window. We project those bytes
/// into printable notation, while the underlying document remains unchanged.
///
/// Text elements, rather than UTF-16 code units, are the indivisible unit
/// for clipping. Cell-width rules for common CJK/emoji characters are a
/// conservative terminal-host estimate. They are NOT a promise of identical
/// wcwidth implementations across operating systems and terminals.
/// </remarks>
internal static class ConsoleFirstEdRowProjection
{
    /// <summary>
    /// Returns exactly <paramref name="columns"/> estimated display cells.
    /// Starting inside a text element skips that element rather than painting
    /// half a surrogate pair or a detached combining mark.
    /// </summary>
    internal static ConsoleFirstEdRowDisplay Project(
        string text, int firstCodeUnit, int columns, int cursorCodeUnit = 0)
    {
        ArgumentNullException.ThrowIfNull(text);
        ArgumentOutOfRangeException.ThrowIfNegative(firstCodeUnit);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(columns);
        ArgumentOutOfRangeException.ThrowIfNegative(cursorCodeUnit);

        var answer = new StringBuilder(columns);
        var usedColumns = 0;
        var cursorColumns = 0;
        var enumerator = StringInfo.GetTextElementEnumerator(text);

        while (enumerator.MoveNext())
        {
            var start = enumerator.ElementIndex;
            var element = enumerator.GetTextElement();
            var end = start + element.Length;

            // An EditorWindow's LeftColumn is a logical UTF-16 offset.
            // It may land *inside* a grapheme. Painting the suffix of that
            // grapheme would show a broken Unicode glyph; skip it instead.
            if (end <= firstCodeUnit || start < firstCodeUnit)
                continue;

            var display = SafeElement(element);
            var cellWidth = DisplayCells(element, display);

            if (start < cursorCodeUnit)
                cursorColumns += cellWidth;

            // Do not emit half a caret notation (^A), or a wide character
            // in the last remaining one-cell position. This also reserves
            // against terminal auto-wrap as the renderer passes width-1.
            if (usedColumns + cellWidth > columns)
                break;

            answer.Append(display);
            usedColumns += cellWidth;
        }

        // Cursors may legally sit in virtual space to the right of the
        // stored line. Include those virtual blanks without changing text.
        if (cursorCodeUnit > text.Length)
            cursorColumns += cursorCodeUnit - Math.Max(firstCodeUnit, text.Length);

        if (usedColumns < columns)
            answer.Append(' ', columns - usedColumns);

        return new ConsoleFirstEdRowDisplay(
            answer.ToString(),
            Math.Clamp(cursorColumns, 0, columns - 1));
    }

    private static string SafeElement(string element)
    {
        var printable = new StringBuilder(element.Length);
        foreach (var rune in element.EnumerateRunes())
        {
            var value = rune.Value;
            if (value < 0x20)
            {
                // Printable caret notation for ASCII C0 controls:
                // ESC -> ^[, CR -> ^M, LF -> ^J, NUL -> ^@.
                printable.Append('^').Append((char)(value + '@'));
            }
            else if (value == 0x7F)
            {
                printable.Append("^?");
            }
            else if (value is >= 0x80 and <= 0x9F)
            {
                // C1 controls include the one-byte CSI 0x9B. Output only
                // safe ASCII so the host cannot trigger a terminal action.
                printable.Append('[').Append(value.ToString("X2")).Append(']');
            }
            else if (value == 0xFFFD && element.Any(char.IsSurrogate))
            {
                // Rune enumeration replaces unpaired UTF-16 surrogates.
                // Make that malformed input visible without emitting it.
                printable.Append('?');
            }
            else
            {
                printable.Append(rune.ToString());
            }
        }

        return printable.ToString();
    }

    private static int DisplayCells(string element, string safe)
    {
        // Escaped control codes are ordinary ASCII characters and need the
        // literal number of cells shown. The branch also handles malformed
        // surrogate placeholders without accidentally counting two cells.
        if (!string.Equals(element, safe, StringComparison.Ordinal))
            return safe.Length;

        var cells = 1;
        foreach (var rune in element.EnumerateRunes())
        {
            // Combining marks and joiners are part of their text element,
            // not an additional cell. The whole grapheme uses its widest
            // meaningful scalar's cell budget.
            var category = Rune.GetUnicodeCategory(rune);
            if (category is UnicodeCategory.NonSpacingMark
                or UnicodeCategory.EnclosingMark
                or UnicodeCategory.SpacingCombiningMark
                or UnicodeCategory.Format)
            {
                continue;
            }

            if (IsCommonWideScalar(rune.Value))
                cells = 2;
        }

        return cells;
    }

    private static bool IsCommonWideScalar(int codePoint) =>
        codePoint >= 0x1100
        && (codePoint <= 0x115F
            || codePoint is 0x2329 or 0x232A
            || (codePoint is >= 0x2E80 and <= 0xA4CF && codePoint != 0x303F)
            || (codePoint is >= 0xAC00 and <= 0xD7A3)
            || (codePoint is >= 0xF900 and <= 0xFAFF)
            || (codePoint is >= 0xFE10 and <= 0xFE19)
            || (codePoint is >= 0xFE30 and <= 0xFE6F)
            || (codePoint is >= 0xFF00 and <= 0xFF60)
            || (codePoint is >= 0xFFE0 and <= 0xFFE6)
            || (codePoint is >= 0x1F000 and <= 0x1FAFF)
            || (codePoint is >= 0x20000 and <= 0x3FFFD));
}

/// <summary>
/// A safe physical row and cursor coordinate. CursorColumn is terminal-cell
/// based; the underlying editor model's Cursor.Column remains a text offset.
/// </summary>
internal readonly record struct ConsoleFirstEdRowDisplay(
    string Text, int CursorColumn);
