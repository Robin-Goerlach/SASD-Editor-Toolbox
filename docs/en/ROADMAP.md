# Development roadmap — SASD Editor Toolbox

[Deutsch](../de/ROADMAP.md) | [Repository overview](../../README.md) | [V1 release-gap analysis](V1-RELEASE-GAP-ANALYSIS.md)

**Snapshot: 3 November 2026.** Reviewed default branch: `main` at `c9b60e7d1e42a3a3614b60e86dead007e646aac2` (2 November 2026). The `repository-contract` and `dotnet` GitHub Actions workflows both passed for that *specific* commit. **V1.0 has not been released or approved.**

This roadmap consolidates work already spread across the architecture, compatibility matrix, release-gap assessment, acceptance worksheet and release-candidate gate. It is **a direction and evidence-based work plan, not a publication schedule**. M0–M5 are retrospective groupings of delivered work, **not claims that historical GitHub milestones with those names existed**. Later phases are proposals and must not silently extend the frozen V1 scope.

## How to read the status

- `[x]` means the **specific item** is evidenced by an existing implementation, checked-in document/test, completed CI observation or explicitly qualified owner report. It is **not** a blanket declaration of historical equivalence.
- `[ ]` means the item is still to be implemented, reviewed, manually executed, recorded, approved or released, as stated.
- **IMPLEMENTED** = code exists; **AUTOMATED** = tests or CI exercise it; **REPORTED** = someone observed it in a real terminal, possibly on an earlier revision; **ACCEPTED** = the prescribed recorded gate was passed; **RELEASED** = explicitly approved and published.
- Manual Windows, Debian **under WSL**, and **native Debian/Linux** observations are distinct. A green CI result and a WSL terminal check do not replace a native terminal acceptance record. Evidence for a prior SHA cannot be automatically transferred to a newer commit.

## Milestone overview

| Phase | Objective | Current assessment | Release relevance |
| --- | --- | --- | --- |
| M0 | Repository, independent clean-room rules, architecture, shared contract | **Delivered** | V1 foundation |
| M1 | UI-neutral editor/document/window model | **Implemented and tested** | V1 |
| M2 | Editing, historical compatibility adapters, safe storage | **Implemented; historical evidence review remains** | V1 |
| M3 | Interactive FIRST-ED terminal reference host | **Implemented; reported smoke evidence; final acceptance open** | V1 |
| M4 | MicroStar-style terminal workflows | **Implemented in sample; manual acceptance incomplete** | V1 |
| M5 | Cross-language contract, .NET CI, consumers and candidate packaging | **Automated foundation delivered** | V1 |
| M6 | Complete interactive and historical V1 acceptance | **In progress / blocking** | V1 |
| M7 | Freeze and verify an immutable V1 release candidate | **Pending** | V1 |
| M8 | Owner approval and V1.0 publication | **Pending** | V1 |
| F0+ | Migration to SASD Editor Toolkit, desktop UI and language ports | **Future; not a V1 blocker** | Post-V1 |

## M0 — Project identity, architecture and repository contract

**Objective:** build a maintainable, independent editor foundation rather than copy Pascal source or DOS implementation details.

- [x] Establish the repository, MIT license and project working rules in [`AGENTS.md`](../../AGENTS.md).
- [x] Define an independently written, behavior-oriented modernization of the 1985 Turbo Editor Toolbox; no copied Borland source or proprietary manuals.
- [x] Separate the reusable editor core from terminal and future desktop hosts.
- [x] Establish distinct `src/`, `tests/` and `samples/` implementation lanes for .NET, C++ and Java.
- [x] Make [`spec/editor-v1.md`](../../spec/editor-v1.md) the language-neutral behavior contract and [`conformance/`](../../conformance/README.md) the portable executable-case location.
- [x] Establish English-first and German technical documentation, including the [architecture](ARCHITECTURE.md) and [multi-language rules](MULTI-LANGUAGE-DEVELOPMENT.md).
- [x] Document intended compatibility boundaries and divergences in [the V1 compatibility matrix](BORLAND-V1-COMPATIBILITY.md).

**Exit criterion:** architecture, repository contract and traceable shared behavior are documented. **Met for the established foundation.**

## M1 — Core document, text and view model

**Objective:** enable meaningful editing through a reusable UI-independent API.

- [x] Line-oriented buffer abstraction (`ITextBuffer`), linked-line storage and `EditorDocument`.
- [x] Separate document identity from `EditorWindow` and `EditorSession`; allow linked windows sharing one document but retaining view-local cursor/viewport state.
- [x] Window stacking, screen-row geometry and compatibility-specific Create/Delete Window policies.
- [x] Cursor and viewport navigation, virtual columns, Insert/Overtype, tabs, auto-indent, margins and wrapping.
- [x] Structured line flags, wrapped outgoing boundaries, user color annotations, markers and block endpoint state.
- [x] Central `EditorLineTopology` realignment of affected views, markers and blocks after structural edits.
- [x] Transactional Undo, including compound cross-document block mutations and navigation-state restoration.
- [x] Unit coverage for core invariants and independently consumable public Core interfaces.

**Exit criterion:** the .NET editor model executes without Console, WinForms, WPF or other host UI libraries. **Implemented; individual release-significant edge cases remain subject to M6.**

## M2 — Commands, historical behavior and persistence

**Objective:** isolate historic FIRST-ED semantics while preserving modern safety and extensibility.

- [x] Editing operations: insert/delete/join/new line, block copy/move/delete, case conversion, centering and paragraph reformat.
- [x] Explicit FIRST-ED navigation/primitive/block/reformat compatibility processors and semantic command dispatcher.
- [x] Search/replace and Find Again, typed prompt metadata, predictable command-boundary validation and typed errors.
- [x] FIRST-ED key/prefix mapping, bounded typeahead, Ctrl-U interruption and cooperative scheduling/hooks.
- [x] Modern UTF-8 persistence separate from the opt-in historical 8-bit file codec and `0x8D` wrapped-line boundary.
- [x] Staged/safe file writes, overwrite/cancel handling, revision/concurrency defenses and dedicated regression tests.
- [x] Inventory the **123 printed handbook-index routines** and the **20 separate supplemental names**. At the documented checkpoint, **103/123** indexed routines have named portable conformance-case links; **20/123** require other evidence review. Links or inventory classifications **do not** establish full historical parity.
- [ ] Complete the *release acceptance* of applicable historical behaviors and approved modern differences under M6; do not turn the inventory into a compatibility certificate.

**Evidence:** [compatibility matrix](BORLAND-V1-COMPATIBILITY.md), [procedure index audit](HANDBOOK-PROCEDURE-INDEX-AUDIT.md), [unlinked-entry worklist](HANDBOOK-UNLINKED-INDEX-V1-REVIEW.md), [historical decision register](V1-HISTORICAL-PARITY-DECISION-REGISTER.md).

## M3 — FIRST-ED interactive terminal host and developer demo

**Objective:** show the Core in a real editor without embedding terminal APIs in the Core.

- [x] Interactive `Sasd.Editor.FirstEd.Sample` with FIRST-ED/WordStar-style keys, prompts, status, viewport and multiple/linked windows.
- [x] Guided `--demo` launch and standalone public Core consumer sample.
- [x] Host rendering of visible control characters without emitting escape sequences; window resize/redraw corrections and headless regression coverage.
- [x] Windows Terminal FIRST-ED FE-01..FE-07 and FE-09..FE-11 reported PASS; FE-08 preloaded control-character display reported PASS with a specifically documented test limitation.
- [x] Debian **WSL** FE-01..FE-11 reported PASS after correcting FE-03/FE-07 test procedures, against recorded local `adc3d813e7620dd5c93d7d98a98fa66992332452`.
- [ ] Record the complete worksheet on the final candidate SHA and complete **native Debian** manual acceptance; previous reports remain distinct from the final release sign-off.

**Evidence:** [terminal smoke record](REAL-TERMINAL-V1-SMOKE-RECORD.md), [developer demo acceptance](DEMO-EDITOR-AND-PUBLIC-API-ACCEPTANCE.md), [worksheet](V1-ACCEPTANCE-WORKSHEET.md).

## M4 — MicroStar terminal sample and file/print workflows

**Objective:** cover the relevant F10/menu/sample experience without adding MicroStar UI dependencies to Core.

- [x] F10 menu model, navigation, popups/overlay stack, dialog focus and screen restoration infrastructure.
- [x] Sample-level Find/Replace/Goto, file browser, Save/Write/Read, open and replacement workflows.
- [x] Sample-level block-file Read/Write, block actions and file-safe cancellation/overwrite paths.
- [x] Portable print-to-file job, pagination, status and cancellation model; **not** physical DOS printing.
- [x] Automated tests for the MicroStar-related host components.
- [x] MS-02..MS-05 were reported PASS on Windows and WSL; MS-01 F10 opening was reported but its no-mutation assertion remained PARTIAL in the repository record.
- [ ] Consolidate an additional reported **MS-06 block-operations 5/5 PASS on Windows and Debian/WSL** into the formal worksheet with precise provenance; the inspected repository's smoke record still lists MS-06 as not reported.
- [ ] Execute and record MS-07..MS-10, finish MS-01's nonmutation checks, and perform final candidate/native Debian checks.

**Exit criterion:** all in-scope sample interactions are either recorded PASS on required platforms or explicitly dispositioned with approved evidence. **Not yet met.** See [terminal record](REAL-TERMINAL-V1-SMOKE-RECORD.md).

## M5 — Conformance, CI and prerelease packaging

**Objective:** make the public Core reproducibly buildable, testable and consumable.

- [x] Shared machine-readable `conformance/v1` manifest/cases and .NET conformance adapter.
- [x] `repository-contract` workflow validates repository/shared-contract structure.
- [x] `dotnet` workflow builds and tests .NET 10 on Windows and Ubuntu, including public API and FIRST-ED help smokes.
- [x] Independently execute a source/project-reference Core consumer.
- [x] Produce a **provisional, unpublished** `Sasd.Editor.Core.1.0.0-rc.1.nupkg` and execute an independent package-reference consumer using a local feed and isolated package cache.
- [x] Inspect package metadata/README/assembly; emit SHA-256 sidecar and candidate build-provenance record.
- [x] Both workflows completed successfully for inspected `main` `c9b60e7d1e42a3a3614b60e86dead007e646aac2`: [repository contract](https://github.com/Robin-Goerlach/SASD-Editor-Toolbox/actions/runs/41394415645) and [.NET](https://github.com/Robin-Goerlach/SASD-Editor-Toolbox/actions/runs/41394415519).
- [ ] Verify the **new frozen candidate** and its actual downloadable artifacts independently under M7. Historical green runs are not future release approval.

**Exit criterion:** reproducible automated project/package consumption on the two CI operating systems. **Foundation met; publication gate not met.**

## M6 — V1.0 interactive and historical acceptance (current priority)

**Objective:** close the release gate instead of indefinitely expanding the feature list. The feature freeze has applied since 30 October 2026: fix only a **reproduced V1 blocker**, with a regression test and retest.

### M6.1 — Hands-on FIRST-ED and MicroStar

- [x] Define the numbered **11 FE**, **10 MS**, **5 TAB** and **5 safe legacy-I/O** checks in [the acceptance worksheet](V1-ACCEPTANCE-WORKSHEET.md).
- [x] Record qualified FIRST-ED Windows/WSL reports; preserve exact limitations and retest history.
- [x] Record partial MicroStar Windows/WSL evidence (MS-01..MS-05); separately track the later MS-06 5/5 report pending consolidation.
- [ ] Finish any missing checks on Windows and record actual source SHA, SDK, terminal and reviewer.
- [ ] Repeat the prescribed checks on a **native Debian/Linux terminal**, not merely Debian in WSL.
- [ ] Record per-view TAB-01..TAB-05 (numeric value, linked views, focus, invalid/cancel and empty Enter).
- [ ] Record legacy IO-01..IO-05 using **only disposable files**, including 8-bit roundtrip, encoding error, cancellation, overwrite refusal and Write identity.
- [ ] Review FAIL/BLOCKED entries, resolve actual blockers and rerun affected checks on the fixed SHA.

### M6.2 — Documented historical differences

- [x] Adopt the product direction: **modern functional editor foundation with documented differences**, not exact Borland Pascal API/ABI or unrestricted 1985 parity (owner direction, 29 October 2026).
- [x] Document modern policies for HP-01..HP-10 in [product scope](V1-PRODUCT-SCOPE-PROPOSAL.md) and [decision register](V1-HISTORICAL-PARITY-DECISION-REGISTER.md).
- [ ] Record individual HP-01..HP-10 acceptance dispositions/evidence and any explicit approved waivers.
- [ ] Review release-significant entries among the 20 indexed routines without direct portable-case links; host-only/DOS-only mappings **do not** automatically require new portable cases.
- [ ] Check README, license notices, package metadata and release notes for accurate third-party attribution and *no* unsupported historical-parity claim.

**Deliberate V1 boundaries:** no historical `Optstr` parser claim, no binary `EDITERR.MSG` equivalence, no global tab-size parity, no DOS screen-memory/assembler code, no physical printer, no Lightning spell-check integration, no original Pascal ABI. Modern replacements must still pass their *in-scope* acceptance checks.

**M6 exit criterion:** M6.1 observations and M6.2 decisions are traceably recorded for the accepted source revision, with every release-significant failure fixed or explicitly handled. **OPEN.**

## M7 — Reproducible release candidate and final evidence

**Objective:** freeze the actual build being approved, not just trust past CI runs.

- [ ] Name and freeze an exact `main` SHA; verify a clean checkout and document OS/.NET SDK.
- [ ] Require both workflows to pass for **that exact SHA**.
- [ ] Download its CI-produced NuGet archive and compare SHA-256 with the sidecar; inspect embedded `lib/net10.0/Sasd.Editor.Core.dll`, manifest and README.
- [ ] Verify independent source and actual package consumers for that candidate.
- [ ] Finalize V1 scope, user documentation, license/attribution review and [release notes](V1-RELEASE-NOTES-DRAFT.md).
- [ ] Attach completed platform worksheets, HP dispositions, remaining known limitations and evidence links to the [release-candidate gate](V1-RELEASE-CANDIDATE-GATE.md).
- [ ] Record proposed version, publication destination and reviewer decision. No production upload in this phase without authorization.

**M7 exit criterion:** one immutable candidate is fully traceable, technically evaluated and ready for an explicit release decision. **OPEN.**

## M8 — Owner approval and V1.0 release

- [ ] Obtain explicit dated owner approval referring to the accepted SHA and documented deviations.
- [ ] Create the approved V1.0 tag/release **only after** all mandatory acceptance gates are satisfied or expressly dispositioned.
- [ ] Publish only to the specifically approved location; do not silently upload to NuGet.org or the separate [SASDGMBH/Editor-Toolbox](https://github.com/SASDGMBH/Editor-Toolbox) release repository.
- [ ] Publish definitive release notes, package identity/checksum, installation instructions and known limitations.
- [ ] Verify a clean external consumer of the published artifact and archive the acceptance evidence.

**M8 exit criterion:** a publicly identifiable approved version with reproducible provenance, accurate claims and successful post-publication consumption. **Not yet reached.**

## After V1 — future SASD Editor Toolkit evolution (not committed V1 scope)

The long-term product destination is **[SASD Editor Toolkit](https://github.com/Robin-Goerlach/SASD-Editor-Toolkit)**, *not* the independent [SASD UI Toolkit](https://github.com/Robin-Goerlach/SASD-UI-Toolkit). Do not alter the Editor Toolkit or discard its existing code solely because the Toolbox Core has matured. The ordering is deliberate: **first accept Toolbox V1.0, then plan and execute migration; continue new editor development in the Editor Toolkit**.

### F0 — Migration architecture and repository transition

- [ ] Inventory the Editor Toolkit's existing APIs, modules, tests, compatibility commitments and overlap with the accepted Toolbox Core.
- [ ] Document migration ADR(s): adopt/refactor existing Core, component-by-component migration, package boundary, namespace changes, history attribution and rollback strategy.
- [ ] Preserve proven tests, conformance contracts, editing invariants and legacy compatibility adapters; avoid an unreviewed rewrite or wholesale deletion of prior Toolkit work.
- [ ] Decide package/version naming, support/migration policy and public clean-room/branding language after an appropriate licensing review.
- [ ] Complete an independently testable vertical migration slice with unchanged observable behavior before retiring any old code path.

### F1 — Desktop editor hosts and SASD UI Toolkit evaluation

- [ ] Define a UI-agnostic editor-view/host adapter contract for rendering, selection/caret, input/IME, clipboard, file dialogs and focus.
- [ ] Prototype one small real desktop editor with the accepted Core, preferably testing a thin host boundary before introducing large new abstractions.
- [ ] Evaluate the **separate C++ SASD UI Toolkit** as one optional UI backend, not as the editor-domain model or mandatory runtime dependency for every language.
- [ ] Make ownership, lifetime, threading, encoding (UTF-8/UTF-16), exceptions, callback and disposal boundaries explicit before any C++/.NET or C++/Java interop. Compare native per-language hosts with a small stable C-compatible bridge; do not commit to a universal FFI prematurely.
- [ ] Keep UI performance/ergonomics measurable and application-facing; no GUI release claim before native hands-on tests.

### F2 — Independent peer language implementations

- [x] Create C++/Java scaffold lanes and document shared-contract rules; .NET is the current active reference.
- [ ] Implement a first C++ vertical slice (buffer, document/view, cursor and portable conformance runner), followed by compatible editing/storage/undo layers.
- [ ] Implement an idiomatic Java peer with the same contract and its own tests/build/CI; **do not** wrap .NET or require .NET garbage-collector semantics in C++.
- [ ] Consider Python or further languages when a concrete consumer exists, creating independent lanes and conformance adapters rather than promising immediate full parity.
- [ ] Let language-specific package versions differ while contract conformance remains explicit.

### F3 — Product growth after real consumer feedback

- [ ] Decide whether enhancements such as more editor host interfaces, extensibility, richer text features or additional backends belong in the Editor Toolkit; require use cases and explicit milestones.
- [ ] Defer a full IDE, syntax service/LSP integration, plugin marketplace, broad web/mobile UI and speculative buffer optimization until justified by a consumer.
- [ ] Benchmark before replacing linked-line storage with Rope/Piece Table; preserve compatibility and topology tests.
- [ ] Review license strategy explicitly before any future change: the **current repository's `LICENSE` is MIT**. A future AGPL or commercial policy is **not** silently in effect and cannot retroactively erase already granted rights.

## Next practical action and release discipline

**Next work item:** consolidate the reported MS-06 observations into the acceptance record (platforms, reviewer, exact SHA and five subchecks), then execute **MS-07 block-file Read/Write** on disposable data. Continue MS-08..MS-10, TAB and I/O checks, with a **separate native Debian run**. Do not add a new editor feature while these gates remain open.

Every milestone closes only on its **stated evidence**, not on a percentage estimate. For each implementation or defect fix, preserve architecture boundaries, meaningful comments, regression coverage and English/German documentation. If the accepted candidate SHA changes, reassess affected manual evidence and both workflows.

**Authoritative V1 release records:** [gap analysis](V1-RELEASE-GAP-ANALYSIS.md) · [acceptance worksheet](V1-ACCEPTANCE-WORKSHEET.md) · [historical decisions](V1-HISTORICAL-PARITY-DECISION-REGISTER.md) · [release-candidate gate](V1-RELEASE-CANDIDATE-GATE.md) · [release notes draft](V1-RELEASE-NOTES-DRAFT.md). This roadmap summarizes them; it does not override their recorded gate states.
