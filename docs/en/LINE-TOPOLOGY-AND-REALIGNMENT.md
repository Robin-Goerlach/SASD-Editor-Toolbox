# Line topology and reference realignment

## Purpose

Turbo Editor Toolbox stores text in linked line descriptors. Several pieces of editor state point directly at those descriptors: each window's current line and top line, text markers, and the two block limits. Deleting or inserting a line therefore has consequences beyond changing the text stream.

The SASD Editor Toolbox does not reproduce Pascal pointers or descriptor free lists. The .NET implementation represents those references with a stable `DocumentId` and zero-based logical line numbers. `EditorLineTopology` is the coordination layer that keeps those line-number references consistent after structural edits.

This is a clean-room behavioral transfer. The handbook is used to identify observable requirements; the historical implementation is not copied.

## Historical behavior being preserved

The V1 audit currently transfers the following observable rules:

- `EditDeleteLine` deletes the current logical line. When it is the only remaining line, the line descriptor remains and the line is blanked instead of removing the final line from the stream.
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

The historical editor can represent `Blockfrom` and `Blockto` independently and set one pointer to `nil`. The current V1 .NET model uses a complete `EditorBlock` pair. Therefore, when a deleted line is one of the two block limits, the .NET implementation clears the complete active block and its highlighting rather than manufacturing a half-defined block object.

This is a deliberate conservative representation choice, not a claim that the 1985 implementation destroyed both pointers. A future block-anchor model may represent independently defined start/end limits if exact low-level parity becomes useful.

Deleting a line *inside* a block does not clear the block. The later boundary is shifted so the block continues to cover the same surviving logical lines.

## Undo and dirty state

Compatibility line deletion captures one snapshot before the mutation, marks the document dirty and leaves one undo entry. This is intentionally simpler than the historical line-by-line undo representation. The snapshot backend is a correctness-first implementation detail behind the existing undo boundary and can be optimized later.

## Current integration boundary

The topology service is now used by:

- the semantic `DeleteLine` compatibility path;
- compatibility file insertion through `EditorFileService`.

It is not yet the universal mutation gateway for every structural operation. Newline insertion, automatic wrapping, line joining, block copy/move/delete and paragraph reformatting still require a procedure-by-procedure topology audit. Until that audit is complete, the compatibility matrix describes `EditDelline`/`EditRealign` parity as an expanding foundation rather than complete coverage.

## Tests

`LineTopologyCompatibilityTests` covers:

- marker invalidation on a deleted line;
- shifting a later marker;
- cursor and top-line realignment in multiple linked windows;
- block-boundary deletion and highlight cleanup;
- deletion inside an existing block;
- the one-line blank-but-do-not-remove rule;
- compatibility file insertion realigning linked views, markers and block limits while preserving the initiating cursor.

The tests intentionally validate observable editor behavior, not Pascal pointer layout.

## Performance policy

The current implementation scans the open windows and marker table when a structural change is reported. V1 favors correctness, traceability and simple invariants. If profiling later shows that large documents or many anchors need a faster representation, interval trees, persistent anchors or buffer-native anchor tracking can replace the internal strategy without changing the compatibility contract.
