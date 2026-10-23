# V1 historical feature parity — release decision register

**Status: OPEN — no historical parity sign-off.** Baseline: `ab638b54879e2b5d04d531fe65c9a6149080d966`.

## Method and authoritative scopes

The handbook's printed procedure/function index (pp. 243–244) has **123**
classified entries: **84** conceptually implemented, **8** modern replacements,
**27** host/sample responsibilities, **3** DOS-only/non-applicable, and **1**
documented divergence. Of those, **103** have named portable conformance
case links and **20** do not. Neither classification nor link existence
proves complete equivalence to the 1985 implementation.

A **separate supplemental inventory** records **20** additional historical
names: 7 implemented, 5 modern replacements, 4 host replacements,
1 handbook-only example and 3 entries still classified `sample-missing`.
Its scope is explicitly non-exhaustive. Original Pascal source/API parity
and DOS hardware compatibility are NOT claimed.

## Decisions required before calling V1 functionally equivalent

| Decision ID | Historical feature | Current evidence | Release decision still required |
|---|---|---|---|
| HP-01 | Original `Optstr` find/replace options | Modern C/W/R terminal grammar and core search options documented in `HANDBOOK-SUPPLEMENT-V1.md` | Approve documented UI divergence or implement verified original semantics; do not invent original grammar |
| HP-02 | `EDITERR.MSG`, `EditMessage`, `EditErrormsg` | Typed `EditorErrorCatalog` and FIRST-ED host presentation | Approve loss of historical numeric message-file customization, or implement an explicitly specified compatibility adapter |
| HP-03 | Global `Tabsize` / `EditDefineTab` | Per-window tab width, recorded in handbook index | Approve modern per-window policy with tests showing linked and independent views retain intended widths |
| HP-04 | DOS assembler/video-memory routines | `EditUpdrowasm`, `MoveFromScreen`, `MoveToScreen` marked non-applicable; model-based repaint/overlay stack exists | Accept observable redraw/restore equivalence rather than literal BIOS/video-memory access, with terminal scenarios |
| HP-05 | MicroStar `MakeWindow` and `RestoreWindow` | Current host has `ConsoleMicroStarOverlayStack`, `ConsoleMicroStarPopupLayout`, renderer and headless overlay tests | Reconcile outdated `sample-missing` supplemental mapping with current implementation, compare historical behavior, and record actual terminal results |
| HP-06 | MicroStar `PrintNext` | Modern print-to-file queue/pagination is documented; supplemental inventory still says `sample-missing` | Inspect source and tests, decide if generic queued printing is sufficient functional replacement; physical DOS printing is not claimed |
| HP-07 | MicroStar Lightning spell-check integration | `HANDBOOK-SUPPLEMENT-V1.md` explicitly says not implemented, historically separate integration | Decide optional/out-of-scope versus required MicroStar parity; never quietly claim full MicroStar equivalence |
| HP-08 | FIRST-ED interactive prompts and command-row editing | `HOST-COMMAND-LINE-V1-AUDIT.md`; `EditAppchar`, `EditAppcmdnam`, `EditAskfor`, `EditZapcmdnam` host-mapped | Review original recall/typeahead, display width and Ctrl-U cancellation differences with real-terminal observations |
| HP-09 | Legacy file encoding and safe Save/Write operations | `FirstEdLegacyFileCodec`, staged file writer and focused regression tests | Confirm 8-bit roundtrip and cancellation/overwrite under terminal interaction; distinguish modern Unicode editing from legacy output |
| HP-10 | Full 123-entry behavior and 20 supplemental names | `HANDBOOK-PROCEDURE-INDEX-AUDIT.md` and `HANDBOOK-UNLINKED-INDEX-V1-REVIEW.md` | Record exact evidence and reviewer result per applicable operation; portable links alone are insufficient |

## Release evidence gates

For each decision, write **PASS**, **FAIL**, **BLOCKED** or
**APPROVED DIVERGENCE** with specific tests, manual environment, and
source/handbook references. Leave entries **OPEN** until that evidence exists.
A `sample-missing` flag must not be cleared merely because a similarly named
modern service exists. Conversely, a missing portable-test link does not by
itself imply a missing public function.

Before tagging V1.0:

1. Both CI workflows pass for the proposed release SHA.
2. The 123-entry index and supplemental inventory have no unresolved
   release-significant **functional** gaps; accepted modern differences are
   listed and justified.
3. Individual FIRST-ED and MicroStar real-terminal checks are recorded, not
   inferred from a successful Windows/Debian editor launch.
4. The MicroStar acceptance policy's evidence/review gates have an explicit
   outcome for each claimed feature.
5. The release notes state **functional modernization**, not source-level
   Borland Pascal API identity or unrestricted historical compatibility.

## Next implementation/review slices

1. **HP-05/HP-06 evidence reconciliation:** inspect and test existing popup
   restoration and print scheduler behavior before changing the supplemental
   inventory's stale `sample-missing` states.
2. **HP-03:** add focused regression evidence for per-window tab-width
   independence (including linked-document views) and capture the divergence
   decision.
3. **HP-08/HP-09:** complete the remaining named interactive terminal
   scenarios on Windows and Debian without repeating already confirmed launch.
4. **HP-01/HP-02/HP-07:** resolve the actual product-scope decisions rather than
   treating them as routine unit-test omissions.

This register is a review/work-planning artifact; it is **not** an
implementation certificate or an assertion that the historical source
can be reconstructed solely from handbook descriptions.
