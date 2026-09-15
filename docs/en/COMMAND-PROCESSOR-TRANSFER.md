# FIRST-ED command-processor transfer

## Why this is a separate layer

The Turbo Editor Toolbox handbook separates keyboard dispatch from the procedures that actually perform editor operations. SASD follows the same boundary, but keeps historical input conventions out of the general editing engine.

`FirstEdKeyMap` answers **which** semantic command a key sequence means. `EditorCommandDispatcher` decides **which processor** to call. `FirstEdCompatibilityProcessor` contains translation rules that are specifically historical, while `EditorSearchService`, `EditorFileService`, the window-layout service and scheduling/lifecycle services own their respective stateful concerns.

This arrangement is intentional for later C++, Java and JavaScript implementations: language-neutral behavior can be reproduced without inheriting DOS keyboard or screen assumptions.

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
| `EditWindowCreate` | `CreateWindow` + `EditorWindowLayout.Split` | requested lower rows become a new blank window; both windows retain the three-row minimum |
| `EditWindowDelete` | `DeleteWindow` + layout reclamation | first window gives space to second; otherwise the window above receives it |
| `EditWindowTopFile` / `EditWindowBottomFile` | compatibility processor | includes the documented cursor-column and top-line placement |
| `EditUpLine` / `EditDownLine` | compatibility processor | viewport follows the cursor at display edges |
| `EditScrollUp` / `EditScrollDown` | compatibility processor | one-row viewport slide plus documented edge-cursor adjustment |
| `EditUpPage` / `EditDownPage` | compatibility processor | viewport moves by displayed rows minus one |
| margin/tab/undo settings | compatibility processor | user-visible values translated to modern internal state |
| `EditFind` repeat behavior | `EditorSearchService.FindAgain` | remembers the prior pattern and resumes after the previous match |
| `EditReatxtfil` insertion | `EditorFileService.ReadIntoCurrentWindowAsync` | inserts after current line and preserves cursor |
| `EditFileWrite` | `EditorFileService.WriteCurrentWindowAsync` | writes current stream to an explicit path |
| wrapped file lines | `FirstEdLegacyFileCodec` | high-bit CR (`0x8D`) maps to `EditorLineFlags.Wrapped` |
| `EditExit` / `Rundown` | `RequestRundown`, `RundownRequested` | exit requests loop termination and does not save |
| `EditSchedule` | `EditorScheduler.RunCycleAsync` | pending input wins over background work |
| `EditSystem` | `EditorSystemLoop.RunAsync` | repeats scheduler cycles until rundown |

## Important modern replacement: window and stream ownership

The Pascal implementation had to splice pointers, adjust screen coordinates and explicitly destroy abandoned text streams. SASD keeps screen-row allocation in `EditorWindowLayout` and represents shared text by several `EditorWindow` objects referencing one `EditorDocument`. Deleting one linked view therefore cannot accidentally free text still shown elsewhere; normal garbage collection owns document lifetime once references disappear.

The historical minimum of one status row plus two text rows is preserved as `EditorWindowFrame.MinimumHeight == 3`. The terminal-resize policy is modern and explicitly separate from the historical Create/Delete rules; see `WINDOW-GEOMETRY.md`.

## Compatibility file I/O versus modern persistence

`FileTextStorage` remains the modern UTF-8 whole-document provider. The historical byte convention is isolated behind `IEditorFileCodec` / `FirstEdLegacyFileCodec`. This prevents a compatibility requirement from becoming the storage format of every future SASD application.

## Lifecycle and page-policy note

The host owns the confirmation UI for Ctrl-K X; the core owns the direct exit/rundown action. The handbook specifies page displacement but not a separate post-page cursor screen row. SASD therefore preserves the relative visible cursor row as an explicit modern policy. See `LIFECYCLE-AND-SCROLLING.md`.

## Still deliberately pending

- the final procedure-by-procedure V1 compatibility audit and remaining edge-case tests;
- typed historical error resources where they add compatibility value without coupling normal applications to legacy wording;
- MicroStar-specific menu, popup and background-print examples.

Keeping these concerns separate is more important than making a large monolithic port quickly.
