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

## 3. Displayed-window geometry and destructive stream reset

- Displayed windows have host-neutral vertical frames separate from document/view state.
- A V1 compatibility frame height includes one status row and must be at least three rows total, leaving at least two text rows.
- Create Window receives a requested new-window height and a donor displayed-window number. It rejects a height below three rows or a split that would reduce the donor below three rows.
- A successful Create Window takes the requested rows from the donor's lower displayed area, creates a blank new document/view directly after the donor in displayed-window order, and keeps total allocated rows unchanged.
- If donor compression would leave its cursor below the donor's remaining visible text area, the cursor is moved to the donor's last displayed text line while preserving its column.
- Delete Window rejects deletion of the sole displayed window. Deleting window 1 gives the freed rows to the following window; deleting another window gives the freed rows to the displayed window immediately above it.
- Deleting a window containing the active block clears that block.
- Shared-document lifetime must not depend on explicit pointer/free-list manipulation: deleting one linked view must leave a document alive while another view references it.
- Delete Window Text is deliberately destructive and non-undoable. It removes the current stream, resets affected views to blank `NONAME` documents, and destroys links between views that had shared that stream.
- A block belonging to a destructively removed stream is cleared. Implementations must also invalidate any modern navigation state that would otherwise point at a document identity no longer reachable by a view.
- Dynamic host-resize behavior is implementation policy, not historical V1 behavior, and must be documented separately from compatibility Create/Delete rules.

## 4. Editing and viewport primitives

V1 includes character/word/line movement, page movement, beginning/end/top/bottom navigation, text insertion, newline, tab, control-character insertion, left/right deletion, word deletion, line deletion, delete-to-end-of-line, case change, centering and paragraph reformatting.

- Character-left from the first column of a non-first logical line moves to the previous line immediately after its last non-blank character. At the beginning of the text stream it does nothing.
- Character-right increments the logical column without crossing to the next logical line. A compatibility implementation must therefore support a virtual cursor column beyond the current line length.
- Tab advances to the next configured tab stop. In Insert mode the padding is inserted into the document; in Overtype mode only the cursor moves.
- Display-dependent commands receive the number of visible text rows from the host. Line-up/down and scroll-up/down keep the cursor visible according to the historical edge rules.
- Page-up/down move the viewport by `visibleRows - 1`. Because the handbook does not separately specify the cursor's post-page screen row, implementations must document their cursor policy; the .NET implementation preserves the relative visible row when possible.
- Top-of-file selects the first line and first column. Bottom-of-file selects the last line and first column and places that last line at the top of the viewport.

The historical implementation's 16-bit integer ceiling is not a portable document-size requirement. Target-language overflow must be guarded without artificially limiting modern documents to Turbo Pascal's `Maxint`.

## 5. Blocks and markers

- The historical V1 compatibility block is whole-line and contiguous.
- One active block may be begun/ended, copied, moved, deleted and hidden.
- Logical block limits and visible `InBlock` flags are distinct state. A compatibility operation equivalent to `EditOffblock` clears `InBlock` from every open text stream without deleting the logical block limits; the active block can subsequently be projected into flags again.
- Numbered markers 1..20 identify a document and logical line, not a saved column.
- Jumping to a marker may select a window that displays its document and changes the cursor line while preserving that target view's current column.
- Implementations must eventually preserve marker/block/window anchor semantics across line insert/delete topology, but they do not need to reproduce raw descriptor pointers to do so.

## 6. Search and replace

- Literal forward search is required.
- The compatibility search remembers the most recent non-empty pattern for a Find Again operation.
- Find Again resumes after the previous match when the cursor is still positioned on that match.
- FIRST-ED-compatible forward search does not wrap by default. Wrap-around remains an explicit option for hosts that want it.
- Case-sensitive, case-insensitive and whole-word behavior are explicit options.
- Replace-next and replace-all are services above the buffer layer.
- Hosts may intercept a replacement decision.

## 7. Undo and dirty state

- Every ordinary text mutation marks the document dirty.
- Successful persistence clears dirty state when the operation is semantically a save.
- A direct compatibility Write File operation does not implicitly change document identity.
- Undo is a replaceable service. Correctness is more important than storage efficiency in the first implementation.
- Historical destructive operations explicitly documented as not entering undo must not be made reversible merely because a modern undo backend exists. Obsolete snapshots for a destroyed document must not remain usable.

## 8. Command dispatch, normalized input and typeahead

- Host-specific keyboard events are normalized before compatibility mapping.
- User input is translated to semantic commands outside the editing engine.
- The FIRST-ED compatibility map is stateful because Ctrl-K, Ctrl-O and Ctrl-Q introduce a second keystroke.
- A key map never prompts the user directly. Instead it declares whether a command requires text, a number, two numbers, a character, a file path, find/replace values or confirmation.
- Numbered prefix commands may carry their number directly in the binding.
- A command-filter hook may rewrite a semantic command before dispatch.
- The engine itself must remain callable directly by application code.
- Hosts may add modern aliases (for example arrow keys) without removing the historical command sequences.
- V1 provides an editor-owned bounded typeahead abstraction with a default capacity of 500 logical input units.
- Physical/host input appends at the back in FIFO order. Macro/user-pushed input can be inserted at the front, including a sequence whose subsequent read order matches the caller's sequence order.
- Typeahead overflow clears pending typeahead instead of keeping an ambiguous partial command sequence.
- A host-originated Ctrl-U clears pending typeahead immediately and raises an abort state instead of being queued behind existing commands.
- Front-injected macro input does not acquire physical-input Ctrl-U semantics merely because it contains a Ctrl-U key stroke.
- Raw DOS scan codes, circular-buffer indices and byte-layout details are not part of the portable V1 contract.

## 9. Lifecycle and cooperative scheduling

- The session exposes a rundown state equivalent to the historical `Rundown` variable.
- Direct Exit requests rundown and does not save files.
- Confirmation before interactive exit is a host/prompt responsibility declared by the input binding.
- A scheduler cycle first offers the host one opportunity to process pending editor/host input. If input is processed, background work is skipped for that cycle.
- If no input is available, one cooperative background slice runs.
- The system loop repeats scheduler cycles until rundown is requested or external cancellation occurs.
- Background tasks must keep their own resumable state and return after a bounded unit of work.
- Long-running compatibility operations may observe the editor abort state in addition to the target platform's normal cancellation primitive. Exact procedure coverage is audited separately.

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

## 13. Low-level compatibility policy

- Observable text, cursor, window, block, marker, undo and persistence behavior is part of the portable contract.
- Pascal pointer addresses, line-descriptor free lists, manual heap release and video-memory representation are not portable requirements.
- Low-level operations such as historical line deletion must preserve the externally visible anchor relationships without requiring target languages to expose pointer-shaped APIs.
- Where the handbook leaves behavior unspecified, a modern implementation policy must be documented as such rather than presented as historical behavior.

## 14. Compatibility source policy

The Borland handbook is a requirements/reference source only. Implementations must be independently written and must not copy historical source code.
