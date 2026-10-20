# Borland Turbo Editor Toolbox V1 compatibility matrix

Status legend: **Implemented**, **Foundation**, **Planned**.

The goal is behavioral coverage, not source-level or public-name identity.

| Historical area | SASD V1 mapping | Status |
|---|---|---|
| Text as lines / text streams | `ITextBuffer`, `EditorDocument` | Implemented |
| Linked-line storage | `LinkedLineTextBuffer` | Implemented |
| Line flags: block / wrapped / special color | `EditorLineFlags` | Implemented |
| `EditColorLine` / `EditColorFile` user-color flags | `EditorSession.ColorCurrentLine` / `ColorCurrentDocument` | Implemented; presentation-only policy |
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
| `EditHscroll` all-window cursor visibility | `EditorWindow.EnsureCursorHorizontallyVisible` + `EditorSession.EnsureAllWindowCursorsHorizontallyVisible` | Implemented, host-supplied width |
| `EditGenlineno` absolute line counts | indexed `EditorWindow.Cursor.Line` + `EditorViewportBuilder` | Modern replacement, O(1) |
| Page up/down documented viewport displacement | compatibility processor, `visibleLines - 1` | Implemented |
| Top/bottom file viewport placement | compatibility processor | Implemented |
| `EditDeleteLeftChar` character + previous-line join rule | primitive compatibility processor + topology | Implemented; virtual-space behavior documented separately |
| `EditDeleteRightChar` last-nonblank cursor-positioned join rule | primitive compatibility processor + topology | Implemented |
| `EditDeleteRightWord` three classes, following blanks and cursor-positioned join | primitive compatibility processor + topology | Implemented |
| `EditDeleteLine` single-line preservation, marker invalidation and block-boundary cleanup | `FirstEdPrimitiveCompatibilityProcessor.DeleteLine` | Implemented |
| `EditDelline` / `EditRealign` observable window-marker-block reference repair | `EditorLineTopology` | Implemented foundation; audited single-line, reformat and block-bulk paths |
| Compatibility file insertion reference realignment | `EditorFileService` + `EditorLineTopology.LinesInserted` | Implemented |
| Generic newline / automatic-wrap insertion realignment | `EditorEngine` + `EditorLineTopology.LinesInserted` | Implemented |
| Reformat expansion/contraction reference realignment | reformat processor + `EditorLineTopology` | Implemented |
| Change case / center line | `EditorEngine` | Implemented foundation; helper parity audit pending where applicable |
| `EditCompressLine` / `EditShiftLine` responsibilities | reformat plan normalization / left-margin shift | Implemented |
| `EditLongLine` / `EditShortLine` responsibilities | reformat plan push-down / pull-up | Implemented |
| `EditReformat` Wrapped traversal / margin reflow | `FirstEdReformatCompatibilityProcessor` | Implemented |
| Reformat word-too-long failure before mutation | preflight plan validation | Implemented |
| Whole-line block model | `EditorBlockState` + complete `EditorBlock` projection | Implemented |
| `EditBlockCopy` | `FirstEdBlockCompatibilityProcessor.CopyToCursor` + topology | Implemented |
| `EditBlockMove` | block compatibility processor + topology | Implemented |
| `EditBlockMove` cursor-inside-source rejection | compatibility processor validation | Implemented |
| `EditBlockDelete` | block compatibility processor + topology | Implemented |
| Cross-window/document block operations | source block + current target window/document | Implemented |
| Block copy/move line metadata | preserve `Wrapped`/`UserColored`; regenerate `InBlock` | Implemented |
| Block hide/display | `EditorSession.ToggleBlockHidden` | Implemented |
| Independent `Blockfrom` / `Blockto` undefined states | nullable `EditorBlockState.Start` / `End` endpoints | Implemented |
| `EditOffblock` global InBlock clear, limits retained | `EditorSession.ClearBlockHighlights` | Implemented |
| `EditMarkblock` active-range flag projection | `EditorSession.MarkBlockHighlights` | Implemented |
| Top/bottom of active block commands | compatibility processor | Implemented |
| Markers 1..20 | `EditorMarker` | Implemented |
| Marker identifies line; jump preserves target view column | line-only marker/session jump | Implemented |
| Marker realignment for audited line insertion/deletion paths | `EditorLineTopology` | Implemented |
| Stable anchors through reformat/block bulk topology | compatibility processors + topology | Implemented |
| Marker policy for lines moved out of source block | source deletion semantics; block recreated at target | Documented modern policy |
| Undo limit and undo operation | `EditorUndoManager` + validated compatibility command | Implemented (transactional snapshot backend; compatibility limit >= 1) |
| One logical mutation = one undo history item | `EditorUndoTransaction` commit boundary | Implemented modern policy |
| Cross-document block-move compound undo | one multi-document transaction restoring source + target together | Implemented |
| Undo navigation/anchor restoration | post-state-aware window/marker/block/current-window restoration | Implemented modern policy |
| Destructive-document undo cleanup | `EditorUndoManager.DiscardDocument` removes a whole compound item | Implemented |
| Forward find / remembered Find Again | `EditorSearchService` | Implemented |
| Replace-next + replace hook | `EditorSearchService`, `IEditorHooks` | Implemented |
| Modern UTF-8 document storage | `ITextStorage`, `FileTextStorage` | Implemented foundation |
| Historical read/write command processors | `EditorFileService` | Implemented |
| High-bit-CR wrapped-line file convention | `FirstEdLegacyFileCodec` | Implemented |
| Host prompting contract | `EditorCommandArgumentKind` | Implemented |
| Terminal prompt implementation | `ConsoleFirstEdPromptService` | Implemented sample |
| Dirty/change flag | `EditorDocument.IsDirty` | Implemented |
| General command dispatcher | `EditorCommandDispatcher` | Implemented foundation |
| Semantic command-boundary validation | `EditorCommandValidator` + typed validation categories | Implemented |
| Normalized host-independent keystrokes | `EditorKeyStroke` | Implemented |
| Historical default typeahead capacity | `EditorTypeaheadBuffer.DefaultCapacity = 500` | Implemented |
| `Pokechr` queue-style host insertion | `EditorTypeaheadBuffer.EnqueueFromHost` | Implemented |
| `EditPushtbf` front insertion | `EditorTypeaheadBuffer.PushNext` | Implemented |
| `EditUserpush` ordered sequence/macro insertion | `PushSequence`, `PushText` | Implemented |
| Typeahead overflow clears pending input | bounded buffer write results | Implemented |
| Immediate host Ctrl-U / `EditAbort` buffer semantics | clear queue + `AbortRequested` | Implemented |
| `EditBreathe`-style bounded host polling | `IEditorHooks.PollInputDuringOperationAsync` + `EditorInterruptionService.CheckpointAsync` | Implemented |
| Ctrl-U during interruptible long operations | file I/O wrapper + search/reformat checkpoints + typed `OperationAborted` | Implemented / audited |
| External cancellation remains distinct from Ctrl-U | `CancellationToken` propagates; no editor abort error | Implemented |
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
| Historical error-message catalog | deliberately not copied; modern `EditorErrorId` + stable codes/resource keys replace presentation-specific text | Replaced / Implemented |
| Typed host-neutral error/resource model | `EditorErrorCatalog`, `IEditorHooks.OnErrorAsync` | Implemented |
| DOS/video-memory assembly routines | intentionally not reproduced | Replaced by host rendering |
| Overlay support | obsolete on modern platforms | Not applicable |

## Word-class modernization policy

The handbook's three word classes are retained. For modern Unicode text the .NET implementation treats Unicode letters/digits as alphanumeric, Unicode whitespace as blank, and other characters as punctuation. This is behaviorally compatible for ASCII input while avoiding an ASCII-only reusable core. The same classifier is used by the audited right-word movement and right-word deletion compatibility paths.

## Wrapped-boundary policy

`EditorLineFlags.Wrapped` denotes the soft boundary after the flagged logical line. The policy is shared by the explicit reformatter, automatic wrapping and the legacy high-bit-carriage-return codec. An explicit New Line clears the previously current line's `Wrapped` bit and therefore establishes a hard paragraph boundary.

The historical `EditCompressLine` wording names spaces specifically. The .NET reformat planner accepts Unicode whitespace while preserving the same ASCII behavior; this is a deliberate modern extension rather than a claim about the original character set.

## Line-topology modernization policy

The historical implementation obtains stable line identity through descriptor pointers. SASD represents the same observable relationships through `DocumentId` plus logical line numbers and an explicit `EditorLineTopology` realignment service. Pointer addresses, splicing mechanics and manual descriptor release are not part of the portable API.

The audited single-line insertion, New Line, automatic wrap, file insertion, deletion/join, paragraph-reformat and multi-line block copy/move/delete paths now report topology changes centrally. The former block bulk-topology gap is closed.

The .NET block state stores independently nullable begin/end endpoints and exposes a complete `EditorBlock` only when both limits form an ordered range in one document. Structural insertion realigns each endpoint independently; deleting one boundary invalidates only that endpoint and forces highlighting off until the pair becomes complete again.

For a block move, the handbook does not separately specify whether markers or other anchors inside the moved source range follow the physical moved lines. The .NET implementation applies normal source-deletion semantics to those anchors and explicitly recreates the logical block at the destination. This is a documented modern policy, not a historical claim.

## Virtual-column policy

FIRST-ED explicitly permits a cursor to move beyond a line's physical buffer length. Non-negative virtual columns are therefore valid editor state. `EditorWindow.ClampCursor` normalizes line and viewport coordinates after structural changes but deliberately preserves the column. For Backspace inside unstored virtual space, the handbook is not explicit; the .NET V1 implementation uses cursor-only movement and documents that as a modern policy.

## Low-level compatibility policy

Raw Pascal pointers, descriptor free lists and 16-bit integer ceilings are implementation mechanisms, not portable requirements. The V1 audit preserves observable behavior: which line/window/block/marker remains selected, which text changes, what is undoable and what the host sees. Where modern data structures remove a historical memory-management operation entirely, the compatibility matrix records the replacement instead of manufacturing a pointer-shaped API.

## Typeahead modernization policy

Behavioral ordering and the historical 500-entry default are retained, but raw DOS bytes, circular-buffer indices and scan codes are deliberately not part of the portable V1 contract. Macro input and physical input are normalized to `EditorKeyStroke` first. A host-originated Ctrl-U takes the immediate abort path; front-injected macro input does not silently become physical input.

## Interruptibility modernization policy

The handbook explicitly describes `EditAbort` as immediate Ctrl-U handling and `EditBreathe` as keyboard polling from long operations, with compatibility file reading specifically abortable during the read. The .NET implementation preserves that behavior without polling Console APIs from the core: a host hook performs bounded physical-input polling, `EditorInterruptionService` consumes the one-shot abort state, and interruptible async I/O receives a linked cancellation token.

Search and reformat also use safe checkpoints as a modern extension for responsiveness. Their state is committed only after the interruptible scan/planning phase succeeds. Short structural commit phases deliberately do not accept Ctrl-U halfway through mutation, because a partially spliced document would be less safe than completing the bounded in-memory change.

External `CancellationToken` cancellation is not reported as Ctrl-U and remains caller control flow.

## Page cursor policy

The handbook states that page movement slides the window by one less than the number of displayed text lines, but it does not separately specify the cursor's resulting screen row. SASD preserves the cursor's relative visible row when possible and documents this as a modern host-neutral policy rather than claiming it as historical behavior.

## Modern resize policy

The historical screen was fixed-size. Resizing a modern terminal is therefore not a historical compatibility rule. The .NET reference implementation gives growth to the bottom window and removes spare rows from bottom to top while preserving the three-row minimum. This policy is documented separately from `EditWindowCreate`/`EditWindowDelete` compatibility semantics.

## Portable sequence regression evidence

The portable suite now exercises multi-step block Begin/End, marker identity through line topology changes, block copy/move/delete and atomic cross-document Undo. These cases live in `conformance/v1/cases/block-topology-and-undo.json` and are individually linked from the historical procedure inventory. They are targeted behavioral evidence, not proof of complete Pascal implementation equivalence.

## Portable viewport navigation evidence

The shared `conformance/v1/cases/viewport-navigation.json` fixture now covers eight historical cursor, vertical scroll, page and file-boundary routines. It checks minimal edge movement, the documented relative-row policy, linked-view isolation and arithmetic limits. The one-row viewport case has **zero page displacement**, while the dispatcher may still consider a valid navigation request successfully handled. These are observable fixtures for future independent C++ and Java adapters, not DOS API translations.

## Portable search/replace evidence

The `conformance/v1/cases/search-and-replace.json` manifest tests the historical empty-prompt repetition behavior, missing-pattern rejection, Find Again, empty-string replacements and Undo. It contributes focused cross-language evidence for `EditFind` and `EditReplace` without coupling conformance to the .NET search-service internals.

Portable case-insensitive literals use ASCII examples; locale-dependent Unicode case-folding is not defined by the shared V1 fixtures. Additional Unicode matching is a documented language/host extension rather than an accidental cross-language requirement.

## Portable file-read topology evidence

`conformance/v1/cases/file-read-topology.json` adds cross-language semantic Read File tests based on decoded logical lines. The fixtures verify insertion placement, undo, per-document isolation, metadata and line-anchor realignment. They intentionally avoid filesystem paths and bytes; real FIRST-ED high-bit carriage-return decoding remains verified by dedicated .NET codec tests.

## Async search and replacement correctness

Interruptible Find/Find Again now retain their initiating view and immutable document snapshot across host checkpoints; stale searches cannot overwrite later editor navigation or the most recently completed search. Awaited replace confirmation is likewise guarded against stale matches and active-window changes. These modern defensive rules are regression-tested separately from historical search matching semantics.

## Find Again / Undo interaction

The search history is separate from the snapshot Undo journal. Restoring a document discards its stale remembered match anchor but preserves the remembered search pattern and options. An unchanged prior match may still be skipped on ordinary repeats; a changed line cannot suppress a newly exposed occurrence. The same undo boundary invalidates pending asynchronous searches. Focused .NET regression cases verify these modern state-consistency rules, in addition to portable ordinary-command fixtures.

## Logical-line search/replace validation

Single-line literal Find/Replace rejects embedded CR/LF before modifying search state, document text or Undo. Direct service and semantic dispatcher tests distinguish the normal typed invalid-argument response from structural editing. Future multiline replacement remains outside V1 until a separate topology-safe contract is designed.

## Replacement-confirmation accuracy

Host hooks now receive the actual selected document substring instead of an input pattern whose letter case may differ; the candidate is extracted from the guarded search snapshot before asynchronous confirmation. Focused tests verify confirm, skip, cancel and no-match behavior across case-insensitive and whole-word searches.

## Built-in file write safety

The .NET modern UTF-8 storage provider and historical byte codec now stage output beside the destination and replace it only once staging and cancellation checks succeed. Deterministic tests inject staging failures and cancellation to protect the original file bytes. This is a documented modern host-safety policy; filesystem durability, metadata replacement semantics and custom codec behavior remain explicit limits rather than assumptions of 1985 compatibility.

## Complete codec import validation

The compatibility read service now copies and validates every decoded line before creating an Undo entry or altering document topology. Invalid embedded structural delimiters, null provider records and unsupported flag bits cause a typed file-I/O failure with no partial document import. Long validation runs include abort checkpoints. The built-in legacy decoder already returns valid lines; these guards apply at the extensibility boundary for custom codecs.

## Legacy separator boundary regressions

The .NET codec now has separate byte-level cases for terminal and consecutive high-bit CR, defensive 0x8D/LF, ordinary CR/LF variants, blank logical lines, empty physical streams and Latin-1 byte round trips. These strengthen implementation confidence without claiming undocumented DOS input variants are historically normative.

## Concurrent destination-write serialization

The built-in staged file writers now reserve a normalized target-path gate during staging and publication. Same-path writes no longer overtake a prior in-progress write, while distinct paths remain independent. Cancellation while waiting and failure during a staged write release the gate without publishing a partial target. This is a modern in-process persistence policy, not a distributed locking or historical DOS guarantee.

## UTF-8 save/load topology safety

Modern UTF-8 writes now reject embedded CR/LF inside buffer lines and invalid configured newline styles before any staged write, preventing a lossy logical-line round trip. Save acknowledgement also checks the newline convention that was serialized, alongside text and version. These guards are modern storage constraints and do not impose FIRST-ED's reserved 0x8D byte on UTF-8 documents.

## Save As completion order and document association

The document assigns a save-order token before any awaited Save/Save As. Successful newer requests take precedence over slower older completions even when their target paths differ, preventing the older request from reverting a newer file association. Failed/cancelled newer requests do not suppress older successful saves. Direct Write File deliberately remains outside document-identity acknowledgements.

## Portable direct Write File evidence

A new recording decoded-line codec adapter verifies the selected document snapshot, preserved Wrapped/UserColored flags, blank-path rejection and the historical direct Write File rule of not changing document identity/dirty/Undo state. The `EditFileWrite` inventory entry now links to four portable cases. Modern explicit Save is tested separately and must not be confused with the 1985 direct write operation.

## New portable evidence for historical primitive commands

The shared primitive-command cases cover historical line/word navigation, virtual columns, per-view editing modes, one-based margin settings, and actual versus nonmutating single-line edits. `ChangeCase` and `DeleteToEndOfLine` now avoid false dirty and Undo entries on empty operations; this is explicitly documented as a modern consistency policy. Individual historic routines link to executable fixtures without implying complete DOS implementation identity.

## Portable Tab and delete-command traces

Five additional portable tests cover `EditTab`, `EditDeleteLeftChar`, `EditDeleteRightChar` and `EditDeleteRightWord`, distinguishing insertion, cursor-only virtual-space behavior and real Undoable deletion. Only these concrete semantic behaviors are linked to the respective historic inventory entries.

## Window relinking and unreachable-document history

The session now owns compatibility window reattachment and cleans up stale marker/Undo references when the previously displayed text stream loses its **last** view. The same rule applies to directly closed views. Remaining linked views keep their document and history; cross-document Undo transactions involving an orphaned stream are discarded as whole units. Historical Delete Window still clears an active block according to its documented rule, even when another linked view survives.

## Executable window/block lifecycle evidence

Portable window/frame and block lifecycle cases now exercise historical view navigation, Create/Delete/Link Window, destructive Delete Window Text, hidden block projection and top/bottom block navigation. The host supplies total row geometry without any UI-specific runtime. Only test-observable semantics are linked to routine names; modern last-view history cleanup is documented separately from historical pointer mechanics.

## Reformat planning consistency across host callbacks

Reformat now captures the initiating document/view, margins, cursor, block state, version and complete logical-line snapshot before any await. A final guarded commit rejects stale plans after linked-view edits, direct buffer mutations, cursor/margin changes or view reattachment. Switching the active view alone does not redirect the destination. Focused .NET tests use deterministic input-checkpoint callbacks to verify all branches.

## Cooperative scheduler rundown/registration invariants

The modern scheduler stops starting background slices as soon as rundown is requested, even when requested in an idle callback or an earlier background slice. Unregistered tasks are skipped in the same cycle and new tasks are deferred until a later idle cycle. External cancellation is observed before idle/slice invocation. Deterministic .NET regressions verify these modern lifecycle boundaries without introducing threaded background execution.

## Portable paragraph-reflow evidence

Eight shared conformance cases now verify compress, left-margin shift, long-line wrap, short-line pull-up, hard-boundary stop, too-wide-word rejection, no-op and Undo through the semantic Reformat command. Five historical reformat-family routine entries link to these actual observable tests. Async host interleaving remains covered separately by deterministic .NET tests.

## Portable typeahead/abort traces

Eight language-neutral typeahead cases now verify host FIFO vs. macro front insertion, queued macro Ctrl-U versus immediate physical Ctrl-U abort, CR/LF and ASCII control-key normalization, and full-buffer overflow clearing. Four historical routines (Pokechr, EditPushtbf, EditUserpush, EditAbort) now link to executable portable cases. Platform-specific key polling remains a host responsibility.

## Host wrapper prompt-to-command acceptance

Portable `host-prompt-resolution.json` cases now exercise the host-neutral interpretation of cancelled and submitted prompt replies through real Ctrl-K/O/Q bindings and semantic dispatch. The tests distinguish null Find from empty Find Again, cancelled replacement from intentional deletion, valid and invalid numbers, confirmation, and filepath/character prompts. Seven historically named interactive Cp wrappers receive explicit executable links for their observable prompt/command duties. Terminal presentation remains a host sample obligation.

## Portable kernel routine audit

Ten shared `kernel-routines.json` cases link six historically named kernel routines to directly executed behavior. These cover changed and unchanged Center Line commands, crossing a logical-line boundary to the previous nonblank end, Undoable joining, literal control-character insertion and independent block flag projection. The no-op centering fix is a modern state-consistency guarantee; no Pascal pointer ABI or historical video-memory API is required.

## Cooperative system lifecycle conformance

Ten host-neutral scenarios in `cooperative-lifecycle.json` verify input-first scheduler priorities, repeatable bounded background slices, prompt rundown/Exit, the host-neutral system loop and the single-use physical Ctrl-U interruption checkpoint. Five additional historical scheduler/lifecycle routines now have direct portable case references, without leaking platform-specific input polling into the reusable core.

## Extended host wrapper conformance

Twelve new portable host-prompt sequences cover Create/Delete/Go To Window, Set/Jump Marker, left/right margin, tab width and Undo-limit prompts, including cancellation and state-based rejection. Nine additional historical `EditCp...` wrappers link to those directly executed semantic boundaries. The tests require no Pascal-shaped prompt API or terminal renderer in the shared core.

## Large virtual tab stops

Direct and FIRST-ED Insert-mode Tab now reject unsafe generated padding and virtual-column overflow before changing text or Undo. The numeric budget is a documented modern implementation guard, not an imposed historical tab-width limit. FIRST-ED Overtype Tab keeps its cursor-only semantics at large virtual positions.

## V1 release gate

The C#/.NET implementation now has executable coverage for the major FIRST-ED structural areas, including paragraph reformatting, whole-line block bulk topology, independent block endpoints and atomic multi-document undo/navigation restoration. The [handbook procedure/function index audit](HANDBOOK-PROCEDURE-INDEX-AUDIT.md) now records all 123 printed entries with first-pass classifications; its mapped behavior still requires a final regression-coverage and host-interface audit before calling V1 complete. MicroStar-specific demonstrations may ship as samples rather than core dependencies.

## Default UTF-8 malformed-input safety (.NET modern storage)

The `FileTextStorage` default now decodes UTF-8 strictly: invalid byte streams fail before an editor document is created, while a valid UTF-8 BOM remains readable and the default save continues BOM-less. It also rejects unpaired UTF-16 surrogates on write without replacing existing disk bytes or acknowledging a nonexistent save. Explicit alternate encodings preserve their previous caller-selected decoding policy. This is an additional **.NET-only modern persistence-safety policy**, not a new claim of 1985 compatibility or a portable FIRST-ED conformance fixture.

## Protection against external changes (modern .NET storage)

A successfully loaded default UTF-8 document now tracks a raw-byte SHA-256 revision. Before a modern Save publishes to the same path, the staged writer validates that the on-disk bytes still match. A changed/deleted file is not overwritten; successful publication advances the document-owned revision. This is a **.NET-only best-effort persistence safeguard**, not a portably required historical FIRST-ED behavior or cross-process atomic transaction. The relevant tests exercise equal-length changes, deletion, BOM-only changes, successive saves, explicit Save As and the pre-publication hook.
