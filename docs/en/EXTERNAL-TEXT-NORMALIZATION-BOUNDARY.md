# External newline normalization — SysAdmin/freeware integration boundary

**State:** integration note for V1; no executable dependency introduced.

## Context

SASD-Sysadmin/freeware is a separate administrative-tool repository:
https://github.com/SASD-Sysadmin/freeware. A prospective Linux/DOS newline
normalizer can be useful for controlled ingestion of ordinary text, but it
must not become an implicit editor save/load step.

At the inspected freeware `main` SHA
`c84d0e2a838a2d1d93f9cc71b759524681c00a97`, no uniquely identified
DOS newline-conversion executable was found in its tracked `bin/` tree.
Re-evaluate the exact command, options and guarantees once it is committed
and documented; do not invent a command-line invocation.

## Ownership and invariants

1. **Editor Core owns editor I/O.** `FirstEdLegacyFileCodec` reads and writes
   the historical 8-bit stream. Byte `0x8D` means a soft-line boundary,
   while CR/LF is an ordinary physical delimiter. Blind text conversion
   must not reinterpret those bytes.
2. **Modern text storage owns its encoding/line-ending policy.** Keep
   `FileTextStorage` and the legacy codec distinct. Neither delegates to
   system shell commands or depends on an optional SysAdmin installation.
3. **External normalization is opt-in, on a separate disposable copy.**
   A future host or import tool may run an explicitly selected normalizer
   *before* opening known ordinary text. Preserve the original bytes until
   encoding, file type and user intent are established.
4. **Never normalize arbitrary binary files.** Format detection and overwrite
   authorization belong to the external workflow. Never perform destructive
   transformations automatically just because a document was opened.
5. **Use contracts, not repository coupling.** If a future adapter is useful,
   define input/output, idempotence, error handling and an explicit
   capability check. Do not add a project/package dependency between the two
   repositories for V1.

## Future acceptance tests if the integration becomes concrete

- LF-only input is left unchanged; CRLF ordinary text is converted only when
  requested, with a byte-identical backup retained.
- Repeated conversion is idempotent and cancellation cannot truncate output.
- Invalid encodings, binary data and legacy FIRST-ED `0x8D` streams are
  rejected or handled only under an explicitly reviewed format policy.
- The editor can open the resulting ordinary text without silently changing
  the original file, regardless of whether SysAdmin/freeware is installed.

This is a **scope boundary**, not proof that an external converter exists or
has been integrated. It does not close the V1 manual file-I/O acceptance gate.
