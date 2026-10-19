# FIRST-ED lifecycle and viewport navigation

## Scope

This milestone transfers the lifecycle/scheduler model and the display-row-dependent movement behavior described by the Turbo Editor Toolbox handbook. The implementation remains host-neutral: the core does not poll DOS keyboard hardware and does not write video memory.

## Exit and rundown

The handbook describes two distinct concepts:

- `EditExit` sets the global `Rundown` variable to true and does **not** save files.
- `EditCpExit` asks the user for confirmation and calls `EditExit` only when the response is `YES` (case-insensitive).

SASD maps these responsibilities separately:

- `EditorSession.RundownRequested` is the modern rundown state.
- `EditorSession.RequestRundown()` is the direct `EditExit` equivalent.
- `EditorCommandId.Exit` requests rundown and deliberately does not save documents.
- `FirstEdKeyMap` already declares `Confirmation` for Ctrl-K X. The interactive host owns the prompt and dispatches `Exit` only after an affirmative answer.

This keeps UI prompting out of the reusable core while preserving the historical split between confirmation and actual exit.

## EditSchedule and EditSystem

The handbook states that `EditSchedule` first checks whether editor input is available. If input exists, the input classifier runs; otherwise the background process runs. `EditSystem` repeatedly calls `EditSchedule` until `Rundown` becomes true.

The C# mapping is:

| Historical concept | SASD implementation |
|---|---|
| typeahead/input availability | `IEditorInputPump.TryProcessInputAsync` |
| `EditSchedule` | `EditorScheduler.RunCycleAsync` |
| background/UserTask slice | `EditorScheduler.RunIdleCycleAsync`, `IEditorBackgroundTask`, `IEditorHooks.OnIdleAsync` |
| `Rundown` | `EditorSession.RundownRequested` |
| `EditSystem` | `EditorSystemLoop.RunAsync` |

Input has priority: a scheduler cycle that processes input does not run background work. Background tasks remain cooperative and must return after a bounded unit of work, matching the handbook's requirement that `UserTask` preserve state and return control quickly.

## Line movement and scrolling

The compatibility processor now uses the host-supplied visible text-row count (`EditorCommandRequest.PageSize`) for display-dependent commands.

- `EditUpLine`: move one logical line up; if the cursor was on the top displayed row, scroll the viewport up to keep it visible.
- `EditDownLine`: move one logical line down; if the cursor was on the last displayed row, scroll the viewport down.
- `EditScrollUp`: slide the viewport up one line; if the cursor was on the last displayed row, move it up one line as well.
- `EditScrollDown`: slide the viewport down one line; if the cursor was on the top displayed row, move it down one line as well.
- `EditUpPage` / `EditDownPage`: move the viewport by one less than the number of displayed text rows.

The handbook explicitly defines the viewport displacement for page commands but does not separately define which screen row the cursor should occupy after those page operations. SASD therefore uses an explicit modern policy: preserve the cursor's relative visible row when possible and clamp only at document boundaries. This policy is documented rather than being presented as a historical fact.

## Top and bottom of file

`EditWindowTopFile` is represented as first document line, first column, top line zero. `EditWindowBottomFile` moves to the last document line, first column, and also places that final line at the top of the viewport, as specified by the handbook.

## Remaining host work

Physical window creation/compression still belongs to a layout-aware host because the historical routine manipulates screen rows directly. The interactive FIRST-ED sample also still needs to connect a real input source, prompt service and renderer to the now-implemented core contracts.

## Oversized host viewport boundary

The FIRST-ED compatibility processor now calculates last-visible-line and page destinations using widened intermediate arithmetic before clamping to the document's actual final line. A positive `PageSize` can approach `int.MaxValue` even for a short document. `ScrollUp` still recognizes the final document line as its bottom edge; `PageDown` cannot wrap the top line into a negative coordinate. `ViewportNavigationOverflowTests` documents four focused regressions. This is defensive modern API behavior, not a historical 16-bit line-limit requirement.

## One-row page behavior and portable fixtures

When only one text row is visible, `visibleRows - 1` is zero. Page Up and Page Down therefore leave the cursor and top line unchanged; the dispatcher may still acknowledge the valid request. `conformance/v1/cases/viewport-navigation.json` makes this explicit alongside standard and extremely large viewport sizes, and covers the all-window independence of linked views.

### Rundown and dynamic background task registration

The modern cooperative scheduler now checks cancellation and `RundownRequested` before idle and after the host idle callback, and again before each registered background slice. If a callback asks the editor to exit, later slices in that cycle are skipped. A registration snapshot allows callbacks to add/remove tasks safely; each pending task is checked against the *live* registry immediately before execution, so removing it during an earlier slice prevents its execution. Tasks registered during an active cycle begin no sooner than the next idle cycle. These checks preserve input-first priority and are host-independent; an individual background task must still return promptly and honor cancellation.

## Direct engine navigation with huge page sizes

The direct `EditorEngine.MovePageDown`/ `MovePageUp` API may be called by a modern host with a page size as large as `int.MaxValue`. Vertical destination calculations now use wide arithmetic before clamping, so a huge positive PageDown cannot wrap to the first line when invoked from a nonzero index. Tests verify both directions, linked-view cursor independence and no text/Undo mutation. This supplements the separately tested FIRST-ED semantic viewport rules.
