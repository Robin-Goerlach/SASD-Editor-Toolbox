# Compound undo transactions and navigation state

## Purpose

The first .NET implementation deliberately uses a correctness-first snapshot undo backend. The historical Turbo Editor Toolbox used a different, line-oriented Undo stack: its `EditUndo` operation removes one saved text line and inserts it above the current line, and the block-delete documentation says low-level deletion prepares deleted lines for Undo.

That historical storage model does **not** define the semantics of a modern command transaction spanning two documents. SASD therefore treats compound command undo as an explicit modernization boundary.

## Decision: one logical mutation, one history item

A successful logical SASD mutation command contributes at most one undo history item.

This matters most for a block move between documents. The command removes lines from the source and inserts them into the target. Recording those changes as two independent user-visible undo items is unsafe: one Undo could restore the source while leaving the target copy in place, temporarily duplicating the moved text.

`EditorUndoManager.BeginTransaction` therefore accepts one or more participating windows. It de-duplicates their documents and captures every affected document before the mutation. `EditorUndoTransaction.Commit` records the transaction only after the command has completed its text mutation, topology repair and final navigation state.

A cross-document block move consequently increments `EditorUndoManager.Count` by exactly one.

## Why Begin/Commit is explicit

The previous `Capture` API immediately pushed a snapshot. That is adequate for a very small single-document mutation, but it cannot distinguish a command that subsequently fails from one that completes, and it cannot know the command's final cursor/anchor state.

The transaction API separates the two moments:

```text
BeginTransaction
    -> capture pre-command documents + session state
perform mutation
repair topology / place final cursor
Commit
    -> capture post-command session state
    -> add exactly one history entry
```

If the command throws or returns before `Commit`, the pending transaction is not added to history.

The older `Capture(EditorWindow, ...)` method remains available as a low-level compatibility API for existing callers, but repository-owned mutation paths now use explicit transactions.

## State restored by a committed transaction

Document snapshots restore:

- logical line text and line flags;
- persistence metadata;
- dirty state;
- document version.

The transaction also records the session state that structural editing can change indirectly:

- cursor, top-line and left-column state for every displayed window;
- current-window identity;
- numbered markers;
- independent block begin/end state.

Window editing options, stacked-window geometry and document/window attachment are not part of a text transaction because the audited mutation commands do not own those concerns.

## Preserving later explicit user actions

Undo must repair command-induced topology state, but it should not blindly erase navigation performed *after* the edit.

For that reason, the transaction records both pre-command and post-command session state. During Undo a window, marker, block state or current-window selection is restored only when its current value still matches the recorded post-command value. A later explicit cursor movement, marker assignment or block redefinition therefore wins.

After document restoration, surviving markers and block endpoints are normalized against the restored streams. An anchor that a later user action placed on a line that no longer exists after Undo is invalidated instead of leaving an out-of-range reference.

This post-state comparison is a correctness-first policy; it avoids needing stable per-line object identities merely to distinguish automatic topology movement from later user navigation.

## Destructive commands and compound history

`EditWindowDeleteText` remains deliberately destructive and non-undoable. When it destroys a document, `EditorUndoManager.DiscardDocument` removes every transaction referencing that document.

For a compound transaction this removal is all-or-nothing. If a cross-document move transaction references source and target and one side is deliberately destroyed, the target half is not retained as a separately undoable fragment.

## Undo limit

`EditorUndoManager.Limit` counts logical transactions. A two-document move consumes one slot, not two.

The FIRST-ED compatibility command for setting the limit still requires a value of at least one. The lower-level modern manager can use zero to disable capture when configured directly by an application.

## Regression coverage

`CompoundUndoCompatibilityTests` verifies:

- one cross-document block move creates exactly one undo item;
- one Undo restores both source and target documents;
- linked-window cursor/top-line/left-column state is restored;
- block limits and marker positions affected by topology are restored;
- explicit post-edit navigation and marker changes are preserved when still valid;
- destroying one document discards the whole compound transaction.

The snapshot design intentionally favors reviewable correctness over memory efficiency. A future delta/journal backend can replace the storage strategy without changing these transaction semantics.


## Window identity after Link Window — legacy Capture safety

The preferred transaction-based Undo records each window's `WindowId` **and** attached `DocumentId`, and restores navigation only while the recorded post-command view still matches. The older public low-level `EditorUndoManager.Capture(EditorWindow, description)` path is retained for existing integrations. It originally restored its cursor by `WindowId` alone. After historical **Link Window** reattached that view to another text stream, Undo could wrongly activate the now unrelated window and place the captured cursor into the wrong document. The text itself was already restored by its correct `DocumentId`.

`RestoreLegacyCursor` now also checks document identity. If the captured view is **still present but relinked**, it does **not** restore navigation to that view, and it preserves the current focus. An original document kept alive by a linked sibling is still restored as text. If the captured view was actually **closed**, the established fallback may select a surviving sibling of the original document. A captured view that still displays the original document retains the ordinary cursor restoration. This change does not create a Pascal pointer-emulation layer or change compound Undo accounting.

Four direct regression tests in `UndoWindowIdentityTests` cover relinking, intact capture, closed-view fallback and the newer transaction API. A portable `windows.undo-relinked-view-preserves-unrelated-current-window` case additionally checks the modern, host-neutral navigation behavior. These are focused regression guarantees, not historical Undo-stack internals.


### Explicit marker reassignment after a cross-document block move

`UndoMarkerReassignmentTests` checks an independent selective-restoration invariant. When Move Block deletes a marked source line or shifts a marker on a surviving line, Undo restores those *command-induced* changes. However, if the user explicitly reassigns another marker after the move, that later choice remains authoritative. One compound Undo restores both documents and the moved block endpoints, but does not rewind the separately updated marker. This is modern SASD state ownership, not historical Pascal pointer recreation.
