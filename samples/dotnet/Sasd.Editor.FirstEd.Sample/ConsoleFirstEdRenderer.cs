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
    private readonly ConsoleMicroStarOverlayStack _overlays = new();

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
                ? "F10 Menu | Ctrl-O O New window | Ctrl-K X Exit | Esc Undo | Ctrl-Q F Find"
                : message,
            width,
            role: FirstEdColorRole.Command);

        // Recompose nested overlays from the model rather than terminal cells.
        foreach (var overlay in _overlays.Active)
            DrawMicroStarPopup(overlay);
        if (_overlays.Count == 0)
            PositionCursor(textWidth);
        Console.CursorVisible = _overlays.Count == 0;
    }

    /// <summary>
    /// Displays a MicroStar-style application menu in the existing command
    /// area. This FIRST slice intentionally reuses the safe prompt renderer:
    /// actual overlapping screen popups (MakeWindow/RestoreWindow) are a
    /// separate historical feature and are not claimed by this method.
    /// </summary>
    public string? ReadMicroStarMenu()
    {
        // This overlay is temporary terminal presentation, not an editor
        // window. Do NOT insert it into WindowLayout: that would change
        // document view geometry, cursor anchoring and historical commands.
        var layout = ConsoleMicroStarPopupLayout.Create(
            Console.WindowWidth, Console.WindowHeight,
            ConsoleMicroStarPopupLayout.MenuLines);

        // With a very small terminal there is no safe rectangle. Preserve
        // the previous command-line fallback rather than writing off-screen.
        if (layout is null)
            return ReadPrompt(ConsoleMicroStarMenuPolicy.Prompt);

        // Navigation state is independent of physical Console APIs. The host
        // translates the keyboard into a pure policy result; no text mutation
        // or editor command occurs until a selection is explicitly accepted.
        var navigation = new ConsoleMicroStarMenuNavigation();
        using var lease = _overlays.Push(layout);
        try
        {
            Render();
            DrawMicroStarPopup(layout, navigation.SelectedChoice);
            while (true)
            {
                var key = Console.ReadKey(intercept: true);
                var result = navigation.Handle(key.Key, key.KeyChar, key.Modifiers);
                if (result.Kind == MicroStarMenuResultKind.Cancel)
                    return null;
                if (result.Kind == MicroStarMenuResultKind.Accept)
                    return result.Choice.ToString();

                // Only selection changes require repainting. The underlying
                // editor remains the source of truth and is never edited by
                // navigating a menu.
                if (result.Kind == MicroStarMenuResultKind.SelectionChanged)
                    DrawMicroStarPopup(layout, navigation.SelectedChoice);
            }
        }
        finally
        {
            // Pop first; repainting then restores the parent, if any.
            lease.Dispose();
            Render();
        }
    }

    private static void DrawMicroStarPopup(
        ConsoleMicroStarPopupLayout layout, char? selected = null)
    {
        for (var row = 0; row < layout.Height; row++)
        {
            var line = layout.GetRow(row, selected);
            // Reuse WriteRow's Unicode/control-code sanitization. Draw only
            // a bounded horizontal span and preserve cells on either side.
            // Popup rows intentionally consist of safe printable ASCII.
            Console.SetCursorPosition(layout.Left, layout.Top + row);
            Console.Write(line);
        }

        Console.SetCursorPosition(layout.Left + 2, layout.Top + 1);
    }

    /// <summary>
    /// An actual modal Yes/No dialog built upon the reusable focus policy
    /// and overlay stack. It is deliberately conservative: the initial
    /// focused action is NO, so an unintended Enter cannot overwrite a file.
    /// This is a host concern; no disk operation occurs inside this method.
    /// </summary>
    public bool ConfirmMicroStarOverwrite()
    {
        const string question = "Overwrite existing block file?";
        var contents = new[]
        {
            question,
            "N No       Y Yes",
            "Tab / arrows to choose",
            "Enter confirms, ESC cancels"
        };
        var layout = ConsoleMicroStarPopupLayout.Create(
            Console.WindowWidth, Console.WindowHeight, contents);

        // Tiny terminals cannot display the popup while preserving the
        // editor's top/bottom rows. Keep an explicit typed confirmation,
        // never assume confirmation when the window is too small.
        if (layout is null)
            return ReadPrompt("File exists. Type YES to overwrite: ") == "YES";

        var focus = new MicroStarDialogFocus(
        [
            new("no", MicroStarDialogControlKind.Action),
            new("yes", MicroStarDialogControlKind.Action)
        ]);

        using var lease = _overlays.Push(layout);
        try
        {
            Render();
            DrawMicroStarPopup(layout, focus.FocusedId == "no" ? 'N' : 'Y');
            while (true)
            {
                var key = Console.ReadKey(intercept: true);
                // The direct Y/N shortcuts are deliberately an explicit
                // action. Modified combinations must not bypass confirmation.
                if ((key.Modifiers & (ConsoleModifiers.Control | ConsoleModifiers.Alt)) == 0)
                {
                    if (char.ToUpperInvariant(key.KeyChar) == 'Y') return true;
                    if (char.ToUpperInvariant(key.KeyChar) == 'N') return false;
                }

                var result = focus.Handle(key.Key, key.Modifiers);
                if (result == MicroStarDialogNavigationResult.Cancel)
                    return false;
                if (result == MicroStarDialogNavigationResult.Activate)
                    return focus.FocusedId == "yes";
                if (result == MicroStarDialogNavigationResult.FocusChanged)
                    DrawMicroStarPopup(layout, focus.FocusedId == "no" ? 'N' : 'Y');
            }
        }
        finally
        {
            // Always pop before rebuilding the screen. If this dialog was
            // nested, repaint re-exposes its parent rather than erasing it.
            lease.Dispose();
            Render();
        }
    }

    /// <summary>
    /// Interactive, read-only file selection for MicroStar Read Block.
    /// Filesystem discovery and navigation stay in MicroStarFileBrowser;
    /// this method only paints a bounded overlay and normalizes keys.
    /// </summary>
    public string? BrowseMicroStarFile(string startDirectory)
    {
        // One row for the folder title, one for help and up to eight entries.
        // The popup layout reserves a frame and avoids the terminal's last
        // physical column. Small consoles fall back to the existing prompt.
        const int visibleRows = 8;
        if (Console.WindowWidth < 32 || Console.WindowHeight < 12)
            return ReadPrompt("Read block file path: ");

        var browser = new MicroStarFileBrowser(startDirectory, visibleRows);
        while (true)
        {
            // Project the current directory and selected page afresh. Never
            // use a filename as a terminal command or as unvalidated markup.
            // Limit very long captions; the reusable row projection handles
            // Unicode/control-character sanitization before Console.Write.
            var content = new List<string> { "MICROSTAR FILE BROWSER" };
            var folder = browser.CurrentDirectory;
            content.Add(MicroStarBrowserDisplay.SafeLabel(folder, 50));
            var visible = browser.VisibleEntries;
            var index = 0;
            foreach (var entry in visible)
            {
                var selected = browser.Selection.Selected?.Id == entry.FullPath;
                // Reuse the directory catalog's caption policy. In particular,
                // directory symlinks must show [LNK], not [DIR], because
                // the browser deliberately refuses to enter linked targets.
                var label = MicroStarDirectoryCatalog.Caption(entry);
                label = MicroStarBrowserDisplay.SafeLabel(label, 50);
                content.Add((selected ? "> " : "  ") + label);
                index++;
            }
            if (index == 0) content.Add("  (Empty directory)");
            content.Add("Enter=Open  F5=Copy  F6=Rename  F8=Delete  Backspace=Up  Esc=Cancel");
            var layout = ConsoleMicroStarPopupLayout.Create(
                Console.WindowWidth, Console.WindowHeight, content);

            // If a resize made the terminal too small, require an explicit
            // typed path; do not attempt to draw outside the screen bounds.
            if (layout is null)
                return ReadPrompt("Read block file path: ");

            using var lease = _overlays.Push(layout);
            ConsoleKeyInfo key;
            try
            {
                Render();
                DrawMicroStarPopup(layout);
                key = Console.ReadKey(intercept: true);
            }
            finally
            {
                lease.Dispose();
                Render();
            }

            // F5/F6 are browser-only file actions. Do not feed them to
            // the normal editor key map or interpret directory captions
            // as paths. The operation is performed only after explicitly
            // entering a new target name; existing destinations are refused
            // by the filesystem service itself.
            var fileAction = MicroStarFileActionPolicy.Resolve(key.Key,
                key.Modifiers, browser.SelectedEntry);
            if (fileAction.HasValue)
            {
                // Permanent deletion is separately approved, never inferred
                // from F8 alone. The user must type the exact affirmative
                // token; Enter, Escape, and any other response deny it.
                if (fileAction.Value == MicroStarFileAction.Delete)
                {
                    var source = browser.SelectedEntry!.FullPath;
                    var name = MicroStarBrowserDisplay.SafeLabel(
                        Path.GetFileName(source), 35);
                    var answer = ReadPrompt(
                        $"PERMANENTLY delete {name}? Type DELETE: ");
                    if (answer != "DELETE")
                        continue;

                    try
                    {
                        MicroStarFileOperations.Delete(source);
                        browser.Refresh();
                    }
                    catch (IOException error)
                    {
                        ReadPrompt("Delete failed: " +
                            MicroStarBrowserDisplay.SafeLabel(error.Message, 50) +
                            "  Press Enter: ");
                    }
                    catch (UnauthorizedAccessException)
                    {
                        ReadPrompt("Access denied. Press Enter: ");
                    }
                    continue;
                }

                var destinationText = ReadPrompt(
                    fileAction.Value == MicroStarFileAction.Copy
                        ? "Copy to new file path: "
                        : "Rename to new file path: ");
                if (string.IsNullOrWhiteSpace(destinationText))
                    continue;

                var destination = MicroStarFileActionPolicy.ResolveDestination(
                    browser.CurrentDirectory, destinationText);
                try
                {
                    var source = browser.SelectedEntry!.FullPath;
                    if (fileAction.Value == MicroStarFileAction.Copy)
                        MicroStarFileOperations.Copy(source, destination);
                    else
                        MicroStarFileOperations.Rename(source, destination);
                    browser.Refresh();
                }
                catch (IOException error)
                {
                    // A failed operation must be visible instead of silently
                    // looking successful. Never offer to overwrite on error.
                    ReadPrompt("File operation failed: " +
                        MicroStarBrowserDisplay.SafeLabel(error.Message, 50) +
                        "  Press Enter: ");
                }
                catch (UnauthorizedAccessException)
                {
                    ReadPrompt("Access denied. Press Enter: ");
                }
                continue;
            }

            // A failed directory transition propagates as an error; no
            // editor text or current-document association has changed.
            var result = browser.Handle(key.Key, key.Modifiers);
            if (result.Kind == MicroStarBrowserResultKind.FileChosen)
                return result.Path;
            if (result.Kind == MicroStarBrowserResultKind.Cancelled)
                return null;
        }
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
