# FIRST-ED kernel compatibility audit

## Scope of this audit slice

This document records the clean-room review of several small but important Turbo Editor Toolbox V1 procedures whose observable behavior differs from common modern editor defaults.

The goal is not a source translation. The handbook is used as a behavioral specification; C# data structures, memory management and naming remain independent.

## Audited procedures

| Historical routine/concept | .NET implementation | Preserved behavior |
|---|---|---|
| `EditLeftChar` | `FirstEdPrimitiveCompatibilityProcessor.MoveLeftChar` | Moving left from column 1 enters the previous line immediately after its last non-blank character. At the first character of the text stream, no movement occurs. |
| `EditRightChar` | `FirstEdPrimitiveCompatibilityProcessor.MoveRightChar` | The command increments the column without crossing a logical line boundary. A virtual column beyond end-of-line is therefore possible. |
| `EditLeftWord` | `FirstEdPrimitiveCompatibilityProcessor.MoveLeftWord` | Leading indentation first collapses to column 1; from column 1 the previous line is entered after its last non-blank. Otherwise the previous same-line word/class start is selected. |
| `EditRightWord` | `FirstEdPrimitiveCompatibilityProcessor.MoveRightWord` | Alphanumeric, punctuation and blank runs are distinct classes. Movement crosses the current class and following blanks; from beyond the last non-blank it enters the next line at column 1. |
| `EditTab` | `FirstEdPrimitiveCompatibilityProcessor.Tab` | The cursor advances to the next tab stop. In Insert mode the required padding is inserted; in Overtype mode the operation is cursor movement only. |
| `EditSetMarker` / `EditJumpMarker` | `EditorMarker`, `EditorSession.SetMarker`, `JumpToMarker` | A marker identifies a logical line. Jumping to it may switch windows, but the selected target view keeps its own column. |
| `EditOffblock` | `EditorSession.ClearBlockHighlights` | Clears the `InBlock` flag across every open text stream while retaining the logical block limits. |
| `EditMarkblock` | `EditorSession.MarkBlockHighlights` | Re-projects the active whole-line block into `InBlock` line flags without changing the block limits. |

## Why these rules do not live in `EditorEngine`

`EditorEngine` is the reusable editing layer for SASD applications. Several historical FIRST-ED rules are deliberately unusual by modern standards. For example, a right-character command may leave the cursor in a virtual column rather than move to the next logical line, while left-character movement from column one uses the previous line's last *non-blank* position.

The historical word commands also use a three-class model rather than the more common modern notion of an identifier token: alphanumeric characters, punctuation and blanks are separate runs. These rules therefore live in `FirstEdPrimitiveCompatibilityProcessor`. This keeps the modern engine useful on its own while making the compatibility profile explicit, testable and portable to later C++, Java and JavaScript implementations.

## Word-class policy

For ASCII text, the implementation reproduces the handbook's three visible classes directly. The .NET implementation deliberately uses Unicode-aware `char.IsLetterOrDigit` and `char.IsWhiteSpace`; every other character is treated as punctuation. This preserves the historical result for ASCII input while avoiding an artificial ASCII-only restriction in the reusable library.

`EditRightWord` moves across the current non-blank class and then any blanks. If the cursor is already beyond the last non-blank character, the next invocation moves to column zero of the following logical line. `EditLeftWord` treats leading indentation specially: a cursor inside or immediately after the leading blank region first moves to column zero, and only a subsequent invocation crosses to the preceding logical line.

## Marker model

The Turbo Editor Toolbox marker is line-oriented. The first .NET version now represents a marker as document identity plus logical line index instead of document identity plus complete cursor position. A jump changes the line but not the target window's existing column.

The historical implementation used line-descriptor pointers, so a marker naturally followed that descriptor while other lines were inserted around it. The .NET implementation stores a logical index and realigns it through `EditorLineTopology` on the audited insertion, deletion, join, reformat and bulk-block paths. This describes audited command paths, not an assertion that every future internal mutation automatically participates. See [line topology](LINE-TOPOLOGY-AND-REALIGNMENT.md).

## Block highlighting model

Logical block definition and visual block flags are separate concepts. `ClearBlockHighlights` removes every `InBlock` bit from every unique open document but leaves `EditorSession.Block` intact. `MarkBlockHighlights` can then restore the active block's line flags.

This distinction matters for recovery from stale or corrupted block highlighting and mirrors the handbook's separation between block limit state and `EditOffblock`.

## Deliberate modern replacements

- The Pascal `Maxint` guard used by `EditRightChar` is replaced with an ordinary CLR `int.MaxValue` overflow guard. A 16-bit integer limit is not imposed on modern documents.
- Pointer identities, descriptor free lists and manual memory release are not reproduced. Their observable editor invariants are audited instead.
- The historical `EditMarkblock` could stop marking when keyboard input appeared. The current in-memory flag pass is small and synchronous; input-first scheduling and cancellation remain separate concerns. We do not claim this old paint-time optimization as implemented behavior.
- The historical toolbox exposes a global tab-size variable. The current SASD model keeps `TabSize` with window options so independent views can differ. That is a documented modern design choice and remains part of the final V1 divergence review.

## Tests added

`CompatibilityKernelAuditTests` now covers:

- left-character movement across a line boundary with trailing blanks;
- right-character movement into a virtual column without changing the line;
- left-word behavior around leading indentation and previous-line transitions;
- right/left word-class behavior across alphanumeric, punctuation and blank runs;
- right-word transition from the end of one logical line to the next;
- Insert-mode tab mutation, dirty state and undo capture;
- Overtype-mode tab movement without mutation, dirty state or undo;
- line-only marker jumps preserving the destination view column;
- `EditOffblock`-style global flag clearing followed by block re-marking.

## Next low-level audit

The later line-topology, reformat, command-boundary validation and typed error/resource audits have been implemented and tested. Consult the focused documents for their current status:

- [Line topology](LINE-TOPOLOGY-AND-REALIGNMENT.md)
- [Reformat compatibility](REFORMAT-COMPATIBILITY.md)
- [Command-boundary validation](COMMAND-BOUNDARY-VALIDATION.md)
- [Error model](ERROR-MODEL.md)

The remaining V1 review follows the [full handbook procedure/function inventory](HANDBOOK-PROCEDURE-INDEX-AUDIT.md). `EditDestxtdes` is a memory-release primitive, not a C# pointer API to recreate.

## Center Line: repeat commands and virtual margins

`EditorEngine.CenterLine` now computes padding using wide integer arithmetic to avoid signed overflow on large virtual margin values. An already centered physical line leaves Version, dirty state and Undo unchanged, even when the active cursor is returned from virtual space to the physical line end. Other linked windows keep their cursor state. Real centering remains one undoable mutation; an unrepresentable resulting physical line is rejected before any edit. This is a modern no-op and size-safety policy.

Centering also compares the required line with the existing buffer before allocating a duplicate. A modern resource-safety guard rejects attempts to generate more than **1,000,000 leading spaces** from huge virtual margins, before beginning Undo or mutating data. This limit applies to automatic padding generation only, not to the ability to keep or inspect an existing longer line. It prevents a valid but extreme one-based margin request from causing a large accidental allocation.

## Direct editor engine: virtual-space/no-op editing guards

The public `EditorEngine` is also usable without the FIRST-ED command dispatcher. Its direct entrypoints now avoid creating dirty/version/Undo transactions for CR-only text insertion and right-character/right-word deletion where no physical character or following line remains. Direct Backspace through virtual space decrements only the initiating view's cursor; it no longer attempts an invalid substring removal after an Undo transaction has begun. These modern safety rules mirror the already-tested command boundaries without replacing the historical command processors.
