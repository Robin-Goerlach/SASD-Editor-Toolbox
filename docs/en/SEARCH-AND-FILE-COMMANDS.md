# FIRST-ED search and file commands

## Scope

This milestone transfers the next group of Turbo Editor Toolbox behaviors into the C# implementation without mixing host prompts, editor state and byte-level file compatibility into one class.

The handbook remains the behavioral reference; the implementation is independently written.

## Search state and Find Again

The historical `EditFind` asks for a pattern and, when the supplied string is empty, reuses the previous search string. FIRST-ED also exposes a dedicated repeat-search command. SASD represents that behavior explicitly in `EditorSearchService`:

- `FindNext(pattern, options)` records the pattern and options;
- an explicitly empty pattern (`FindNext("")` or `FindNextAsync("")`, including through the command dispatcher) repeats the remembered search and options. An omitted/null command text is still invalid, and an empty pattern with no prior search simply reports no match;
- `FindAgain()` repeats the recorded search;
- if the cursor is still on the previous match, Find Again resumes after that match instead of returning the same occurrence again;
- forward search does not wrap by default in the FIRST-ED compatibility path;
- wrap-around remains an explicit `SearchOptions` feature for modern hosts.

The search service remains independent from the keyboard map. `FirstEdKeyMap` maps Ctrl-L to `FindAgain`, while the command dispatcher invokes the remembered search state.

## File-command separation

The handbook distinguishes between command processors that ask the user for a filename (`EditCprfw`, `EditCpwfw`) and lower-level operations that receive a filename (`EditFileRead`, `EditFileWrite`). SASD keeps the same architectural boundary:

1. the host asks for a path;
2. the key map only declares that a path is required;
3. `EditorCommandDispatcher` receives a fully resolved `EditorCommandRequest`;
4. `EditorFileService` performs the operation;
5. `IEditorFileCodec` owns byte-format compatibility.

This means a WPF dialog, terminal prompt, web UI or scripted caller can all use the same core operation.

## Historical read semantics

The handbook describes `EditReatxtfil` as inserting the contents of a named file **after the current line** while leaving the cursor position unchanged. `EditorFileService.ReadIntoCurrentWindowAsync` preserves those observable semantics. The operation is undoable and marks the document dirty.

Reading a file this way does not rename the current document. It is an insertion operation, not a modern "Open document" operation.

## Wrapped-line file marker

The historical file routines use a carriage-return byte with the high bit set, decimal 141 / hexadecimal `0x8D`, to identify a line carrying the `Wrapped` attribute. `FirstEdLegacyFileCodec` preserves this convention:

- `0x8D` ends a logical line and sets `EditorLineFlags.Wrapped` on that line;
- ordinary CR/LF and LF are accepted as ordinary line boundaries;
- writing a wrapped logical line emits `0x8D` instead of the ordinary CR/LF separator;
- the codec is explicitly an 8-bit Latin-1 compatibility codec.
- CR, LF and `0x8D` are reserved delimiter bytes and cannot appear as literal characters *within* an `EditorLineSnapshot.Text`. Encoding rejects these values and non-Latin-1 characters before it opens/truncates the target file; the regression suite checks that existing file bytes are not lost.

`FileTextStorage` remains the modern UTF-8 whole-document storage provider; its byte encoding and newline policy are independent of the historical 8-bit codec. Both providers now share the same safe save-acknowledgement rule through `EditorDocument.MarkSnapshotSaved` without mixing their file formats.

## Write and Save semantics

`WriteFile` writes the current text stream to an explicit path but does not rename the document and does not clear the dirty flag. This corresponds to the direct named-file write operation.

`SaveFile` writes to the document's associated path and clears the dirty flag only when the saved snapshot still matches the document. An explicit path may also be supplied by a modern host. This is a deliberate integration adaptation: the handbook clearly documents direct read/write behavior, while SASD's document identity is a modern abstraction rather than a Pascal global/window field.

## Subsequent V1 work

Lifecycle, viewport movement, window layout and terminal FIRST-ED host contracts are now maintained and tested in their own compatibility modules. This file focuses on search/file semantics; the remaining V1 gate is a routine-by-routine behavioral evidence audit rather than assuming those previously documented milestones remain unimplemented.


## Interruption and cancellation

Compatibility Read/Write and modern Save now execute codec I/O through `EditorInterruptionService`. The service periodically gives the host an EditBreathe-style opportunity to collect physical input while awaited I/O is outstanding.

For Read File, decoded lines are not inserted until the codec operation has completed and a final interruption checkpoint succeeds. A Ctrl-U during the read therefore cancels the codec token and leaves the current document, cursor, dirty flag and undo stack untouched.

Write/Save pass the same linked token to the codec. A cancelled external file write can still be partially visible at the filesystem/provider level; editor document state is updated only after successful completion. Save does not clear dirty state if the write is interrupted.

Semantic Find/Find Again use the interruptible search path. Scanning checkpoints once per logical line and only commits the cursor and remembered-search state after a successful scan. Direct synchronous service methods remain available for applications that deliberately want an in-process non-interruptible call.

## Save consistency during concurrent editing

A host may edit a document, including through another linked window, while asynchronous file I/O is still in progress. The file service and modern UTF-8 storage now capture the document version and logical-line snapshot before writing. After I/O successfully completes, `EditorDocument.MarkSnapshotSaved` clears the dirty flag **only** when the current lines and version still match the snapshot that was written. A successful write still associates the requested file path even if a newer edit leaves the document dirty. `WriteFile` continues to leave both file identity and dirty state alone.

This is a modern persistence-safety guarantee rather than a claim about cooperative single-threaded DOS behavior. `FileSaveConsistencyTests` uses an intentionally paused codec to prove that newer edits are not mislabeled as persisted.

## Asynchronous file-read destination safety

`ReadIntoCurrentWindowAsync` binds the read request to its initiating window, document, original insertion line and document snapshot **before** any awaited I/O. A host is free to switch to another window while the file is decoded; the imported lines still belong to the original stream, and active-window selection is not reset. If a user moves the initiating cursor in the meantime, the cursor remains on the same surviving logical line after standard topology repair rather than being moved back by the delayed import.

If the original window was closed or reattached, or the target text/metadata was edited while decoding, the pending import fails with the existing typed `OperationFailed` command error and does not create a second edit or Undo entry. A stale numeric insertion index must not be applied to an uncertain document. These safeguards are modern asynchronous-host policy, not a statement about the 1985 single-threaded DOS input loop. Deterministic tests use a pausing codec rather than real wall-clock timing.

## Async search and replacement safety

Host checkpoint callbacks can run while an interruptible Find or Find Again is suspended. The .NET reference now scans the initiating document's **immutable line snapshot**, rather than resolving `CurrentWindow` after every checkpoint. A focus change alone does not change the search destination: any match updates the original window and leaves the new active window selected. Before committing match/cursor or remembered-pattern state, a completed scan checks the originating view, unchanged cursor, document version and actual line contents, and whether a newer search has already taken precedence. If any check fails, a typed `OperationFailed` is reported through the semantic dispatcher, without overwriting the newer navigation or search.

`ReplaceNextAsync` additionally guards the awaited `BeforeReplaceAsync` decision. Confirmation is applied only when the original match, text, view and active replacement window are still valid. An intervening edit, window switch, reattachment or newer search rejects the outdated replacement instead of modifying unrelated text. A deliberate `Cancel` decision remains a harmless no-op; `Skip` advances the matched view's cursor only if the search context remains valid. These rules are modern host-safety policies rather than claims about historical DOS concurrency.

## Find Again after edits and Undo

The remembered Find Again pattern and options belong to the search service, not to the text Undo journal. However, a previous match location must **not** be treated as valid after the line containing the match is edited. `EditorSearchService` now remembers the original matched line text; only an unchanged line at the original cursor **and the same document version** may be skipped when repeating the search. This avoids missing a newly exposed occurrence when a previous match is removed by an empty replacement.

When a document is restored by Undo, the historical match anchor for that document is explicitly cleared, while `LastPattern` and options stay available. Find Again therefore starts at the restored cursor position. Undo also invalidates pending asynchronous search revisions, preventing delayed results from being applied over a restored text/marker/block state. These search/Undo interactions are a documented modern policy rather than a claim about the historical line-stack implementation.

## Validation of logical line boundaries

V1 literal search operates on one logical line at a time. Patterns with embedded carriage return or line feed are now rejected with an argument error, rather than storing an impossible cross-line Find Again pattern. `ReplaceNextAsync` and `ReplaceAll` also reject CR/LF in replacement text **before** starting search or editing. Their single-line buffer update cannot safely manufacture new structural line boundaries: doing so would invalidate marker, block and file/viewport assumptions. An empty replacement remains valid. The semantic command dispatcher reports an invalid argument through the normal host-neutral typed error hook; direct service callers receive `ArgumentException`.

This is a documented V1 boundary, not a claim that all future editors are prohibited from multiline search/replace. A future feature would need an explicit multi-line edit and topology contract rather than embedding line separators in one line's text.

## Wrap-around boundary semantics

`SearchOptions(WrapAround: true)` is an opt-in modern-host behavior. A forward scan first considers match **start positions** from the cursor onward, then wraps to earlier starting positions. An occurrence that begins before the original cursor and extends beyond it is still a valid wrapped match. The cursor divides candidate starting positions, **not** the text of an occurrence. `EditorSearchService` now uses one well-commented per-line matcher for synchronous and interruptible searches so their whole-word, case and boundary decisions cannot drift. This is an explicit modern option; historical FIRST-ED search still defaults to no wrap-around.

The shared conformance runner now also accepts `find_next_with_options` to exercise these modern host settings independently from the default FIRST-ED semantic command. This preserves the historical key mapping while making opt-in wrap, whole-word and case settings portable for future C++/Java implementation lanes.

## Replacement confirmation displays actual text

For an opt-in case-insensitive search, the literal input `alpha` may match the differently capitalized document text `Alpha`. `IEditorHooks.BeforeReplaceAsync` now receives `ReplaceCandidate.ExistingText = "Alpha"` rather than the user's search pattern. The displayed candidate is cut from the **same captured document snapshot** that must still match after the awaited confirmation. This keeps replace/skip/cancel decisions grounded in what would actually change on screen, with no new UI dependency in the core.
