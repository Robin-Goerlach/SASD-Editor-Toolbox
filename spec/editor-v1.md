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
- A non-negative cursor column may be greater than the physical length of the selected line. Structural normalization must not silently destroy this virtual-column state merely because a line is short.

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

V1 includes character/word/line movement, page movement, beginning/end/top/bottom navigation, text insertion, line insertion, New Line/Return, tab, control-character insertion, left/right deletion, word deletion, line deletion, delete-to-end-of-line, case change, centering and paragraph reformatting.

- Character-left from the first column of a non-first logical line moves to the previous line immediately after its last non-blank character. At the beginning of the text stream it does nothing.
- Character-right increments the logical column without crossing to the next logical line. A compatibility implementation must therefore support a virtual cursor column beyond the current line length.
- Word-right uses three character classes: alphanumeric, punctuation and blank. Starting on a non-blank class moves across that run and any following blanks; starting on blanks moves to the next non-blank. If the cursor is beyond the current line's last non-blank, it moves to column zero of the following line when one exists.
- Word-left treats leading blanks as a boundary: from within or immediately after leading indentation it moves to column zero. From column zero it enters the preceding line immediately after that line's last non-blank. Otherwise it moves to the beginning of the preceding same-line word/class run.
- Target languages may use Unicode-aware letter/digit and whitespace classification as a documented extension; ASCII input must preserve the three historical classes.
- Tab advances to the next configured tab stop. In Insert mode the padding is inserted into the document; in Overtype mode only the cursor moves.

### Insert Line and New Line

`EditInsertLine` and `EditNewLine` are distinct V1 operations and must not be collapsed into one semantic command.

- Insert Line at column zero produces a blank current/upper line and moves the old line text to a newly inserted line immediately below; the active cursor remains on the upper line.
- Insert Line at or beyond the current line's last non-blank character inserts a blank logical line below the current line and leaves the active cursor on the upper line.
- Insert Line inside meaningful text splits the line at the cursor, moves text to the right of the cursor to the new lower line, and leaves the active cursor on the upper line.
- New Line in Insert mode uses the same structural split rules as Insert Line, but the active cursor moves to the newly created lower line.
- New Line in Overtype mode normally moves the cursor down one logical line without splitting text. When invoked on the final logical line it appends a new blank line and moves to it.
- If Autoindent is active, New Line places the cursor under the first non-blank character of the previously current line. If that line is blank or Autoindent is inactive, the target column is zero.
- New Line clears the `Wrapped` flag from the previously current line, marking an explicit paragraph boundary for reformatting.
- FIRST-ED maps Ctrl-N to Insert Line and Return/Enter to New Line; compatibility hosts must preserve this distinction even if they also expose modern menu aliases.

### Deletion and joining

- Delete Left Character removes the stored character immediately left of the cursor and moves the cursor left. At column zero it joins the current line to the preceding line when one exists; at the beginning of the stream it performs no mutation.
- The previous-line join point is immediately after that previous line's last non-blank character. Removing the current logical line is a topology mutation and must realign surviving references.
- FIRST-ED documents virtual columns but does not separately specify Backspace through unstored virtual space. Implementations must document their policy. The .NET V1 implementation moves the cursor left without text mutation or undo until a stored character is reached.
- Delete Right Character removes the stored character at the cursor while the cursor is inside meaningful text. At or beyond the position after the last non-blank character it joins the following logical line when one exists.
- Delete Right Word uses the same three character classes as Word Right, removes the current class run plus immediately following blanks, and uses the same following-line join rule when the cursor is beyond the last non-blank.
- A line join places the first character of the joined line at the cursor location used by the join operation; it must not manufacture a gap merely because the surviving line has trailing blank storage beyond that logical join point.
- Deleting the current line invalidates a marker on that line. If the line is a block boundary, that boundary becomes undefined and visible block highlighting is removed.
- A text stream must retain at least one logical line. Deleting its sole line blanks that line rather than removing the final logical-line container.

### Viewport movement

- Display-dependent commands receive the number of visible text rows from the host. Line-up/down and scroll-up/down keep the cursor visible according to the historical edge rules.
- Page-up/down move the viewport by `visibleRows - 1`. Because the handbook does not separately specify the cursor's post-page screen row, implementations must document their cursor policy; the .NET implementation preserves the relative visible row when possible.
- Top-of-file selects the first line and first column. Bottom-of-file selects the last line and first column and places that last line at the top of the viewport.

The historical implementation's 16-bit integer ceiling is not a portable document-size requirement. Target-language overflow must be guarded without artificially limiting modern documents to Turbo Pascal's `Maxint`.

## 5. Blocks, markers and line topology

- The historical V1 compatibility block is whole-line and contiguous.
- One active block may be begun/ended, copied, moved, deleted and hidden.
- Logical block limits and visible `InBlock` flags are distinct state. A compatibility operation equivalent to `EditOffblock` clears `InBlock` from every open text stream without deleting the logical block limits; the active block can subsequently be projected into flags again.
- Numbered markers 1..20 identify a document and logical line, not a saved column.
- Jumping to a marker may select a window that displays its document and changes the cursor line while preserving that target view's current column.
- Structural insertion or deletion must keep every affected window's current-line and top-line references valid.
- References to surviving logical lines after an insertion/deletion must be realigned so they continue to identify those surviving lines rather than merely retaining stale numeric indices.
- A marker that points directly at a deleted logical line becomes undefined; markers on later surviving lines are realigned.
- If a deletion removes a block boundary, a complete active block is no longer available for highlighting until valid boundaries are defined again. An implementation may represent this as one undefined boundary or conservatively clear its complete active block object.
- Insert Line, New Line, automatic word-wrap insertion and compatibility file insertion are structural insertions and must participate in the same realignment contract.
- Raw line-descriptor pointers are not required. A centralized anchor/topology service, buffer-native anchors or another target-language mechanism may provide the observable behavior.

The current .NET V1 topology coverage is complete for the audited single-line insertion/deletion paths. Multi-line block mutations and bulk paragraph reformatting remain separate audit targets before full topology parity is claimed.

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
- Cursor-only movement, including a documented implementation policy for movement through virtual columns, must not create a text-undo entry merely because it passed through a compatibility command.

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
- Existing window, marker and block references to logical lines following a read insertion must be realigned to those same surviving logical lines.
- Hosts collect filenames; core file services receive resolved paths and perform no UI prompting.

## 13. Low-level compatibility policy

- Observable text, cursor, window, block, marker, undo and persistence behavior is part of the portable contract.
- Pascal pointer addresses, line-descriptor free lists, manual heap release and video-memory representation are not portable requirements.
- Low-level operations such as historical line insertion/deletion must preserve externally visible anchor relationships without requiring target languages to expose pointer-shaped APIs.
- Where the handbook leaves behavior unspecified, a modern implementation policy must be documented as such rather than presented as historical behavior.

## 14. Compatibility source policy

The Borland handbook is a requirements/reference source only. Implementations must be independently written and must not copy historical source code.
