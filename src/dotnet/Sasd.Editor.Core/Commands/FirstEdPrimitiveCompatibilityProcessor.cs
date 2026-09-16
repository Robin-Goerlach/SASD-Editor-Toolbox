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
    /// Clean-room transfer of EditInsertLine. A line is always inserted below
    /// the current descriptor. At column zero the current line becomes blank and
    /// its complete text moves to the new line; beyond the last non-blank a blank
    /// line is inserted; otherwise the text is split at the cursor. The current
    /// window remains on the upper line.
    /// </summary>
    public static bool InsertLine(EditorSession session)
    {
        var window = RequireWindow(session);
        InsertLineCore(session, window, "Insert line");
        return true;
    }

    /// <summary>
    /// Clean-room transfer of EditNewLine (the Return-key operation). In Insert
    /// mode it performs the same split as EditInsertLine but moves the cursor to
    /// the lower line. In Overtype mode it moves down without splitting unless the
    /// current line is the final line, in which case a blank line is appended.
    /// Autoindent affects only the resulting cursor column. The previously current
    /// line's Wrapped flag is always cleared, as documented by the handbook.
    /// </summary>
    public static bool NewLine(EditorSession session)
    {
        var window = RequireWindow(session);
        var document = window.Document;
        var previousLineIndex = window.Cursor.Line;
        var previous = document.Buffer.GetLine(previousLineIndex);
        var targetColumn = AutoIndentColumn(window, previous.Text);

        if (window.Options.InsertMode)
        {
            var lowerLine = InsertLineCore(session, window, "New line");
            var upper = document.Buffer.GetLine(previousLineIndex);
            document.Buffer.SetFlags(previousLineIndex, upper.Flags & ~EditorLineFlags.Wrapped);
            window.Cursor = new TextPosition(lowerLine, targetColumn);
            return true;
        }

        var appendLine = previousLineIndex == document.Buffer.LineCount - 1;
        var clearsWrapped = previous.Flags.HasFlag(EditorLineFlags.Wrapped);

        if (appendLine || clearsWrapped)
        {
            session.Undo.Capture(window, "New line");
        }

        if (appendLine)
        {
            var insertedLine = previousLineIndex + 1;
            document.Buffer.InsertLine(insertedLine, string.Empty);
            session.Topology.LinesInserted(document, insertedLine, 1);
        }

        if (clearsWrapped)
        {
            document.Buffer.SetFlags(previousLineIndex, previous.Flags & ~EditorLineFlags.Wrapped);
        }

        if (appendLine || clearsWrapped)
        {
            document.MarkChanged();
            session.RefreshBlockFlags();
        }

        var targetLine = Math.Min(previousLineIndex + 1, document.Buffer.LineCount - 1);
        window.Cursor = new TextPosition(targetLine, targetColumn);
        return true;
    }

    /// <summary>
    /// Clean-room transfer of EditDeleteLeftChar. Inside a line the character to
    /// the left of the cursor is removed and the cursor follows it one column.
    /// At column zero the current line is joined to the previous line at the
    /// previous line's last non-blank end. At the start of the stream no action is
    /// performed.
    /// </summary>
    public static bool DeleteLeftCharacter(EditorSession session)
    {
        var window = RequireWindow(session);
        var document = window.Document;
        var lineIndex = window.Cursor.Line;
        var column = Math.Max(0, window.Cursor.Column);

        if (column > 0)
        {
            var line = document.Buffer.GetLine(lineIndex);
            var deletionColumn = column - 1;

            // EditRightChar can create virtual columns beyond the physical text.
            // Backspacing through that virtual area is cursor movement only until
            // the cursor reaches a stored character.
            if (deletionColumn >= line.Text.Length)
            {
                window.Cursor = window.Cursor with { Column = deletionColumn };
                return true;
            }

            session.Undo.Capture(window, "Delete left character");
            document.Buffer.ReplaceLine(lineIndex, line.Text.Remove(deletionColumn, 1));
            window.Cursor = window.Cursor with { Column = deletionColumn };
            document.MarkChanged();
            session.RefreshBlockFlags();
            return true;
        }

        if (lineIndex == 0)
        {
            return false;
        }

        var previousLineIndex = lineIndex - 1;
        var previous = document.Buffer.GetLine(previousLineIndex);
        var current = document.Buffer.GetLine(lineIndex);
        var joinColumn = LastNonBlankEnd(previous.Text);

        session.Undo.Capture(window, "Delete left character");
        document.Buffer.ReplaceLine(
            previousLineIndex,
            PrefixToColumn(previous.Text, joinColumn) + current.Text,
            previous.Flags);
        document.Buffer.RemoveLine(lineIndex);
        session.Topology.LinesDeleted(document, lineIndex, 1);
        window.Cursor = new TextPosition(previousLineIndex, joinColumn);
        document.MarkChanged();
        session.RefreshBlockFlags();
        return true;
    }

    /// <summary>
    /// Clean-room transfer of EditDeleteRightChar. A character at the cursor is
    /// removed normally. Once the cursor is at or beyond the first column after
    /// the last non-blank character, the command joins the following logical line
    /// instead. The join participates in line-topology realignment so linked
    /// windows, markers and block boundaries cannot retain stale line numbers.
    /// </summary>
    public static bool DeleteRightCharacter(EditorSession session)
    {
        var window = RequireWindow(session);
        var document = window.Document;
        var lineIndex = window.Cursor.Line;
        var line = document.Buffer.GetLine(lineIndex);
        var column = Math.Max(0, window.Cursor.Column);

        if (column >= LastNonBlankEnd(line.Text))
        {
            return JoinFollowingLine(session, window, "Delete right character");
        }

        if (column >= line.Text.Length)
        {
            return false;
        }

        session.Undo.Capture(window, "Delete right character");
        document.Buffer.ReplaceLine(lineIndex, line.Text.Remove(column, 1));
        document.MarkChanged();
        session.RefreshBlockFlags();
        return true;
    }

    /// <summary>
    /// Clean-room transfer of EditDeleteRightWord. A word is the run of characters
    /// in the cursor's current historical class (alphanumeric, punctuation or
    /// blank) plus immediately following blanks. When the cursor is already at or
    /// beyond the last non-blank character, the following line is joined instead.
    /// </summary>
    public static bool DeleteRightWord(EditorSession session)
    {
        var window = RequireWindow(session);
        var document = window.Document;
        var lineIndex = window.Cursor.Line;
        var line = document.Buffer.GetLine(lineIndex);
        var column = Math.Max(0, window.Cursor.Column);

        if (column >= LastNonBlankEnd(line.Text))
        {
            return JoinFollowingLine(session, window, "Delete right word");
        }

        if (column >= line.Text.Length)
        {
            return false;
        }

        var wordClass = Classify(line.Text[column]);
        var end = column;
        while (end < line.Text.Length && Classify(line.Text[end]) == wordClass)
        {
            end++;
        }

        while (end < line.Text.Length && IsBlank(line.Text[end]))
        {
            end++;
        }

        session.Undo.Capture(window, "Delete right word");
        document.Buffer.ReplaceLine(lineIndex, line.Text.Remove(column, end - column));
        document.MarkChanged();
        session.RefreshBlockFlags();
        return true;
    }

    /// <summary>
    /// Clean-room transfer of EditDeleteLine together with the observable
    /// EditDelline/EditRealign responsibilities. The line is captured for modern
    /// snapshot undo, line-number references are repaired centrally, markers on
    /// the removed line are invalidated, and a block touching the removed boundary
    /// is hidden/cleared by the topology coordinator.
    /// </summary>
    public static bool DeleteLine(EditorSession session)
    {
        var window = RequireWindow(session);
        var document = window.Document;
        var lineIndex = window.Cursor.Line;
        var line = document.Buffer.GetLine(lineIndex);

        session.Undo.Capture(window, "Delete line");

        if (document.Buffer.LineCount == 1)
        {
            // The historical command keeps the sole descriptor and blanks its
            // contents instead of deleting the final line from the stream.
            session.Topology.InvalidateLineReferences(document, lineIndex);
            var preservedFlags = line.Flags & EditorLineFlags.UserColored;
            document.Buffer.ReplaceLine(
                lineIndex,
                new string(' ', line.Text.Length),
                preservedFlags);
        }
        else
        {
            document.Buffer.RemoveLine(lineIndex);
            session.Topology.LinesDeleted(document, lineIndex, 1);
        }

        document.MarkChanged();
        session.RefreshBlockFlags();
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

    /// <summary>
    /// Performs the common structural split used by EditInsertLine and the Insert
    /// mode branch of EditNewLine. The caller decides whether the cursor follows
    /// the text onto the newly created lower line.
    /// </summary>
    private static int InsertLineCore(EditorSession session, EditorWindow window, string undoDescription)
    {
        var document = window.Document;
        var lineIndex = window.Cursor.Line;
        var line = document.Buffer.GetLine(lineIndex);
        var column = Math.Max(0, window.Cursor.Column);
        var lastNonBlankEnd = LastNonBlankEnd(line.Text);
        string upperText;
        string lowerText;

        if (column == 0)
        {
            upperText = string.Empty;
            lowerText = line.Text;
        }
        else if (column >= lastNonBlankEnd)
        {
            upperText = line.Text;
            lowerText = string.Empty;
        }
        else
        {
            var splitColumn = Math.Min(column, line.Text.Length);
            upperText = line.Text[..splitColumn];
            lowerText = line.Text[splitColumn..];
        }

        session.Undo.Capture(window, undoDescription);
        document.Buffer.ReplaceLine(lineIndex, upperText, line.Flags);
        var insertedLine = lineIndex + 1;
        document.Buffer.InsertLine(insertedLine, lowerText);
        session.Topology.LinesInserted(document, insertedLine, 1);
        document.MarkChanged();
        session.RefreshBlockFlags();
        return insertedLine;
    }

    /// <summary>
    /// Joins the following logical line into the current one while applying the
    /// same topology repair that a descriptor deletion historically required.
    /// The following line begins at the cursor column, so trailing blanks after
    /// that location are not retained as an artificial gap.
    /// </summary>
    private static bool JoinFollowingLine(EditorSession session, EditorWindow window, string undoDescription)
    {
        var document = window.Document;
        var lineIndex = window.Cursor.Line;
        if (lineIndex + 1 >= document.Buffer.LineCount)
        {
            return false;
        }

        var current = document.Buffer.GetLine(lineIndex);
        var following = document.Buffer.GetLine(lineIndex + 1);
        var joinColumn = Math.Max(0, window.Cursor.Column);

        session.Undo.Capture(window, undoDescription);
        document.Buffer.ReplaceLine(
            lineIndex,
            PrefixToColumn(current.Text, joinColumn) + following.Text,
            current.Flags);
        document.Buffer.RemoveLine(lineIndex + 1);
        session.Topology.LinesDeleted(document, lineIndex + 1, 1);
        document.MarkChanged();
        session.RefreshBlockFlags();
        return true;
    }

    private static string PrefixToColumn(string text, int column)
    {
        if (column <= text.Length)
        {
            return text[..column];
        }

        return text.PadRight(column);
    }

    private static int AutoIndentColumn(EditorWindow window, string previousText)
    {
        if (!window.Options.AutoIndent)
        {
            return 0;
        }

        var firstNonBlank = FirstNonBlank(previousText);
        return firstNonBlank < previousText.Length ? firstNonBlank : 0;
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
