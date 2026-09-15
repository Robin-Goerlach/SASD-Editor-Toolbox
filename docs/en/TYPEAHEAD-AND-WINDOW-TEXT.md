# Typeahead, macro injection and destructive window-text deletion

## Scope of this transfer

This milestone transfers two low-level Turbo Editor Toolbox behaviors that are easy to overlook when only the visible FIRST-ED commands are considered:

1. the editor-owned typeahead buffer used between physical keyboard input and command classification; and
2. `EditWindowDeleteText`, which deliberately destroys the text stream shown by the current window without placing that text on the undo stack.

The implementation remains a clean-room behavioral transfer. It does **not** reproduce the Pascal circular-buffer storage, DOS keyboard scan codes, pointer variables or memory-management routines.

## Historical input behavior mapped to modern abstractions

The handbook describes a default typeahead capacity of 500 entries and two insertion directions:

- physical keyboard input is appended in queue order (`Pokechr` style);
- `EditPushtbf` pushes one character at the front so it is read next;
- `EditUserpush` pushes a whole string in reverse insertion order so the string is read in its original order.

`EditorTypeaheadBuffer` preserves those observable rules but stores `EditorKeyStroke` values rather than raw bytes. This is important because the reusable core must work equally with terminal, WPF, WinForms, browser and scripted hosts.

The public operations are intentionally explicit:

- `EnqueueFromHost` - append one normalized physical/host key;
- `PushNext` - place one normalized key at the front;
- `PushSequence` - place a sequence at the front while retaining natural playback order;
- `PushText` - convenience macro-string normalization, including classic ASCII control characters;
- `TryRead` - remove the next logical input unit;
- `ConsumeAbortRequest` - observe and clear the historical abort state.

The default capacity is 500. If a write would overflow the buffer, pending input is cleared. `PushSequence` performs a capacity preflight instead of partially inserting before discovering overflow; the final observable state is the same as the historical behavior: the pending typeahead queue is empty.

## Ctrl-U / EditAbort boundary

The handbook gives Ctrl-U special treatment on the physical-input (`Pokechr`) path: it invokes abort immediately instead of allowing the character to wait behind already buffered commands. SASD therefore intercepts a host-originated normalized Ctrl-U in `EnqueueFromHost`, clears pending typeahead, raises `AbortRequested`, and does not enqueue Ctrl-U.

A Ctrl-U inserted with `PushNext` or `PushSequence` is **not** treated as an immediate abort. This distinction is deliberate: the handbook assigns the direct-abort behavior to `Pokechr`, whereas `EditPushtbf` is a separate front-insertion path.

The current terminal host resets a pending prefix and reports the immediate buffer abort. The `AbortRequested` state is also exposed for future interruptible long-running operations. Connecting that flag to every long-running compatibility routine is a separate procedure-by-procedure audit item; modern async code already has `CancellationToken` as its platform-neutral cancellation mechanism.

## Terminal input flow

The interactive FIRST-ED sample now follows this logical pipeline:

```text
Console key
   -> ConsoleKeyTranslator
   -> EditorTypeaheadBuffer.EnqueueFromHost
   -> EditorTypeaheadBuffer.TryRead
   -> FirstEdKeyMap
   -> EditorCommandDispatcher
   -> editor services / engine
```

Macro input enters at the typeahead stage and therefore travels through the same key map and dispatcher as keyboard input. This is useful beyond historical compatibility: scripted editor demonstrations and future macro facilities can reuse the same path without faking console events.

## EditWindowDeleteText

`EditorCommandId.DeleteWindowText` and `EditorSession.DeleteCurrentWindowText()` implement the historical destructive operation.

Observable compatibility behavior:

- all text from the current stream is lost;
- the operation creates no undo entry;
- existing undo snapshots for the destroyed document are discarded;
- the affected window becomes a blank `NONAME` document with one empty logical line;
- if multiple windows were linked to the same document, all affected views lose the old stream and the link relationship is destroyed;
- the active block belonging to that stream is removed;
- cursor and scroll positions of affected views are reset to the beginning of the new blank documents.

The historical implementation managed linked text streams through pointers. The .NET implementation instead reattaches each affected view to a **distinct new blank `EditorDocument`**. This produces the same important result - the old text is gone and the views are no longer linked - without emulating unsafe pointer lifetime rules.

### Marker cleanup is a modern safety rule

The handbook explicitly mentions clearing an active block when its text stream is destroyed, but the retrieved `EditWindowDeleteText` description does not specify marker behavior. SASD removes markers that refer to the destroyed document so no marker can retain an unreachable document identity. This is documented as a modern consistency rule, not claimed as a historical Borland rule.

## Keyboard mapping note

The semantic `DeleteWindowText` operation is available to hosts, menus, scripts and future compatibility profiles. This milestone does not invent a new FIRST-ED key sequence for it. A key binding will only be added when the corresponding default-editor mapping is confirmed during the command audit. MicroStar-specific confirmation behavior likewise belongs to the later MicroStar sample/profile rather than the reusable semantic command.

## Tests

The regression suite covers:

- FIFO host insertion;
- front insertion precedence;
- sequence playback order;
- macro control-character normalization;
- immediate host Ctrl-U abort semantics;
- the deliberate non-aborting macro Ctrl-U path;
- overflow clearing;
- destructive, non-undoable window-text deletion;
- linked-view detachment;
- block/marker cleanup and preservation of unrelated documents.
