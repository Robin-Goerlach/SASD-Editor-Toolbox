# Line topology and reference realignment

## Purpose

Turbo Editor Toolbox stores text in linked line descriptors. Several pieces of editor state point directly at those descriptors: each window's current line and top line, text markers, and the two block limits. Deleting or inserting a line therefore has consequences beyond changing the text stream.

The SASD Editor Toolbox does not reproduce Pascal pointers or descriptor free lists. The .NET implementation represents those references with a stable `DocumentId` and zero-based logical line numbers. `EditorLineTopology` is the coordination layer that keeps those line-number references consistent after structural edits.

This is a clean-room behavioral transfer. The handbook is used to identify observable requirements; the historical implementation is not copied.

## Historical behavior being preserved

The V1 audit currently transfers the following observable rules:

- `EditDeleteLine` deletes the current logical line. When it is the only remaining line, the line descriptor remains and the line's existing stored character span is filled with spaces instead of removing the final line from the stream.
- A marker that points at a deleted line becomes undefined.
- If a deleted line is a block boundary, that boundary becomes undefined and block highlighting is removed.
- Windows that pointed at a deleted current/top line must remain attached to a valid line.
- `EditRealign` updates window line relationships after lines are inserted or deleted.
- Reading a file into a window inserts its lines after the current line while preserving the current cursor position; references to pre-existing logical lines must continue to identify those logical lines after the insertion.

## Modern mapping

`EditorLineTopology` provides three focused operations:

- `LinesInserted(document, firstInsertedLine, count)` shifts existing window cursors, top-line anchors, markers and block limits that refer to logical lines at or after the insertion point.
- `LinesDeleted(document, firstDeletedLine, count)` invalidates markers in the removed range, shifts later anchors and reattaches window cursor/top-line anchors to a valid remaining line when they pointed into the removed range.
- `InvalidateLineReferences(document, lineIndex)` supports the special one-line `EditDeleteLine` case: the final descriptor remains, but pointer-like marker/block state that historically referenced the deleted contents is invalidated.

The semantic `DeleteLine` command now passes through `FirstEdPrimitiveCompatibilityProcessor.DeleteLine`. This keeps historical line-deletion rules out of the reusable modern `EditorEngine` defaults.

`EditorFileService.ReadIntoCurrentWindowAsync` invokes `LinesInserted` after inserting decoded file lines. The initiating window's cursor is restored afterwards, while linked windows, markers and block limits remain attached to the logical lines they referred to before the insertion.

## Block-boundary policy

The current `EditorBlockState` holds independently nullable `Start` and `End` endpoints. Deleting a boundary invalidates **only the affected endpoint**, preserving its counterpart for a later compatible Begin/End command. A complete `EditorBlock` exists only when both endpoints define an ordered range within the same document; incomplete state is not highlighted. Insertions and deletions realign surviving endpoints independently.

Deleting a line *inside* a block retains both surviving endpoints; later anchors shift to preserve the intended logical span. See [block compatibility](BLOCK-COMPATIBILITY.md) and `BlockEndpointStateTests`.

## Undo and dirty state

Compatibility line deletion is one logical mutation in a transactional snapshot-undo model. `BeginTransaction` captures pre-command state; `Commit` records one history item after the text, topology and command-induced navigation are finalized. Undo restores matching document and anchor state without overwriting later independent user navigation. See [undo transactions](UNDO-TRANSACTIONS.md).

## Current integration boundary

`EditorLineTopology` is used by the audited structural command paths:

- compatibility `DeleteLine`, including final-line blanking;
- compatibility file insertion through `EditorFileService`;
- `InsertLine`, `NewLine`, and generic newline/automatic wrap;
- deletion-side joins and line removal;
- expansion/contraction during paragraph reformat;
- multi-line block copy/move/delete, including cross-document operations.

This is still an **explicit coordination boundary**, not an automatic interceptor for every future `ITextBuffer` mutation. New structural commands must invoke topology and add regression tests. The [compatibility matrix](BORLAND-V1-COMPATIBILITY.md) records audited coverage, not Pascal pointer equivalence.

## Tests

`LineTopologyCompatibilityTests` covers:

- marker invalidation on a deleted line;
- shifting a later marker;
- cursor and top-line realignment in multiple linked windows;
- independent block-endpoint invalidation and highlight cleanup;
- deletion inside an existing block;
- the one-line blank-but-do-not-remove rule;
- compatibility file insertion realigning linked views, markers and block limits while preserving the initiating cursor.

Additional coverage in `BlockEndpointStateTests`, `BlockCompatibilityTests`, `ReformatCompatibilityTests` and `CompoundUndoCompatibilityTests` exercises bulk realignment and undo. Tests intentionally validate observable editor behavior, not Pascal pointer layout.

## Performance policy

The current implementation scans the open windows and marker table when a structural change is reported. V1 favors correctness, traceability and simple invariants. If profiling later shows that large documents or many anchors need a faster representation, interval trees, persistent anchors or buffer-native anchor tracking can replace the internal strategy without changing the compatibility contract.
