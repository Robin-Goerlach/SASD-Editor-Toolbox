# MicroStar popup rendering — first terminal window layer

The sample now draws an actual **bounded, centered F10 popup** over the
current terminal editor screen. The popup's layout is calculated by the
host-only, pure `ConsoleMicroStarPopupLayout`, and its accepted keys are
validated independently of the console. Escape/Ctrl-U cancels; supported
single letters are returned to the existing command-dispatch pipeline.

The popup is **not** an editor `EditorWindow`: no document views or
`EditorWindowLayout` frames are created, resized or removed.
After dismissal, including cancellation or exceptions, the existing
renderer redraws the authoritative editor frame. This is the portable
equivalent of restoring a text-mode popup's underlying content, without
DOS framebuffer readback or raw video-memory snapshots.

On terminals too small for the fixed popup, the existing one-line
menu prompt remains available. The last terminal column is kept clear
to avoid wrapping.

This is an initial host-local popup **presentation** implementation, not
a historically exact Borland `MakeWindow`/`RestoreWindow` API or full
arrow-key navigable pull-down menu. Menu labels and shortcuts are SASD
choices, not verified historical scan codes. Further work can extract
a generic overlay stack if nested dialogs require it.
