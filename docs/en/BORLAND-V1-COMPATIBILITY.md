# Borland Turbo Editor Toolbox V1 compatibility matrix

Status legend: **Implemented**, **Foundation**, **Planned**.

The goal is behavioral coverage, not source-level or public-name identity.

| Historical area | SASD V1 mapping | Status |
|---|---|---|
| Text as lines / text streams | `ITextBuffer`, `EditorDocument` | Implemented |
| Linked-line storage | `LinkedLineTextBuffer` | Implemented |
| Line flags: block / wrapped / special color | `EditorLineFlags` | Implemented |
| Multiple windows | `EditorSession`, `EditorWindow` | Implemented |
| Linked windows sharing one stream | multiple `EditorWindow` objects over one `EditorDocument` | Implemented |
| Insert / overtype | `EditorWindowOptions.InsertMode` | Implemented |
| Word-wrap | `EditorWindowOptions.WordWrap`, engine wrap logic | Implemented |
| Auto-indent | `EditorWindowOptions.AutoIndent` | Implemented |
| Left/right margins and tab width | `EditorWindowOptions` | Implemented |
| Cursor movement | `EditorEngine` plus compatibility navigation processors | Implemented |
| Historical begin/end/goto navigation details | `FirstEdCompatibilityProcessor` | Implemented |
| Character/word/line deletion | `EditorEngine` delete methods | Implemented |
| Change case / center line / paragraph reformat | `EditorEngine` | Implemented |
| Whole-line block begin/end | `EditorBlock`, `EditorSession` | Implemented |
| Block copy/move/delete/hide | `EditorEngine` / `EditorSession` | Implemented |
| Top/bottom of active block commands | compatibility processor | Implemented |
| Markers | `EditorMarker`, markers 1..20 | Implemented |
| Undo limit and undo operation | `EditorUndoManager` + command binding | Implemented (snapshot backend) |
| Forward find / remembered Find Again | `EditorSearchService` | Implemented |
| Replace-next + replace hook | `EditorSearchService`, `IEditorHooks` | Implemented |
| Modern UTF-8 document storage | `ITextStorage`, `FileTextStorage` | Implemented foundation |
| Historical read/write command processors | `EditorFileService` | Implemented |
| High-bit-CR wrapped-line file convention | `FirstEdLegacyFileCodec` | Implemented |
| Host prompting for file/search parameters | `EditorCommandArgumentKind` | Implemented contract |
| Dirty/change flag | `EditorDocument.IsDirty` | Implemented |
| General command dispatcher | `EditorCommandDispatcher` | Foundation / expanding |
| Normalized host-independent keystrokes | `EditorKeyStroke` | Implemented |
| Prefixed Ctrl-K / Ctrl-O / Ctrl-Q dispatchers | `FirstEdKeyMap` prefix state machine | Implemented (mapping) |
| FIRST-ED / WordStar-compatible command map | `FirstEdKeyMap`, `EditorCommandBinding` | Implemented (mapping) |
| Window up/down/goto and stream linking | compatibility processor + `EditorWindow.AttachDocument` | Implemented |
| `UserCommand` concept | `IEditorHooks.FilterCommand` | Implemented |
| `UserError` concept | `IEditorHooks.OnErrorAsync` | Implemented |
| `UserStatusLine` concept | `IEditorHooks.TransformStatus` | Implemented |
| `UserReplace` concept | `IEditorHooks.BeforeReplaceAsync` | Implemented |
| `UserTask` concept | `IEditorHooks.OnIdleAsync`, `EditorScheduler` | Implemented |
| Screen image/update routines | `EditorViewportBuilder`; renderer is host-specific | Foundation |
| Text/status/block/special display attributes | line flags + host renderer | Foundation |
| FIRST-ED demonstration editor | console sample now; interactive host later | Foundation |
| Exit / editor rundown | lifecycle service | Planned |
| Exact display-dependent scroll/page semantics | host-aware navigation | Planned |
| MicroStar pull-down menus | host sample | Planned |
| MicroStar pop-up helpers | host sample | Planned |
| Background printing | scheduler sample | Planned |
| Historical error-message catalog | typed error codes/resources | Planned |
| DOS/video-memory assembly routines | intentionally not reproduced | Replaced by host rendering |
| Overlay support | obsolete on modern platforms | Not applicable |

## V1 release gate

Input mapping, a substantial set of command processors, remembered search and the compatibility file path are now independently testable. V1 should not be called complete until lifecycle/exit behavior, exact display-dependent scrolling, an interactive FIRST-ED-style host, remaining compatibility tests and documentation are present. MicroStar-specific demonstration features may be shipped as samples rather than core dependencies.
