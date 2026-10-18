# V1 historical handbook procedure/function index audit

## Source and interpretation

The *Turbo Editor Toolbox Owner's Handbook* (1985), **Procedures and Functions Index** on printed pages **243–244**, references **123** named routine descriptions on printed pages **118–240**. Each independently transcribed name and page reference is in [the machine-readable inventory](../handbook-procedure-index-v1.json). The historical handbook itself and any Pascal code remain outside the repository.

The classification describes **observable responsibility**, never source-level Pascal API parity. Each row references SASD technical documentation. The status `implemented` is an initial conceptual mapping to the active .NET reference, **not** proof of exact historical behavior on every edge case. The audit is **not yet a V1 release sign-off**.

## Classification summary

| Classification | Count | Interpretation |
|---|---:|---|
| Implemented | 84 | Documented conceptual behavior is present in the .NET reference. |
| Modern replacement | 8 | A host-neutral invariant or newer abstraction replaces an internal historical mechanism. |
| Host/sample concern | 27 | Presentation, prompting or command wrapper responsibility is outside the portable core. |
| Not applicable | 3 | Platform-dependent DOS/BIOS/video-memory machinery is not portable. |
| Documented divergence | 1 | Historical behavior differs from a documented modern policy. |
| Needs review | 0 | The present evidence does not justify a complete compatibility claim. |
| **Total** | **123** | One entry for each printed routine description page. |

## Final two routine mappings

- `EditGenlineno` (manual p. 176): its historical backwards traversal is replaced by `Cursor.Line + 1` in per-window status projection; this is indexed O(1) computation without display side effects.
- `EditHscroll` (manual p. 179): horizontal visibility of **all** windows is now handled in the UI-independent model with host-supplied width; the terminal sample calls it before rendering all windows.

See [absolute line and horizontal viewport compatibility](SCREEN-POSITION-COMPATIBILITY.md), including the explicit treatment of historical input-abort checks.

No rows remain under `needs-review` in this **first-pass mapping inventory**. That does **not** establish full V1 acceptance: test breadth, uncertain host wrappers and every `implemented` mapping still require focused release review.

`EditDefineTab` is deliberately classified as a divergence: the handbook used a global tab interval, while SASD uses per-window options. Interactive command processors and screen/update routines require separate host-level scrutiny; their classification does not claim that the complete DOS interface was cloned.

## Executable evidence increment: portable fixtures

The repository now reads every declared file in `conformance/v1/manifest.json`, rather than silently limiting .NET execution to `core-smoke.json`. The new `screen-and-annotations.json` cases cover observable outcomes mapped to `EditGenlineno`, `EditHscroll`, `EditColorFile` and `EditColorLine`, including linked views, flags and text/Undo invariants. This is **specific executable evidence for these mappings**, not a certificate that all 123 historical routines have been checked to the same depth.

## Machine-checked links to executable evidence

The inventory may include an optional `conformanceCases` array on a historical routine. The repository contract validator verifies that each referenced case ID occurs in a file declared by the portable manifest, rejects duplicate or unknown IDs, and reports the number of rows with links. **Currently 60 of 123 rows** link to specific portable cases; the other rows retain conceptual/documentation evidence and still require individual regression review.

These links represent **focused examples**, not exhaustive proof of historical behavior. The verifier deliberately does not fabricate conformance evidence for a procedure merely because its conceptual mapping is marked `implemented` or `modern-replacement`.

The nine new portable command-sequence cases add evidence links for ten more routines covering independent block endpoints, line realignment, marker identity and compound Undo. Rejections are checked explicitly rather than treated as edits.

The new `viewport-navigation.json` fixtures provide focused portable regression evidence for eight additional historical navigation routines: vertical cursor movement, scroll up/down, page up/down and file boundaries. They include linked windows, virtual columns, one-row viewports and very large host-provided row counts. Their reference IDs are checked against the declared conformance manifest.

The new `search-and-replace.json` portable command-sequence cases add concrete evidence for historical `EditFind` and `EditReplace`. They distinguish an explicitly empty Find input from a missing argument, and cover Find Again, unsuccessful searches, deletion through an empty replacement and Undo. These fixtures exercise the public semantic dispatcher and do not require a specific keyboard implementation.

The portable `file-read-topology.json` fixtures now trace `EditFileRead` and `EditReatxtfil` through an injected **decoded-line provider** and the public Read File command. They cover source window/cursor anchoring, linked views, block/marker realignment, Wrapped flags, empty input and single-step Undo. Because byte-level file decoding is tested separately, these links do not by themselves certify every aspect of the historical 0x8D format.

Eight additional portable search/replace cases check the case-insensitive default, non-wrapping Find Again, isolated linked views, document changes between ordinary commands, very large virtual columns and subsequent Find Again after replacement. These augment, but do not replace, the separate .NET asynchronous-host interleaving tests.

Four additional portable rejection cases protect the single-logical-line search/replace boundary: CR and LF in patterns/replacements must be rejected without text, cursor or Undo side effects.

Portable command-sequence regression evidence now includes Find Again after Undo and empty replacements, unrelated-document Undo isolation and replacement inside visible blocks with linked views. The shared fixtures specify observable behavior, while the .NET-only tests cover asynchronous host interleavings.

Seven additional portable fixtures in `search-and-replace.json` exercise the modern opt-in wrap-around search service via `find_next_with_options`. This remains separate from historical FIRST-ED's default non-wrapping search. Tests check match-start boundaries, whole-word filtering, preceding-line order and remembering options across Find Again.

Three portable negative `file-read-topology.json` cases now prove that malformed decoded lines (embedded CR, LF or 0x8D) reject the *whole* insertion with typed `file_io_failed` and no document/Undo mutation. They complement separate .NET codec and asynchronous read consistency tests; they do not establish byte-level historical equivalence.

Concurrent destination-write tests now explicitly check the in-process built-in staged writer's per-path serialization, cancellable waiters, cleanup after failures and independent writes to different destinations. This strengthens file persistence evidence; it does not turn the 32 linked historical routine rows into a blanket V1 certificate.

Five new portable file-output cases exercise the public WriteFile/SaveFile semantic path via a recording logical-line codec, without filesystem timing or byte-format dependencies. Four cases now provide direct executable traceability for the previously unlinked historical `EditFileWrite` entry. There are now **33 of 123** historical routine rows linked to specific portable evidence; other mappings remain subject to the full behavioral audit.

The new `compatibility-primitives.json` cases link 13 previously unlinked historical procedures to directly executable checks. Observable coverage includes linked-window option independence, virtual columns, one-based commands, historical word classes, Undo and modern nonmutating edit semantics. **46 of 123** index entries now carry explicit portable evidence links, not release-level equivalence certificates.

Five further shared cases now trace `EditTab`, `EditDeleteLeftChar`, `EditDeleteRightChar` and `EditDeleteRightWord` through their semantic commands. They distinguish insert versus overtype Tab behavior, virtual-space Backspace without a text mutation and deletion of punctuation/blank runs. The inventory now has **50 of 123** directly linked routines.

The portable `windows-and-block-lifecycle.json` suite now provides direct executable coverage for all 10 previously unlinked historical window and block navigation/lifecycle routines. The handbook inventory therefore has **60 of 123** entries with explicit portable cases. Window/document association and host-neutral frame assertions supplement earlier .NET-only geometry and block tests; the remaining unlinked procedures still require review.

## Next review and acceptance criteria

For each *needs-review* row, examine its individual handbook description, check the existing architecture and tests, add focused regression coverage for genuine behavior gaps, then independently implement only those gaps. Revisit the conceptual *implemented* mappings and identify any missing tests without fabricating Pascal-shaped APIs. Keep the English and German audit, compatibility matrix, and any affected shared specification/conformance cases synchronized.

The Python repository-contract gate validates **123 distinct names**, sequential printed pages **118–240**, classification vocabulary and existence of evidence paths. This guarantees **inventory integrity**, not runtime equivalence. V1 remains open until the wider behavioral and regression-coverage release audit is signed off.

