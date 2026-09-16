# FIRST-ED kernel compatibility audit

## Scope of this audit slice

This document records the clean-room review of several small but important Turbo Editor Toolbox V1 procedures whose observable behavior differs from common modern editor defaults.

The goal is not a source translation. The handbook is used as a behavioral specification; C# data structures, memory management and naming remain independent.

## Audited procedures

| Historical routine/concept | .NET implementation | Preserved behavior |
|---|---|---|
| `EditLeftChar` | `FirstEdPrimitiveCompatibilityProcessor.MoveLeftChar` | Moving left from column 1 enters the previous line immediately after its last non-blank character. At the first character of the text stream, no movement occurs. |
| `EditRightChar` | `FirstEdPrimitiveCompatibilityProcessor.MoveRightChar` | The command increments the column without crossing a logical line boundary. A virtual column beyond end-of-line is therefore possible. |
| `EditTab` | `FirstEdPrimitiveCompatibilityProcessor.Tab` | The cursor advances to the next tab stop. In Insert mode the required padding is inserted; in Overtype mode the operation is cursor movement only. |
| `EditSetMarker` / `EditJumpMarker` | `EditorMarker`, `EditorSession.SetMarker`, `JumpToMarker` | A marker identifies a logical line. Jumping to it may switch windows, but the selected target view keeps its own column. |
| `EditOffblock` | `EditorSession.ClearBlockHighlights` | Clears the `InBlock` flag across every open text stream while retaining the logical block limits. |
| `EditMarkblock` | `EditorSession.MarkBlockHighlights` | Re-projects the active whole-line block into `InBlock` line flags without changing the block limits. |

## Why these rules do not live in `EditorEngine`

`EditorEngine` is the reusable editing layer for SASD applications. Several historical FIRST-ED rules are deliberately unusual by modern standards. For example, a right-character command may leave the cursor in a virtual column rather than move to the next logical line, while left-character movement from column one uses the previous line's last *non-blank* position.

Those rules therefore live in `FirstEdPrimitiveCompatibilityProcessor`. This keeps the modern engine useful on its own while making the compatibility profile explicit, testable and portable to later C++, Java and JavaScript implementations.

## Marker model

The Turbo Editor Toolbox marker is line-oriented. The first .NET version now represents a marker as document identity plus logical line index instead of document identity plus complete cursor position. A jump changes the line but not the target window's existing column.

The historical implementation used line-descriptor pointers, so a marker naturally followed that descriptor while other lines were inserted around it. The .NET implementation currently stores an index. Maintaining marker identity through every low-level line-topology mutation belongs to the upcoming `EditDelline`/`EditRealign` audit and is **not** claimed as complete yet.

## Block highlighting model

Logical block definition and visual block flags are separate concepts. `ClearBlockHighlights` removes every `InBlock` bit from every unique open document but leaves `EditorSession.Block` intact. `MarkBlockHighlights` can then restore the active block's line flags.

This distinction matters for recovery from stale or corrupted block highlighting and mirrors the handbook's separation between block limit state and `EditOffblock`.

## Deliberate modern replacements

- The Pascal `Maxint` guard used by `EditRightChar` is replaced with an ordinary CLR `int.MaxValue` overflow guard. A 16-bit integer limit is not imposed on modern documents.
- Pointer identities, descriptor free lists and manual memory release are not reproduced. Their observable editor invariants are audited instead.
- The historical `EditMarkblock` could stop marking when keyboard input appeared. The current in-memory flag pass is small and synchronous; input-first scheduling and cancellation remain separate concerns. We do not claim this old paint-time optimization as implemented behavior.
- The historical toolbox exposes a global tab-size variable. The current SASD model keeps `TabSize` with window options so independent views can differ. That is a documented modern design choice and remains part of the final V1 divergence review.

## Tests added

`CompatibilityKernelAuditTests` covers:

- left-character movement across a line boundary with trailing blanks;
- right-character movement into a virtual column without changing the line;
- Insert-mode tab mutation, dirty state and undo capture;
- Overtype-mode tab movement without mutation, dirty state or undo;
- line-only marker jumps preserving the destination view column;
- `EditOffblock`-style global flag clearing followed by block re-marking.

## Next low-level audit

The next group should concentrate on line topology rather than raw Pascal memory management:

1. `EditDelline` and the observable anchor rules for window cursor/top line, block limits and markers;
2. `EditRealign` and viewport repair after a line is removed;
3. `EditLeftWord` / `EditRightWord`, including FIRST-ED's exact whitespace and boundary rules;
4. the `EditLongLine`, `EditShortLine` and `EditShiftLine` helpers used by paragraph reformatting;
5. historical error codes/resources where they materially affect command behavior.

`EditDestxtdes` itself is a memory-release primitive and should not be recreated as a public C# pointer/free-list API.
