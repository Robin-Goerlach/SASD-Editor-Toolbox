# FIRST-ED deletion compatibility

## Scope

This note records the clean-room transfer of the deletion behaviors routed through the FIRST-ED compatibility layer. It complements the line-topology documentation: deletion is both a text mutation and, when a complete logical line disappears, a topology mutation.

## Delete Left Character

`EditDeleteLeftChar` removes the character immediately to the left of the cursor and moves the cursor left one column. At column zero, the current logical line is joined to the preceding line if one exists; at the first column of the first logical line the command performs no operation.

`FirstEdPrimitiveCompatibilityProcessor.DeleteLeftCharacter` keeps the line-boundary operation structural. The current line descriptor disappears, the current text is appended at the previous line's last non-blank end, and `EditorLineTopology` repairs linked windows, markers and block limits. The active cursor finishes at the join column on the surviving previous line.

FIRST-ED can also hold a cursor in a virtual column beyond the physical line length. The handbook documents this ability for rightward cursor movement but does not separately define Backspace in such unstored space. The .NET V1 policy is therefore explicit rather than presented as historical fact: Backspace through virtual space moves the cursor left without dirtying text or creating undo until a stored character is reached.

## Delete Right Character

`EditDeleteRightChar` removes the character at the cursor while the cursor is still inside meaningful text. The handbook gives a separate line-boundary rule: when the cursor is beyond the current line's last non-blank character, the line below is joined to the current line if one exists.

The join is performed at the cursor position, matching the contract of the historical `EditJoinline` helper: the first character of the following line is placed where the cursor stands. Trailing blank storage to the right of that logical join point is therefore not retained as an artificial gap. Removing the following logical line is reported to `EditorLineTopology`, so linked windows, markers and block limits are repaired consistently.

## Delete Right Word

The historical word definition is shared with the word-navigation audit: characters are divided into three classes—alphanumeric, punctuation and blanks. `EditDeleteRightWord` removes the run in the class under the cursor and then removes immediately following blanks. Punctuation is therefore not silently merged with the preceding alphanumeric word.

Example:

```text
abc!!  def
   ^
```

Deleting right word at the first `!` removes `!!  ` and leaves `abcdef`.

When the cursor is at or beyond the first position after the last non-blank character, Delete Right Word joins the line below instead of deleting trailing blanks. As with Delete Right Character, the join location is the cursor position. If no following line exists, no mutation is performed.

The .NET implementation uses Unicode-aware `char.IsLetterOrDigit` and `char.IsWhiteSpace`. ASCII input retains the historical three-class behavior while the reusable core remains usable for modern text.

## Delete Line

The semantic `DeleteLine` command is routed through the compatibility processor. Its special rules are documented in `LINE-TOPOLOGY-AND-REALIGNMENT.md`: the sole remaining logical-line container is retained and blanked, markers on a deleted line become undefined, block-boundary deletion disables the active block representation, and all surviving references are realigned.

## Virtual columns and structural normalization

Virtual non-negative cursor columns are valid compatibility state. `EditorWindow.ClampCursor` therefore normalizes line and viewport coordinates after structural edits without collapsing the column to `Text.Length`. This is particularly important for linked windows: `EditRealign` repairs line-related window state, but should not silently rewrite an independent view's horizontal cursor position.

## Undo and no-op behavior

A successful text deletion captures one snapshot before mutation and marks the document dirty. A boundary command that has nothing to delete or join returns `false` without creating an undo entry or dirtying the document. Cursor-only movement through virtual space also leaves undo and dirty state untouched.

This remains intentionally correctness-first. V1 does not optimize snapshots or line-anchor lookup prematurely.

## Tests

`DeletionCompatibilityTests` covers:

- ordinary Delete Left Character behavior;
- previous-line joining at column zero and realignment of linked windows/markers;
- the start-of-stream no-op;
- the documented .NET policy for Backspace through virtual columns;
- punctuation as an independent right-word class;
- removal of immediately following blanks;
- Delete Right Word line joining at the trailing boundary;
- marker invalidation and shifting caused by joins;
- ordinary Delete Right Character behavior;
- Delete Right Character line joining and linked-window realignment.

Together with `LineTopologyCompatibilityTests` and `InsertionCompatibilityTests`, these tests exercise the insertion/deletion topology boundary in both directions.

## Remaining deletion-related audit

The primary character/word/line deletion semantics are now transferred. Remaining structural risk is concentrated in bulk block move/copy/delete and the existing paragraph-reformat `ReplaceAll` path, which must be audited against `EditorLineTopology` before exact V1 structural parity is claimed for those operations.
