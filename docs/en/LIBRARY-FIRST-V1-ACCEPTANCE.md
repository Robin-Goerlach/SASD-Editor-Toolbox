# Library-first V1.0 acceptance — public Core is the product

[Deutsch](../de/BIBLIOTHEK-ZUERST-V1-ABNAHME.md) |
[Roadmap](ROADMAP.md) |
[Public API and demo evidence](DEMO-EDITOR-AND-PUBLIC-API-ACCEPTANCE.md) |
[Release candidate gates](V1-RELEASE-CANDIDATE-GATE.md)

**Policy confirmed by owner: 2026-11-16. Status: acceptance protocol,
NOT a passed release certificate.** The historical Turbo Editor Toolbox
was a developer library of source modules and building blocks, not a
finished end-user editor. SASD Editor Toolbox V1 retains that purpose in
a modern, independently implemented .NET 10 Core, with future peer
language implementations deliberately out of V1 scope.

## Product and evidence hierarchy

1. **Primary deliverable:** a usable, documented, UI-independent
   `Sasd.Editor.Core` library, public API and actual consumable package.
2. **Correctness:** deterministic tests for editing, shared documents with
   independent views, search, blocks, Undo, storage, error/cancellation
   behavior, and semantic/structural invariants.
3. **Reference hosts:** FIRST-ED and MicroStar are real applications proving
   that the library can support complex hosts. Their terminal-specific
   menus, key bindings, status lines, popups and printing belong to samples,
   NOT to the general-purpose API. They remain valuable, and previously
   accepted in-scope terminal checks are not erased.
4. **Historical source:** the 1985 handbook informs behavior. No
   original Borland Pascal API/ABI, source translation, DOS internals,
   physical printer or whole-program parity is claimed.

**Stop rule:** no new editor-feature implementation merely to improve
a reference UI. Fix only a reproduced, release-significant contract defect
or explicitly authorized scope change. A passing terminal editor does not
prove that an external developer can integrate the library; nor may a
passing NuGet consumer silently waive agreed terminal tests.

## Independent public-library review worksheet

Complete separately for the **frozen candidate SHA**. An already green
older commit, code inventory or historical mapping does not prove the
new candidate. For each row record **PASS / FAIL / BLOCKED / APPROVED
LIMITATION**, the SHA, exact executable evidence, reviewer and date.

| ID | Core developer acceptance question | Expected evidence | Status |
| --- | --- | --- | --- |
| LIB-01 | Does Core build and operate without Console, FIRST-ED, MicroStar, WinForms, WPF or other concrete UI host dependencies? | Dependency/project inspection, build and tests | OPEN |
| LIB-02 | Can an outside assembly create a document/session, edit, navigate, search, select blocks and Undo with only public APIs? | Independently compiled source/project consumer and public-API tests; identify uncovered API paths | OPEN |
| LIB-03 | Are document, linked-view and cursor/viewport ownership, lifetime, mutation and disposal semantics intelligible and safe? | API review, tests, documented ownership/edge cases | OPEN |
| LIB-04 | Are UTF-8 storage, optional legacy 8-bit codec, error propagation, invalid input, cancellation and existing-file safety clearly separated? | Focused regression tests, package consumer and disposable I/O | OPEN |
| LIB-05 | Are commands, input mapping, rendering and hooks extensible without requiring host source code or duplicating the editing engine? | Host-neutral boundary review; independent integration example | OPEN |
| LIB-06 | Can a clean external project restore and execute the **actual locally packed NuGet artifact**, not only a ProjectReference? | Windows/Ubuntu package-reference CI consumers for **candidate SHA** | OPEN |
| LIB-07 | Are documented public entry points, API examples, errors, nullability, compatibility promises and exclusions suitable for another developer? | README, XML docs, samples/dotnet, API review, release notes | OPEN |
| LIB-08 | Do spec/conformance, .NET tests, package metadata, license and provenance agree with the selected distributable? | Manifest+tests, package contents, .sha256, build evidence, rights review | OPEN |

The present CI already executes independent project and NuGet package
consumers and checks package contents and SHA-256 on Windows/Ubuntu.
**That is strong existing automated evidence**, but the individual
LIB-01..08 reviewer verdicts and final-release-SHA association have not
been entered here. Do not backfill PASS from these descriptions.

## Reusable contract boundaries to review

- Core owns document/buffer editing and model state; UI owns native
  events, command prompts, keyboard layouts, rendering and file dialogs.
- A direct programmatic consumer must not need to simulate Ctrl-K/Ctrl-Q
  or rely on FIRST-ED internals to execute normal editing operations.
- Linked views share document contents but retain independent cursor,
  viewport and view-local options; cross-document undo and structural
  topology rules must remain deterministic.
- Storage is pluggable: modern Unicode persistence and opt-in legacy
  8-bit import/export must never be confused. Failed saves must not
  silently destroy existing data.
- Public API reviews should identify incidental internal exposure,
  misleading names, untested error paths and compatibility-breaking
  changes. Do **not** demand an entirely new abstraction layer without
  a concrete external consumer problem.
- `spec/` and `conformance/` define portable behavior; C++ and Java
  are planned peer lanes, not runtime or release dependencies of .NET.

## Reference hosts: secondary, not disposable

The separate [terminal smoke record](REAL-TERMINAL-V1-SMOKE-RECORD.md)
contains owner-reported Windows/WSL FE-01..11, MS-01..10,
TAB-01..05 and IO-01..05 checks, including later Windows existing-file
Unicode rejection. Preserve those observations and their provenance.
WSL is **not** native Debian; the exact interactive test SHA is not
always known. Previously required terminal gates must be completed or
explicitly scoped/waived by the owner, not silently marked PASS.
The historical HP-01..HP-10 register remains a *scope-and-evidence*
review, not a reason to implement a full editor or claim 1985 parity.

## Candidate record (fill only when observed)

| Field | Value |
| --- | --- |
| Exact candidate source SHA | NOT FROZEN |
| .NET SDK and OS/runner | NOT RECORDED FOR FINAL CANDIDATE |
| Independent ProjectReference consumer | PENDING EXACT CANDIDATE EVIDENCE |
| Independent NuGet PackageReference consumer | PENDING EXACT CANDIDATE EVIDENCE |
| NuGet package filename / SHA-256 / provenance | PENDING FINAL DISTRIBUTION PACKAGE |
| Completed LIB-01..08 reviewer verdicts | OPEN |
| In-scope reference-host exceptions/waivers | OPEN |
| Owner/reviewer, date and release authorization | NOT GRANTED |

The development repository currently carries **MIT**. The intended
organization release is **AGPL-3.0-only**, subject to rights/provenance
review and actual consistent release-package metadata. Older MIT packages
must never be labeled AGPL retroactively. Commercial licensing is
deferred. No tagging, uploading, repository transfer or publication
without explicit owner authorization.
