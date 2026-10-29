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
names: 7 implemented, 6 modern replacements, 6 host replacements,
1 handbook-only example. The three previously `sample-missing` items are now
included in the modern/host replacement counts; they still lack historical
parity sign-off.
Its scope is explicitly non-exhaustive. Original Pascal source/API parity
and DOS hardware compatibility are NOT claimed.

## Decisions required before calling V1 functionally equivalent

| Decision ID | Historical feature | Current evidence | Release decision still required |
|---|---|---|---|
| HP-01 | Original `Optstr` find/replace options | Modern C/W/R terminal grammar and core search options documented in `HANDBOOK-SUPPLEMENT-V1.md` | Approve documented UI divergence or implement verified original semantics; do not invent original grammar |
| HP-02 | `EDITERR.MSG`, `EditMessage`, `EditErrormsg` | Typed `EditorErrorCatalog`; validated optional JSON resource file; safe FIRST-ED host presentation; headless hook integration tests | **Modernization decision recorded:** use stable resource keys and optional JSON instead of the unverified Borland numeric-message format. Original `EDITERR.MSG` file interchange remains unsupported; interactive release acceptance is still OPEN. |
| HP-03 | Global `Tabsize` / `EditDefineTab` | Per-window `TabSize`; independent and linked-view prompt/dispatcher regression tests | **Modernization selected:** preserve each window's tab width rather than recreating global Pascal `Tabsize`; original global-setting parity is not claimed. Manual terminal sign-off remains OPEN. |
| HP-04 | DOS assembler/video-memory routines | `EditUpdrowasm`, `MoveFromScreen`, `MoveToScreen` marked non-applicable; model-based repaint/overlay stack exists | Accept observable redraw/restore equivalence rather than literal BIOS/video-memory access, with terminal scenarios |
| HP-05 | MicroStar `MakeWindow` and `RestoreWindow` | Current host has `ConsoleMicroStarOverlayStack`, `ConsoleMicroStarPopupLayout`, renderer and headless overlay tests | Inventory classification reconciled; compare historical behavior and record actual terminal results before signing off |
| HP-06 | MicroStar `PrintNext` | Modern print-to-file queue/pagination is documented; supplemental inventory now records a modern replacement | Review whether the documented queued print-to-file replacement meets the V1 functional goal; physical DOS printing is not claimed |
| HP-07 | MicroStar Lightning spell-check integration | `HANDBOOK-SUPPLEMENT-V1.md` explicitly says not implemented, historically separate integration | Decide optional/out-of-scope versus required MicroStar parity; never quietly claim full MicroStar equivalence |
| HP-08 | FIRST-ED interactive prompts and command-row editing | `HOST-COMMAND-LINE-V1-AUDIT.md`; `EditAppchar`, `EditAppcmdnam`, `EditAskfor`, `EditZapcmdnam` host-mapped | Review original recall/typeahead, display width and Ctrl-U cancellation differences with real-terminal observations |
| HP-09 | Legacy file encoding and safe Save/Write operations | `FirstEdLegacyFileCodec`, staged file writer and focused regression tests | Confirm 8-bit roundtrip and cancellation/overwrite under terminal interaction; distinguish modern Unicode editing from legacy output |
| HP-10 | Full 123-entry behavior and 20 supplemental names | `HANDBOOK-PROCEDURE-INDEX-AUDIT.md` and `HANDBOOK-UNLINKED-INDEX-V1-REVIEW.md` | Record exact evidence and reviewer result per applicable operation; portable links alone are insufficient |

See [V1 release-candidate gate](V1-RELEASE-CANDIDATE-GATE.md) for the
centralized entry/exit checklist, evidence fields and explicit unresolved
manual verification requirements.

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

1. **HP-05/HP-06 historical acceptance:** the stale supplemental flags are
   reconciled to existing host replacements; run and record focused real-terminal
   popup and printing scenarios before approving functional equivalence.
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

## HP-02 — documented modernization decision (2026-10-24)

**Implementation policy: modern substitute selected; historical compatibility
not certified.** FIRST-ED uses the portable typed `EditorErrorId`, stable
`Code` and `ResourceKey`, with an optional UTF-8 JSON file selected by
`SASD_FIRSTED_MESSAGES_JSON`. The sample contains `messages.de.json` with
all eight current catalog keys. Host-only loading, validation and formatting
are isolated from the editor core. Invalid resources fail back to the built-in
catalog; error codes survive translation unchanged.

Evidence: `ConsoleFirstEdMessageResourcesTests`, including the end-to-end
file → loader → `ConsoleFirstEdHooks.OnErrorAsync` → displayed message path;
`ConsoleFirstEdErrorPresentationTests`; `docs/en/ERROR-MODEL.md`.

**Explicitly not claimed:** numeric `EDITERR.MSG` message IDs, original file
syntax, byte-for-byte wording, original DOS runtime loading semantics, or
all historical user-interaction details. The final interactive Windows/Linux
review and overall V1 sign-off remain OPEN. This is a product design decision,
not a passing historical parity result.

## HP-03 — per-window tab-width modernization (2026-10-26)

**Implementation decision: preserve the modern per-window setting.** The
historical `EditDefineTab` / global `Tabsize` behavior differs from SASD's
`EditorWindow.Options.TabSize`. Two linked windows can show the same document
while retaining independent tab intervals. The FIRST-ED host's empty Enter
special case derives the new width from the initiating window's zero-based
cursor column; cancelling does not apply a command, and a zero-width default
at column zero is rejected. Explicit numeric input uses the shared semantic
command path, not a new host-specific mutation.

**Automated evidence:** `ConsoleFirstEdTabWidthPromptTests` exercises linked
and independent windows, the empty-input convention, explicit numeric widths,
cancellation, invalid input, unchanged text/Dirty state, and Undo isolation.

**Remaining evidence:** a real-terminal Windows/Debian run must verify visible
tab-stop behavior and prompt cancellation across window switches. The result
must be recorded before HP-03 receives historical V1 acceptance. Choosing a
modern per-view policy is not proof of identical 1985 global-state behavior.

## Product direction confirmation — 2026-10-29

The product owner explicitly agreed to **modern functional reconstruction
with documented historical deviations** as the intended V1.0 identity.
This resolves the *high-level choice of product direction*, not individual
HP-01..HP-10 release verdicts. Previously recorded modernization policies
for HP-02 and HP-03 remain in place. The other historical dispositions
still require evidence/explicit scope review; manual terminal acceptance
and final release approval remain OPEN. Details:
[V1 product scope](V1-PRODUCT-SCOPE-PROPOSAL.md).
