# Cross-language conformance

This directory contains implementation-neutral behavior cases for SASD Editor Toolbox.

The purpose is not to create a cross-language runtime or ABI. Each implementation remains independent. Instead, each language can provide a small **test adapter** that:

1. creates editor state described by a case;
2. maps a conceptual operation identifier to its native API;
3. performs the operation;
4. projects native state back into the portable fields used by the case; and
5. compares only the expected fields present in the case.

## Versioning

`v1/manifest.json` belongs to the V1 portable contract in `spec/editor-v1.md`. The conformance file format has its own `formatVersion` so the harness representation can evolve without changing editor semantics.

Operation identifiers are test-harness vocabulary, not required public API names. For example, `move_right_character` can map to a C# compatibility processor, a C++ method or a Java service with entirely idiomatic naming.

## Case shape

Each case has a stable `id`, an `area`, an `initial` portable state and an `expect` object. A case has **either** one semantic `operation` **or** a nonempty array `operations` of steps against the same state. Steps must succeed by default; an expected negative result is declared on that step with `commandSucceeded: false`. The outer `expect.commandSucceeded` describes the last step's outcome. An operation may carry portable `text`, `text2`, `number`, `number2` or `pageSize` fields. Expectations may include `commandSucceeded` so invalid-boundary behavior can be tested without depending on target-language exception types. They may also include one `error` object with portable `id`, stable machine `code` and `resourceKey`; language adapters map those values to their native typed error model.

The initial smoke cases intentionally cover small, well-established invariants. More cases should be added as command-boundary behavior is finalized. Avoid encoding implementation details such as C# GUID types, pointer addresses, collection classes or exception class names.

Multi-step portable fixtures may use `position_cursor` and `select_window` for test-only view navigation. These are not FIRST-ED command IDs; they are host-neutral fixture operations that preserve virtual cursor columns. Semantic operations such as `begin_block`, `end_block`, `insert_line`, `move_block`, `delete_block` and `undo` go through the same command dispatcher as ordinary UI actions. This keeps native C++, Java and C# implementations independent.

## Historical audit traceability

Individual rows in `docs/handbook-procedure-index-v1.json` may refer to concrete portable case IDs through `conformanceCases`. The repository contract checker verifies that each link targets an actual declared case. Unlinked rows are **not silently counted as covered**; the remaining evidence review is part of the V1 release gate. A link provides focused regression evidence only, not complete historical parity.

## Validation

Run:

```bash
python3 tools/verify_repository_contract.py
```

This validates implementation-lane declarations, required directory roots, conformance manifests and the basic case envelope.

The .NET test lane reads **every case file listed in `v1/manifest.json`** and runs it through a native adapter. Semantic editor commands use the public dispatcher. Portable session/view operations such as user-color flags, horizontal visibility and viewport projection use the corresponding public core services instead of inventing keyboard bindings. C++ and Java should follow the same separation when their independent implementations begin.

The shared `screen-and-annotations.json` cases exercise all-window horizontal scrolling, independent linked-view coordinates, per-window absolute line status, and non-dirty user-color metadata. The `viewport-navigation.json` fixtures extend portable coverage to FIRST-ED vertical movement, display-row-aware scrolling, single-row and oversized PageSize, linked views and file boundaries. The new `block-topology-and-undo.json` sequence cases cover independent block endpoints, marker realignment, structural edits, block copy/move/delete and atomic Undo, without imposing any language-specific class layout. All snapshots and expected fields are language-neutral; no CLR types or terminal coordinates are serialized.

The shared `search-and-replace.json` scenarios exercise the public Find, Find Again and Replace semantic commands. An explicitly empty Find `text` repeats the last remembered pattern, while a missing `text` is rejected as invalid. Empty replacement `text2` is valid. The test adapter preserves this distinction when building requests for each language's implementation.

## Portable decoded-file insertion

The `file-read-topology.json` scenarios supply `initial.fileReadLines` as an array of line snapshots (text and optional flags). A portable `read_file` command with nonempty `text` then executes through the normal editor dispatcher. Language adapters provide a deterministic **decoded-line** codec for this fixture only: no disk access or encoding is required. This separates file insertion/topology behavior, which must be portable, from byte-format compatibility, which is verified independently. An empty provider result must leave text, dirty state and Undo unchanged. Future language lanes implement equivalent fixture adapters, not the C# class structure.

Additional `search-and-replace.json` cases cover linked-view cursor independence, moving between documents during normal command sequences, ASCII case-insensitive matching, non-wrapping Find Again and virtual cursor columns near the native 32-bit boundary. These are ordinary portable behavioral fixtures; asynchronous host callback interleavings are currently covered separately by the .NET reference tests and the written V1 contract.

Portable `search-and-replace.json` now also includes error cases for CR/LF in literal search patterns and replacements. Invalid input must return the same host-neutral invalid-argument code without changing cursor, document version, dirty state or Undo. This is a single-logical-line contract, separate from any future topology-aware multiline editor command.

The `search-and-replace.json` suite also covers Find Again after document Undo, empty replacements that expose a new occurrence at the old cursor, Undo in an unrelated document and replacement within a selected block shown by linked windows. These are ordinary semantic-command sequences, independent of the .NET implementation's search-history object model.

## Portable search options (modern host service)

The conceptual `find_next_with_options` operation calls the language's **ordinary reusable search service**, rather than changing the historical FIRST-ED command mapping. Its `searchOptions` object accepts optional `caseSensitive`, `wholeWord` and `wrapAround` booleans (omission means false). The returned match is observed through the current view's cursor; no disk, UI prompt or C# API naming is required. A following `find_again` still uses the remembered options. New V1 fixtures use this adapter to prove that a wrapped match is selected by its *starting position*, even if the matched text extends across the cursor.

## Portable rejection of malformed decoded file lines

Decoded compatibility files are not trusted simply because a codec returned successfully. Three `file-read-topology.json` cases provide a valid line followed by an invalid record containing embedded CR, LF or `0x8D` within its text. Adapters must reject the **entire** pending import through the stable `file_io_failed` error rather than insert the valid prefix: cursor, document version, dirty flag and Undo count remain unchanged. Raw byte encoding/decoding and filesystem staging are still tested in each language implementation.

## Portable file write and Save As evidence

The `file-read-topology.json` suite now also includes `file-write` cases using `initial.fileWriteSink: true`. A deterministic codec captures each `write_file` or `save_file` invocation as a filename plus logical line snapshots (`expect.fileWrites`); no actual filesystem or byte encoding is involved. Expectations may also specify `associatedFileName` to check only the portable filename component instead of a language/OS-specific absolute path.

Direct Write File must preserve document identity, dirty state and Undo and must write the selected document with line flags. A blank filename must not invoke the provider. Modern Save with an explicit path is a separately documented convenience: only that save acknowledges the persisted snapshot and updates document identity. Four of the five new cases link directly to historical `EditFileWrite`; modern Save is intentionally not presented as an additional 1985 requirement.

## Portable FIRST-ED primitive-command audit

The new `compatibility-primitives.json` fixture covers line beginning/end, one-based virtual-column targeting, three-class word motion, per-window insert/word-wrap/auto-indent modes, one-based margins, Change Case, and Delete To End Of Line. These execute public semantic commands rather than any C#-private compatibility APIs.

Expected window projections can contain `options` with individual `insertMode`, `wordWrap`, `autoIndent`, `leftMargin`, `rightMargin` or `tabSize` fields. Only named fields are asserted, permitting independent C++ and Java option representations. Linked views sharing a document retain independent modes. No-op editing requests are handled without dirty/version/Undo changes, whereas actual edits retain Undo behavior.

The same primitive suite includes historical Tab (insert versus overtype), character deletion through virtual cursor space and right-word deletion across punctuation plus blanks. Their command IDs remain semantic rather than DOS-keyboard-specific. Tests distinguish actual edits from cursor-only results and verify the corresponding Undo/dirty states.

## Portable window and block lifecycle evidence

`windows-and-block-lifecycle.json` adds conformance steps for one-based historical window selection, wrapped previous/next navigation, linking existing views, configured row splitting/reallocation, the single-window deletion guard, destructive linked-stream text reset, and hidden/visible whole-line blocks. Optional `initial.layoutRows` configures an already constructed workspace; `expect.layoutFrames` checks zero-based top rows and frame heights in display order. `expect.windowCount` and `expect.currentWindowIndex` check the window lifecycle without requiring fixture IDs for dynamically created views. Per-window `documentId`, `documentLines`, `sameDocumentAs` and `differentDocumentFrom` inspect actual view/document attachments without assuming that new document UUIDs match across languages.

Modern Link Window cleanup is also covered: if the old document becomes unreachable, its Undo history must not consume a later attempt; if another linked view keeps it alive, history must survive. These are modern lifetime guarantees, not claims about Pascal pointer/free-list internals.

Additional window lifecycle fixtures verify rejected self/redundant links and invalid window numbers, minimum split geometry, correct row ownership after deleting a non-first window, and invalidation of markers/incomplete block endpoints only when relinking abandons the last view of the old document. Such rejection tests preserve window topology, text and Undo without assuming a particular UI or pointer layout.

## Portable paragraph reformatting

`reformat-compatibility.json` exercises the public `reformat_paragraph` semantic command, never private .NET methods or historical Pascal pointer helpers. A `Wrapped` flag on a logical line marks a *soft outgoing boundary*; it therefore includes the next line in the paragraph. The fixtures verify whitespace compression, nonzero left-margin padding, pushing words down with new Wrapped boundaries, pulling words up across soft boundaries, stopping at a hard paragraph boundary, atomic word-too-long rejection, an unchanged no-op and Undo restoring the original paragraph. They also link concrete observable duties to five historical reformat-family routine names in the handbook index. Native C++/Java implementations may organize their internal algorithms differently.

## Host input vs. macro input (portable typeahead)

The `typeahead-input.json` fixtures distinguish physical host-key enqueueing (`host_typeahead`) from macro front insertion (`push_typeahead`) and macro text normalization (`push_typeahead_text`). Each receives a single normalized character, optionally `control:true` for Control+letter, except text macros. Input operations can assert portable `expectResult` values `queued`, `ignored`, `aborted` or `overflow`. `read_typeahead` checks the next normalized `expectKind` (`character` or `enter`), `expectCharacter` and/or `expectControl`; `consume_typeahead_abort` verifies that abort is consumable exactly once. The final `expect.typeahead` may assert `count` and `abortRequested`. No DOS scan codes, queue-node classes or host UI calls appear in the shared fixture.

The historical distinction is important: physical Ctrl-U immediately aborts and flushes queued input, while macro-inserted Ctrl-U remains a queued keystroke. Macro front insertion preserves its natural read order and overflow flushes the buffer without leaving a partial macro. These tests link only the corresponding historical Pokechr, EditPushtbf, EditUserpush and EditAbort behaviors.

## Portable FIRST-ED key mapping and host request boundary

`firsted-keymap.json` tests the stateful **normalized keystroke → action/binding → optional semantic dispatcher** pipeline independently of any terminal scan codes. `translate_firsted_key` accepts a one-character `character` or host-neutral `specialKey` (`escape`, `enter`, `backspace`, `delete`, arrows, etc.), and boolean `control`, `alt`, `shift` modifiers. Its per-step expectations may assert `expectAction` (`prefix_pending`, `command`, `insert_text`, `ignored`), `expectCommand` in snake_case, `expectArgument`, `expectFixedNumber`, `expectText`, `expectPrefixDisplay` or `expectPendingPrefix`. A portable case has its own persistent key-map state: Ctrl-K/O/Q prefix and next key operate on the same map, without leaking state to another case.

An `execute_firsted_key` step simulates the **host** completing optional prompt values (`text`, `text2`, `number`, `number2`, `pageSize`) and then dispatches the action through ordinary editor services; it does not show or implement UI prompts. `reset_firsted_keymap` models a host abandoning an incomplete prefix. `expect.firstEdKeymap.pendingPrefix` can check the final parser state. Specific cases are linked to historical EditK, EditO, EditQ, EditPrccmd and EditPrctxt duties. Alt-special-key ignoring is a documented modern host-safety policy, not a claim about exact 1985 modifier handling.

## Portable host prompt completion and cancellation

`host-prompt-resolution.json` introduces an optional `resolve_host_prompt` step after a public FIRST-ED `translate_firsted_key` command action. The operation accepts nullable `firstResponse`, `secondResponse`, `characterResponse` plus optional `pageSize`; `expectCancelled` distinguishes no request from a fully resolved semantic command. For a resolved command, `expectRequest` may inspect portable `id`, `text`, `text2`, `number`, `number2` and `pageSize`. The subsequent `execute_resolved_prompt` step invokes the normal semantic dispatcher, or reports unsuccessful execution without any editor mutation when the prompt was cancelled.

This contracts the important distinction between `null` (abandoned prompt) and `""` (an explicitly submitted empty value). A null replacement *never* means delete; an empty replacement does. An empty Find repeats the previous pattern, while null Find cancels. A host can reuse these cases without adopting console UI or C# method names. The wrappers are considered covered only for the **recorded prompt-to-command duty**, not for terminal graphics or all historical DOS interactions.

## Audited kernel command primitives

The `kernel-routines.json` cases exercise already implemented historical kernel duties through host-neutral semantic commands and explicit block-flag projection operations. Cases cover actual versus no-op Center Line, margins, linked-text cursor navigation across a line boundary, Undoable logical-line joining, literal control-character insertion and restoring/clearing projected `in_block` flags without changing the block's persistent logical endpoints. The `clear_block_highlights` and `mark_block_highlights` steps represent observational projection actions, not newly required editor command IDs.

Six historical index routines (`EditCenterLine`, `EditLeftChar`, `EditJoinLine`, `EditInsertCtrlChar`, `EditOffblock`, `EditMarkblock`) now have case-level portable evidence. These are explicit behavior links, not proof of matching 1985 Pascal implementation internals.

## Portable cooperative scheduler and interruption lifecycle

`cooperative-lifecycle.json` describes a bounded, host-neutral simulation of FIRST-ED's input-first cooperative loop. `register_background_task` registers named slices with an optional `requestRundown`; `run_scheduler_cycle` supplies a deterministic input-pump answer and checks `expectResult` (`input_processed` or `background_processed`). `run_system_loop` uses a bounded positive `exitOnInputCall`, so a case cannot spin forever. `enqueue_physical_abort` and `breathe_checkpoint` check the one-shot Ctrl-U interruption distinct from ordinary queued text. Expectations under `scheduler` may include `inputCalls`, `idleCalls`, `pollCalls`, `userAborts` and ordered `taskTrace` alongside `rundownRequested`.

These fixtures expose observable ordering/cancellation responsibilities for the historical EditBackground, EditBreathe, EditSchedule, EditSystem and EditExit names. They do not require threads, DOS polling interfaces or .NET task implementations in other language lanes. Callback behavior is bounded and inputs are simulated, not physically typed.

## Additional historical interactive Cp wrapper evidence

The host-prompt suite now checks two-parameter window creation, window deletion, one-based window selection, marker setting/jumping, left/right margins, tab width and Undo limit. Both completed and cancelled prompts use actual FIRST-ED Ctrl-K/O/Q bindings and the ordinary command dispatcher. Layout assertions verify that cancelled window operations leave their frames intact; margin rejection tests check that invalid state-dependent values never change view settings. These fixtures link nine additional *observable wrapper responsibilities* from the 1985 handbook; they do not reproduce its console dialog rendering.

## Tab safety at extreme virtual positions

Four portable Tab boundary cases verify that Insert-mode requests with extreme tab widths or distant virtual-column gap fill reject the operation *before* any mutation or Undo; an unrepresentable next tab stop is rejected even in Overtype mode. By contrast, Overtype Tab can advance the cursor at a distant but representable virtual position without creating text. The conservative 4,096-space insertion budget is a documented modern safety rule, not a claimed historical DOS size limit. Ordinary Insert-mode and Overtype-mode Tab fixtures remain part of the same suite.

## V1 evidence-quality requirement

The repository verifier rejects portable cases with an empty `expect` object; merely executing a sequence without checking observable state is insufficient release evidence. The historical index links to focused tests, not automatically to complete 1985 behavioral equivalence. Four new host-prompt cases exercise Ctrl-K R Read File cancellation/dispatch and numeric whitespace/overflow handling through the .NET semantic command boundary.


## Linked-window Undo and original-view lifetime

The portable window-lifecycle suite now checks that after an edited view has been relinked to another document, Undo can restore the original text through its surviving sibling while preserving the user's later explicitly selected current window. This is the host-neutral `windows.undo-relinked-view-preserves-unrelated-current-window` fixture. Low-level legacy `Capture` cursor compatibility is exercised separately by focused .NET tests because that call is intentionally **not** a portable historical Pascal API. Portable suite total: **254**; the new case is mapped to `EditUndo` and `EditWindowLink` responsibilities.
