# MicroStar interactive read-only file browser

The console sample file browser composes existing, separately tested parts:

1. `MicroStarDirectoryCatalog` obtains a **read-only snapshot** of a folder.
2. `MicroStarSelectionList` tracks the focused entry and scrolling viewport.
3. `MicroStarFileBrowser` decides whether Enter enters a subfolder or
   returns a selected regular-file path, and whether Backspace visits the
   parent directory.
4. The console renderer displays this state; the **existing** Core file
   service performs Read Block only after the user selects a file.

The browser controller uses neither Console APIs nor EditorSession. Navigation
never reads file contents, writes files, deletes data, or modifies Undo.
Escape/Ctrl-U cancel; an empty directory cannot invent a selection.
Directory reads are prepared before changing the browsing state so a failed
transition cannot strand the user in an inaccessible location.

Keyboard and geometry are modern SASD application behavior, not an assertion
of exact historical MicroStar scan codes. The first connected operation is
F10 → L (Read Block). Later iterations can reuse the same browser for other
file operations with different explicit approval rules.
