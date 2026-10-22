# MicroStar overwrite confirmation dialog

Block-file export now uses a visible Yes/No popup whenever the destination
already exists. The dialog reuses `MicroStarDialogFocus`, the bounded popup
geometry and the LIFO overlay stack; **no additional editing or filesystem
logic** was introduced into the dialog.

**Default focus is No**. Enter without navigation rejects overwriting.
Tab, Shift+Tab and Up/Down move between No and Yes; Enter confirms the
focused action. The user can also explicitly type Y or N. Escape/Ctrl-U
cancel. If the terminal is too small to show the popup, the previous
explicit typed `YES` safeguard remains.

This is an application-host UX decision, not a claim about the exact
MicroStar historical dialog layout. The prior `File.Exists` race caveat
remains: a display confirmation is not an atomic filesystem overwrite
guarantee. The file service and editor core stay unchanged.
