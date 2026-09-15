# FIRST-ED command-processor transfer

## Why this is a separate layer

The Turbo Editor Toolbox handbook separates keyboard dispatch from the procedures that actually perform editor operations. SASD follows the same boundary, but keeps historical input conventions out of the general editing engine.

`FirstEdKeyMap` answers **which** semantic command a key sequence means. `EditorCommandDispatcher` decides **which processor** to call. `FirstEdCompatibilityProcessor` contains translation rules that are specifically historical, while `EditorSearchService` and `EditorFileService` own their respective stateful services.

This arrangement is intentional for the later C++, Java and JavaScript implementations: language-neutral behavior can be reproduced without inheriting DOS keyboard or screen assumptions.

## Transferred command semantics

| Historical concept | C# implementation | Compatibility detail |
|---|---|---|
| `EditBeginningEndLine` | `MoveBeginningOrEndOfLine` | non-first column -> first column; first column -> after last nonblank character |
| `EditEndLine` | `MoveEndOfLine` | ignores trailing whitespace when finding the end |
| `EditGotoLine` | `GoToLine` | one-based value; past EOF clamps to last line; column is preserved |
| `EditGotoColumn` | `GoToColumn` | one-based value; virtual columns are allowed |
| `EditTopBlock` / `EditBottomBlock` | `GoToBlockBoundary` | can switch to a window showing the block document |
| `EditWindowUp` / `EditWindowDown` | compatibility/session window navigation | wraps at first/last window |
| `EditWindowGoto` | `GoToWindow` | one-based window number interpreted modulo displayed windows |
| `EditWindowLink` | `LinkWindows` | existing destination view is attached to source document |
| margin/tab/undo settings | compatibility processor | user-visible values translated to modern internal state |
| `EditFind` repeat behavior | `EditorSearchService.FindAgain` | remembers the prior pattern and resumes after the previous match |
| `EditReatxtfil` insertion | `EditorFileService.ReadIntoCurrentWindowAsync` | inserts after current line and preserves cursor |
| `EditFileWrite` | `EditorFileService.WriteCurrentWindowAsync` | writes current stream to an explicit path |
| wrapped file lines | `FirstEdLegacyFileCodec` | high-bit CR (`0x8D`) maps to `EditorLineFlags.Wrapped` |

## Important modern replacement: window linking

The Pascal implementation had to splice pointers and explicitly destroy an abandoned text stream. In C#, an `EditorWindow` safely reattaches to another `EditorDocument`. If the old document has no remaining references, normal garbage collection owns its lifetime. Linked windows share text but retain independent cursor and scrolling state.

## Compatibility file I/O versus modern persistence

`FileTextStorage` remains the modern UTF-8 whole-document provider. The historical byte convention is isolated behind `IEditorFileCodec` / `FirstEdLegacyFileCodec`. This prevents a compatibility requirement from becoming the storage format of every future SASD application.

See `SEARCH-AND-FILE-COMMANDS.md` for the detailed mapping.

## Still deliberately pending

- interactive exit confirmation and editor-loop rundown state;
- physical window sizing for Create Window, which belongs to a host/layout layer;
- exact scrolling/page-display behavior, because the historical code couples these operations to displayed rows;
- the interactive FIRST-ED host;
- MicroStar-specific menu, popup and background-print examples.

Keeping these concerns separate is more important than making a large monolithic port quickly.
