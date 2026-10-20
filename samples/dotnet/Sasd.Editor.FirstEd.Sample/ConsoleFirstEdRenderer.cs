using Sasd.Editor.Editing;
using Sasd.Editor.Rendering;
using Sasd.Editor.Windows;

namespace Sasd.Editor.FirstEd.Sample;

/// <summary>
/// Terminal renderer for the FIRST-ED reference sample. It renders every
/// displayed window from host-neutral <see cref="EditorWindowFrame"/> geometry;
/// console APIs remain outside the reusable editor core.
/// </summary>
internal sealed class ConsoleFirstEdRenderer(EditorSession session)
{
    private readonly EditorViewportBuilder _viewportBuilder = new(session);

    public int VisibleTextRows
    {
        get
        {
            EnsureLayout();
            return session.WindowLayout.GetFrame(session.CurrentWindow.WindowId)?.TextRows
                   ?? Math.Max(1, Console.WindowHeight - 2);
        }
    }

    public void Render(string? message = null)
    {
        var width = Math.Max(1, Console.WindowWidth - 1);
        var height = Math.Max(3, Console.WindowHeight);

        Console.CursorVisible = false;
        Console.Clear();

        if (!EnsureLayout())
        {
            WriteRow(0, "Terminal is too small for the current FIRST-ED window layout.", width);
            WriteRow(height - 1, "Resize the terminal to at least three rows per editor window.", width);
            Console.CursorVisible = true;
            return;
        }

        // EditHscroll affects all displayed windows, not just the active one.
        // The terminal supplies the current text width; no Console dependency
        // is needed in the reusable Core to enforce horizontal visibility.
        session.EnsureAllWindowCursorsHorizontallyVisible(width);

        foreach (var frame in session.WindowLayout.Frames)
        {
            var window = session.Windows.First(candidate => candidate.WindowId == frame.WindowId);
            EnsureCursorVerticallyVisible(window, frame.TextRows);
            var viewport = _viewportBuilder.Build(window, frame.TextRows, width);
            var windowNumber = GetWindowNumber(window.WindowId);
            var current = window.WindowId == session.CurrentWindow.WindowId;

            var mode = viewport.Status.InsertMode ? "INS" : "OVR";
            var dirty = viewport.Status.IsDirty ? " *" : string.Empty;
            var active = current ? ">" : " ";
            WriteRow(
                frame.TopRow,
                $"{active}W{windowNumber} {viewport.Status.FileName}  Ln {viewport.Status.Line}  Col {viewport.Status.Column}  {mode}" +
                $"  WW:{OnOff(viewport.Status.WordWrap)} AI:{OnOff(viewport.Status.AutoIndent)}{dirty}",
                width);

            for (var row = 0; row < frame.TextRows; row++)
            {
                var text = row < viewport.Lines.Count ? viewport.Lines[row].Text : string.Empty;
                WriteRow(frame.TopRow + EditorWindowFrame.StatusRowCount + row, text, width);
            }
        }

        WriteRow(
            height - 1,
            string.IsNullOrWhiteSpace(message)
                ? "Ctrl-O O New window | Ctrl-O Y Delete | Ctrl-K X Exit | Esc Undo | Ctrl-Q F Find"
                : message,
            width);

        PositionCursor(width);
        Console.CursorVisible = true;
    }

    public string? ReadPrompt(string prompt)
    {
        var width = Math.Max(1, Console.WindowWidth - 1);
        var row = Math.Max(0, Console.WindowHeight - 1);
        // A host-local editor retains the complete response even when the
        // last physical row is too narrow to show the whole prompt. It also
        // makes cancellation distinguishable from a submitted empty string.
        var editor = new ConsolePromptEditor();
        void Paint()
        {
            var display = editor.Project(prompt, width);
            WriteRow(row, display.Text, width);
            Console.SetCursorPosition(display.CaretColumn, row);
        }

        Paint();
        Console.CursorVisible = true;
        // Redirected input remains a separate non-interactive fallback; the
        // executable sample normally rejects redirection at startup.
        if (Console.IsInputRedirected)
        {
            return Console.ReadLine();
        }

        while (true)
        {
            var result = editor.Process(Console.ReadKey(intercept: true));
            if (result == ConsolePromptInputResult.Cancelled)
                return null;
            if (result == ConsolePromptInputResult.Submitted)
                return editor.Answer;

            Paint();
        }
    }

    public char? ReadCharacterPrompt(string prompt)
    {
        var width = Math.Max(1, Console.WindowWidth - 1);
        var row = Math.Max(0, Console.WindowHeight - 1);
        WriteRow(row, prompt, width);
        Console.SetCursorPosition(Math.Min(prompt.Length, width - 1), row);
        Console.CursorVisible = true;
        var key = Console.ReadKey(intercept: true);
        // Escape aborts single-character prompts as well. This is a host
        // action, not a character intended for the editor document.
        return key.Key is ConsoleKey.Escape || key.KeyChar == '\0'
            ? null
            : key.KeyChar;
    }

    public void Shutdown()
    {
        try
        {
            Console.CursorVisible = true;
            Console.Clear();
        }
        catch (IOException)
        {
            // Terminal disappeared while the process was exiting.
        }
    }

    private bool EnsureLayout()
    {
        var workspaceRows = Math.Max(0, Console.WindowHeight - 1);
        return session.WindowLayout.ResizeWorkspace(workspaceRows);
    }

    private static void EnsureCursorVerticallyVisible(EditorWindow window, int visibleRows)
    {
        if (visibleRows < 1)
        {
            return;
        }

        // Vertical placement still uses the host's concrete text-row count.
        // Horizontal placement is now shared by all front ends through Core.
        if (window.Cursor.Line < window.TopLine)
        {
            window.TopLine = window.Cursor.Line;
        }
        else if (window.Cursor.Line - window.TopLine >= visibleRows)
        {
            window.TopLine = Math.Max(0, window.Cursor.Line - visibleRows + 1);
        }
    }

    private void PositionCursor(int width)
    {
        var window = session.CurrentWindow;
        var frame = session.WindowLayout.GetFrame(window.WindowId);
        if (frame is null || frame.TextRows < 1)
        {
            return;
        }

        var visibleRow = Math.Clamp(window.Cursor.Line - window.TopLine, 0, frame.TextRows - 1);
        var row = frame.TopRow + EditorWindowFrame.StatusRowCount + visibleRow;
        var column = Math.Clamp(window.Cursor.Column - window.LeftColumn, 0, width - 1);
        Console.SetCursorPosition(column, Math.Min(row, Console.WindowHeight - 2));
    }

    private int GetWindowNumber(Guid windowId)
    {
        for (var index = 0; index < session.Windows.Count; index++)
        {
            if (session.Windows[index].WindowId == windowId)
            {
                return index + 1;
            }
        }

        return 1;
    }

    private static string OnOff(bool value) => value ? "ON" : "OFF";

    private static void WriteRow(int row, string text, int width)
    {
        if (row < 0 || row >= Console.WindowHeight)
        {
            return;
        }

        var clipped = text.Length > width ? text[..width] : text;
        Console.SetCursorPosition(0, row);
        Console.Write(clipped.PadRight(width));
    }
}
