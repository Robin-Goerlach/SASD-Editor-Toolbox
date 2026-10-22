# MicroStar overlay stack

`ConsoleMicroStarOverlayStack` is a host-only LIFO owner for temporary
popups. Every `Push` returns an idempotent disposable lease. Closing an
outer popup before its nested child throws without corrupting the stack.

`ConsoleFirstEdRenderer` now draws the editor's authoritative model and
then all active popups from bottom to top. Closing a nested child redraws
its parent; no fragile DOS/terminal video-cell readback or editor document
mutation is required. The stack is not an `EditorWindow` and never enters
the reusable editor Core.

This is a foundation for `MakeWindow`/`RestoreWindow`-style visual
behavior, not a claim of exact 1985 API equivalence. Input focus handling,
resizing during open dialogs, arbitrary content and keyboard-driven menu
trees remain pending. Headless tests cover nested lifetimes, order,
idempotent release and snapshot isolation.
