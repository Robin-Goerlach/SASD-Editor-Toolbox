# Interruptibility and Ctrl-U

## Historical behavior being preserved

The handbook gives three relevant behavioral rules:

1. host/physical Ctrl-U is handled immediately instead of being queued behind older typeahead;
2. the abort operation clears pending typeahead and raises an abort state that input-oriented procedures can observe;
3. long operations can call an EditBreathe-style routine so physical input is collected while the operation is running. Compatibility file reading is explicitly described as abortable during the read.

The modern implementation preserves those effects without reproducing BIOS/DOS keyboard buffers or polling platform APIs from the reusable core.

## Architecture

`EditorTypeaheadBuffer.EnqueueFromHost` remains the single place where physical Ctrl-U receives immediate semantics. It clears pending typeahead and raises `AbortRequested`.

`EditorInterruptionService` coordinates long operations. A checkpoint:

1. honors external `CancellationToken` cancellation;
2. calls `IEditorHooks.PollInputDuringOperationAsync`;
3. honors cancellation again;
4. consumes a pending editor abort exactly once and throws `EditorOperationAbortedException`.

The semantic dispatcher converts that exception to the typed `OperationAborted` editor error. External `OperationCanceledException` is intentionally not caught by that path and remains caller/platform control flow.

## Host boundary

The core never calls Console, WPF, WinForms or browser input APIs. A host may implement `PollInputDuringOperationAsync` and feed normalized input through the session typeahead buffer.

The FIRST-ED console sample polls at most one physical key per checkpoint. Ordinary keys stay queued for later normal command processing. Ctrl-U uses the immediate abort path. This bounded rule prevents an interruptibility checkpoint from turning into a nested command loop.

When Ctrl-U is received by the ordinary top-level input pump rather than during a long operation, that pump consumes the one-shot abort state after reporting it. This prevents stale abort state from cancelling an unrelated later command.

## Awaited I/O

`RunInterruptibleAsync` is intended for cancellable awaited I/O. While an operation is outstanding, the service periodically gives the host a polling opportunity. On Ctrl-U it cancels a linked token, observes the cancelled operation, then propagates the editor-specific abort.

The built-in compatibility codec honors cancellation tokens. Custom `IEditorFileCodec` implementations are expected to do the same.

Read File remains editor-state atomic: decoded data is held outside the document until the read and final checkpoint complete. Write/Save can of course be partially visible at the external filesystem/provider level if cancellation occurs during a write; editor metadata changes only after successful completion.

## CPU-bound loops and safe commit points

Search and reformat use explicit checkpoints rather than a timer-driven background thread.

Find/Find Again checkpoint at logical-line boundaries and do not commit cursor or remembered search state until the scan completes.

Reformat checkpoints while reading/compressing/planning and immediately before mutation. Once the prepared plan begins committing to the document, no further abort checkpoint is taken. This keeps the structural edit atomic and lets normal topology repair finish.

Short structural commands such as block splice operations follow the same principle: validate first, then complete the bounded mutation instead of accepting an abort halfway through line insertion/removal.

## Cancellation semantics

Ctrl-U and external cancellation are deliberately different:

- **Ctrl-U** is editor semantics, reported as `EditorErrorId.OperationAborted`.
- **CancellationToken** is application/platform control flow and propagates as `OperationCanceledException`.

This avoids duplicating cancellation systems while still preserving the historical immediate-abort behavior that matters to an interactive compatibility host.

## Regression coverage

`InterruptionCompatibilityTests` verifies:

- Ctrl-U cancellation of already in-flight file I/O before editor mutation;
- atomic reformat abort during planning;
- Find abort without cursor/search-state commit;
- ordinary EditBreathe-style input remains queued;
- external cancellation is not converted into an editor abort.

The terminal-specific physical-key poll remains a sample host concern; the reusable tests exercise the same contract through deterministic hooks.
