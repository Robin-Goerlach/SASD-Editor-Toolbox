# MicroStar — block-only Write File

The handbook describes MicroStar's **Write Block** as separate from
writing the current document. The F10 sample menu now exposes this feature
using the modern letter `T` (not asserted to be the original keybinding).

A valid whole-line block is required; absent/incomplete blocks perform no
file operation. The selected lines are snapshotted from their **source**
document even if another window is active, and the original legacy file
codec is used. Visual `InBlock` is removed; `Wrapped` line topology remains.
The user must explicitly confirm overwriting an existing file with YES.

The application helper does **not** save, rename, mark clean, edit or push
Undo for the underlying document. It uses the shared cancellation/interrupt
service to avoid bypassing Ctrl-U handling during asynchronous writes.

Regression coverage: full source/target isolation, undefined block,
original text/flags/Undo invariants, and actual legacy codec roundtrip.

Still open: block-only **Read File** with correct selection and topology,
full MicroStar popups, directory browser, print scheduling and optional
Lightning spell-check integration.
