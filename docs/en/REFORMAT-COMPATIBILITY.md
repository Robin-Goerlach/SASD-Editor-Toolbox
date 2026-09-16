# FIRST-ED paragraph reformat compatibility

## Scope

This note records the clean-room transfer of the historical `EditReformat` family into the .NET implementation. The handbook is used as a behavioral specification; the Pascal pointer-splicing implementation is not copied.

The historical family consists of `EditCompressLine`, `EditShiftLine`, `EditLongLine`, `EditShortLine` and `EditReformat`. In the .NET version their observable responsibilities are coordinated by `FirstEdReformatCompatibilityProcessor` instead of exposing a pointer-shaped API.

## Historical behavior preserved

`EditCompressLine` reduces repeated spaces so the later line-splicing logic can work with normalized word separators. `EditShiftLine` ensures the first non-blank character is at or to the right of the current left margin. `EditLongLine` moves words down when the current line exceeds the right margin, while `EditShortLine` pulls words up from a wrapped continuation when they still fit. `EditReformat` repeats that process until the paragraph is exhausted and works independently of the current Wordwrap mode.

The implementation expresses those rules as a planning pass:

1. identify the paragraph beginning on the current logical line;
2. normalize its words;
3. validate that every individual word fits between the current margins;
4. build the complete target set of lines without mutating the document;
5. apply replacements/insertions/deletions in one committed edit;
6. report structural line-count changes to `EditorLineTopology`.

This is deliberately easier to review and test than reproducing the original descriptor-splicing sequence.

## Meaning of `Wrapped`

V1 now treats `EditorLineFlags.Wrapped` as a **soft outgoing line boundary**: when the bit is set on line *N*, the boundary from line *N* to line *N+1* came from word wrapping rather than from an explicit paragraph-ending newline.

That interpretation is important because the handbook states that `EditNewLine` clears `Wrapped` on the previously current line to mark the end of a paragraph. The compatibility file format expresses the same relationship with the high-bit carriage return written after a wrapped line.

The automatic word-wrap path in `EditorEngine` has been aligned with that model: the upper line receives `Wrapped`; a pre-existing downstream soft boundary is transferred to the newly inserted lower fragment. Ordinary character insertion no longer clears the boundary flag.

## Margins and word movement

The reformatter uses the inclusive range from `LeftMargin` through `RightMargin`. Text is emitted with left-margin padding, and words are separated by a single blank after compression. Words are greedily added to a line for as long as the next word still fits. This reproduces the observable push-down/pull-up behavior of the historical long-line and short-line helpers without exposing those helpers as public APIs.

The handbook describes a "word too long" error when an indivisible word cannot be fitted. The .NET implementation validates the entire plan first. If a word is wider than the available margin width, dispatch fails without changing text, dirty state, cursor state or undo history.

For modern text, whitespace normalization uses .NET Unicode whitespace classification. ASCII behavior remains compatible with the handbook; accepting additional Unicode whitespace is a documented extension.

## Cursor and Wordwrap mode

The technical reference describes where reformatting starts and how words are moved, but does not prescribe a final cursor relocation. The .NET V1 policy therefore preserves the initiating logical cursor line and column, including a valid virtual column.

`EditReformat` is independent of `EditorWindowOptions.WordWrap`. Wordwrap controls automatic wrapping during text entry; explicit paragraph reformatting always obeys the configured margins.

## Topology and line identity

The planner may produce more or fewer logical lines than the source paragraph. Surviving line positions are replaced first; extra target lines are inserted after the old paragraph footprint, while surplus source lines are deleted from the tail of that footprint. Insert/delete counts are then reported through `EditorLineTopology`.

Consequently, windows, top-line anchors, markers and block limits that refer to later surviving logical lines are realigned. Markers on paragraph lines that are physically removed by a shrinking reformat become undefined according to the normal topology contract.

`UserColored` is retained on surviving descriptor positions. `InBlock` is not copied blindly: block highlighting is re-projected from the logical block after structural realignment.

## Undo and failure atomicity

A successful reformat captures one undo snapshot before mutation and marks the document dirty. A no-op reformat creates no undo entry and leaves the dirty state unchanged. Plan-validation failures happen before the snapshot and before any buffer mutation.

This is intentionally correctness-first. V1 does not optimize paragraph planning, snapshots or line-anchor lookup prematurely.

## Tests

`ReformatCompatibilityTests` covers margin wrapping with Wordwrap both disabled and enabled, space compression, pulling words up, hard paragraph boundaries, line-count expansion and contraction, marker/linked-window realignment, cursor preservation, `UserColored` preservation, no-op behavior and atomic rejection of an over-wide word.

`InsertionCompatibilityTests` additionally verifies the outgoing-boundary interpretation during automatic word wrapping and ordinary text insertion on an already wrapped line.

## Remaining related work

The paragraph-reformat family is now behaviorally represented, but V1 still needs the final bulk-topology audit for block copy/move/delete, command-boundary validation, complete long-operation abort coverage and typed historical error resources. Those concerns remain separate from the reformat algorithm so later optimization can replace the internal planning strategy without changing the compatibility contract.
