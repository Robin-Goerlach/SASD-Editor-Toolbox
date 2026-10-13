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

Each case has a stable `id`, an `area`, an `initial` portable state, one semantic `operation`, and an `expect` object containing the subset of state that must match.

The initial smoke cases intentionally cover small, well-established invariants. More cases should be added as command-boundary behavior is finalized. Avoid encoding implementation details such as C# GUID types, pointer addresses, collection classes or exception class names.

## Validation

Run:

```bash
python3 tools/verify_repository_contract.py
```

This validates implementation-lane declarations, required directory roots, conformance manifests and the basic case envelope. Language-specific runners will later provide the behavioral execution step.
