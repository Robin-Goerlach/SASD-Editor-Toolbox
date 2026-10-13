# Portable specification

This directory contains the language-neutral behavior contract for SASD Editor Toolbox.

## Source of truth

`editor-v1.md` defines portable observable V1 behavior. It intentionally does not prescribe C#, C++, Java or any other language's public API shape, object model, ownership strategy or build system.

When implementations disagree, do not choose the behavior of the most mature implementation automatically. First determine whether the contract is already explicit:

1. If the contract is explicit, implementations should converge on it.
2. If a shared conformance case covers the behavior, that case must agree with the contract.
3. If the contract is ambiguous, clarify the contract and documentation first, then update conformance cases and implementations.
4. If behavior is intentionally implementation-specific, document it as a host/language policy rather than adding it to the portable contract.

The historical handbook remains a behavioral reference for V1 compatibility, subject to the clean-room rules documented in `AGENTS.md`.

## Relation to conformance cases

`../conformance/` contains machine-readable examples of portable behavior. Those files are not a replacement for the prose contract; they are executable examples that make parallel implementations easier to compare.

A language implementation may expose completely different native method and type names. Its conformance adapter maps the shared conceptual operations and state into that language's API.
