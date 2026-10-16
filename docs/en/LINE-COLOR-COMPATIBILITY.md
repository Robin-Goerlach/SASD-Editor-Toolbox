# User-colored line compatibility

## Historical behavior and modern representation

The Turbo Editor Toolbox 1.0 handbook describes `EditColorLine` (printed p. 138) as setting the `Colored` bit on the current logical line and `EditColorFile` (p. 137) as setting it on every line of the current window's text stream. Existing colored lines remain colored. Neither routine inherently selects a different document, moves a cursor, or manipulates text. The original implementation used the flag to select the user-defined drawing color rather than the ordinary or block color.

The UI-independent SASD counterparts are `EditorSession.ColorCurrentLine()` and `EditorSession.ColorCurrentDocument()`. Both set `EditorLineFlags.UserColored` with a bitwise OR, preserving `InBlock` and `Wrapped`. The flag belongs to `EditorDocument.Buffer`; all linked views observe the same line metadata. `EditorViewportBuilder` exposes line flags to the rendering host, which decides actual colors.

## Deliberate modern policy

The historical descriptions establish the **flag mutation**, but do not require that display-only coloring be saved as ordinary text or create undo history. In V1, these operations are idempotent presentation annotations. They do **not** change `IsDirty`, `Version`, the selected window/cursor, or Undo history. Historical color encoding is not added to the modern UTF-8 text format. A future rich-document storage format may persist annotations explicitly, separate from plain-text I/O.

These utility methods have **no implicit keyboard bindings**. Hosts may expose them from menus or custom actions; the core remains independent of screen APIs.

## Regression coverage

`LineColorCompatibilityTests` checks the current-line scope, preservation of wrap/block bits, no dirty/version/undo mutation, idempotent whole-stream application, shared-document visibility across linked windows, and isolation from other documents.
