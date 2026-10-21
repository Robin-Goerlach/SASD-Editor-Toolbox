# FIRST-ED console Find/Replace options (2026-10-21)

## Evidence and scope

The handbook's back cover explicitly advertises **Find and Find/Replace
with options**. Its variables reference describes `Optstr` as the
find/replace-option string (printed p. 113). The manual excerpts do **not**
specify the original letters or complete `Optstr` parsing rules.
No claim of historical key-for-key syntax is made here.

The core already supports `SearchOptions(CaseSensitive, WholeWord, WrapAround)`.
The console host now offers **modern, explicit** optional shortcuts
after a nonempty Find pattern or a submitted Find/Replace pair:

- `C`: case-sensitive literal comparison;
- `W`: whole-word match;
- `R`: wrap back to the document start;
- Enter with no letters: existing FIRST-ED defaults (case-insensitive,
  substring, no wrap).

Letters may be combined, case-insensitively; unknown or repeated letters
cancel the request safely instead of silently changing its meaning.
Null/Ctrl-U/Escape cancellation never edits the document.

The `EditorCommandRequest.Options` init-only property lets the host pass
existing `SearchOptions` through the semantic dispatcher without adding
Console behavior to Core or changing the request's positional arity.
`FindAgain`, including an explicitly empty Find submission, keeps the
remembered pattern and options and does not ask for new options.

## Tests and boundaries

`ConsoleFirstEdSearchOptionsTests` verifies defaults, invalid input,
case-sensitive Find/FindAgain, whole-word Replace and Undo. These shortcuts
are SASD sample-host policy and are **not** a reconstruction of Borland
`Optstr` option codes. An original-code/handbook proof would be needed
before claiming 1985 syntax parity.
