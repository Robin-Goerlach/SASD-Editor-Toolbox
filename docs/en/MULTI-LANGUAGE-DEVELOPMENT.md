# Multi-language development architecture

## Goal

SASD Editor Toolbox is one product contract with multiple independent native implementations. The repository must support C#/.NET, C++, Java and later languages without turning one language into a wrapper around another.

The central rule is:

> **Share behavior, not implementation.**

The .NET implementation is currently the most complete executable reference. It is useful for understanding intent and edge cases, but the portable contract in `spec/` remains authoritative.

## Repository lanes

Every implementation uses the same three peer roots:

```text
src/<language>/      reusable production code
tests/<language>/    native unit/integration/conformance adapters
samples/<language>/  host/reference applications
```

The current language identifiers are `dotnet`, `cpp` and `java`. Additional languages may be added by creating all three matching roots and registering the implementation in `implementations.json`.

A language lane owns its own package/module layout and build system. The repository deliberately does not force one universal build tool across .NET, C++ and Java.

## What is shared

The following artifacts are shared across every implementation:

- `spec/editor-v1.md`: normative portable observable behavior;
- `conformance/`: machine-readable examples of that behavior;
- compatibility and architecture documentation under `docs/`;
- clean-room/legal rules in `AGENTS.md`;
- conceptual terminology such as document, window/view, cursor, marker, block, wrapped boundary, typeahead, command and viewport.

These shared artifacts should describe externally observable semantics and invariants, not a language's class hierarchy.

## What stays language-specific

Each implementation may choose idiomatic namespace/package conventions, ownership and lifetime management, value/reference types, collection and string types, async/cancellation mechanisms, error/result representation, build/test tools, public API naming where conventions differ, and internal buffer/indexing data structures.

C++ does not need to emulate .NET garbage collection or records. Java does not need to reproduce C# events, structs or nullable syntax. C# does not need to imitate native pointer ownership. The observable editor result is what must agree.

## Synchronization workflow

For a portable semantic change:

1. establish or clarify the behavior in `spec/`;
2. add or update a shared conformance case when the behavior can be represented portably;
3. update the compatibility/architecture documentation in English and German;
4. update each actively maintained implementation lane;
5. run repository-contract validation plus the native test suite for each affected implementation;
6. record intentional temporary gaps rather than weakening the shared contract.

This order prevents the most mature language from silently becoming the specification.

## Conformance adapters

A language-specific conformance adapter belongs under `tests/<language>/`. It may translate shared JSON cases into native test objects but must not change the meaning of the shared cases.

Expected-state objects are intentionally partial. A runner checks the fields present in `expect` and may ignore unrelated native state. This keeps cases stable even when implementations expose additional diagnostics or internal metadata.

## CI strategy

Repository-wide validation and language builds are separate concerns:

- `repository-contract.yml` validates structure and shared JSON metadata.
- `dotnet.yml` builds/tests the .NET lane.
- C++ and Java receive their own workflows when implementation work starts.

A failure in one native build should not require another language as a build dependency. Cross-language parity is established through the shared contract and conformance cases, not cross-linking binaries.

## Versioning and release policy

The V1 **contract version** is shared. Package/library versions may initially progress independently while ports catch up. A language should only claim V1 conformance after it passes the required V1 conformance set plus its native regression suite.

If a later V2 contract introduces incompatible semantics, V1 cases remain available so older package lines can continue to verify their contract.

## Porting order

When a new language implementation begins, start with the smallest architecture skeleton that can execute shared conformance cases: text model, document/view separation, cursor primitives and topology. Add compatibility processors, storage, undo, search, scheduling and hosts afterward.

Do not begin by mechanically translating every C# file. Recreate the architecture using the target language's strengths while preserving the shared contract.
