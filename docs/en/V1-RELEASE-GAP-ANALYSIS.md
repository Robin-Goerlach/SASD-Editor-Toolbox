# V1.0 release-gap analysis — evidence and prioritized closure plan

**Review date:** 2026-10-29  
**Inspected source:** `11e07c709d3e066c42fb9cb354d69938514acaf6` (`main`).  
**Release status:** **NOT RELEASED — sign-off blocked**.

This analysis distinguishes implemented functionality, repeatable automated evidence,
interactive evidence, and product-owner decisions. A historical inventory entry,
a source file, a test name, or a successful build must **never** be promoted
automatically into historical behavioral parity or final release approval.

## Executive conclusion

The reusable .NET Core and its integration/package-distribution path are
mature enough for a *proposed, non-published release candidate*. The principal
remaining work is **acceptance evidence and scope approval**, not a large new
editor subsystem. MicroStar and FIRST-ED are terminal samples, not dependencies
of the UI-independent Core. A full claim of 1985 Borland compatibility is
not justified by the available evidence.

At review time, the `repository-contract` and `dotnet` GitHub Actions
workflows both reported **completed/success** for the exact inspected SHA.
These results do **not** preapprove a later release SHA. The `dotnet`
workflow builds/tests Windows and Ubuntu, executes the independent
project-reference consumer, packs the provisional `1.0.0-rc.1` Core
NuGet archive, checks its manifest/README, and restores/runs a separate
package-reference consumer using the locally built archive.

## Evidence matrix (RC-01 through RC-10)

| Gate | What is demonstrably available | Remaining gap / classification | Priority |
| --- | --- | --- | --- |
| RC-01 source | A pin-able reviewed main SHA and CI history | Record clean checkout, exact proposed RC SHA and local SDK/OS; **release-process** | P1 |
| RC-02 CI | Both workflows green for inspected SHA | Rerun/verify both for the **final candidate SHA**; **release-process** | P0 at freeze |
| RC-03 public API consumer | Core-only source consumer **executes** on Windows and Ubuntu CI | No current implementation gap shown; keep final-SHA evidence; **automated evidence present** | P1 |
| RC-04 FIRST-ED | Windows and Debian-under-WSL launches, multiple/linked windows, resize and status-bar observations | Complete 11 individual checks on Windows and **native** Debian, including dialogs and file actions; **manual blocker** | P0 |
| RC-05 MicroStar | F10 sample, menus/file operations/print queue; headless focused tests | Record 10 individual terminal checks, cancellation, file browser/popup and print-to-file on both target terminals; **manual blocker** | P0 |
| RC-06 tabs (HP-03) | Per-view tab-width policy and headless prompt tests | Explicit numbered/cancel/empty-Enter/focus evidence in terminal; modern per-window behavior requires acceptance; **manual/decision** | P0 |
| RC-07 historic scope | 123 indexed routines, 20 supplemental names, ten HP decisions; 103 named conformance links | Product-owner review of HP-01..HP-10, unlinked evidence and permitted divergences; **scope/approval blocker** | P0 |
| RC-08 legacy I/O | Staged saves, 8-bit codec, explicit cancellation/overwrite and concurrency tests | Disposable terminal roundtrip/encoding-rejection/cancel/overwrite checks on each target; **manual blocker** | P0 |
| RC-09 distribution | Provisional RC NuGet pack + package manifest + independent package consumer in CI; artifact upload | Release notes, exact final version/artifact identity, user-approved publication destination; **release-process/approval** | P1 |
| RC-10 sign-off | Worksheet and recorded partial Windows/WSL observations | Named reviewer, dated approval, exact accepted SHA and gate outcomes; **release approval** | P0 at decision |

**Definitions:** P0 = must close or explicitly waive with scope justification
before V1.0; P1 = required at candidate packaging/freeze; P2 = reasonable
deferred improvement, not a release blocker absent a demonstrated defect.
A green automated test cannot waive human terminal verification.

## Historical release scope — explicit owner decisions

- **HP-01** original `Optstr`: modern find options exist; original grammar
  is unverified. Accept documented divergence or provide authoritative behavior.
- **HP-02** `EDITERR.MSG`: JSON/typed-error modernization selected and tested;
  original numeric resource format is not claimed. Confirm release wording.
- **HP-03** global `Tabsize`: view-local width selected; obtain real-terminal
  evidence and accept non-equivalence to the original global setting.
- **HP-04** DOS video-memory/assembler: approve rendered redraw as a modern
  functional replacement, subject to terminal behavior checks.
- **HP-05** MicroStar popup window/restore: test overlay cancellation, resize
  and return-to-editor restoration in interactive terminals.
- **HP-06** `PrintNext`: accept explicitly labeled print-to-file/pagination
  replacement or change release scope; DOS physical printing is not supported.
- **HP-07** Lightning spell integration: explicitly exclude optional separate
  integration from V1 or require a later independently specified capability.
- **HP-08** FIRST-ED prompt/recall/typeahead: approve documented host mapping
  only after cancel, prompt width and key-input terminal tests.
- **HP-09** 8-bit historical persistence: terminal roundtrip and rejected
  encoding/overwrite/cancellation evidence is still necessary.
- **HP-10** broader operation list: classification + case links are evidence
  *indexing*, not a claim of complete original behavioral parity. Resolve
  release-significant outstanding behaviors, or narrow release wording.

The product owner must decide whether V1.0 means **modern SASD functional
foundation with documented divergences** (recommended) or **full historical
behavioral parity** (not demonstrated). Never silently equate the two.
No Borland source/API identity or DOS printer/graphics compatibility is claimed.

## Next work in dependency order

1. **Freeze the candidate scope:** explicitly decide HP-01, HP-04–HP-08,
   HP-10 and carry forward recorded HP-02/HP-03 modernization decisions.
   Do not invent undocumented original semantics.
2. **Complete one reproducible terminal worksheet per environment:**
   run FIRST-ED 1–11, MicroStar 1–10, HP-03 and safe disposable legacy
   roundtrip in Windows Terminal and **native** Debian. WSL evidence is
   useful but a separate category. Record PASS/FAIL/BLOCKED plus reproduction
   steps, not screenshot-only blanket approval.
3. **Implement only reproduced P0 defects:** fix minimal architectural
   boundary, add deterministic regression and a manual retest note. Avoid
   unbounded small-test accumulation when no remaining release requirement
   motivates it.
4. **Prepare release candidate:** freeze exact SHA, verify clean checkouts
   and CI for it, validate downloaded `.nupkg`, write concise release notes,
   identify version/release host and reviewer.
5. **Explicit approval:** product owner signs or blocks the candidate.
   Only after authorization publish/tag the agreed V1.0. Do not touch
   the separate SASD-Editor-Toolkit until Toolbox V1 acceptance.

## Reproduction and recording contract

For each manual result write: date, reviewer, environment (Windows/native
Debian/WSL), terminal/version, keyboard layout, `dotnet --version`,
`git rev-parse HEAD`, test number, expected/observed behavior,
PASS/FAIL/BLOCKED, disposable file paths, and any linked fix SHA.
No original customer data or sensitive file content is needed.

Source-of-truth detail:
- [Release candidate gate](V1-RELEASE-CANDIDATE-GATE.md)
- [Historical parity decisions](V1-HISTORICAL-PARITY-DECISION-REGISTER.md)
- [Real-terminal procedure](REAL-TERMINAL-V1-SMOKE-RECORD.md)
- [Compatibility matrix](BORLAND-V1-COMPATIBILITY.md)

**This is a gap assessment, not an acceptance certificate or a request to
publish an artifact.**

## Confirmed overall product direction — detailed approvals outstanding

The owner confirmed V1 as a modern functional editor foundation with
documented historical differences. The [product scope statement](V1-PRODUCT-SCOPE-PROPOSAL.md)
records that direction and proposes HP-01..HP-10 dispositions. Individual
historical behavior and release-significant deviations still need evidence
and explicit review; out-of-scope integrations never waive in-scope file
safety or terminal acceptance.

## Owner confirmation of overall V1 direction — 2026-10-29

The product owner confirmed the intended modern functional foundation with
documented historical differences. This removes the open binary choice
between modernized V1 and literal DOS/Pascal parity. It does **not** sign off
individual historical divergences, missing manual evidence, the frozen RC
SHA, or publication. See [confirmed product direction](V1-PRODUCT-SCOPE-PROPOSAL.md).

## V1 closure mode — feature freeze (2026-10-30)

**Product-owner direction:** Finish the existing modern V1 scope rather than
continue an open-ended sequence of minor improvements. The project is now in
**release closure mode**, not in a new-feature milestone. This is a planning
freeze; it does not assert that an immutable RC SHA has been selected.

1. **Manual terminal evidence (P0):** execute the existing
   [acceptance worksheet](V1-ACCEPTANCE-WORKSHEET.md) on Windows Terminal and
   native Debian. Group FIRST-ED (11), MicroStar (10), tabs (5), and legacy
   I/O (5) by session. Previously recorded WSL observations remain partial.
2. **Historical review (P0):** reconcile HP-01..HP-10 with the agreed modern
   policy, recording any remaining functional gap rather than requiring DOS
   ABI/physical printer parity.
3. **Defect-only fixes (conditional):** reproduce a failing acceptance case,
   correct the smallest responsible component, add regression coverage and
   repeat the affected manual observations on the new commit.
4. **Candidate freeze (P0/P1):** choose an exact SHA, check both workflows on
   that SHA, retrieve and verify the packaged NuGet artifact, and complete
   the release notes/evidence matrix.
5. **Owner approval and publication (P0):** publish/tag V1.0 only after an
   explicit approval tied to the accepted SHA and remaining documented
   limitations.

**Expected remaining execution:** four principal phases when tests pass
(manual acceptance, historical review, RC verification, owner release), or
five to six work packages if defect fixes or separate platform sessions are
required. These are dependencies and evidence gates, not a calendar forecast.

**Stop rule:** Do not add a new feature, cosmetic refactor, speculative
hardening test or package-metadata extension unless a reproducible V1 blocker
requires it. Do not mark any manual result PASS without an actual reviewer
observation. Core-only V1 remains distinct from the future Editor-Toolkit
migration, and UI Toolkit remains a separate project.
