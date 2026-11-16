# V1.0 release-candidate technical review — 2026-11-16

**Status: REVIEWED BASELINE; NOT RELEASED; NO OWNER SIGN-OFF.**

This document is a point-in-time technical audit of commit
`eff8876e475559604b750836e5934ca85daf355c`. It is **not** a
V1.0 tag or a declaration of historical source/API parity. Subsequent
commits require independent verification against their own SHA.

## Exact CI baseline

| Gate | Evidence | Result |
| --- | --- | --- |
| Repository contract | [workflow run 42348989013](https://github.com/Robin-Goerlach/SASD-Editor-Toolbox/actions/runs/42348989013) | completed / success |
| .NET Windows and Ubuntu | [workflow run 42348989065](https://github.com/Robin-Goerlach/SASD-Editor-Toolbox/actions/runs/42348989065) | completed / success; both platform jobs |
| Public project-reference and NuGet-package consumers | Executed as part of the preceding `dotnet` workflow | automated gate success; independent terminal proof not implied |

## Downloaded, independently inspected CI artifacts

Both ZIP archives were retrieved from GitHub workflow run 42348989065 and
unpacked for this review. Each contained a NuGet file, its .sha256 sidecar
and `rc-build-evidence.txt`. In both cases the actual nupkg SHA-256
matched the sidecar and the build-evidence file; the latter recorded
the above source SHA and workflow URL.

| Runner artifact | NuGet package SHA-256 | Findings |
| --- | --- | --- |
| `editor-core-rc-package-windows-latest` (artifact 13565200466) | `9d647a385aa5906f07cc9ef5cf17c8552ed6546699c7eaa03f80efb128f4b451` | checksum verified |
| `editor-core-rc-package-ubuntu-latest` (artifact 13563999403) | `b69f21ded9c5352f76e3c3b2067439d7e558e3960111c8d1dc229ff7dc5128d4` | checksum verified |

Different checksums are expected from independently built Windows and Ubuntu
packages; do not treat the ZIP artifact digest as the inner nupkg checksum.
Both archives contain `Sasd.Editor.Core.1.0.0-rc.1.nupkg`.
The **nupkg** manifest inspection confirms:
- package ID `Sasd.Editor.Core`, version `1.0.0-rc.1`;
- `lib/net10.0/Sasd.Editor.Core.dll` (nonempty), XML API documentation,
  `README.md`, and `Sasd.Editor.Core.nuspec`;
- author `SASD-GmbH`, repository URL and matching commit metadata;
- license expression **MIT**, on **both** built packages.

The independent package consumer's execution is represented by the CI
workflow result. This audit verified the downloaded archive contents and
hashes, but **did not run the package consumer locally**.

## Important publication decision: license

The current `Sasd.Editor.Core` NuGet metadata explicitly declares **MIT**.
This must be deliberately reviewed by the product owner **before any
publication**. MIT grants downstream proprietary redistribution subject to
its terms, and is not interchangeable with AGPL-3.0 or other copyleft
licensing. Do not silently change license text or metadata or publish a
package while intended licensing and all owned/third-party rights are
unconfirmed. An AGPL alternative requires a separate compatibility and
provenance review, including whether the repository `LICENSE`, project
metadata, release notes, and all distributed material agree.

## Interactive and historical scope evidence

The [real-terminal smoke record](REAL-TERMINAL-V1-SMOKE-RECORD.md)
contains owner-reported successful FIRST-ED FE-01..11,
MicroStar MS-01..10 (with scoped print cancellation), TAB-01..05,
and IO-01..05 for Windows and Debian/WSL. The separate
Unicode overwriting safety test was later reported on Windows,
with matching before/after SHA-256 and retained `ORIGINAL` content;
it has **not yet been written into that smoke record**.

Retain these limitations:
1. Later interactive Windows/WSL session SHAs were not consistently recorded;
   do not claim exact candidate-SHA manual revalidation from earlier runs.
2. Debian/WSL is **not native Debian**; a native Linux terminal session
   remains outstanding unless an explicit, justified scope change is approved.
3. The historical HP-01..HP-10 product direction favors modern functional
   reconstruction with documented deviations, not Pascal/DOS ABI parity.
   Individual HP evidence verdicts and final acceptance remain open.
4. Print-to-file is in scope; physical LPT1 printer support is not.
   The owner saw `Background print cancelled.` but did not directly observe
   cancellation while work was still running.
5. The complete user-facing release notes must reflect the actual accepted
   source version and the correct license.

## Release gate recommendation

**Do not publish/tag V1.0 yet.** Remaining meaningful tasks are:

1. Confirm the intended public license and corresponding distribution rights;
   resolve repository/package metadata if the approved license differs.
2. Complete per-HP-01..10 evidence review, classify any residual functional
   blockers or explicitly accepted modern divergences, and record the owner's
   decisions.
3. Close or explicitly waive native Debian and exact-SHA terminal recheck
   gaps, with dates/environment and reasons recorded.
4. After final changes, freeze a **new exact candidate SHA**; verify both CI
   workflows and the package/hash/provenance for **that SHA**, not this older
   snapshot.
5. Obtain named, dated product-owner V1.0 publication approval, including
   license, destination, version and release notes, **before** creating a
   tag or uploading a package.

No source code or existing historical acceptance outcomes were modified as
part of the technical inspection.

## Post-review release-repository and licensing decision — 2026-11-16

**Owner-selected release direction (not yet executed):** retain the current
MIT license in `Robin-Goerlach/SASD-Editor-Toolbox` as a development/debug
repository; prepare the official V1.0 source release in the separate SASD
organization repository under **AGPL-3.0-only**, with possible commercial
licensing considered only later. The owner has not approved a V1.0 tag or
publication. The contemplated `SASDGmbH/SASD-Editor-Toolbox` repository was
not accessible through the connected GitHub account at inspection time;
verify the exact owner/repository name and permissions before attempting a
release transfer. An earlier discussion mentioned a differently named
`SASDGMBH/Editor-Toolbox` repository; these identifiers must not be assumed
to designate the same remote repository.

**Licensing caveat:** Two repositories do not create distinct exclusive
rights in identical source code. Code legitimately offered under MIT remains
available under its already granted MIT terms, notwithstanding an additional
AGPL version. A separately distributed AGPL version must retain required
existing copyright/permission notices as applicable, and must not falsely
advertise exclusive AGPL obligations over identical MIT-available material.
Before publication, establish the rights holder(s), third-party contributions,
and legal authority to license the intended package under AGPL. In particular,
the current `LICENSE` and `PackageLicenseExpression` **remain MIT** in the
development repository, and the existing `1.0.0-rc.1` CI NuGet artifacts
**must not be relabeled as AGPL**. Prepare consistent AGPL source/package
metadata in a reviewed release working tree only after that rights review.

### Remaining work ordered by release risk

1. **Historical scope closure (HP-01..HP-10):** use the existing selected
   modern policies and owner-reported FE/MS/TAB/IO observations to produce
   explicit per-feature verdicts; leave unknown source-parity and incomplete
   procedure evidence as limitations, not inferred PASS. This is not a
   request to implement 1985 DOS or Lightning behavior.
2. **Terminal acceptance provenance:** decide explicitly whether native
   Debian and exact-candidate-SHA interactive retest remain mandatory; if
   required, execute them. Windows/WSL observations are not native Debian.
3. **Release provenance/licensing:** verify target repository identity and
   rights ownership, reconcile legacy MIT grants, and prepare an AGPL-only
   distribution snapshot with consistent license and NuGet metadata.
4. **Final artifact gate:** freeze the release snapshot SHA, run Windows and
   Ubuntu CI against it, inspect its distributable package, record package
   SHA-256 and version. Do not use older MIT-labeled RC packages as AGPL
   release evidence.
5. **Owner sign-off:** approve explicit source SHA, license, destination,
   documented limitations and release notes before any tag or upload.

**No change to the development repository's MIT license is authorized by this
record. No package has been published.**
