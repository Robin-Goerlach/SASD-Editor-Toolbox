# Absolute line numbers and horizontal viewport compatibility

## Historical source and scope

The **Turbo Editor Toolbox 1.0 Owner's Handbook** describes `EditGenlineno` on printed p. 176 and `EditHscroll` on printed p. 179. The handbook is an external **behavioral** reference. No Pascal code, pages or historical assets are copied into this repository.

- `EditGenlineno` computes absolute line numbers for all displayed windows by stepping back through linked line descriptors; it does not itself paint the status line.
- `EditHscroll` checks **all** windows and adjusts each window's left text edge when its cursor would otherwise lie outside the visible horizontal text area.

The old procedures mention exiting the scan when keyboard input is detected because line-descriptor and screen walks could be time-consuming. A modern direct index and a bounded per-view pass remove those particular long scans. Input-first scheduling remains the responsibility of `EditorSystemLoop` and the host; this is a documented modernization, not a claim to preserve intermediate DOS loop timing.

## SASD mappings

`EditorViewportBuilder.Build(window, height, width)` reads `EditorWindow.Cursor.Line` and exposes the **one-based** absolute line number in that window's `EditorStatus.Line`. The line value remains associated with its own view when multiple views share a document. This is the O(1) modern replacement for `EditGenlineno`. Producing status metadata is not equivalent to drawing it: the host renderer owns the actual status line.

`EditorWindow.EnsureCursorHorizontallyVisible(visibleColumns)` enforces minimal viewport movement for one view, and `EditorSession.EnsureAllWindowCursorsHorizontallyVisible(visibleColumns)` applies it to all open views with a common text width. An optional host that allocates different text widths can call the window method separately for each view.

For visible width `w >= 1`, after normalizing a negative left offset to zero:

- If `cursor.Column < LeftColumn`, set `LeftColumn = cursor.Column`.
- If `cursor.Column - LeftColumn >= w`, set `LeftColumn = cursor.Column - w + 1`.
- Otherwise keep `LeftColumn` unchanged.

The subtraction-based check avoids overflow near `int.MaxValue`. Virtual columns beyond physical text length remain valid. No method changes document text, dirty state, history, cursor coordinates, current-window selection or vertical offsets. Invalid widths are rejected before any session view is changed.

## Terminal host integration and regression coverage

`ConsoleFirstEdRenderer` supplies its text width once before building the displayed viewports; horizontal correction is therefore no longer hidden in the terminal-specific renderer. Its vertical cursor correction remains a separate host concern.

`ScreenPositionCompatibilityTests` covers both edges of the horizontal viewport, all linked and independent windows, idempotence, invalid widths, large virtual columns, viewport text clipping and per-window absolute status line numbers.

