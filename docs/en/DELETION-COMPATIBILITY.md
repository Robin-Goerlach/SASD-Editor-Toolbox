# FIRST-ED deletion compatibility

## Scope

This note records the clean-room transfer of the deletion behaviors currently routed through the FIRST-ED compatibility layer. It complements the line-topology documentation: deletion is both a text mutation and, when a complete logical line disappears, a topology mutation.

## Delete Right Character

`EditDeleteRightChar` removes the character at the cursor while the cursor is still inside meaningful text. The handbook gives a separate line-boundary rule: when the cursor is beyond the current line's last non-blank character, the line below is joined to the current line if one exists.

`FirstEdPrimitiveCompatibilityProcessor.DeleteRightCharacter` keeps that distinction. It does not treat trailing blanks as ordinary characters once the cursor has reached the position after the last non-blank. A line join removes the following logical line and reports that deletion to `EditorLineTopology`, so linked windows, markers and block limits are repaired consistently.

## Delete Right Word

The historical word definition is shared with the word-navigation audit: characters are divided into three classes—alphanumeric, punctuation and blanks. `EditDeleteRightWord` removes the run in the class under the cursor and then removes immediately following blanks. Punctuation is therefore not silently merged with the preceding alphanumeric word.

Examples of the resulting class behavior:

```text
abc!!  def
   ^
```

Deleting right word at the first `!` removes `!!  ` and leaves `abcdef`.

When the cursor is at or beyond the first position after the last non-blank character, Delete Right Word joins the line below instead of deleting trailing blanks. If no following line exists, no mutation is performed.

The .NET implementation uses Unicode-aware `char.IsLetterOrDigit` and `char.IsWhiteSpace`. ASCII input retains the historical three-class behavior while the reusable core remains usable for modern text.

## Delete Line

The semantic `DeleteLine` command is also routed through the compatibility processor. Its special rules are documented in `LINE-TOPOLOGY-AND-REALIGNMENT.md`: the sole remaining logical-line container is retained and blanked, markers on a deleted line become undefined, block-boundary deletion disables the active block representation, and all surviving references are realigned.

## Undo and no-op behavior

A successful deletion captures one snapshot before mutation and marks the document dirty. A boundary command that has nothing to delete or join returns `false` without creating an undo entry or dirtying the document.

This is intentionally correctness-first. V1 does not optimize snapshots or line-anchor lookup prematurely.

## Tests

`DeletionCompatibilityTests` covers:

- punctuation as an independent word class;
- removal of immediately following blanks;
- Delete Right Word line joining at the trailing boundary;
- marker invalidation and shifting caused by that join;
- ordinary one-character deletion;
- Delete Right Character line joining and linked-window realignment.

Together with `LineTopologyCompatibilityTests`, these tests make the deletion/topology boundary executable rather than documentation-only.

## Remaining deletion audit

`DeleteLeftCharacter` and the lower-level details of some block/reformat mutations still use general engine paths and need their own handbook audit before exact V1 parity is claimed. The compatibility matrix therefore distinguishes implemented right-side/line deletion rules from the remaining deletion work.
