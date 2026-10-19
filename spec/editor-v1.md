# SASD Editor Toolbox - language-neutral V1 contract

This document defines behavior that every language implementation should preserve. Public API names may be idiomatic for the target language; observable editor behavior should remain compatible.

## 1. Text model

- A document contains one or more logical lines.
- A line carries text plus flags for block membership, a soft word-wrap boundary and user-defined highlighting.
- Coloring the current line or all lines of the current text stream sets the `UserColored` flag without clearing other line flags. It affects the shared document metadata, so linked windows observe the same result. The operations do not switch the active window or move its cursor.
- As an explicit modern policy, user-color annotations are presentation-only: setting an already set flag is idempotent, and coloring does not set text dirty state, change the document text version or add an Undo history item. The historical handbook defines setting the flag but not how this annotation participates in modern text persistence/Undo.
- `Wrapped` belongs to the flagged line's **outgoing boundary**. If line N is wrapped, the boundary from line N to line N+1 is a soft word-wrap continuation rather than an explicit paragraph-ending newline.
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
- Portable workspace fixtures can assign a host row count before window commands and verify indexed top-row/height frames; row geometry remains host-neutral, not a terminal API contract.
- Create Window receives a requested new-window height and a donor displayed-window number. It rejects a height below three rows or a split that would reduce the donor below three rows.
- A successful Create Window takes the requested rows from the donor's lower displayed area, creates a blank new document/view directly after the donor in displayed-window order, and keeps total allocated rows unchanged.
- If donor compression would leave its cursor below the donor's remaining visible text area, the cursor is moved to the donor's last displayed text line while preserving its column.
- Delete Window rejects deletion of the sole displayed window. Deleting window 1 gives the freed rows to the following window; deleting another window gives the freed rows to the displayed window immediately above it.
- Deleting a window containing the active block clears that block.
- Shared-document lifetime must not depend on explicit pointer/free-list manipulation: deleting one linked view must leave a document alive while another view references it.
- When closing or relinking the **last view** of a document, modern sessions discard that now-unreachable document's marker references and all Undo transactions involving it, including compound cross-document entries. This prevents stale Undo records from consuming an Undo attempt before reaching the next valid operation. Another linked view retaining that document must retain its markers and Undo intact. Historical Delete Window's independent block-clearing rule is unchanged.
- Delete Window Text is deliberately destructive and non-undoable. It removes the current stream, resets affected views to blank `NONAME` documents, and destroys links between views that had shared that stream.
- A block belonging to a destructively removed stream is cleared. Implementations must also invalidate any modern navigation state that would otherwise point at a document identity no longer reachable by a view.
- Dynamic host-resize behavior is implementation policy, not historical V1 behavior, and must be documented separately from compatibility Create/Delete rules.

## 4. Editing and viewport primitives

V1 includes character/word/line movement, page movement, beginning/end/top/bottom navigation, text insertion, line insertion, New Line/Return, tab, control-character insertion, left/right deletion, word deletion, line deletion, delete-to-end-of-line, case change, centering and paragraph reformatting.

- Character-left from the first column of a non-first logical line moves to the previous line immediately after its last non-blank character. At the beginning of the text stream it does nothing.
- As a modern no-op edit policy, Change Case at or beyond the physical line end, Change Case on characters that do not change under invariant case conversion, and Delete To End Of Line when nothing remains must not mutate text, increment document version, mark dirty, or create Undo. The semantic command is still accepted; success means handled, not necessarily mutated.
- The direct .NET editing engine follows the same modern nonmutating policy for carriage-return-only inserts, backspacing through virtual whitespace and deleting a character/word at the final physical line end. These cases must not create dirty/Undo changes or lose another linked view's cursor; direct Backspace moves the initiating cursor left through virtual space. Historical semantic command wrappers retain their separately tested behavior.
- Center Line uses the active window's inclusive margins. When centering produces unchanged stored text, it may bring the initiating cursor back from virtual space to the physical end but must not change the document version, dirty state or Undo. Padding calculations must not overflow, and unrepresentable physical results are rejected without editing. As a modern resource-safety policy, the editor refuses to **generate more than one million leading spaces** for a newly centered line; an existing already-centered long line can still be recognized as a no-op without allocating another copy.
- Character-right increments the logical column without crossing to the next logical line. A compatibility implementation must therefore support a virtual cursor column beyond the current line length.
- Word-right uses three character classes: alphanumeric, punctuation and blank. Starting on a non-blank class moves across that run and any following blanks; starting on blanks moves to the next non-blank. If the cursor is beyond the current line's last non-blank, it moves to column zero of the following line when one exists.
- Word-left treats leading blanks as a boundary: from within or immediately after leading indentation it moves to column zero. From column zero it enters the preceding line immediately after that line's last non-blank. Otherwise it moves to the beginning of the preceding same-line word/class run.
- Target languages may use Unicode-aware letter/digit and whitespace classification as a documented extension; ASCII input must preserve the three historical classes.
- Tab advances to the next configured tab stop. In Insert mode the padding is inserted into the document; in Overtype mode only the cursor moves.
- Ordinary text insertion preserves the existing outgoing `Wrapped` boundary unless the insertion itself triggers a new automatic word wrap.
- Automatic word wrapping marks the upper line's outgoing boundary as `Wrapped`. If the original line already had a downstream soft boundary, that boundary follows the newly inserted lower fragment.

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
- A text stream must retain at least one logical line. Deleting its sole line fills its existing stored character span with spaces rather than removing the final logical-line container; the stored line length therefore remains observable until later editing changes it.

### Paragraph reformatting

- Reformat begins on the current logical line and follows soft `Wrapped` boundaries until it reaches the first hard boundary or the end of the stream. The hard-boundary line is part of the paragraph being reformatted.
- Reformat works whether or not automatic Wordwrap mode is enabled.
- Reformat normalizes repeated word-separating whitespace, shifts content to at least the left margin, moves words down when a line would exceed the right margin and pulls words up while they still fit.
- The left and right margins are inclusive. A single word wider than the available margin width makes the operation fail before any text mutation or undo entry is created.
- Every resulting paragraph line except the final line has `Wrapped` set. The final paragraph line has a hard outgoing boundary.
- Structural expansion or contraction of the paragraph participates in the normal line-topology realignment contract.
- The handbook does not prescribe a final cursor relocation after reformat. Implementations must document their policy; the .NET V1 implementation preserves the initiating logical line and column.
- Target languages may recognize Unicode whitespace as a documented extension to the handbook's space-oriented compression behavior.

### Viewport movement

- The absolute line number displayed in each view's status is the one-based value of that view's current logical document line. A port need not traverse historical linked descriptors to calculate it; a stable index is an equivalent modern implementation.
- Horizontal cursor visibility applies to **all** displayed views, not only the active view. For a host-supplied positive text width `w`, if the cursor lies left of `LeftColumn`, set `LeftColumn` to the cursor column; if the cursor lies at or beyond `LeftColumn + w`, move `LeftColumn` just enough for the cursor to be the rightmost visible column. Otherwise preserve the offset. Do not move the cursor or modify text, and honor virtual columns beyond line length.
- The original `EditGenlineno`/`EditHscroll` routines could stop a long descriptor/screen pass when input arrived. In V1 these are bounded indexed/view operations; no long historical scan is performed. Input-first cooperative scheduling remains the host responsibility.

- Display-dependent commands receive the number of visible text rows from the host. Line-up/down and scroll-up/down keep the cursor visible according to the historical edge rules.
- Page-up/down move the viewport by `visibleRows - 1`. Because the handbook does not separately specify the cursor's post-page screen row, implementations must document their cursor policy; the .NET implementation preserves the relative visible row when possible. With exactly one visible row, this displacement is zero: no view/cursor state changes (a semantic dispatcher may nevertheless report a valid command as handled).
- A positive host-reported row count may greatly exceed a document's line count; page navigation and last-visible-row checks must clamp to the last logical document line without integer overflow or document/Undo mutation.
- Direct `EditorEngine` page/line movement likewise calculates an out-of-range target line using wide arithmetic before clamping. Huge positive page sizes must move downward to the last logical line (not wrap backward to the first), and huge upward movement must clamp to the first. This navigation-only API never creates dirty text or Undo.
- Top-of-file selects the first line and first column. Bottom-of-file selects the last line and first column and places that last line at the top of the viewport.

The historical implementation's 16-bit integer ceiling is not a portable document-size requirement. Target-language overflow must be guarded without artificially limiting modern documents to Turbo Pascal's `Maxint`.

## 5. Blocks, markers and line topology

- The historical V1 compatibility block is whole-line and contiguous.
- Begin and end limits are independently definable. Until both limits identify an ordered range in the same document/text stream, no operational block range is available and block highlighting remains hidden.
- Repointing the begin limit invalidates an existing end limit when that end is no longer reachable later in the same stream, matching the documented Begin behavior. For the inverse End-before-Begin/cross-stream case, V1 uses a documented clean-room validity policy: no reversed or cross-stream operational block is materialized.
- One active block may be begun/ended, copied, moved, deleted and hidden, and block manipulation may be initiated from another open window/document.
- Copy inserts copies of the block lines immediately before the current cursor line and leaves the source text and logical source block unchanged.
- Move removes the source block and inserts those lines immediately before the current cursor line. For a same-document move, the current cursor line must not lie inside the source block. The logical block follows the moved lines to their destination.
- Delete removes every logical line in the block. If the block spans the entire stream, the implementation must still retain the text model's required single blank logical line.
- Structural block insertions and deletions participate in the same topology contract as ordinary line mutations. A target cursor anchored to a surviving line remains attached to that line even when inserted block lines change its numeric index.
- Copied/moved line content retains ordinary line metadata such as soft-wrap and user-highlight state, but `InBlock` is not cloned as independent text metadata; visible block flags are regenerated from the logical block definition.
- Logical block limits and visible `InBlock` flags are distinct state. A compatibility operation equivalent to `EditOffblock` clears `InBlock` from every open text stream without deleting the logical block limits; the active block can subsequently be projected into flags again.
- Numbered markers 1..20 identify a document and logical line, not a saved column.
- Jumping to a marker may select a window that displays its document and changes the cursor line while preserving that target view's current column.
- Structural insertion or deletion must keep every affected window's current-line and top-line references valid.
- References to surviving logical lines after an insertion/deletion must be realigned so they continue to identify those surviving lines rather than merely retaining stale numeric indices.
- A marker that points directly at a deleted logical line becomes undefined; markers on later surviving lines are realigned.
- If a deletion removes one block boundary, that endpoint becomes undefined independently. The surviving endpoint remains available for a later Begin/End command, while the complete block projection and visible highlighting remain unavailable until both valid limits exist again.
- Insert Line, New Line, automatic word-wrap insertion, compatibility file insertion, paragraph-reformat expansion/contraction and block bulk mutations are structural changes and must participate in the same realignment contract.
- Raw line-descriptor pointers are not required. A centralized anchor/topology service, buffer-native anchors or another target-language mechanism may provide the observable behavior.

The handbook does not separately define whether markers or other non-block anchors inside a moved source range follow the physical moved lines. Implementations must document their policy instead of claiming undocumented historical behavior. The .NET V1 implementation applies ordinary deletion semantics to source anchors and explicitly recreates the logical block at the destination.

## 6. Search and replace

- Literal forward search is required.
- The compatibility search remembers the most recent non-empty pattern for a Find Again operation.
- Historical EditFind with an explicitly empty input repeats the remembered pattern and options. This is distinct from a missing/null semantic-command argument, which is invalid. With no prior pattern, empty Find returns no match without document mutation.
- Find Again resumes after the previous match when the cursor is still positioned on that match **and its original logical line text and document version are unchanged**. Identical text newly occupying a numeric line index after structural edits must not be mistaken for the previous occurrence. A stale coordinate after text editing does not authorize skipping a new occurrence at the same position.
- Undo restores document contents and navigation/marker/block state, not the remembered search pattern or its options. It invalidates the last-match anchor for restored documents, so a subsequent Find Again starts at the current cursor (without blindly skipping a match left at the same coordinates). A document restoration also supersedes in-flight asynchronous search results.

- FIRST-ED-compatible forward search does not wrap by default. Wrap-around remains an explicit option for hosts that want it. When enabled, search visits candidate *match start positions* at/after the current column and then before it; a wrapped match is valid when it starts before that column even if its text extends beyond it. The original column is an exclusive bound on starting positions, not on matching spans. Sync and interruptible search use the same rule.
- Case-sensitive, case-insensitive and whole-word behavior are explicit options.
- Portable case-insensitive literal examples guarantee ASCII case folding; locale-specific Unicode case folding is implementation policy until separately specified and cross-language verified.
- Interruptible searches scan the initiating document's captured logical-line snapshot. Host checkpoints may switch views, navigate, edit text, close windows or run another search; before a completed scan commits its cursor and remembered-pattern state, it must verify that its initiating view/document and cursor remain valid, its document contents/version match, and no newer search superseded it. A window switch alone must not redirect results or steal focus; the initiating view receives its own result.
- Replace-next may await a host confirmation after matching. It must not mutate or advance the cursor if the initiating view, cursor, document snapshot or remembered search has changed while the confirmation was pending; an explicitly cancelled confirmation performs no replacement. In the .NET reference, confirmation-time focus changes also reject replacement because the synchronous editing engine targets the active view. These are modern safe asynchronous-host policies, not claimed 1985 DOS timing semantics.
- Literal search patterns and replacements operate **within a single logical line** in V1. Embedded CR or LF in a literal pattern or replacement is rejected before any cursor, remembered-search, buffer or Undo mutation; empty replacement remains valid. Multi-line replacement requires a future structural editing API that preserves line topology and anchored views.
- Replace-next and replace-all are services above the buffer layer.
- Hosts may intercept a replacement decision. The candidate supplied for confirmation contains the **actual matched document substring**, not the potentially differently cased literal search pattern; the original substring is captured before awaiting the host and revalidated before modification.

## 7. Undo and dirty state

- Every ordinary text mutation marks the document dirty.
- Successful persistence clears dirty state when the operation is semantically a save **and** the document still matches the version and line snapshot that was actually written.
- Concurrent Save/Save As operations on one document receive request-order tokens. An older save that completes after a newer successful save must not roll back the document's associated path or dirty acknowledgement. Only successful saves advance the completion watermark, so a newer failed/cancelled request does not suppress an older successful save to another target. Distinct paths may publish independently; the per-target write gate applies only when normalized paths match. If editing continues while asynchronous I/O is in progress, the successful write may update file association but must not hide unsaved newer edits. Direct Write File leaves the identity/dirty state alone.
- A direct compatibility Write File operation does not implicitly change document identity.
- Undo is a replaceable service. Correctness is more important than storage efficiency in the first implementation.
- The handbook's historical `EditUndo` is a line-stack operation rather than a general command journal. SASD's command-oriented snapshot undo is therefore a documented modernization boundary, not a claim of storage-level historical equivalence.
- Historical destructive operations explicitly documented as not entering undo must not be made reversible merely because a modern undo backend exists. Obsolete snapshots for a destroyed document must not remain usable.
- Cursor-only movement, including a documented implementation policy for movement through virtual columns, must not create a text-undo entry merely because it passed through a compatibility command.
- A successful paragraph reformat is one logical undoable mutation. Validation failure or a no-op must not create a snapshot merely because the command was attempted.
- One successful logical SASD mutation command contributes at most one undo transaction. A mutation spanning multiple documents, such as a cross-document block move, must not expose a state in which one Undo restores only one side of the command.
- The correctness-first .NET backend captures every affected document before the mutation and records final session navigation/anchor state only when the command commits. One Undo restores all affected document snapshots atomically from the editor user's perspective.
- Command-induced cursor/top-line/left-column changes in linked views, marker realignment, block endpoint state and current-window selection are restored when they still match the recorded post-command state. Explicit navigation or marker/block changes made after the edit are not blindly overwritten; any surviving anchors are normalized against the restored documents.
- If a deliberately destructive operation destroys a document, every compound undo transaction that references that document is discarded as a whole rather than retaining a partially restorable transaction.
## 8. Command dispatch, normalized input and typeahead

- Host-specific keyboard events are normalized before compatibility mapping.
- User input is translated to semantic commands outside the editing engine.
- The FIRST-ED compatibility map is stateful because Ctrl-K, Ctrl-O and Ctrl-Q introduce a second keystroke.
- A prefixed key sequence is consumed as one two-step input command: the first key creates a pending prefix (without editing), and the next recognized command character resolves a semantic action and clears the prefix. Unsupported second keys and Escape clear the pending prefix without executing an unrelated command. Without a prefix, printable text remains text and Escape retains Undo. The key map does not directly mutate documents, and host-supplied prompt values are required before constructing a semantic command request.
- A key map never prompts the user directly. Instead it declares whether a command requires text, a number, two numbers, a character, a file path, find/replace values or confirmation.
- Host-collected prompt responses distinguish **cancelled/null** from **explicitly empty** values. A null second Find/Replace response must not become an empty replacement and delete text. Only an explicitly submitted empty replacement means deletion. A null Find response cancels; an explicitly empty Find repeats the remembered pattern (Find Again). Prompt resolution is UI-neutral and does not itself execute or mutate commands.
- Numbered prefix commands may carry their number directly in the binding.
- A command-filter hook may rewrite a semantic command before dispatch.
- The semantic command boundary validates request shape and stable ranges before mutation or file I/O. Invalid requests fail without applying the command.
- One-based line/column/window values, tab width and the FIRST-ED undo-limit command require values of at least 1. Create Window additionally requires a size of at least 3 rows. Numbered markers are restricted to 1..20.
- A supplied visible-page size must contain at least one text row.
- Find requires a supplied textual pattern. An explicitly empty Find string repeats the remembered search pattern/options (historical EditFind behavior); an omitted/null pattern is invalid. When there is no remembered search, an empty Find simply reports no match. Replace requires a non-empty pattern and a replacement value; an empty replacement is valid. Portable request models represent pattern and replacement as separate textual arguments.
- Read/Write file commands require a non-blank resolved path. Save may omit a path when the document already has an associated path.
- Stateful constraints, such as a left margin not exceeding the current right margin or a donor window retaining its three-row minimum, are validated by the service that owns that state after the request envelope has passed boundary validation.
- The engine itself must remain callable directly by application code.
- Hosts may add modern aliases (for example arrow keys) without removing the historical command sequences.
- As a modern host-safety policy, Alt-modified normalized keys are not mapped to FIRST-ED commands, including special navigation/editing keys; hosts may reserve these combinations for application or operating-system accelerators. When a prefix is already pending, an unsupported modified second key consumes the pending prefix without executing any command. Plain Escape while waiting for a prefix cancels the prefix rather than triggering Undo. This is not asserted as a DOS scan-code compatibility rule.
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
- Rundown requested while input, an idle hook, or a background slice executes prevents *subsequent* background slices in that cycle. A task removed during the cycle must not run from an outdated registration snapshot; newly added tasks start no earlier than the next idle cycle. External cancellation is checked before idle and between slices. These are modern cooperative execution safety rules, not DOS preemption.
- If no input is available, one cooperative background slice runs.
- The system loop repeats scheduler cycles until rundown is requested or external cancellation occurs.
- Background tasks must keep their own resumable state and return after a bounded unit of work.
- Long-running compatibility operations use a host-neutral interruption checkpoint equivalent in purpose to historical EditBreathe: the host may collect a bounded amount of physical input while the operation runs, ordinary input remains queued, and a physical Ctrl-U raises the one-shot editor abort state.
- User-originated Ctrl-U abort and platform/external cancellation are distinct. Ctrl-U is translated into the typed editor error `OperationAborted`; `CancellationToken` cancellation remains platform control flow and propagates to the caller.
- Async compatibility file I/O is wrapped so Ctrl-U can cancel the in-flight codec token. For Read File, decoded data is not committed into the document until the read completes and the final interruption checkpoint succeeds.
- Interruptible Find/Find Again scan logical lines at safe checkpoints and commit cursor/remembered-search state only after the scan completes.
- Paragraph reformatting polls during its side-effect-free planning phase and performs one final checkpoint before mutation. Once the small in-memory commit phase starts, it completes without Ctrl-U checkpoints so the paragraph cannot be left half reformatted.
- Short structural edit commits (for example whole-line block splice operations) are intentionally not interrupted mid-mutation; they preserve structural atomicity and are covered by their normal topology/undo invariants.

## 10. Host hooks

Equivalent extension points must exist for command filtering, error handling, status transformation, replace confirmation and cooperative idle/background work.

Error notifications use stable semantic categories independent of UI wording. A portable V1 implementation provides a typed error identifier plus stable machine code and resource key; hosts own localization and rendering. The English fallback message is diagnostic convenience rather than normative text. Validation failures, state-dependent command rejections, expected file/access/unsupported-operation failures, typeahead overflow and host-originated aborts all use this common error contract. Historical numeric/message catalogs are not part of the portable contract.

## 11. Rendering

The core does not write directly to console/video memory. It exposes viewport/status projections for individual windows plus host-neutral window frames. WPF, WinForms, terminal, web and other hosts decide how those projections are drawn.

- A viewport requested with positive `height` and `width` projects at most the remaining logical document lines. Its row count must not overflow integer arithmetic when a host reports a very large positive height; zero and negative dimensions are invalid.
- Projecting a viewport is observational. It must not change the document, dirty state, cursor, top line, horizontal offset or Undo history.
- A stale negative horizontal offset is normalized to zero **for the projection only** as a defensive modern .NET policy. Hosts remain responsible for maintaining nonnegative view state; this is not a claimed historical 1985 rule.

## 12. Persistence and compatibility file I/O

- Modern document persistence is abstracted. The first .NET `ITextStorage` provider supports UTF-8 files and preserves the detected newline convention for subsequent saves.
- Compatibility file commands use a separate file-codec boundary so historical formats do not become mandatory modern storage formats.
- The Turbo Editor Toolbox wrapped-line marker is a high-bit carriage return (`0x8D`, decimal 141). Decoding it marks the preceding logical line's outgoing boundary as wrapped; encoding a wrapped boundary emits it after that line.
- The modern UTF-8 whole-document writer must reject CR/LF *inside logical line text* and any configured newline convention other than CRLF, LF or CR before touching the destination; otherwise a save/load round trip would silently alter logical-line topology. The newline style used for the written snapshot also participates in save acknowledgement so metadata changes during async writing do not falsely acknowledge the old serialized representation.
- The compatibility writer must reject embedded CR, LF and `0x8D` inside logical line text: these bytes are structural separators, and writing them as content would silently corrupt line topology on readback. Non-Latin-1 characters are likewise rejected before the destination is changed.
- As a modern filesystem safety policy, the .NET UTF-8 storage provider and legacy byte codec stage writes in a temporary file **in the target directory** and replace the destination only after the full stage succeeds and cancellation is checked. A failed/cancelled stage must leave the prior destination bytes and editor save/dirty state untouched. The final rename is non-cancellable; rename atomicity and crash durability depend on filesystem/platform guarantees, and this policy does not assert metadata preservation. Custom codecs remain responsible for their own write semantics.
- Built-in staged writers targeting the same normalized path are **serialized for the full staging-and-publication interval within one process**: a later write cannot publish before an earlier in-progress write has finished. Distinct paths can proceed concurrently, cancelled waiters must never open a staging file, and path gates are released after use. This does not coordinate custom codecs, separate processes, symlink/hardlink aliases or arbitrary filesystem races; hosts requiring those guarantees must provide additional coordination.
- The FIRST-ED-compatible read operation inserts decoded lines after the current line and preserves the current cursor position when no later host navigation intervenes.
- Decoded compatibility file input must contain only valid logical-line text (no embedded CR, LF or `0x8D` separator) and known V1 line flags. A custom decoder's entire result is snapshotted and validated before any document/Undo mutation. Malformed input fails as a typed file-I/O error; the previous document and Undo history are preserved. Validation of long inputs remains cancellable at bounded checkpoints.
- In asynchronous hosts, the file-read destination is the view/document and logical insertion position **at request initiation**, not whichever window becomes active after decoding. Selecting another window during I/O must not redirect the import or steal window focus. If the initiating view is closed, reattached, or its document text changes before insertion, the modern safe policy rejects the import without additional mutation or an Undo entry. Explicit cursor navigation during I/O must not be silently undone; topology realignment still applies.
- Existing window, marker and block references to logical lines following a read insertion must be realigned to those same surviving logical lines.
- Hosts collect filenames; core file services receive resolved paths and perform no UI prompting.

## 13. Low-level compatibility policy

- Observable text, cursor, window, block, marker, undo and persistence behavior is part of the portable contract.
- Pascal pointer addresses, line-descriptor free lists, manual heap release and video-memory representation are not portable requirements.
- Low-level operations such as historical line insertion/deletion must preserve externally visible anchor relationships without requiring target languages to expose pointer-shaped APIs.
- Reformat helper names describe historical responsibilities; target-language implementations may combine them into a side-effect-free plan before committing structural changes.
- Whole-line block operations may similarly use snapshots plus a centralized topology service instead of physically splicing historical descriptor pointers.
- Where the handbook leaves behavior unspecified, a modern implementation policy must be documented as such rather than presented as historical behavior.

## 14. Compatibility source policy

The Borland handbook is a requirements/reference source only. Implementations must be independently written and must not copy historical source code.
