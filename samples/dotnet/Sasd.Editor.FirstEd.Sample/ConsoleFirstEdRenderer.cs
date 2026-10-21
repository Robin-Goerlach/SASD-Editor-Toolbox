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

    /// <summary>
    /// FIRST-ED may display hard/soft line-end and end-of-file indicators in
    /// one right-edge column (handbook p. 69). MicroStar deliberately hides
    /// these indicators. This switch belongs to the sample host, not Core.
    /// </summary>
    internal bool ShowRightEdgeMarkers { get; set; } = true;

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
        // Reserve one safe cell for a historical line-end indicator when
        // enabled. The terminal's final physical column remains untouched to
        // avoid implicit line wrapping on consoles with different VT behavior.
        var textWidth = ShowRightEdgeMarkers && width > 1 ? width - 1 : width;
        session.EnsureAllWindowCursorsHorizontallyVisible(textWidth);

        foreach (var frame in session.WindowLayout.Frames)
        {
            var window = session.Windows.First(candidate => candidate.WindowId == frame.WindowId);
            EnsureCursorVerticallyVisible(window, frame.TextRows);
            var viewport = _viewportBuilder.Build(window, frame.TextRows, textWidth);
            var windowNumber = GetWindowNumber(window.WindowId);
            var current = window.WindowId == session.CurrentWindow.WindowId;

            var mode = viewport.Status.InsertMode ? "INS" : "OVR";
            var dirty = viewport.Status.IsDirty ? " *" : string.Empty;
            var active = current ? ">" : " ";
            WriteRow(
                frame.TopRow,
                $"{active}W{windowNumber} {viewport.Status.FileName}  Ln {viewport.Status.Line}  Col {viewport.Status.Column}  {mode}" +
                $"  WW:{OnOff(viewport.Status.WordWrap)} AI:{OnOff(viewport.Status.AutoIndent)}{dirty}",
                width,
                role: FirstEdColorRole.Status);

            for (var row = 0; row < frame.TextRows; row++)
            {
                // ViewportLine supplies the logical line *identity*. Its
                // generic UTF-16 substring is intentionally not used for
                // physical console painting: a substring could start/end
                // inside a surrogate pair, and document text may contain
                // terminal control bytes inserted via historical Ctrl-P.
                //
                // Render from the complete stored logical line, applying
                // the independent host-only text-element and control-code
                // projection at the window's horizontal offset.
                var hasLine = row < viewport.Lines.Count;
                var text = hasLine
                    ? window.Document.Buffer.GetLine(viewport.Lines[row].DocumentLine).Text
                    : string.Empty;
                var flags = hasLine ? viewport.Lines[row].Flags : Model.EditorLineFlags.None;

                // Colored is dominant over InBlock in the handbook: the
                // user's special-color flag overrides block/ordinary text.
                // InBlock is already cleared by Core when the block is hidden.
                // Neither selection nor the outgoing Wrapped flag is mutated
                // by this purely physical drawing step.
                var role = ConsoleFirstEdPresentationPolicy.TextRole(flags);
                var marker = ConsoleFirstEdPresentationPolicy.RightEdgeMarker(
                    ShowRightEdgeMarkers && width > 1, hasLine, flags);
                WriteRow(
                    frame.TopRow + EditorWindowFrame.StatusRowCount + row,
                    text, width, window.LeftColumn, role, marker);
            }
        }

        WriteRow(
            height - 1,
            string.IsNullOrWhiteSpace(message)
                ? "Ctrl-O O New window | Ctrl-O Y Delete | Ctrl-K X Exit | Esc Undo | Ctrl-Q F Find"
                : message,
            width,
            role: FirstEdColorRole.Command);

        PositionCursor(textWidth);
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
            // The prompt model uses UTF-16 offsets, but the physical
            // terminal caret needs the measured display-cell column.
            // For example, "界" occupies approximately two cells while
            // "e" with a combining accent occupies one.
            var cellCaret = ConsoleFirstEdRowProjection.Project(
                display.Text, 0, width, display.CaretColumn).CursorColumn;
            Console.SetCursorPosition(cellCaret, row);
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
        var cellCaret = ConsoleFirstEdRowProjection.Project(
            prompt, 0, width, prompt.Length).CursorColumn;
        Console.SetCursorPosition(cellCaret, row);
        Console.CursorVisible = true;
        var key = Console.ReadKey(intercept: true);

        // The historical EditInsertCtrlChar (Ctrl-P) accepts a key and then
        // converts it into an ASCII control byte before text insertion.
        // ConsoleKeyInfo is deliberately interpreted by a pure sample-host
        // policy: the renderer reads, the policy normalizes, and the core
        // receives only the already-resolved semantic control character.
        // Escape/Ctrl-U and unsupported input cancel without dispatch.
        return ConsoleFirstEdControlCharacterPromptPolicy.Resolve(key);
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
        // Cursor.Column and LeftColumn are logical UTF-16 offsets. Never
        // subtract them directly to obtain a *physical* terminal column:
        // a stored ESC shows as "^[", an emoji may occupy two cells, and
        // combining accents must not advance the terminal cursor twice.
        var line = window.Document.Buffer.GetLine(window.Cursor.Line).Text;
        var column = ConsoleFirstEdRowProjection.Project(
            line, Math.Max(0, window.LeftColumn), width,
            Math.Max(0, window.Cursor.Column)).CursorColumn;
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

    private static void WriteRow(
        int row, string text, int width, int firstCodeUnit = 0,
        FirstEdColorRole role = FirstEdColorRole.Text, char? rightEdgeMarker = null)
    {
        if (row < 0 || row >= Console.WindowHeight)
        {
            return;
        }

        // This is the SINGLE boundary before emitting untrusted document,
        // filename, hook/status and prompt text to the terminal. Raw ESC,
        // CR, LF and other controls must never reach Console.Write:
        // otherwise editing text such as ESC[2J could clear the display.
        //
        // The projection expands controls into caret notation, clips at
        // complete Unicode text elements, estimates terminal cell widths
        // and pads the row to erase stale characters from prior frames.
        // The last real terminal column is reserved by the caller.
        // A marker consumes its own physical cell; it is not part of the
        // stored line, and its special glyph must never affect document text,
        // Unicode grapheme clipping, horizontal scrolling or cursor offsets.
        var markerCells = rightEdgeMarker.HasValue && width > 1 ? 1 : 0;
        var display = ConsoleFirstEdRowProjection.Project(
            text, Math.Max(0, firstCodeUnit), width - markerCells);
        Console.SetCursorPosition(0, row);

        // Restore the caller's terminal colors after EACH row, including
        // exceptions during output. Core exposes semantic flags only; the
        // five historic roles are represented by host-local theme colors.
        var previousForeground = Console.ForegroundColor;
        var previousBackground = Console.BackgroundColor;
        try
        {
            var textColors = ConsoleFirstEdPresentationPolicy.Colors(role);
            Console.ForegroundColor = textColors.Foreground;
            Console.BackgroundColor = textColors.Background;
            Console.Write(display.Text);

            if (markerCells != 0)
            {
                var markerColors = ConsoleFirstEdPresentationPolicy.Colors(
                    FirstEdColorRole.Status);
                Console.ForegroundColor = markerColors.Foreground;
                Console.BackgroundColor = markerColors.Background;
                Console.Write(rightEdgeMarker!.Value);
            }
        }
        finally
        {
            Console.ForegroundColor = previousForeground;
            Console.BackgroundColor = previousBackground;
        }
    }
}
