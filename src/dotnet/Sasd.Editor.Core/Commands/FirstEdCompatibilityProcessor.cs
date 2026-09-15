using Sasd.Editor.Editing;
using Sasd.Editor.Model;
using Sasd.Editor.Windows;

namespace Sasd.Editor.Commands;

/// <summary>
/// Preserves FIRST-ED's user-visible command semantics without leaking 1985
/// screen, pointer and one-based conventions into the general editing engine.
/// </summary>
internal static class FirstEdCompatibilityProcessor
{
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

    public static void MoveEndOfLine(EditorSession session)
    {
        var window = RequireWindow(session);
        var text = window.Document.Buffer.GetLine(window.Cursor.Line).Text;
        window.Cursor = window.Cursor with { Column = LastNonBlankEnd(text) };
    }

    /// <summary>
    /// FIRST-ED EditUpLine behavior. The cursor moves one logical line upward;
    /// when it was already on the top displayed row, the viewport scrolls up as
    /// well so the cursor remains visible.
    /// </summary>
    public static bool MoveUpLine(EditorSession session, int visibleLines)
    {
        ValidateVisibleLines(visibleLines);
        var window = RequireWindow(session);
        if (window.Cursor.Line <= 0)
        {
            return false;
        }

        if (window.Cursor.Line == window.TopLine)
        {
            window.TopLine = Math.Max(0, window.TopLine - 1);
        }

        window.Cursor = window.Cursor with { Line = window.Cursor.Line - 1 };
        return true;
    }

    /// <summary>
    /// FIRST-ED EditDownLine behavior. If the cursor is on the last displayed
    /// row, the viewport follows it downward by one line.
    /// </summary>
    public static bool MoveDownLine(EditorSession session, int visibleLines)
    {
        ValidateVisibleLines(visibleLines);
        var window = RequireWindow(session);
        var lastDocumentLine = window.Document.Buffer.LineCount - 1;
        if (window.Cursor.Line >= lastDocumentLine)
        {
            return false;
        }

        var lastDisplayedLine = Math.Min(lastDocumentLine, window.TopLine + visibleLines - 1);
        if (window.Cursor.Line == lastDisplayedLine)
        {
            window.TopLine = Math.Min(lastDocumentLine, window.TopLine + 1);
        }

        window.Cursor = window.Cursor with { Line = window.Cursor.Line + 1 };
        return true;
    }

    /// <summary>
    /// Slides the viewport up by one logical line. If the cursor occupied the
    /// last displayed row, it also moves up by one line as described by the
    /// historical EditScrollUp command.
    /// </summary>
    public static bool ScrollUp(EditorSession session, int visibleLines)
    {
        ValidateVisibleLines(visibleLines);
        var window = RequireWindow(session);
        if (window.TopLine <= 0)
        {
            return false;
        }

        var lastDocumentLine = window.Document.Buffer.LineCount - 1;
        var lastDisplayedLine = Math.Min(lastDocumentLine, window.TopLine + visibleLines - 1);
        if (window.Cursor.Line == lastDisplayedLine)
        {
            window.Cursor = window.Cursor with { Line = Math.Max(0, window.Cursor.Line - 1) };
        }

        window.TopLine--;
        return true;
    }

    /// <summary>
    /// Slides the viewport down by one logical line. If the cursor occupied the
    /// top displayed row, it also moves down by one line as described by the
    /// historical EditScrollDown command.
    /// </summary>
    public static bool ScrollDown(EditorSession session, int visibleLines)
    {
        ValidateVisibleLines(visibleLines);
        var window = RequireWindow(session);
        var lastDocumentLine = window.Document.Buffer.LineCount - 1;
        if (window.TopLine >= lastDocumentLine)
        {
            return false;
        }

        if (window.Cursor.Line == window.TopLine)
        {
            window.Cursor = window.Cursor with { Line = Math.Min(lastDocumentLine, window.Cursor.Line + 1) };
        }

        window.TopLine++;
        return true;
    }

    /// <summary>
    /// Slides the viewport upward by one page, where a page is one less than the
    /// number of displayed text rows. The handbook specifies the viewport
    /// displacement but not a separate cursor-row rule; SASD therefore preserves
    /// the cursor's relative visible row as a host-neutral invariant.
    /// </summary>
    public static bool PageUp(EditorSession session, int visibleLines)
    {
        ValidateVisibleLines(visibleLines);
        var window = RequireWindow(session);
        if (window.TopLine <= 0)
        {
            return false;
        }

        var relativeRow = VisibleCursorRow(window, visibleLines);
        var step = Math.Max(1, visibleLines - 1);
        window.TopLine = Math.Max(0, window.TopLine - step);
        var lastDocumentLine = window.Document.Buffer.LineCount - 1;
        window.Cursor = window.Cursor with { Line = Math.Min(lastDocumentLine, window.TopLine + relativeRow) };
        return true;
    }

    /// <summary>
    /// Slides the viewport downward by one page, where a page is one less than
    /// the number of displayed text rows. The cursor keeps its relative visible
    /// row unless the end of the document forces clamping.
    /// </summary>
    public static bool PageDown(EditorSession session, int visibleLines)
    {
        ValidateVisibleLines(visibleLines);
        var window = RequireWindow(session);
        var lastDocumentLine = window.Document.Buffer.LineCount - 1;
        if (window.TopLine >= lastDocumentLine)
        {
            return false;
        }

        var relativeRow = VisibleCursorRow(window, visibleLines);
        var step = Math.Max(1, visibleLines - 1);
        window.TopLine = Math.Min(lastDocumentLine, window.TopLine + step);
        window.Cursor = window.Cursor with { Line = Math.Min(lastDocumentLine, window.TopLine + relativeRow) };
        return true;
    }

    /// <summary>
    /// Historical EditWindowTopFile semantics: first text line, first column,
    /// with the first text line at the top of the viewport.
    /// </summary>
    public static void MoveWindowTopFile(EditorSession session)
    {
        var window = RequireWindow(session);
        window.TopLine = 0;
        window.Cursor = new TextPosition(0, 0);
    }

    /// <summary>
    /// Historical EditWindowBottomFile semantics: last text line, first column,
    /// and the last line itself becomes the viewport's top line.
    /// </summary>
    public static void MoveWindowBottomFile(EditorSession session)
    {
        var window = RequireWindow(session);
        var lastLine = window.Document.Buffer.LineCount - 1;
        window.TopLine = lastLine;
        window.Cursor = new TextPosition(lastLine, 0);
    }

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

    private static int VisibleCursorRow(EditorWindow window, int visibleLines) =>
        Math.Clamp(window.Cursor.Line - window.TopLine, 0, visibleLines - 1);

    private static void ValidateVisibleLines(int visibleLines)
    {
        if (visibleLines < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(visibleLines), "At least one visible text line is required.");
        }
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
