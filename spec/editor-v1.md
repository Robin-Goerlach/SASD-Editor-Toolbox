# SASD Editor Toolbox - language-neutral V1 contract

This document defines behavior that every language implementation should preserve. Public API names may be idiomatic for the target language; observable editor behavior should remain compatible.

## 1. Text model

- A document contains one or more logical lines.
- A line carries text plus flags for block membership, word-wrap origin and user-defined highlighting.
- A storage implementation must support line read/replace/insert/remove and a stable snapshot operation.
- The storage mechanism is replaceable. The first .NET implementation uses linked logical lines because this closely matches the historical model while keeping later rope/piece-table work possible.

## 2. Document/view separation

- Text belongs to a document.
- A window/view references one document and owns cursor, scroll position and per-view modes.
- Multiple windows may reference the same document; edits are immediately visible in every linked view.
- Views may independently use insert/overtype, word-wrap and auto-indent modes.

## 3. Editing primitives

V1 includes character/word/line movement, page movement, beginning/end/top/bottom navigation, text insertion, newline, tab, control-character insertion, left/right deletion, word deletion, line deletion, delete-to-end-of-line, case change, centering and paragraph reformatting.

## 4. Blocks and markers

- The historical V1 compatibility block is whole-line and contiguous.
- One active block may be begun/ended, copied, moved, deleted and hidden.
- Numbered markers 1..20 identify a document and text position and can be jumped to while the document remains open.

## 5. Search and replace

- Literal forward search is required.
- The compatibility search remembers the most recent non-empty pattern for a Find Again operation.
- Find Again resumes after the previous match when the cursor is still positioned on that match.
- FIRST-ED-compatible forward search does not wrap by default. Wrap-around remains an explicit option for hosts that want it.
- Case-sensitive, case-insensitive and whole-word behavior are explicit options.
- Replace-next and replace-all are services above the buffer layer.
- Hosts may intercept a replacement decision.

## 6. Undo and dirty state

- Every text mutation marks the document dirty.
- Successful persistence clears dirty state when the operation is semantically a save.
- A direct compatibility Write File operation does not implicitly change document identity.
- Undo is a replaceable service. Correctness is more important than storage efficiency in the first implementation.

## 7. Command dispatch and input mapping

- Host-specific keyboard events are normalized before compatibility mapping.
- User input is translated to semantic commands outside the editing engine.
- The FIRST-ED compatibility map is stateful because Ctrl-K, Ctrl-O and Ctrl-Q introduce a second keystroke.
- A key map never prompts the user directly. Instead it declares whether a command requires text, a number, two numbers, a character, a file path, find/replace values or confirmation.
- Numbered prefix commands may carry their number directly in the binding.
- A command-filter hook may rewrite a semantic command before dispatch.
- The engine itself must remain callable directly by application code.
- Hosts may add modern aliases (for example arrow keys) without removing the historical command sequences.

## 8. Host hooks

Equivalent extension points must exist for command filtering, error handling, status transformation, replace confirmation and cooperative idle/background work.

## 9. Rendering

The core does not write directly to console/video memory. It exposes a viewport/status projection from which WPF, WinForms, terminal, web and other hosts can render.

## 10. Persistence and compatibility file I/O

- Modern document persistence is abstracted. The first .NET `ITextStorage` provider supports UTF-8 files and preserves the detected newline convention for subsequent saves.
- Compatibility file commands use a separate file-codec boundary so historical formats do not become mandatory modern storage formats.
- The Turbo Editor Toolbox wrapped-line marker is a high-bit carriage return (`0x8D`, decimal 141). Decoding it marks the preceding logical line as wrapped; encoding a wrapped separator emits it again.
- The FIRST-ED-compatible read operation inserts decoded lines after the current line and preserves the current cursor position.
- Hosts collect filenames; core file services receive resolved paths and perform no UI prompting.

## 11. Compatibility policy

The Borland handbook is a requirements/reference source only. Implementations must be independently written and must not copy historical source code.
