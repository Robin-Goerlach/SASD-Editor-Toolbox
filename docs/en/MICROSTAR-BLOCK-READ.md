# MicroStar Read Block: transactional file import

The MicroStar Read Block application feature now uses the **same**
`EditorFileService` decoder, full-payload validation, interruption
checkpoints, initiating-window identity, topology repair, and Undo transaction
as ordinary FIRST-ED Read File. There is no duplicate insertion engine.

`ReadBlockIntoCurrentWindowAsync` selects exactly the imported whole-line
range within the existing transaction. Empty input is a no-op and preserves
the former selection. Malformed decoded text fails before state mutation.
Ordinary Read File continues to import without creating a block.

The new F10 sample entry `L` asks for a filename and delegates to Core.
The `L` choice is **modern SASD UI policy**, not claimed historical
MicroStar keyboard syntax. Headless tests cover linked views, Wrapped
flags, cursor anchoring, Undo, malformed and empty input.

Still open: historical popups/menus, directory commands, save/open
workflows, cooperative print integration and optional spell checking.
