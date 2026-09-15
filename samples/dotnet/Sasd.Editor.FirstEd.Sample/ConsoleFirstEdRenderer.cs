using Sasd.Editor.Editing;
using Sasd.Editor.Rendering;

namespace Sasd.Editor.FirstEd.Sample;

/// <summary>
/// Simple terminal renderer for the FIRST-ED reference sample. It intentionally
/// lives outside Sasd.Editor.Core so console concerns never leak into the
/// reusable editor library.
/// </summary>
internal sealed class ConsoleFirstEdRenderer(EditorSession session)
{
    private readonly EditorViewportBuilder _viewportBuilder = new(session);

    public int VisibleTextRows => Math.Max(1, Console.WindowHeight - 2);

    public void Render(string? message = null)
    {
        var width = Math.Max(1, Console.WindowWidth - 1);
        var height = Math.Max(3, Console.WindowHeight);
        var visibleRows = Math.Max(1, height - 2);

        EnsureCursorVisible(visibleRows, width);
        var viewport = _viewportBuilder.Build(visibleRows, width);
        var windowNumber = GetCurrentWindowNumber();

        Console.CursorVisible = false;
        Console.Clear();

        var mode = viewport.Status.InsertMode ? "INS" : "OVR";
        var dirty = viewport.Status.IsDirty ? " *" : string.Empty;
        WriteRow(
            0,
            $"W{windowNumber} {viewport.Status.FileName}  Ln {viewport.Status.Line}  Col {viewport.Status.Column}  {mode}" +
            $"  WW:{OnOff(viewport.Status.WordWrap)} AI:{OnOff(viewport.Status.AutoIndent)}{dirty}",
            width);

        for (var row = 0; row < visibleRows; row++)
        {
            var text = row < viewport.Lines.Count ? viewport.Lines[row].Text : string.Empty;
            WriteRow(row + 1, text, width);
        }

        WriteRow(
            height - 1,
            string.IsNullOrWhiteSpace(message)
                ? "Ctrl-K X Exit | Esc Undo | Ctrl-Q F Find | Ctrl-K S Save | arrows also work"
                : message,
            width);

        PositionCursor(width, visibleRows);
        Console.CursorVisible = true;
    }

    public string? ReadPrompt(string prompt)
    {
        var width = Math.Max(1, Console.WindowWidth - 1);
        var row = Math.Max(0, Console.WindowHeight - 1);
        WriteRow(row, prompt, width);
        var column = Math.Min(prompt.Length, width - 1);
        Console.SetCursorPosition(column, row);
        Console.CursorVisible = true;
        return Console.ReadLine();
    }

    public char? ReadCharacterPrompt(string prompt)
    {
        var width = Math.Max(1, Console.WindowWidth - 1);
        var row = Math.Max(0, Console.WindowHeight - 1);
        WriteRow(row, prompt, width);
        Console.SetCursorPosition(Math.Min(prompt.Length, width - 1), row);
        Console.CursorVisible = true;
        var key = Console.ReadKey(intercept: true);
        return key.KeyChar == '\0' ? null : key.KeyChar;
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

    private void EnsureCursorVisible(int visibleRows, int width)
    {
        var window = session.CurrentWindow;
        if (window.Cursor.Line < window.TopLine)
        {
            window.TopLine = window.Cursor.Line;
        }
        else if (window.Cursor.Line >= window.TopLine + visibleRows)
        {
            window.TopLine = Math.Max(0, window.Cursor.Line - visibleRows + 1);
        }

        if (window.Cursor.Column < window.LeftColumn)
        {
            window.LeftColumn = window.Cursor.Column;
        }
        else if (window.Cursor.Column >= window.LeftColumn + width)
        {
            window.LeftColumn = Math.Max(0, window.Cursor.Column - width + 1);
        }
    }

    private void PositionCursor(int width, int visibleRows)
    {
        var window = session.CurrentWindow;
        var row = 1 + Math.Clamp(window.Cursor.Line - window.TopLine, 0, visibleRows - 1);
        var column = Math.Clamp(window.Cursor.Column - window.LeftColumn, 0, width - 1);
        Console.SetCursorPosition(column, row);
    }

    private int GetCurrentWindowNumber()
    {
        for (var index = 0; index < session.Windows.Count; index++)
        {
            if (session.Windows[index].WindowId == session.CurrentWindow.WindowId)
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
