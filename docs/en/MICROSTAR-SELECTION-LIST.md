# MicroStar selection-list model

The terminal sample now includes `MicroStarSelectionList`: a **pure**
list-navigation and scrolling model for future file pickers and command
selection dialogs. Each entry has a stable ID, a caption, and an enabled
state. The constructor snapshots the input so later collection mutations
cannot silently redirect a pending selection.

Up/Down, PageUp/PageDown, Home/End move through enabled entries; Enter
returns an acceptance result only when an enabled entry is selected;
Escape/Ctrl-U cancel. Disabled rows remain visible but cannot be selected.
The first visible row is automatically adjusted to keep the selection in
the configured viewport.

This first slice is a host-local **model**, not yet an interactive file
browser, directory service or completed historical MicroStar UI.
No `Console`, `EditorSession` or filesystem dependency is introduced.
Tests cover scrolling, disabled items, empty lists, cancellation, stable
identity and invalid definitions.

Future work: a generic visual list control, bounded directory enumeration
and explicit read-only list-to-command binding.
