# MicroStar dialog focus policy

`MicroStarDialogFocus` introduces a reusable, testable focus model for
future file pickers, confirmation dialogs, and input forms in the terminal
sample. Each modal dialog owns its own focus instance, independent of
EditorSession, Console input/output, window geometry, and Undo.

The model takes immutable control descriptions with stable unique IDs,
roles (`Input`, `Choice`, `Action`) and enabled states. The first enabled
control receives initial focus. Tab/Down traverse enabled controls forward;
Shift+Tab/Up traverse backward. Enter returns **Activate** and Escape/Ctrl-U
returns **Cancel**. Unhandled keys are never interpreted as text edits or
commands. Invalid dialog definitions fail before presentation.

This is foundational application-host policy; existing F10 command routing
has not been replaced, and no interactive file dialog is claimed yet.
The keyboard choices are modern SASD behavior rather than guaranteed
historical MicroStar semantics. Headless tests cover focus, disabled items,
wrapping, abort, nesting and validation.
