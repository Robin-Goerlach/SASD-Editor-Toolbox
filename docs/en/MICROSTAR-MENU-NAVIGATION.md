# MicroStar F10 keyboard navigation

The terminal sample's popup now supports **arrow-key navigation**, Home/End,
Enter and Escape/Ctrl-U, while retaining all earlier one-letter shortcuts.

The new `ConsoleMicroStarMenuNavigation` is a **pure state machine**:
movement returns `SelectionChanged`, Enter returns `Accept`, Escape or
Ctrl-U returns `Cancel`. Only `Accept` reaches the existing semantic
command dispatcher. Invalid and terminal-modified keys cannot edit text.

Selected items are indicated with `>` in the existing bounded popup
projection, leaving the menu's original strings and geometry unchanged.
This is modern SASD keyboard policy, not a verified historical MicroStar
menu order. The reusable Editor Core is not modified.

Headless tests cover movement, wraparound, cancellation, shortcuts, invalid
keys and the non-mutating highlight projection. The current implementation
uses a flat traversal order; hierarchical menus and keyboard focus across
multiple simultaneously active dialogs remain future work.
