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

The shared `screen-and-annotations.json` cases exercise all-window horizontal scrolling, independent linked-view coordinates, per-window absolute line status, and non-dirty user-color metadata. All snapshots and expected fields are language-neutral; no CLR types or terminal coordinates are serialized.
