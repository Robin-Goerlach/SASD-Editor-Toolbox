# V1.0 product scope and historical divergence — approved direction

**State: PRODUCT DIRECTION CONFIRMED by owner on 2026-10-29; terminal acceptance and release approval OPEN.**  
**Reviewed baseline:** `997a675fb6be9b5c4830e6be9b9145e9c6df099f` on `main` (both Actions workflows
`repository-contract` and `dotnet` completed successfully at review).
**Not** a release authorization or historical parity certificate.

## Confirmed release identity

SASD Editor Toolbox V1.0 is an independently implemented, modern,
reusable editor *foundation* with observable FIRST-ED/MicroStar-inspired
behavior where verified. It is **not** a source translation of historical
Pascal, an ABI-compatible Borland library, a DOS emulator, a claim of
identical historical screen operation, or an endorsed Borland product.
All wording should consistently say *functional modernization with
documented differences*, not *100% faithful reproduction*.

### In scope — the V1 deliverable

- UI-independent `Sasd.Editor.Core`: document and view identity,
  text/line editing, linked windows, markers/blocks, search and replace,
  undo, command boundaries, compatibility policies and storage services.
- Modern UTF-8 document persistence and an explicitly separate
  opt-in legacy FIRST-ED 8-bit codec, including conservative I/O failure
  handling. Legacy output cannot represent arbitrary Unicode.
- A terminal sample providing interactive FIRST-ED and MicroStar-style
  workflows as **demonstrations/hosts**, not a promise that the reusable
  Core contains terminal UI or original DOS integration.
- Windows and Linux build/test evidence, independently executed
  project-reference and NuGet package-reference Core consumers, and
  a reproducible prerelease package for evaluation.
- Clear API, test, compatibility and user-facing release documentation.

### Out of scope for V1.0 — agreed direction

- Exact Pascal API/ABI, Pascal memory layout, DOS assembler and direct
  screen-memory access; original source/published manual redistribution.
- Physical DOS printer and printer-port compatibility; MicroStar
  print-to-file/pagination is a portable substitute.
- Lightning spell-check integration as a separate optional capability.
- Binary-format compatibility with the original `EDITERR.MSG` and
  undocumented `Optstr` syntax.
- GUI desktop host, WinForms/WPF migration, Java/C++ parity and
  incorporation into the separate `SASD-Editor-Toolkit`.
- Complete unqualified equivalence of every 1985 procedure and
  unspecified keyboard/terminal behavior.

These boundaries do not waive defects in *in-scope* documented behavior,
data integrity or user-facing terminal flows.

## Historical dispositions under the confirmed product direction

| ID | Proposed V1 disposition | Acceptance still needed |
| --- | --- | --- |
| HP-01 | **ACCEPT DIVERGENCE**: modern C/W/R find/replace options; original `Optstr` semantics not asserted | Owner approves release wording |
| HP-02 | **ACCEPT DIVERGENCE**: typed error codes and optional JSON messages, not Borland `EDITERR.MSG` | Host error-display smoke plus owner approval |
| HP-03 | **ACCEPT DIVERGENCE**: per-view tab width rather than global `Tabsize` | Numbered/cancel/empty/focus terminal checks |
| HP-04 | **ACCEPT DIVERGENCE**: model-driven redraw/overlay instead of DOS memory/assembler | Terminal redraw checks |
| HP-05 | **MODERN REPLACEMENT**: overlay windows and restore semantics | Popup + cancellation + redraw terminal checks |
| HP-06 | **ACCEPT DIVERGENCE**: queued print-to-file rather than physical DOS printing | Disposable output, status and cancellation terminal checks |
| HP-07 | **EXCLUDE FROM V1**: Lightning is optional separate integration | Owner must explicitly approve exclusion |
| HP-08 | **MODERN HOST MAPPING**: prompts, input handling and typeahead | Real-terminal prompt/cancel/recall observations |
| HP-09 | **MODERN CODEC BOUNDARY**: explicit historical 8-bit import/export | Disposable roundtrip, unsupported-character and cancellation tests |
| HP-10 | **SCOPED ACCEPTANCE**: covered modern behaviors with evidence; no blanket 123+20 routine parity promise | Review unresolved release-significant gaps and owner scope approval |

**Decision boundary:** The owner confirmed the overall direction of a modern
functional library with documented historical differences on 2026-10-29.
The individual HP rows above remain proposals for concrete acceptance
until corresponding evidence and scope review are recorded. This broad
approval is not a blanket certification of all ten historical features,
not a terminal acceptance result and not permission to publish V1.0.

## Blocking acceptance and publication rule

A proposed release candidate may be tested without releasing V1.0.
The product owner must approve the in/out scope and each historical
divergence, and reviewers must record the final SHA, green CI,
platform-specific FIRST-ED and MicroStar worksheets, tab-width checks,
safe file roundtrip and packaging/release-note evidence.

On failure, record a reproducible issue and fix only the relevant
architectural boundary with a deterministic regression test. Then
repeat affected acceptance rows on the new candidate SHA.

**No V1.0 tag or published NuGet package without explicit approval.**
See [release gap analysis](V1-RELEASE-GAP-ANALYSIS.md),
[historical decision register](V1-HISTORICAL-PARITY-DECISION-REGISTER.md)
and [acceptance worksheet](V1-ACCEPTANCE-WORKSHEET.md).
