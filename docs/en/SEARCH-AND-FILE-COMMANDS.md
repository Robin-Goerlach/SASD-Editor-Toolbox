# FIRST-ED search and file commands

## Scope

This milestone transfers the next group of Turbo Editor Toolbox behaviors into the C# implementation without mixing host prompts, editor state and byte-level file compatibility into one class.

The handbook remains the behavioral reference; the implementation is independently written.

## Search state and Find Again

The historical `EditFind` asks for a pattern and, when the supplied string is empty, reuses the previous search string. FIRST-ED also exposes a dedicated repeat-search command. SASD represents that behavior explicitly in `EditorSearchService`:

- `FindNext(pattern, options)` records the pattern and options;
- `FindAgain()` repeats the recorded search;
- if the cursor is still on the previous match, Find Again resumes after that match instead of returning the same occurrence again;
- forward search does not wrap by default in the FIRST-ED compatibility path;
- wrap-around remains an explicit `SearchOptions` feature for modern hosts.

The search service remains independent from the keyboard map. `FirstEdKeyMap` maps Ctrl-L to `FindAgain`, while the command dispatcher invokes the remembered search state.

## File-command separation

The handbook distinguishes between command processors that ask the user for a filename (`EditCprfw`, `EditCpwfw`) and lower-level operations that receive a filename (`EditFileRead`, `EditFileWrite`). SASD keeps the same architectural boundary:

1. the host asks for a path;
2. the key map only declares that a path is required;
3. `EditorCommandDispatcher` receives a fully resolved `EditorCommandRequest`;
4. `EditorFileService` performs the operation;
5. `IEditorFileCodec` owns byte-format compatibility.

This means a WPF dialog, terminal prompt, web UI or scripted caller can all use the same core operation.

## Historical read semantics

The handbook describes `EditReatxtfil` as inserting the contents of a named file **after the current line** while leaving the cursor position unchanged. `EditorFileService.ReadIntoCurrentWindowAsync` preserves those observable semantics. The operation is undoable and marks the document dirty.

Reading a file this way does not rename the current document. It is an insertion operation, not a modern "Open document" operation.

## Wrapped-line file marker

The historical file routines use a carriage-return byte with the high bit set, decimal 141 / hexadecimal `0x8D`, to identify a line carrying the `Wrapped` attribute. `FirstEdLegacyFileCodec` preserves this convention:

- `0x8D` ends a logical line and sets `EditorLineFlags.Wrapped` on that line;
- ordinary CR/LF and LF are accepted as ordinary line boundaries;
- writing a wrapped logical line emits `0x8D` instead of the ordinary CR/LF separator;
- the codec is explicitly an 8-bit Latin-1 compatibility codec.

The existing `FileTextStorage` is intentionally **not** changed. It remains the modern UTF-8 whole-document storage provider. Historical compatibility and modern persistence therefore do not constrain each other.

## Write and Save semantics

`WriteFile` writes the current text stream to an explicit path but does not rename the document and does not clear the dirty flag. This corresponds to the direct named-file write operation.

`SaveFile` writes to the document's associated path and clears the dirty flag. An explicit path may also be supplied by a modern host. This is a deliberate integration adaptation: the handbook clearly documents direct read/write behavior, while SASD's document identity is a modern abstraction rather than a Pascal global/window field.

## Still pending

The remaining major V1 gaps are now primarily lifecycle and display concerns: editor rundown/exit confirmation, exact display-dependent scroll/page semantics, physical Create Window layout, the interactive FIRST-ED host, and later MicroStar demonstration features.
