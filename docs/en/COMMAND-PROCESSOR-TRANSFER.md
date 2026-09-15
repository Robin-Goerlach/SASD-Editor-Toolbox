# FIRST-ED command-processor transfer

## Why this is a separate layer

The Turbo Editor Toolbox handbook separates keyboard dispatch from the procedures that actually perform editor operations. SASD follows the same boundary, but keeps historical input conventions out of the general editing engine.

`FirstEdKeyMap` answers **which** semantic command a key sequence means. `EditorCommandDispatcher` decides **which processor** to call. `FirstEdCompatibilityProcessor` contains only the small translation rules that are specifically historical, such as one-based line/column numbers and modulo window numbering.

This arrangement is intentional for the later C++, Java and JavaScript implementations: the language-neutral behavior can be reproduced without inheriting DOS keyboard or screen assumptions.

## Transferred in this milestone

The following command-processor semantics are now implemented and tested:

| Historical concept | C# implementation | Compatibility detail |
|---|---|---|
| `EditBeginningEndLine` | `MoveBeginningOrEndOfLine` | non-first column -> first column; first column -> after last nonblank character |
| `EditEndLine` | `MoveEndOfLine` | ignores trailing whitespace when finding the end |
| `EditGotoLine` | `GoToLine` | user number is one-based; past EOF clamps to last line; column is preserved |
| `EditGotoColumn` | `GoToColumn` | user number is one-based; virtual columns are allowed |
| `EditTopBlock` / `EditBottomBlock` | `GoToBlockBoundary` | can switch to a window showing the block document |
| `EditWindowUp` | `PreviousWindow` | wraps from first to last window |
| `EditWindowDown` | existing `NextWindow` | wraps from last to first window |
| `EditWindowGoto` | `GoToWindow` | one-based window number is interpreted modulo displayed windows |
| `EditWindowLink` | `LinkWindows` | existing destination view is attached to source document; view state stays independent |
| left/right margin commands | compatibility processor | one-based user columns translated to zero-based internal margins |
| tab width | compatibility processor | positive width forwarded to window options |
| undo limit | compatibility processor | non-negative limit forwarded to `EditorUndoManager` |

## Important modern replacement: window linking

The Pascal implementation had to splice pointers and explicitly destroy an abandoned text stream. In C#, an `EditorWindow` can safely reattach to another `EditorDocument`. If the old document has no remaining references, normal garbage collection owns its lifetime. The externally visible behavior stays the same: linked windows share text but retain independent cursor and scrolling state.

`EditorWindow.AttachDocument` is therefore internal. Arbitrary UI code cannot silently replace a window's document; linking remains an editor/session operation.

## Virtual columns

The historical `EditGotoLine` keeps the current column even when the target line is shorter, and `EditGotoColumn` can move past the visible text. SASD preserves that behavior at the compatibility-command boundary. Mutation operations may later normalize a virtual cursor when required by the operation. This is preferable to changing the entire modern window model merely to mimic a low-level Pascal storage detail.

## Still deliberately pending

This step does **not** pretend that every key-map entry is executable already. In particular, the following remain separate milestones:

- `FindAgain` state and the remaining find/replace compatibility behavior;
- prompt-driven read/write/save commands over `ITextStorage`;
- interactive exit confirmation and editor-loop rundown state;
- physical window sizing for Create Window, which belongs to a host/layout layer;
- exact scrolling/page-display behavior, because the historical code couples these operations to displayed rows;
- the MicroStar-specific menu, popup and background-print examples.

Keeping these concerns separate is more important than making a large monolithic port quickly.
