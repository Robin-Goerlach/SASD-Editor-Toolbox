# FIRST-ED window geometry

## Scope

This document records the clean-room transfer of the Turbo Editor Toolbox V1 displayed-window geometry into the SASD Editor Toolbox. The historical handbook is used as a behavioral specification; Pascal source is not copied into the implementation.

The important separation is:

- `EditorWindow` owns document/view state such as cursor, top line and horizontal offset.
- `EditorWindowLayout` owns the vertical screen-row allocation of displayed windows.
- `EditorWindowFrame` is an immutable projection of one window's top row and height.
- renderers consume frames; they do not decide window-splitting semantics.

This keeps the reusable core independent of console, WinForms, WPF, Avalonia or browser geometry APIs.

## Historical row model

For V1 compatibility, a displayed window height includes its status row. A valid window therefore requires at least three host rows:

- one status row;
- at least two text rows.

`EditorWindowFrame.MinimumHeight` is consequently `3`, and `TextRows` is `Height - 1`.

## Create Window

The semantic `CreateWindow` command maps to `FirstEdCompatibilityProcessor.CreateWindow(size, donorWindowNumber)`.

The operation follows the documented `EditWindowCreate(Size, Win)` behavior:

1. resolve the displayed donor window;
2. reject a requested new-window height below three rows;
3. reject a split that would leave the donor below three rows;
4. take the requested rows from the lower part of the donor's allocation;
5. create a new blank `NONAME` document/view immediately after the donor in displayed-window order;
6. if compression leaves the donor cursor below its new visible text area, move that cursor up to the donor's last displayed text line while preserving its column.

The layout's total row count does not change during a split.

## Delete Window

Deleting the only displayed window is rejected. Otherwise the target is removed from displayed-window order and its rows are reclaimed using the historical rule:

- if window 1 is deleted, the former window 2 receives the freed rows;
- otherwise, the displayed window immediately above the deleted window receives them.

If the deleted window contains the active block, the block is cleared. This is intentionally not an undoable block operation because the historical window deletion semantics discard that window context directly.

### Linked documents

Historical Pascal code had to decide whether a text stream could be destroyed and explicitly manipulate links and memory. SASD represents a shared text stream as several `EditorWindow` instances referencing one `EditorDocument`. Deleting one view therefore removes the view; the document remains alive automatically while another view references it. Normal managed lifetime replaces explicit pointer/free-list management.

## Host resizing policy

The handbook describes fixed screen geometry, not modern resizable terminal windows. `EditorWindowLayout.ResizeWorkspace` therefore has an explicitly **modern SASD policy** rather than a claimed historical rule:

- extra rows are given to the bottom displayed window;
- when the host shrinks, spare rows are removed from bottom to top;
- no displayed window is reduced below three rows;
- if the host cannot provide three rows per displayed window, the resize is rejected and the terminal reference host asks the user to enlarge the terminal.

This policy can later be replaced or made configurable without changing the historical create/delete command semantics.

## Terminal reference host

`ConsoleFirstEdRenderer` now renders every `EditorWindowFrame` simultaneously. Each window has its own status row and text viewport; the current window is marked with `>` and owns the visible terminal cursor. The bottom terminal row remains reserved for prompts and transient command messages.

The renderer uses `EditorViewportBuilder.Build(window, textRows, width)` for every displayed window, so the multi-window host still consumes the same UI-neutral rendering projection as any future GUI host.

## Tests

`WindowLayoutCompatibilityTests` covers:

- row splitting and insertion order;
- the three-row minimum for both new and donor windows;
- donor-cursor correction after compression;
- freed-row ownership when deleting first and non-first windows;
- active-block clearing;
- host-resize behavior without violating minimum window height.
