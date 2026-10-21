# FIRST-ED line insertion and New Line compatibility

## Scope

This note records the clean-room transfer of the historical `EditInsertLine` and `EditNewLine` behavior into the .NET compatibility layer. The distinction matters because FIRST-ED assigns different semantics to **Ctrl-N** and **Return** even though both operations can create a logical line.

The implementation intentionally preserves observable editing behavior rather than reproducing Turbo Pascal line-descriptor pointers, heap allocation or DOS display mechanics.

## Two different semantic commands

The portable command vocabulary now distinguishes:

- `EditorCommandId.InsertLine` — the compatibility counterpart of `EditInsertLine` and the FIRST-ED Ctrl-N command;
- `EditorCommandId.NewLine` — the compatibility counterpart of `EditNewLine`, used by the Return key.

`FirstEdKeyMap` therefore maps Ctrl-N to `InsertLine` and Enter/Return to `NewLine`. Keeping them separate prevents a host from silently losing the historical Insert/Overtype behavior of Return.

## EditInsertLine

`FirstEdPrimitiveCompatibilityProcessor.InsertLine` implements the three documented cases around the current cursor position:

1. **Column zero:** the current line becomes blank and its complete text is moved to the newly inserted line below it. From the user's point of view this is a blank line inserted above the old text.
2. **At or beyond the last non-blank character:** a blank logical line is inserted below the current line.
3. **Inside meaningful text:** the current line is split at the cursor and the text on the right moves to the newly inserted lower line.

The current window remains on the upper line. A successful operation captures one undo snapshot, marks the document dirty, and reports the new line to `EditorLineTopology` so linked windows, markers and block limits can be realigned.

The historical implementation expresses these relationships through linked line-descriptor identity. The .NET port uses document identity plus logical line numbers. Where the handbook does not specify whether an unrelated anchor should stay with a particular descriptor after a split, the current V1 rule is the documented `EditorLineTopology` line-number policy rather than an invented pointer emulation.

## EditNewLine / Return

`EditNewLine` is mode-sensitive.

### Insert mode

Return performs the same structural split as `EditInsertLine`, but the active cursor follows the split to the newly created lower line. Autoindent changes only the resulting cursor column: when enabled, the cursor is positioned under the first non-blank character of the previously current line; when that line is blank, or Autoindent is disabled, the cursor moves to column zero.

The `Wrapped` flag on the previously current line is cleared. This marks the explicit Return as a paragraph boundary for the historical reformat logic.

### Overtype mode

Return normally performs cursor movement only: it moves to the following logical line without splitting the current line. If the cursor was already on the final line in the text stream, a new blank line is appended and the cursor moves to it.

Autoindent uses the same cursor-column rule as Insert mode. Clearing a previously set `Wrapped` flag is still a text-state mutation; therefore the .NET implementation captures undo and sets dirty state when that flag actually changes even if no line is inserted.

## Structural realignment

Every logical-line insertion performed by these compatibility commands calls `EditorLineTopology.LinesInserted`. The same coordination is now used by the reusable engine when its generic newline or automatic word-wrap path inserts a line.

This closes an important class of stale-reference bugs: a linked window below an insertion point, a marker on a later line, or a later block boundary is shifted with the document instead of continuing to point at the old numeric position.

`EditRealign` in the historical toolbox updates line-related window state after structural changes; it does not define a rule that collapses the window's column to the physical text length. Accordingly, `EditorWindow.ClampCursor` now clamps line and viewport coordinates while preserving non-negative virtual columns.

## Wrapped continuation lines

The reusable word-wrap path inserts a new lower line with `EditorLineFlags.Wrapped` and reports that insertion through the topology coordinator. This is consistent with the reformat helpers' use of the `Wrapped` bit on the continuation line to decide whether the paragraph continues.

The exact `EditLongLine` / `EditShortLine` / `EditShiftLine` / `EditReformat` algorithm is a separate compatibility milestone; this change prepares the structural invariants it needs rather than prematurely replacing the existing paragraph formatter.

## Undo and optimization policy

V1 favors correctness and auditability. A single compatibility line-insertion/New-Line operation creates one snapshot undo entry when it mutates text or line flags. The implementation is intentionally not optimized around descriptor deltas yet; the existing undo boundary allows a more compact journal to replace snapshots later without changing command semantics.

## Regression coverage

`InsertionCompatibilityTests` verifies:

- Return and Ctrl-N are distinct semantic commands;
- all three `EditInsertLine` cases;
- cursor retention for Insert Line;
- Insert-mode New Line cursor movement and Autoindent;
- Overtype New Line without unnecessary text mutation;
- creation of a final blank line in Overtype mode;
- clearing the previous line's `Wrapped` flag;
- linked-window and marker realignment during automatic word-wrap.

Deletion-side joins are covered independently by `DeletionCompatibilityTests` so insertion and deletion topology remain testable from both directions.


## V1 handbook audit: preservation of the outgoing Wrapped boundary

A targeted review of `EditInsertLine` (printed p. 183) and `EditNewLine` (p. 191), together with the already defined V1 outgoing-boundary model, exposed a previously untested serialization defect: `InsertLineCore` retained the old `Wrapped` bit on the upper descriptor and created an unwrapped lower descriptor. That was incorrect when the original line already ended at a soft boundary to the *following* existing line: a split must insert a **new hard separator between upper and lower**, then preserve the **old outgoing soft separator after lower**. Without this transfer, writing the legacy 8-bit file format could emit 0x8D at the new boundary and CR/LF at the old boundary, changing paragraph semantics despite correctly displayed text.

The corrected shared splitter now computes `upperFlags = oldFlags & ~Wrapped` and `lowerFlags = oldFlags & Wrapped` before modifying text. This deliberately keeps `UserColored` and other presentation annotations on the surviving upper descriptor; transient `InBlock` is reprojected after `EditorLineTopology.LinesInserted`. The rule is consistent for column zero, middle and beyond-last-nonblank insertion. Insert-mode `NewLine` keeps the lower fragment's outgoing boundary and moves the cursor; Overtype mode still clears the old upper `Wrapped` boundary without splitting, as directly stated in the handbook. Autoindent, linked-window realignment, markers, block endpoints and single-step Undo retain their established responsibilities.

**Executable evidence:** Seven portable cases in `core-smoke.json` exercise all split positions, both command forms, autoindent and Undo. New `LineSplitOutgoingBoundaryTests` directly verify actual legacy CR/LF and 0x8D output bytes, surviving presentation flags, linked window/marker realignment, Undo, and the existing one-line `EditDeleteLine` exception. The final-line delete rule (p. 162) remains implemented as **blank the existing stored span without deleting its descriptor**, while invalidating marker and block references.

This is an observed V1 .NET bug fixed against the normative outgoing-boundary model, not a claim that every internal Pascal flag or exact 1985 storage transition was independently specified by the manual.
