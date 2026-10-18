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
