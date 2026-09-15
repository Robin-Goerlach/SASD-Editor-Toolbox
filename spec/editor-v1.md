# SASD Editor Toolbox - language-neutral V1 contract

This document defines behavior that every language implementation should preserve. Public API names may be idiomatic for the target language; observable editor behavior should remain compatible.

## 1. Text model

- A document contains one or more logical lines.
- A line carries text plus flags for block membership, word-wrap origin and user-defined highlighting.
- A storage implementation must support line read/replace/insert/remove and a stable snapshot operation.
- The storage mechanism is replaceable. The first .NET implementation uses linked logical lines because this closely matches the historical model while keeping later rope/piece-table work possible.

## 2. Document/view separation

- Text belongs to a document.
- A window/view references one document and owns cursor, scroll position and per-view modes.
- Multiple windows may reference the same document; edits are immediately visible in every linked view.
- Views may independently use insert/overtype, word-wrap and auto-indent modes.

## 3. Displayed-window geometry

- Displayed windows have host-neutral vertical frames separate from document/view state.
- A V1 compatibility frame height includes one status row and must be at least three rows total, leaving at least two text rows.
- Create Window receives a requested new-window height and a donor displayed-window number. It rejects a height below three rows or a split that would reduce the donor below three rows.
- A successful Create Window takes the requested rows from the donor's lower displayed area, creates a blank new document/view directly after the donor in displayed-window order, and keeps total allocated rows unchanged.
- If donor compression would leave its cursor below the donor's remaining visible text area, the cursor is moved to the donor's last displayed text line while preserving its column.
- Delete Window rejects deletion of the sole displayed window. Deleting window 1 gives the freed rows to the following window; deleting another window gives the freed rows to the displayed window immediately above it.
- Deleting a window containing the active block clears that block.
- Shared-document lifetime must not depend on explicit pointer/free-list manipulation: deleting one linked view must leave a document alive while another view references it.
- Dynamic host-resize behavior is implementation policy, not historical V1 behavior, and must be documented separately from compatibility Create/Delete rules.

## 4. Editing and viewport primitives

V1 includes character/word/line movement, page movement, beginning/end/top/bottom navigation, text insertion, newline, tab, control-character insertion, left/right deletion, word deletion, line deletion, delete-to-end-of-line, case change, centering and paragraph reformatting.

Display-dependent commands receive the number of visible text rows from the host. Line-up/down and scroll-up/down keep the cursor visible according to the historical edge rules. Page-up/down move the viewport by `visibleRows - 1`. Because the handbook does not separately specify the cursor's post-page screen row, implementations must document their cursor policy; the .NET implementation preserves the relative visible row when possible.

Top-of-file selects the first line and first column. Bottom-of-file selects the last line and first column and places that last line at the top of the viewport.

## 5. Blocks and markers

- The historical V1 compatibility block is whole-line and contiguous.
- One active block may be begun/ended, copied, moved, deleted and hidden.
- Numbered markers 1..20 identify a document and text position and can be jumped to while the document remains open.

## 6. Search and replace

- Literal forward search is required.
- The compatibility search remembers the most recent non-empty pattern for a Find Again operation.
- Find Again resumes after the previous match when the cursor is still positioned on that match.
- FIRST-ED-compatible forward search does not wrap by default. Wrap-around remains an explicit option for hosts that want it.
- Case-sensitive, case-insensitive and whole-word behavior are explicit options.
- Replace-next and replace-all are services above the buffer layer.
- Hosts may intercept a replacement decision.

## 7. Undo and dirty state

- Every text mutation marks the document dirty.
- Successful persistence clears dirty state when the operation is semantically a save.
- A direct compatibility Write File operation does not implicitly change document identity.
- Undo is a replaceable service. Correctness is more important than storage efficiency in the first implementation.

## 8. Command dispatch and input mapping

- Host-specific keyboard events are normalized before compatibility mapping.
- User input is translated to semantic commands outside the editing engine.
- The FIRST-ED compatibility map is stateful because Ctrl-K, Ctrl-O and Ctrl-Q introduce a second keystroke.
- A key map never prompts the user directly. Instead it declares whether a command requires text, a number, two numbers, a character, a file path, find/replace values or confirmation.
- Numbered prefix commands may carry their number directly in the binding.
- A command-filter hook may rewrite a semantic command before dispatch.
- The engine itself must remain callable directly by application code.
- Hosts may add modern aliases (for example arrow keys) without removing the historical command sequences.

## 9. Lifecycle and cooperative scheduling

- The session exposes a rundown state equivalent to the historical `Rundown` variable.
- Direct Exit requests rundown and does not save files.
- Confirmation before interactive exit is a host/prompt responsibility declared by the input binding.
- A scheduler cycle first offers the host one opportunity to process pending input. If input is processed, background work is skipped for that cycle.
- If no input is available, one cooperative background slice runs.
- The system loop repeats scheduler cycles until rundown is requested or external cancellation occurs.
- Background tasks must keep their own resumable state and return after a bounded unit of work.

## 10. Host hooks

Equivalent extension points must exist for command filtering, error handling, status transformation, replace confirmation and cooperative idle/background work.

## 11. Rendering

The core does not write directly to console/video memory. It exposes viewport/status projections for individual windows plus host-neutral window frames. WPF, WinForms, terminal, web and other hosts decide how those projections are drawn.

## 12. Persistence and compatibility file I/O

- Modern document persistence is abstracted. The first .NET `ITextStorage` provider supports UTF-8 files and preserves the detected newline convention for subsequent saves.
- Compatibility file commands use a separate file-codec boundary so historical formats do not become mandatory modern storage formats.
- The Turbo Editor Toolbox wrapped-line marker is a high-bit carriage return (`0x8D`, decimal 141). Decoding it marks the preceding logical line as wrapped; encoding a wrapped separator emits it again.
- The FIRST-ED-compatible read operation inserts decoded lines after the current line and preserves the current cursor position.
- Hosts collect filenames; core file services receive resolved paths and perform no UI prompting.

## 13. Compatibility policy

The Borland handbook is a requirements/reference source only. Implementations must be independently written and must not copy historical source code.
