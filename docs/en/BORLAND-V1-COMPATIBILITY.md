# Borland Turbo Editor Toolbox V1 compatibility matrix

Status legend: **Implemented**, **Foundation**, **Planned**.

The goal is behavioral coverage, not source-level or public-name identity.

| Historical area | SASD V1 mapping | Status |
|---|---|---|
| Text as lines / text streams | `ITextBuffer`, `EditorDocument` | Implemented |
| Linked-line storage | `LinkedLineTextBuffer` | Implemented |
| Line flags: block / wrapped / special color | `EditorLineFlags` | Implemented |
| `Wrapped` as soft outgoing line boundary | line flags + engine/reformat/file codec | Implemented |
| Multiple windows | `EditorSession`, `EditorWindow` | Implemented |
| Linked windows sharing one stream | multiple `EditorWindow` objects over one `EditorDocument` | Implemented |
| Physical stacked-window row geometry | `EditorWindowLayout`, `EditorWindowFrame` | Implemented |
| `EditWindowCreate(Size, Win)` split/compression rules | compatibility processor + layout | Implemented |
| `EditWindowDelete(Wno)` freed-row ownership | compatibility processor + layout | Implemented |
| `EditWindowDeleteText` destructive stream reset | `DeleteCurrentWindowText`, distinct blank replacement documents | Implemented |
| Insert / overtype | `EditorWindowOptions.InsertMode` | Implemented |
| Word-wrap | `EditorWindowOptions.WordWrap`, engine wrap logic | Implemented foundation |
| Auto-indent | `EditorWindowOptions.AutoIndent` | Implemented |
| Left/right margins | `EditorWindowOptions` | Implemented |
| Tab width | per-window `EditorWindowOptions.TabSize`; historical global scope documented separately | Foundation / documented divergence |
| `EditTab` Insert-vs-Overtype behavior | `FirstEdPrimitiveCompatibilityProcessor.Tab` | Implemented |
| `EditInsertLine` blank-above / blank-below / split behavior | primitive compatibility processor + topology | Implemented |
| `EditNewLine` Insert-vs-Overtype behavior | distinct `NewLine` command + compatibility processor | Implemented |
| Return vs Ctrl-N distinction | `FirstEdKeyMap`: `NewLine` vs `InsertLine` | Implemented |
| New Line Autoindent and previous-line `Wrapped` reset | primitive compatibility processor | Implemented |
| Cursor movement | `EditorEngine` plus compatibility navigation processors | Implemented |
| `EditLeftChar` previous-line last-nonblank rule | primitive compatibility processor | Implemented |
| `EditRightChar` column-only / virtual-column rule | primitive compatibility processor | Implemented |
| Virtual columns survive structural normalization | `EditorWindow.ClampCursor` preserves non-negative column | Implemented |
| `EditLeftWord` leading-indent and previous-word rules | primitive compatibility processor | Implemented |
| `EditRightWord` alphanumeric/punctuation/blank classes and next-line transition | primitive compatibility processor | Implemented |
| Historical begin/end/goto navigation details | `FirstEdCompatibilityProcessor` | Implemented |
| Up/down line viewport-follow behavior | `FirstEdCompatibilityProcessor` + host visible-row count | Implemented |
| Scroll up/down edge-cursor behavior | `FirstEdCompatibilityProcessor` | Implemented |
| Page up/down documented viewport displacement | compatibility processor, `visibleLines - 1` | Implemented |
| Top/bottom file viewport placement | compatibility processor | Implemented |
| `EditDeleteLeftChar` character + previous-line join rule | primitive compatibility processor + topology | Implemented; virtual-space behavior documented separately |
| `EditDeleteRightChar` last-nonblank cursor-positioned join rule | primitive compatibility processor + topology | Implemented |
| `EditDeleteRightWord` three classes, following blanks and cursor-positioned join | primitive compatibility processor + topology | Implemented |
| `EditDeleteLine` single-line preservation, marker invalidation and block-boundary cleanup | `FirstEdPrimitiveCompatibilityProcessor.DeleteLine` | Implemented |
| `EditDelline` / `EditRealign` observable window-marker-block reference repair | `EditorLineTopology` | Implemented foundation; block bulk audit remains |
| Compatibility file insertion reference realignment | `EditorFileService` + `EditorLineTopology.LinesInserted` | Implemented |
| Generic newline / automatic-wrap insertion realignment | `EditorEngine` + `EditorLineTopology.LinesInserted` | Implemented |
| Reformat expansion/contraction reference realignment | reformat processor + `EditorLineTopology` | Implemented |
| Change case / center line | `EditorEngine` | Implemented foundation; helper parity audit pending where applicable |
| `EditCompressLine` / `EditShiftLine` responsibilities | reformat plan normalization / left-margin shift | Implemented |
| `EditLongLine` / `EditShortLine` responsibilities | reformat plan push-down / pull-up | Implemented |
| `EditReformat` Wrapped traversal / margin reflow | `FirstEdReformatCompatibilityProcessor` | Implemented |
| Reformat word-too-long failure before mutation | preflight plan validation | Implemented |
| Whole-line block begin/end | `EditorBlock`, `EditorSession` | Implemented |
| Block copy/move/delete/hide | `EditorEngine` / `EditorSession` | Implemented foundation; bulk topology audit pending |
| `EditOffblock` global InBlock clear, limits retained | `EditorSession.ClearBlockHighlights` | Implemented |
| `EditMarkblock` active-range flag projection | `EditorSession.MarkBlockHighlights` | Implemented |
| Top/bottom of active block commands | compatibility processor | Implemented |
| Markers 1..20 | `EditorMarker` | Implemented |
| Marker identifies line; jump preserves target view column | line-only marker/session jump | Implemented |
| Marker realignment for audited line insertion/deletion paths | `EditorLineTopology` | Implemented |
| Stable anchors through reformat bulk topology | reformat processor + topology | Implemented |
| Undo limit and undo operation | `EditorUndoManager` + command binding | Implemented (snapshot backend) |
| Destructive-document undo cleanup | `EditorUndoManager.DiscardDocument` | Implemented |
| Forward find / remembered Find Again | `EditorSearchService` | Implemented |
| Replace-next + replace hook | `EditorSearchService`, `IEditorHooks` | Implemented |
| Modern UTF-8 document storage | `ITextStorage`, `FileTextStorage` | Implemented foundation |
| Historical read/write command processors | `EditorFileService` | Implemented |
| High-bit-CR wrapped-line file convention | `FirstEdLegacyFileCodec` | Implemented |
| Host prompting contract | `EditorCommandArgumentKind` | Implemented |
| Terminal prompt implementation | `ConsoleFirstEdPromptService` | Implemented sample |
| Dirty/change flag | `EditorDocument.IsDirty` | Implemented |
| General command dispatcher | `EditorCommandDispatcher` | Foundation / expanding |
| Normalized host-independent keystrokes | `EditorKeyStroke` | Implemented |
| Historical default typeahead capacity | `EditorTypeaheadBuffer.DefaultCapacity = 500` | Implemented |
| `Pokechr` queue-style host insertion | `EditorTypeaheadBuffer.EnqueueFromHost` | Implemented |
| `EditPushtbf` front insertion | `EditorTypeaheadBuffer.PushNext` | Implemented |
| `EditUserpush` ordered sequence/macro insertion | `PushSequence`, `PushText` | Implemented |
| Typeahead overflow clears pending input | bounded buffer write results | Implemented |
| Immediate host Ctrl-U / `EditAbort` buffer semantics | clear queue + `AbortRequested` | Implemented foundation |
| Ctrl-U polling inside every interruptible long operation | abort state + modern cancellation boundary | Planned audit |
| Console key normalization | `ConsoleKeyTranslator` | Implemented sample |
| Terminal input through editor-owned typeahead | `ConsoleFirstEdHost` | Implemented sample |
| Prefixed Ctrl-K / Ctrl-O / Ctrl-Q dispatchers | `FirstEdKeyMap` prefix state machine | Implemented (mapping) |
| FIRST-ED / WordStar-compatible command map | `FirstEdKeyMap`, `EditorCommandBinding` | Implemented (mapping) |
| Window up/down/goto and stream linking | compatibility processor + `EditorWindow.AttachDocument` | Implemented |
| `EditExit` / `Rundown` | `EditorCommandId.Exit`, `EditorSession.RundownRequested` | Implemented |
| Ctrl-K X confirmation | host prompt + semantic `Exit` | Implemented sample |
| `EditSchedule` input-first behavior | `EditorScheduler.RunCycleAsync`, `IEditorInputPump` | Implemented |
| `EditSystem` loop-until-rundown | `EditorSystemLoop` | Implemented |
| `UserCommand` concept | `IEditorHooks.FilterCommand` | Implemented |
| `UserError` concept | `IEditorHooks.OnErrorAsync` | Implemented |
| `UserStatusLine` concept | `IEditorHooks.TransformStatus` | Implemented |
| `UserReplace` concept | `IEditorHooks.BeforeReplaceAsync` | Implemented |
| `UserTask` concept | `IEditorHooks.OnIdleAsync`, `EditorScheduler` | Implemented |
| Screen image/update projection | `EditorViewportBuilder` incl. per-window build | Implemented foundation |
| Simultaneous stacked terminal rendering | `ConsoleFirstEdRenderer` + layout frames | Implemented sample |
| Text/status/block/special display attributes | line flags + host renderer | Foundation |
| Interactive FIRST-ED demonstration host | `Sasd.Editor.FirstEd.Sample` terminal host | Implemented sample |
| MicroStar pull-down menus | host sample | Planned |
| MicroStar pop-up helpers | host sample | Planned |
| Background printing | scheduler sample | Planned |
| Historical error-message catalog | typed error codes/resources | Planned |
| DOS/video-memory assembly routines | intentionally not reproduced | Replaced by host rendering |
| Overlay support | obsolete on modern platforms | Not applicable |

## Word-class modernization policy

The handbook's three word classes are retained. For modern Unicode text the .NET implementation treats Unicode letters/digits as alphanumeric, Unicode whitespace as blank, and other characters as punctuation. This is behaviorally compatible for ASCII input while avoiding an ASCII-only reusable core. The same classifier is used by the audited right-word movement and right-word deletion compatibility paths.

## Wrapped-boundary policy

`EditorLineFlags.Wrapped` denotes the soft boundary after the flagged logical line. The policy is shared by the explicit reformatter, automatic wrapping and the legacy high-bit-carriage-return codec. An explicit New Line clears the previously current line's `Wrapped` bit and therefore establishes a hard paragraph boundary.

The historical `EditCompressLine` wording names spaces specifically. The .NET reformat planner accepts Unicode whitespace while preserving the same ASCII behavior; this is a deliberate modern extension rather than a claim about the original character set.

## Line-topology modernization policy

The historical implementation obtains stable line identity through descriptor pointers. SASD represents the same observable relationships through `DocumentId` plus logical line numbers and an explicit `EditorLineTopology` realignment service. Pointer addresses, splicing mechanics and manual descriptor release are not part of the portable API.

The audited single-line insertion, New Line, automatic wrap, file insertion, deletion/join and paragraph-reformat paths now report topology changes centrally. The remaining bulk-topology audit is concentrated in multi-line block copy/move/delete.

The first .NET block model stores a complete start/end pair. When a deleted line is a block boundary it therefore clears the complete active block and highlighting. This is a conservative representation of the historical result (a boundary becomes undefined), not a claim that both historical pointers were set to `nil`.

## Virtual-column policy

FIRST-ED explicitly permits a cursor to move beyond a line's physical buffer length. Non-negative virtual columns are therefore valid editor state. `EditorWindow.ClampCursor` normalizes line and viewport coordinates after structural changes but deliberately preserves the column. For Backspace inside unstored virtual space, the handbook is not explicit; the .NET V1 implementation uses cursor-only movement and documents that as a modern policy.

## Low-level compatibility policy

Raw Pascal pointers, descriptor free lists and 16-bit integer ceilings are implementation mechanisms, not portable requirements. The V1 audit preserves observable behavior: which line/window/block/marker remains selected, which text changes, what is undoable and what the host sees. Where modern data structures remove a historical memory-management operation entirely, the compatibility matrix records the replacement instead of manufacturing a pointer-shaped API.

## Typeahead modernization policy

Behavioral ordering and the historical 500-entry default are retained, but raw DOS bytes, circular-buffer indices and scan codes are deliberately not part of the portable V1 contract. Macro input and physical input are normalized to `EditorKeyStroke` first. A host-originated Ctrl-U takes the immediate abort path; front-injected macro input does not silently become physical input.

## Page cursor policy

The handbook states that page movement slides the window by one less than the number of displayed text lines, but it does not separately specify the cursor's resulting screen row. SASD preserves the cursor's relative visible row when possible and documents this as a modern host-neutral policy rather than claiming it as historical behavior.

## Modern resize policy

The historical screen was fixed-size. Resizing a modern terminal is therefore not a historical compatibility rule. The .NET reference implementation gives growth to the bottom window and removes spare rows from bottom to top while preserving the three-row minimum. This policy is documented separately from `EditWindowCreate`/`EditWindowDelete` compatibility semantics.

## V1 release gate

The C#/.NET implementation now has executable coverage for the major FIRST-ED structural areas, including paragraph reformatting and its topology effects. Before calling V1 complete, close the remaining multi-line block-topology gap, finish systematic command-boundary/error-resource and interruptibility audits, then run a final handbook procedure-index audit. MicroStar-specific demonstrations may ship as samples rather than core dependencies.
