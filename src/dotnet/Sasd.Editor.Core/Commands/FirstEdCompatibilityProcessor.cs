using Sasd.Editor.Editing;
using Sasd.Editor.Model;
using Sasd.Editor.Windows;

namespace Sasd.Editor.Commands;

/// <summary>
/// Implements the small amount of adapter logic needed to preserve FIRST-ED's
/// user-visible command semantics without contaminating the general editing
/// engine with 1985-specific conventions such as one-based line numbers and
/// modulo window numbers.
/// </summary>
/// <remarks>
/// Text mutation remains in <see cref="EditorEngine"/>. This processor is for
/// navigation, window selection/linking and option commands whose historical
/// behavior is primarily about translating user-visible values into the modern
/// document/view model.
/// </remarks>
internal static class FirstEdCompatibilityProcessor
{
    /// <summary>
    /// Historical EditBeginningEndLine semantics: from any non-first column go
    /// to column one; from column one go immediately after the last nonblank
    /// character.
    /// </summary>
    public static void MoveBeginningOrEndOfLine(EditorSession session)
    {
        var window = RequireWindow(session);
        if (window.Cursor.Column != 0)
        {
            window.Cursor = window.Cursor with { Column = 0 };
            return;
        }

        MoveEndOfLine(session);
    }

    /// <summary>
    /// Moves immediately after the last nonblank character, matching the
    /// historical EditEndLine command rather than a modern physical-line-end
    /// convention that would include trailing blanks.
    /// </summary>
    public static void MoveEndOfLine(EditorSession session)
    {
        var window = RequireWindow(session);
        var text = window.Document.Buffer.GetLine(window.Cursor.Line).Text;
        window.Cursor = window.Cursor with { Column = LastNonBlankEnd(text) };
    }

    /// <summary>
    /// Moves to a one-based line number. Values below one are rejected; values
    /// beyond the text stream select its last line. The column is deliberately
    /// preserved, including a virtual column beyond a shorter target line.
    /// </summary>
    public static bool GoToLine(EditorSession session, int oneBasedLine)
    {
        if (oneBasedLine < 1)
        {
            return false;
        }

        var window = RequireWindow(session);
        var targetLine = Math.Min(oneBasedLine - 1, window.Document.Buffer.LineCount - 1);
        window.Cursor = new TextPosition(targetLine, window.Cursor.Column);
        return true;
    }

    /// <summary>
    /// Moves to a one-based column number. FIRST-ED permits the cursor to be
    /// positioned beyond the current text, so this command intentionally does
    /// not clamp to the current line length.
    /// </summary>
    public static bool GoToColumn(EditorSession session, int oneBasedColumn)
    {
        if (oneBasedColumn < 1)
        {
            return false;
        }

        var window = RequireWindow(session);
        window.Cursor = window.Cursor with { Column = oneBasedColumn - 1 };
        return true;
    }

    /// <summary>
    /// Moves to the first or last line of the active whole-line block. If the
    /// block belongs to another displayed document, a window showing that
    /// document becomes current while retaining that window's own column.
    /// </summary>
    public static bool GoToBlockBoundary(EditorSession session, bool end)
    {
        var block = session.Block;
        if (block is null)
        {
            return false;
        }

        var window = session.Windows.FirstOrDefault(candidate => candidate.Document.DocumentId == block.DocumentId);
        if (window is null)
        {
            return false;
        }

        session.SetCurrentWindow(window.WindowId);
        window.Cursor = window.Cursor with { Line = end ? block.LastLine : block.FirstLine };
        return true;
    }

    /// <summary>
    /// Selects the window immediately above the current one and wraps from the
    /// first window to the last.
    /// </summary>
    public static bool PreviousWindow(EditorSession session)
    {
        if (session.Windows.Count == 0)
        {
            return false;
        }

        var currentIndex = IndexOfCurrentWindow(session);
        var targetIndex = (currentIndex - 1 + session.Windows.Count) % session.Windows.Count;
        session.SetCurrentWindow(session.Windows[targetIndex].WindowId);
        return true;
    }

    /// <summary>
    /// Selects a one-based window number using modulo addressing, preserving
    /// each window's independent cursor/scroll state.
    /// </summary>
    public static bool GoToWindow(EditorSession session, int oneBasedWindowNumber)
    {
        var window = ResolveWindow(session, oneBasedWindowNumber);
        if (window is null)
        {
            return false;
        }

        session.SetCurrentWindow(window.WindowId);
        return true;
    }

    /// <summary>
    /// Re-links an existing destination window to the source window's document.
    /// The two windows then share text while retaining independent view state.
    /// The abandoned destination document naturally becomes collectible when no
    /// other window references it, replacing the manual memory destruction that
    /// the Pascal implementation required.
    /// </summary>
    public static bool LinkWindows(EditorSession session, int destinationNumber, int sourceNumber)
    {
        var destination = ResolveWindow(session, destinationNumber);
        var source = ResolveWindow(session, sourceNumber);
        if (destination is null || source is null)
        {
            return false;
        }

        if (destination.WindowId == source.WindowId || destination.Document.DocumentId == source.Document.DocumentId)
        {
            return false;
        }

        destination.AttachDocument(source.Document);
        destination.ClampCursor();
        return true;
    }

    /// <summary>
    /// Deletes a numbered window. SASD deliberately keeps at least one window
    /// alive because the modern session API requires a current view.
    /// </summary>
    public static bool DeleteWindow(EditorSession session, int oneBasedWindowNumber)
    {
        if (session.Windows.Count <= 1)
        {
            return false;
        }

        var window = ResolveWindow(session, oneBasedWindowNumber);
        return window is not null && session.CloseWindow(window.WindowId);
    }

    public static bool SetLeftMargin(EditorSession session, int oneBasedColumn)
    {
        if (oneBasedColumn < 1)
        {
            return false;
        }

        var value = oneBasedColumn - 1;
        var options = RequireWindow(session).Options;
        if (value > options.RightMargin)
        {
            return false;
        }

        options.LeftMargin = value;
        return true;
    }

    public static bool SetRightMargin(EditorSession session, int oneBasedColumn)
    {
        if (oneBasedColumn < 1)
        {
            return false;
        }

        var value = oneBasedColumn - 1;
        var options = RequireWindow(session).Options;
        if (value < options.LeftMargin)
        {
            return false;
        }

        options.RightMargin = value;
        return true;
    }

    public static bool SetTabWidth(EditorSession session, int width)
    {
        if (width < 1)
        {
            return false;
        }

        RequireWindow(session).Options.TabSize = width;
        return true;
    }

    public static bool SetUndoLimit(EditorSession session, int limit)
    {
        if (limit < 0)
        {
            return false;
        }

        session.Undo.Limit = limit;
        return true;
    }

    private static int LastNonBlankEnd(string text)
    {
        var index = text.Length - 1;
        while (index >= 0 && char.IsWhiteSpace(text[index]))
        {
            index--;
        }

        return index + 1;
    }

    private static int IndexOfCurrentWindow(EditorSession session)
    {
        for (var index = 0; index < session.Windows.Count; index++)
        {
            if (session.Windows[index].WindowId == session.CurrentWindow.WindowId)
            {
                return index;
            }
        }

        throw new InvalidOperationException("The current window is not part of the editor session.");
    }

    private static EditorWindow? ResolveWindow(EditorSession session, int oneBasedWindowNumber)
    {
        if (session.Windows.Count == 0 || oneBasedWindowNumber < 1)
        {
            return null;
        }

        var index = (oneBasedWindowNumber - 1) % session.Windows.Count;
        return session.Windows[index];
    }

    private static EditorWindow RequireWindow(EditorSession session)
    {
        ArgumentNullException.ThrowIfNull(session);
        return session.Windows.Count > 0
            ? session.CurrentWindow
            : throw new InvalidOperationException("The editor session has no windows.");
    }
}
