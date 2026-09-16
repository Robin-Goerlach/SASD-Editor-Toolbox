using Sasd.Editor.Editing;
using Sasd.Editor.Model;
using Sasd.Editor.Windows;

namespace Sasd.Editor.Commands;

/// <summary>
/// Small FIRST-ED compatibility primitives whose historical behavior differs
/// intentionally from the reusable modern <see cref="EditorEngine"/> defaults.
/// </summary>
/// <remarks>
/// Keeping these rules in a narrow adapter prevents DOS-era cursor conventions
/// from leaking into the general editing engine. Future C++, Java and JavaScript
/// implementations can reproduce the observable behavior without copying Pascal
/// data structures or pointer mechanics.
/// </remarks>
internal static class FirstEdPrimitiveCompatibilityProcessor
{
    /// <summary>
    /// Clean-room transfer of the historical EditLeftChar behavior. Moving left
    /// from column zero enters the previous logical line immediately after its
    /// last non-blank character rather than after trailing whitespace.
    /// </summary>
    public static bool MoveLeftChar(EditorSession session)
    {
        var window = RequireWindow(session);
        if (window.Cursor.Column > 0)
        {
            window.Cursor = window.Cursor with { Column = window.Cursor.Column - 1 };
            return true;
        }

        if (window.Cursor.Line <= 0)
        {
            return false;
        }

        var previousLine = window.Cursor.Line - 1;
        var previousText = window.Document.Buffer.GetLine(previousLine).Text;
        window.Cursor = new TextPosition(previousLine, LastNonBlankEnd(previousText));
        return true;
    }

    /// <summary>
    /// Clean-room transfer of EditRightChar. The historical command advances the
    /// column only; it does not cross a logical line boundary. This also means a
    /// cursor may occupy a virtual column beyond the current line length.
    /// </summary>
    public static bool MoveRightChar(EditorSession session)
    {
        var window = RequireWindow(session);
        if (window.Cursor.Column == int.MaxValue)
        {
            return false;
        }

        window.Cursor = window.Cursor with { Column = window.Cursor.Column + 1 };
        return true;
    }

    /// <summary>
    /// Clean-room transfer of EditLeftWord. Leading indentation is treated as a
    /// boundary of its own: from within or immediately after the leading blank
    /// region the first movement goes to column zero. A second movement from
    /// column zero enters the preceding logical line at its last non-blank end.
    /// Otherwise the cursor finds the beginning of the previous character class
    /// on the same line.
    /// </summary>
    public static bool MoveLeftWord(EditorSession session)
    {
        var window = RequireWindow(session);
        var text = window.Document.Buffer.GetLine(window.Cursor.Line).Text;
        var firstNonBlank = FirstNonBlank(text);
        var column = Math.Max(0, window.Cursor.Column);

        if (column <= firstNonBlank)
        {
            if (column > 0)
            {
                window.Cursor = window.Cursor with { Column = 0 };
                return true;
            }

            if (window.Cursor.Line <= 0)
            {
                return false;
            }

            var previousLine = window.Cursor.Line - 1;
            var previousText = window.Document.Buffer.GetLine(previousLine).Text;
            window.Cursor = new TextPosition(previousLine, LastNonBlankEnd(previousText));
            return true;
        }

        var scan = Math.Min(column, text.Length) - 1;
        while (scan >= 0 && IsBlank(text[scan]))
        {
            scan--;
        }

        if (scan < 0)
        {
            window.Cursor = window.Cursor with { Column = 0 };
            return true;
        }

        var wordClass = Classify(text[scan]);
        while (scan > 0 && Classify(text[scan - 1]) == wordClass)
        {
            scan--;
        }

        window.Cursor = window.Cursor with { Column = scan };
        return true;
    }

    /// <summary>
    /// Clean-room transfer of EditRightWord. The historical word model has three
    /// classes: alphanumeric characters, punctuation and blanks. Movement crosses
    /// one non-blank class and then any following blanks. Starting on blanks skips
    /// directly to the next non-blank. Once the cursor is beyond the current line's
    /// last non-blank character it moves to column zero of the following line.
    /// </summary>
    public static bool MoveRightWord(EditorSession session)
    {
        var window = RequireWindow(session);
        var text = window.Document.Buffer.GetLine(window.Cursor.Line).Text;
        var column = Math.Max(0, window.Cursor.Column);
        var lastNonBlankEnd = LastNonBlankEnd(text);

        if (column >= lastNonBlankEnd)
        {
            if (window.Cursor.Line + 1 >= window.Document.Buffer.LineCount)
            {
                return false;
            }

            window.Cursor = new TextPosition(window.Cursor.Line + 1, 0);
            return true;
        }

        if (IsBlank(text[column]))
        {
            while (column < text.Length && IsBlank(text[column]))
            {
                column++;
            }

            window.Cursor = window.Cursor with { Column = column };
            return true;
        }

        var wordClass = Classify(text[column]);
        while (column < text.Length && Classify(text[column]) == wordClass)
        {
            column++;
        }

        while (column < text.Length && IsBlank(text[column]))
        {
            column++;
        }

        window.Cursor = window.Cursor with { Column = column };
        return true;
    }

    /// <summary>
    /// Clean-room transfer of EditTab. In insert mode the padding to the next tab
    /// stop is inserted into the document. In overtype mode Tab is cursor movement
    /// only and therefore must not dirty the document or create an undo entry.
    /// </summary>
    public static bool Tab(EditorSession session)
    {
        var window = RequireWindow(session);
        var tabSize = window.Options.TabSize;
        var column = Math.Max(0, window.Cursor.Column);
        var spaces = tabSize - (column % tabSize);

        if (column > int.MaxValue - spaces)
        {
            return false;
        }

        if (window.Options.InsertMode)
        {
            session.Engine.InsertText(new string(' ', spaces));
        }
        else
        {
            window.Cursor = window.Cursor with { Column = column + spaces };
        }

        return true;
    }

    private static int FirstNonBlank(string text)
    {
        var index = 0;
        while (index < text.Length && IsBlank(text[index]))
        {
            index++;
        }

        return index;
    }

    private static int LastNonBlankEnd(string text)
    {
        var index = text.Length - 1;
        while (index >= 0 && IsBlank(text[index]))
        {
            index--;
        }

        return index + 1;
    }

    /// <summary>
    /// The 1985 editor works with ASCII classes. The modern implementation uses
    /// Unicode-aware letter/digit and whitespace predicates; for ASCII input this
    /// is behaviorally equivalent while avoiding an artificial ASCII-only core.
    /// </summary>
    private static WordCharacterClass Classify(char value) =>
        IsBlank(value)
            ? WordCharacterClass.Blank
            : char.IsLetterOrDigit(value)
                ? WordCharacterClass.Alphanumeric
                : WordCharacterClass.Punctuation;

    private static bool IsBlank(char value) => char.IsWhiteSpace(value);

    private static EditorWindow RequireWindow(EditorSession session)
    {
        ArgumentNullException.ThrowIfNull(session);
        return session.Windows.Count > 0
            ? session.CurrentWindow
            : throw new InvalidOperationException("The editor session has no windows.");
    }

    private enum WordCharacterClass
    {
        Blank,
        Alphanumeric,
        Punctuation
    }
}
