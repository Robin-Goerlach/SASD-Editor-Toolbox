# V1 release-candidate gate — SASD Editor Toolbox

**State: PREPARATION ONLY — NOT RELEASED.**
This record separates a repeatable .NET library milestone from a claim of
complete Borland Turbo Editor Toolbox 1.0 (1985) compatibility. A passing
CI workflow is necessary but not sufficient to publish a V1.0 tag.

## Snapshot and reproducible evidence

- Baseline `main`: `cde1b5b137ea9959cba3da979f47d9513211086e`.
- At inspection, GitHub Actions `repository-contract` and `dotnet`
  both reported completed/success for that exact baseline.
- The printed handbook index classifies 123 historical routines; an
  additional 20 supplemental names have separate classifications.
  **Classification is not a completed behavioral acceptance test.**
- FIRST-ED and MicroStar have recorded launches on Windows and Debian,
  but neither complete manual checklist has been signed off.

## Release candidate entry / exit conditions

| Gate | Evidence to collect | Status at baseline |
| --- | --- | --- |
| RC-01: reproducible source | Pin exact commit SHA; clean checkout; document SDK/OS | OPEN |
| RC-02: automated validation | Both CI workflows successful for the **proposed** release SHA | BASELINE PASS; release SHA pending |
| RC-03: public API integration | Execute independent Core-only consumer (edit/find/undo/disposable export) on Windows and Linux CI | PENDING NEW CI |
| RC-04: FIRST-ED terminal | Record all 11 checks from `REAL-TERMINAL-V1-SMOKE-RECORD.md` on Windows and native Linux | OPEN |
| RC-05: MicroStar terminal | Record all 10 F10 checks, including popup and print-to-file, on Windows and native Linux | OPEN |
| RC-06: HP-03 view-local tabs | Run the dedicated numeric/cancel/focus/empty-Enter protocol on both terminals | OPEN |
| RC-07: historical scope | Each HP-01..HP-10 has reviewer verdict + evidence; explicitly accept or block modern divergences | OPEN |
| RC-08: safe legacy file I/O | Confirm disposable overwrite/cancel, encoding errors and roundtrip; never use real data | OPEN |
| RC-09: package and documentation | CI pack test + license/metadata; later actual NuGet package-consumer and release notes | PARTIAL; release approval OPEN |
| RC-10: final sign-off | Named reviewer, evidence references and exact green release SHA | OPEN |

Do not turn `BASELINE PASS` into a release PASS when new commits have
occurred. Do not record a real-terminal PASS on the strength of headless tests.

## Scope decisions to make deliberately

1. **Core V1 product:** UI-independent document editing, windows/views,
   blocks, search/replace, command dispatch, undo, file boundaries and
   supported host integration. No graphical shell is required for this
   library release.
2. **Historical divergences:** HP-02 uses typed errors and JSON resources;
   HP-03 uses per-window `TabSize`; DOS video memory is not reproduced.
   These are visible modern differences and must be disclosed.
3. **Not proven historically equivalent:** original `Optstr` option grammar,
   original `EDITERR.MSG` file compatibility, physical DOS printing and
   MicroStar Lightning integration. Record an explicit scope decision rather
   than silently treating any of these as implemented.
4. **Future integration:** do not modify `SASD-Editor-Toolkit` before V1
   is accepted. A later integration or V2 plan does not justify changing
   this repository's release criteria retroactively.

## Reviewer worksheet (copy for each platform)

- Date/reviewer:
- OS, native/WSL, terminal/version, keyboard/layout:
- `dotnet --version`, `git rev-parse HEAD`:
- Build/test commands and result:
- FIRST-ED checks 1–11 (PASS / FAIL / BLOCKED + notes):
- MicroStar checks 1–10 (PASS / FAIL / BLOCKED + notes):
- HP-03 protocol results (numeric/cancel/empty/focus):
- Disposable file paths and observations (no sensitive data):
- Defects and linked fixes:
- Release recommendation (APPROVE / BLOCK / REQUEST CHANGES):
- Reviewer and sign-off date:

## Rules for declaring completion

A candidate may be named and distributed for targeted testing with the
**RC** label only after its artifact/build and available acceptance evidence
are explicit. The final V1.0 release requires all mandatory gates above to
have an outcome, the product scope to be agreed, and the historical register
to identify approved divergences rather than open questions. Never infer
manual verification or an approval from a passing GitHub Actions run.

## Automated RC evidence added after initial gate definition

The `dotnet` workflow now **runs** `Sasd.Editor.Consumer.Sample` after a
Release build on Windows and Ubuntu. That program uses only the public Core
project reference and checks insert/find/undo/disposable file export.
The workflow also produces and checks a local `1.0.0-rc.1` **test** NuGet
package, without publishing it. The version is a provisional CI artifact
label, not a V1.0 release announcement or completed packaging sign-off.

A later RC-09 gate must still verify installation/consumption from an
actual .nupkg in an independent project (rather than a source ProjectReference),
as well as versioning and release documentation. Record the final CI SHA and
results before turning the above provisional entries into PASS.

## Independent NuGet-consumer gate (pending new CI)

The `Sasd.Editor.PackageConsumer.Sample` project deliberately contains only
`PackageReference Include="Sasd.Editor.Core"` at `1.0.0-rc.1`. It is **not**
part of `Sasd.Editor.Toolbox.slnx`, because the CI first packs Core and only
then restores this separate application exclusively from the local package
feed. It replays the public insert/find/undo/disposable export scenario on
Windows and Ubuntu. A successful result confirms consumption of the built
NuGet artifact, not publication, manual terminal checks or historical parity.

## Retrieve CI-validated package for isolated evaluation

Once the exact commit's Windows and Ubuntu `dotnet` jobs succeed, open its
GitHub Actions `dotnet` workflow run and download either
`editor-core-rc-package-windows-latest` or
`editor-core-rc-package-ubuntu-latest` under **Artifacts**. Each ZIP
contains the same provisional NuGet package name
`Sasd.Editor.Core.1.0.0-rc.1.nupkg`. Record the source commit and job URL
when sharing test results; artifacts are retained for 14 days.

To test the downloaded package, extract its ZIP into a **dedicated temporary
folder**, then point a separate .NET application's NuGet package source at
that folder. Do not upload to nuget.org or treat the RC as a V1.0 release.
The automated source-project and package-reference consumer checks provide
CI evidence, but they cannot replace Windows/Debian interactive acceptance.

## Windows observation recorded 2026-10-28

The user confirmed that the resize/repaint defect no longer reproduces in
Windows Terminal and supplied a screenshot with three correctly arranged
editor windows. Linked-document behavior was also demonstrated earlier.
The same message reported success in the last portions of manual testing,
but did not provide a complete per-step HP-03 or MicroStar acceptance matrix.
See `REAL-TERMINAL-V1-SMOKE-RECORD.md` for the precise supported evidence.

This establishes a **partial Windows terminal smoke result** only. RC-04,
RC-05, RC-06 and final V1 sign-off remain open pending complete recorded
scenarios and native Debian verification.

## WSL Debian evidence recorded 2026-10-28

Windows Terminal hosting Debian WSL successfully ran FIRST-ED, displayed two
linked windows and preserved status bars after resizing. A reported local
Release build succeeded with 1,063 passing tests, using source `53bccd5`.
This is useful **WSL-specific** evidence, but does not establish independent
native Debian coverage or a complete manual MicroStar/file I/O sign-off.
The shared Windows checkout showed cross-platform Git index/permission
issues; future independent Linux acceptance should use a dedicated checkout.
See `REAL-TERMINAL-V1-SMOKE-RECORD.md` for evidence and limits.

## Consolidated gap review — 2026-10-29

See [V1 release-gap analysis](V1-RELEASE-GAP-ANALYSIS.md) for the
prioritized RC-01..RC-10 evidence matrix and dependency-ordered closure plan.
Earlier table cells such as `PENDING NEW CI` are **historical baseline
snapshots**, not current assertions: independent source and package consumers
now run under the Windows/Ubuntu `dotnet` workflow. Both workflows passed
for inspected main `11e07c709d3e066c42fb9cb354d69938514acaf6`, but release verification requires the
*eventual frozen candidate SHA*. Do not mark manual and owner-approval gates
complete merely because CI is green.
