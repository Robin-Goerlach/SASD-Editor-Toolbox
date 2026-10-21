# Interactive FIRST-ED terminal host

## Purpose

The original FIRST-ED program is intentionally tiny because it initializes the Toolbox and then hands control to the Toolbox editor loop. The handbook describes an initial `NONAME` document in Window 1, a status line containing file name plus line/column, WordStar-like control commands, Escape as Undo, and Ctrl-K X as the exit path.

The .NET sample now follows the same architectural idea without importing DOS terminal assumptions into `Sasd.Editor.Core`.

## Host pipeline

```text
ConsoleKeyInfo
    -> ConsoleKeyTranslator
    -> EditorKeyStroke
    -> FirstEdKeyMap
    -> EditorInputAction / EditorCommandBinding
    -> ConsoleFirstEdPromptService (when required)
    -> EditorCommandDispatcher
    -> EditorSession / services
    -> EditorViewportBuilder
    -> ConsoleFirstEdRenderer
```

`ConsoleFirstEdHost` implements `IEditorInputPump`, so `EditorSystemLoop` remains the owner of the main loop. The terminal adapter only supplies one input unit at a time. When no key is waiting, it returns control to the cooperative scheduler.

## Prompt ownership

Prompts are deliberately a host concern. The core key map declares `Number`, `TwoNumbers`, `Text`, `FindReplace`, `Character`, `FilePath` or `Confirmation`; the terminal host turns that metadata into `Console.ReadLine` / `Console.ReadKey` interactions.

For Ctrl-K X, the host requires the word `YES` before dispatching `EditorCommandId.Exit`. This mirrors the handbook's `EditCpExit`/`EditExit` separation while keeping UI code out of the core.

A blank Find prompt is translated to `FindAgain`, preserving the remembered-search behavior already implemented in the core.

**Ctrl-K S (Save File)** is a conditional terminal-host workflow. For a freshly created `NONAME` document with no associated path, the host asks for **Save as file path**; Escape/Ctrl-U or an empty/blank answer cancels without dispatch or disk I/O. Once a Save succeeds and the document has a filename, the same shortcut saves directly without prompting again. This choice is per *document*, not a global last-used path. The small `ConsoleFirstEdSavePromptPolicy` reuses the shared host-neutral filename resolver and has headless regression tests exercising real semantic Save dispatch through a recording file codec. This is an explicitly documented **modern host policy**, not a claim about the exact 1985 `EditCpFileSave` dialog. The shared editor core, direct Write File semantics and Unicode storage policy are unchanged.

The interactive line prompt now uses a small, testable **sample-host input model**. Escape and physical Ctrl-U cancel without constructing a command; Enter submits even an empty response. When a prompt or answer is wider than the terminal row, the display follows the **rightmost characters**, while the full untruncated value is passed to the host-neutral command resolver. This is a modern terminal usability and cancellation policy, not a claim about DOS screen routines. Pure input/projection tests do not replace manual terminal smoke testing.

## Status and rendering

On startup the sample creates one empty unnamed document, so the status line begins with Window 1 / `NONAME`. The renderer shows one-based line and column values and current insert, word-wrap, auto-indent and dirty state.

Modern arrow, Home, End, Page Up and Page Down keys remain aliases because the core input model already supports them. Historical Ctrl-K / Ctrl-O / Ctrl-Q prefix sequences remain available.

## Terminal limitations

Console and terminal APIs do not expose every control-key combination identically on every operating system or terminal emulator. The adapter therefore normalizes what the host actually reports and keeps the core independent of those differences. On platforms that allow it, Ctrl-C is configured as ordinary input so the historical mapping can receive it.

## Current stacked-window rendering

The sample now renders every stacked `EditorWindowFrame`, including its own status row and text rows. The active window owns the physical terminal cursor; the core's `EditorSession.EnsureAllWindowCursorsHorizontallyVisible(width)` keeps the cursor visible **in every view** before rendering, not only the active view.

This arrangement keeps both the historical all-window horizontal policy and per-window line-number status projections in the reusable editor model. Modern terminal resizing and platform-specific key delivery remain host policies; MicroStar pull-down menus and pop-ups remain later host samples.

## Overwrite confirmation for host-selected output filenames

When Ctrl-K S first saves an unnamed `NONAME` document or Ctrl-K W exports text to a **filename newly selected by the user**, the terminal host now probes whether that target already exists. If it does, the host requires a separate `YES` confirmation before it constructs a dispatchable write request. Escape, Ctrl-U, an empty answer, and any other response cancel before any editor command or file I/O runs. The shared prompt resolver supplies the existing YES semantics, while a small console-sample-only `ConsoleFirstEdOverwritePolicy` preserves the original filename and page-size parameters after approval. This policy is covered by headless regression tests with real temporary files and actual semantic dispatch.

A repeated Save to a document's **already associated filename** remains direct; the user has already selected that file. The direct Write File command still does not associate the document with its exported filename or clear the Dirty flag. Read File never asks for overwrite approval. The reusable editor core and its historical file-command semantics remain unchanged.

This is a **modern terminal usability guard**, not a historical 1985 DOS claim, not a replacement for file permissions, and not an atomic guarantee: another process may create or change a file after the host's `File.Exists` probe. Other hosts must choose their own overwrite-confirmation policies.

## Historical empty Set Tab Width prompt (Ctrl-K T)

The handbook's `EditCptabdef` wrapper has a useful exception to ordinary numeric prompting: deliberately submitting an **empty** tab-width answer derives the interval from the current displayed cursor column minus one. The FIRST-ED console host now implements this in the small, headless-testable `ConsoleFirstEdTabWidthPromptPolicy`. Our model's zero-based cursor column is already the required value. Null/Escape remains **cancellation**, while a supplied integer still uses the shared numeric resolver. At the first cursor column the derived interval would be zero: the host declines to dispatch the invalid value, preserving the current tab width.

This is a historical host-prompt behavior with a documented modern boundary. The original editor's Tabsize was global; SASD deliberately keeps per-window `TabSize`. The reusable command dispatcher and all other numeric prompts are unchanged. Automated tests do not certify the visual appearance or key delivery of a real DOS terminal.


## Ctrl-P / historical control-character prompt

The 1985 handbook (EditInsertCtrlChar, printed p. 182) specifies **one following key translated to an ASCII control code**, rather than inserting the ordinary printable key. The terminal host previously passed `A` to the semantic insert command as `A`, instead of ASCII 0x01. The new, independently testable `ConsoleFirstEdControlCharacterPromptPolicy` now translates ASCII A-Z/a-z into 0x01..0x1A and `@ [ \\ ] ^ _` into 0x00 and 0x1B..0x1F. Physical Ctrl+letter key events already containing a control character remain literal. An ordinary U requests 0x15; actual Ctrl-U or Escape cancels. Unsupported keys, non-ASCII characters, Enter and Backspace also cancel instead of inserting ordinary text.

The renderer collects one ConsoleKeyInfo, the policy translates it, and the prompt resolver/dispatcher only see the semantic control code. The prompt now displays the supported alphabet. Headless regression tests cover mappings, cancellation, invalid input, actual dispatcher insertion and Undo. The already linked portable kernel test covers insertion of a **resolved** control character, not physical terminal handling. No DOS scan-code or typeahead timing equivalence is claimed; real Windows/Linux smoke tests remain necessary.


## Command-line input hardening and historical Askfor differences

Host-only `ConsolePromptKeyPolicy` now recognizes Escape and Ctrl-U even when a terminal reports **only raw control bytes 0x1B/0x15**, with no named `ConsoleKey`. Text prompts and single-character Ctrl-P prompts share this decision. In addition, Backspace and narrow right-scrolled prompt projections use full Unicode text elements, so a supplementary character or combining accent is neither deleted nor displayed in fragments. The semantic answer remains complete, even when its projection does not fit the last row. Terminal-specific wide-glyph **cell** widths and physical key delivery still require real-world smoke tests.

The 1985 `EditAskfor` (printed p. 122) differs intentionally: original Ctrl-U recalled prior `Varstring` data, it stopped accepting input at the command-row limit, displayed ASCII controls as caret notation and used the typeahead buffer when `Interactive=false`. The modern sample instead uses **cancel-on-Ctrl-U**, safe untruncated input with horizontal projection, and direct interactive `Console.ReadKey`. Those differences are documented rather than silently presented as historic parity. See `docs/en/HOST-COMMAND-LINE-V1-AUDIT.md`.
