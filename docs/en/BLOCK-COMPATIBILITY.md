# FIRST-ED block compatibility

## Scope

This document records the clean-room transfer of the Turbo Editor Toolbox whole-line block manipulation family into the C#/.NET implementation.

The handbook is used as a behavioral requirements source only. The implementation does not reproduce the Pascal linked-descriptor algorithms, pointer arithmetic, heap management or source structure.

## Historical behavior used as the contract

The handbook defines one contiguous whole-line block at a time. The block may be manipulated from another window or another file/document. The three fundamental operations are:

- **Copy**: insert copies of the block lines at the current cursor location while leaving the source text unchanged.
- **Move**: remove the block lines from their original stream and insert them at the current cursor location. The move must reject a cursor that is inside the block being moved.
- **Delete**: remove every line in the block. The technical reference explicitly requires line deletion to pass through the low-level delete/realignment machinery so multiple windows and Undo state are handled.

The handbook describes these operations in terms of `Blockfrom`, `Blockto`, line-descriptor pointers and `EditDelline`. Those representation details are not part of the portable SASD contract.

## Modern C# design

`FirstEdBlockCompatibilityProcessor` owns the compatibility behavior for Copy, Move and Delete. `EditorCommandDispatcher` and the direct `EditorEngine` block methods both route through this processor so the two entry paths cannot drift.

Structural changes are reported to `EditorLineTopology`. That service realigns linked-window current lines and top lines, markers and the logical block range. The target cursor is therefore treated as an anchor to its existing logical line: inserting a copied or moved block immediately before that line changes its numeric line index but does not silently attach the cursor to one of the inserted lines.

### Copy

The block is snapshotted before insertion. `InBlock` is not copied as ordinary line data because it is a projection of the current logical block. `Wrapped` and `UserColored` metadata are retained. The active block remains the source block; if insertion changes numeric indices in the same document, topology realignment keeps its boundaries attached to the original source lines.

### Delete

The complete block range is removed and then reported as one logical deletion to the topology layer. If the block spans the whole stream, the buffer still retains its required single blank logical line. References to deleted logical lines are invalidated or realigned according to the normal deletion contract.

### Move

The source lines are snapshotted, removed through the topology boundary and inserted before the target cursor line. For a same-document move, the cursor must be outside the source block. After removal, the already-realigned target cursor supplies the insertion index; this avoids fragile manual index arithmetic.

After insertion, the logical block is recreated around the moved lines and its hidden state is retained. Cross-document moves mark both documents dirty and realign references in both streams.

## Explicit modernization boundaries

The current .NET `EditorBlock` stores a complete start/end pair. The historical `Blockfrom` and `Blockto` pointers could be independently undefined. Exact Begin/End endpoint-state parity therefore remains a separate audit item.

The handbook does not separately state whether a marker pointing to a line inside a **moved** block should follow that physical line to its new location. The .NET implementation uses conservative deletion semantics for source anchors: markers on moved-away source lines become undefined; the logical block itself is explicitly recreated at the destination. This is documented implementation policy, not a claim about undocumented historical behavior.

The correctness-first snapshot Undo backend records the source and target of a cross-document move as two snapshots. Both streams are recoverable, but one compound cross-document Undo transaction is not yet modeled. A future undo journal may improve that without changing block or topology semantics.

No Pascal pointer splicing, descriptor free-list behavior or manual memory release is exposed by the modern API.

## Regression coverage

The block compatibility tests cover copying into another document, preservation of non-block line metadata, linked-window and marker realignment on deletion, complete-stream deletion, same-document moves above/below the source range, rejection of a destination inside the source block, cross-document moves, block relocation, and parity between command-dispatch and direct-engine entry points.
