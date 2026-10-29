# SASD Editor Toolbox V1.0 — release notes (draft)

**Status: DRAFT / NOT RELEASED.** These notes describe the proposed V1
product scope, not a completed acceptance or an available V1.0 download.
Version and publication details are intentionally unset pending final
candidate review and explicit product-owner authorization.

## Overview

SASD Editor Toolbox is a modern, independently developed, reusable editor
foundation inspired by the observable behavior of historical terminal editors,
including the 1985 Borland Turbo Editor Toolbox. It is **not** a translation of
Borland Pascal source, a binary-compatible replacement, or an endorsement by
Borland or its successors.

The initial implementation is written in .NET 10/C#. The central
`Sasd.Editor.Core` library has no dependency on terminal or desktop
UI frameworks. The FIRST-ED and MicroStar-style terminal programs are
examples of host integration, not an embedded UI dependency of Core.

## Proposed V1 capabilities

- Document and window/view separation, including multiple views sharing
  one document and independent cursor/viewport state.
- Editing of logical lines, navigation, selections/blocks, markers,
  search/replace, command dispatch, and Undo.
- Compatibility behavior isolated from modern generic editor operations.
- Host-neutral keyboard/command and layout responsibilities, with
  interactive FIRST-ED and MicroStar terminal sample workflows.
- Modern text persistence and an explicitly separate legacy 8-bit
  FIRST-ED-compatible file codec with safety-oriented save and cancellation.
- A provisional NuGet Core package and independent public-API sample
  consumers to validate integration on Windows and Linux.

These are scope descriptions, **not** blanket claims that every historical
procedure or terminal scenario has completed manual acceptance.

## Explicit modernization boundaries

- Global historical `Tabsize` is replaced by per-view tab width.
- Typed errors and optional JSON messages replace unverified numeric
  `EDITERR.MSG` interchange.
- Model-based terminal rendering replaces DOS video-memory/assembler access.
- MicroStar printing uses a portable print-to-file queue/pagination,
  not a physical DOS printer.
- Original `Optstr` grammar, optional Lightning spell integration,
  historical Pascal APIs/ABI, and undocumented DOS environment behavior
  are not provided as compatibility promises.
- Desktop GUI, Java/C++ parity, and integration into the separate
  SASD Editor Toolkit are later work, not V1 deliverables.

## Verification currently available

At documentation preparation, both GitHub Actions workflows
`repository-contract` and `dotnet` succeeded for
`781be23047f7470e0545fdd4db2a5081403f1403`. Windows/Ubuntu CI exercises compilation,
automated tests, an independent Core source consumer, local NuGet packaging
and an independent NuGet package consumer.

Windows Terminal and Debian-under-WSL have partial observed FIRST-ED
interactive results, including linked windows and resize. Complete
Windows/native-Debian FIRST-ED, MicroStar, tab-width and disposable
file-operation checklists remain open. Passing CI is not evidence that
these manual requirements are already signed off.

## Before V1.0 publication — mandatory completion

1. Resolve the HP-01..HP-10 product-scope decisions and record terminal
   results by platform in `V1-ACCEPTANCE-WORKSHEET.md`.
2. Freeze and record a candidate source SHA, reproduce the two passing
   CI workflows for **that exact SHA**, and inspect its produced package.
3. Establish the final version and artifact checksum/identity, license
   notice, publication destination and release date.
4. Obtain a named, dated product-owner release decision. Do not publish
   to nuget.org, create a V1.0 tag, or announce V1.0 before approval.

## Documentation

- [Product scope and modernizations](V1-PRODUCT-SCOPE-PROPOSAL.md)
- [Prioritized release gaps](V1-RELEASE-GAP-ANALYSIS.md)
- [Executable acceptance worksheet](V1-ACCEPTANCE-WORKSHEET.md)
- [Historical differences and evidence](V1-HISTORICAL-PARITY-DECISION-REGISTER.md)
- [Release-candidate gate](V1-RELEASE-CANDIDATE-GATE.md)

**This file is a draft for review, not a release announcement.**
