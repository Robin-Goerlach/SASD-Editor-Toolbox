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

The inventory may include an optional `conformanceCases` array on a historical routine. The repository contract validator verifies that each referenced case ID occurs in a file declared by the portable manifest, rejects duplicate or unknown IDs, and reports the number of rows with links. **Currently 103 of 123 rows** link to specific portable cases; the other rows retain conceptual/documentation evidence and still require individual regression review.

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

Seven further negative and boundary cases verify window-linking rejection, one-based number validation, the three-row split minimum, freed-row ownership and last-view marker/block cleanup. These strengthen the already linked ten historical window/block routines without asserting new routine coverage.

Eight portable reformat cases have been linked to the five historical `EditCompressLine`, `EditShiftLine`, `EditLongLine`, `EditShortLine` and `EditReformat` responsibilities. The inventory now contains **65 of 123** explicit portable test links. These verify observable behavior, not one-to-one historical helper implementation details.

Eight typeahead fixtures now link Pokechr, EditPushtbf, EditUserpush and EditAbort to direct portable input/abort tests. Host-originated Ctrl-U is deliberately not interchangeable with macro input; normalized keys remain separate from physical platform scan codes. The historical inventory now has **69 of 123** entries linked to actual portable tests.

The new `firsted-keymap.json` fixtures link five additional historical routines for Ctrl-K/O/Q command classification and text/command processing to executable tests of the public normalized-input boundary. The inventory now has **74 of 123** entries with explicit portable evidence; host prompts remain separate from the reusable editor core.

Twenty-two new portable host-prompt cases directly test the cancellation boundary between FIRST-ED bindings, user-supplied values and dispatched commands. Seven interactive historical Cp wrapper responsibilities now have specific executable mappings. This strengthens the host-facing V1 gate but does not certify 1985 terminal UI fidelity. **81 of 123** entries now have portable test links.

Ten new portable cases now provide explicit executable evidence for six previously unlinked, already-implemented FIRST-ED kernel routines (Center Line, character-left, line joining, literal control insertion, block flag clearing and reconstruction). The inventory now links **87 of 123** routines. The remaining routines still require separate functionality or host-boundary review, rather than automatically being treated as missing features.

Ten new portable cooperative lifecycle cases now link the remaining five already-implemented scheduler/interrupt/system/exit routines to executable evidence. The fixture checks input precedence, bounded idle slices, orderly shutdown and one-shot physical Ctrl-U interruption. With the kernel cases, **92 of 123** handbook entries have portable case links. Historical DOS scheduling internals remain excluded.

Twelve additional host-neutral Cp wrapper cases link nine previously unlinked historical interactive procedures for window operations, marker commands, margins, tab stops and Undo limits. The tests check confirmed/cancelled prompts and rejected state-dependent values through the actual Ctrl-K/O/Q boundary. **101 of 123** historic routine names now carry direct executable portable evidence, not a blanket DOS UI certification.

## Additional .NET-only sample-host evidence: first Save

The FIRST-ED terminal sample now prompts for a destination when Ctrl-K S is pressed on a new, unnamed document. Tests isolate the Save prompt decision from console rendering, distinguish null/blank cancellation from an actual filename, and dispatch a first and subsequent Save through a recording codec. An existing associated document saves directly; the path is never inherited from another window. This is a **modern sample-host usability policy** with executable .NET evidence, not a portable conformance case or a verified reconstruction of the original `EditCpFileSave` dialog. That inventory row deliberately remains *unlinked* pending exact historical/dialog acceptance.

## Next review and acceptance criteria

For each *needs-review* row, examine its individual handbook description, check the existing architecture and tests, add focused regression coverage for genuine behavior gaps, then independently implement only those gaps. Revisit the conceptual *implemented* mappings and identify any missing tests without fabricating Pascal-shaped APIs. Keep the English and German audit, compatibility matrix, and any affected shared specification/conformance cases synchronized.

The Python repository-contract gate validates **123 distinct names**, sequential printed pages **118–240**, classification vocabulary and existence of evidence paths. This guarantees **inventory integrity**, not runtime equivalence. V1 remains open until the wider behavioral and regression-coverage release audit is signed off.


## .NET terminal sample: overwrite consent for first Save and Write File

The sample host now asks for an explicit YES before overwriting an existing filename selected by a first Ctrl-K S Save As or by Ctrl-K W Write File. Cancellation produces no semantic request, while a confirmed Write File still preserves document name/Dirty/Undo. Tests exercise the pure host decision, real temporary files and actual semantic dispatch. An ordinary Save to an already associated path remains direct. This is **sample-only modern host evidence**, not a direct historical portable conformance mapping for `EditCpFileSave` or the original DOS confirmation UI.

## V1 acceptance — focused evidence review (October 2026)

The release-gate inspection cross-checked all 123 inventory rows against the 14 files listed in the portable conformance manifest and the .NET conformance adapter. Before the new fixtures, the repository held **228 cases, 101 linked routine rows and 172 links** (a routine may cite several cases); every linked case had a nonempty expected-state object. Four of these existing links (`EditK` twice, `EditO` and `EditQ`) deliberately test **key translation only**, not a prompted command execution. Keymap evidence is valid for those responsibilities, but must not be generalized into UI parity.

The following previously unlinked responsibilities now have newly executed portable test cases:

- **`EditCprfw` (printed p. 151):** Ctrl-K R prompts for a Read File path. A null/cancelled filename does not dispatch or insert. A filename surrounded by whitespace is trimmed and passed through the actual semantic dispatcher and decoded-line fixture: the import occurs after the initiating line with the cursor preserved, Dirty set, and one Undo entry. DOS prompt appearance and byte decoding are **not** asserted.
- **`EditCvts2i` (printed p. 158):** The modern numeric host-input boundary accepts surrounding whitespace for a valid one-based Go To Line command and rejects an overflowing 32-bit integer without mutation. These checks do **not** prove the historical Pascal helper's internal parsing behavior.

The repository verifier now rejects a portable case with an empty `expect` object. This prevents assertion-free fixtures being counted as evidence. It does **not** certify the correctness or completeness of each expected result.

**New auditable totals:** 232 portable cases, 103/123 linked rows, 20 without portable case links (7 modern replacements, 9 host/sample, 1 documented divergence, 3 non-applicable DOS routines). In particular, **`EditCpFileSave` remains unlinked** because the modern terminal first-Save and overwrite prompts were not verified against the exact historical DOS dialog.

### Outstanding sign-off checks

- Independently review the actual handbook description, observable .NET behavior and meaningful edge cases for the **84 routines classified implemented**; a linked test is focused regression evidence, not full historical parity.
- Review the **20 unlinked** responsibilities on their merits; avoid inventing a core API for obsolete screen hardware or host presentation machinery.
- Perform real keyboard/terminal smoke tests under Windows and Linux: Ctrl-key input, prompt cancellation, Save/Read/Write, abort, linked windows and terminal resize. Headless CI cannot confirm hardware/terminal delivery.
- Audit public APIs, persistence compatibility, Undo/anchor behavior and final clean release readiness. Tag, package and release remain explicitly approval-gated.

**This is a partial release-gate audit, not a V1 sign-off.**

## Specific handbook finding: EditCptabdef (printed p. 155)

Direct review of the handbook description identified a concrete sample-host gap that was not visible from the index count: deliberately empty numeric input to Set Tab Width is **not** simply a cancelled command. The original wrapper derives a tab interval from the current cursor column minus one. The console host previously treated blank input as cancellation. A new sample-only prompt policy and headless tests now cover empty Enter, true cancellation, positive explicit input, overflow, the column-one zero boundary and per-window configuration isolation. The one-based historical rule is adapted to the zero-based view model without changing `EditorCommandPromptResolver` for other commands. The deliberate global-vs-per-window tab divergence remains documented. This targeted correction strengthens actual historical behavioral acceptance but does not constitute a full routine-by-routine sign-off.

## Historical routine review: Change Case and Delete Text Right (21 October 2026)

This audit slice directly compared the handbook's printed procedure descriptions with the actual .NET dispatcher, engine and executable fixtures, **rather than merely counting existing index links**.

| Manual entry | Printed page | Behavior confirmed and test evidence | Remaining boundary |
|---|---:|---|---|
| `EditChangeCase` | 135 | The current character changes uppercase ↔ lowercase without moving the cursor. A non-case character produces no modification or Undo. Four additional portable fixtures now test Unicode titlecase, an uncased CJK character, lowercase ä → Ä and uppercase Ä → ä; two direct .NET theory cases reinforce the no-op guarantee. | The 1985 document does not define Unicode; invariant, one-code-unit handling is an explicit **modern extension**, not a claim of DOS Unicode semantics. |
| `EditDeleteTextRight` | 165 | The character at the cursor and everything to its right on the **same** line are removed. New portable fixtures confirm the following logical line remains untouched, existing `UserColored` flags survive, and one Undo restores original lines and flags. | Physical video redraw, original DOS memory layout and any untested interaction with external hosts remain outside this focused slice. |

**Concrete defect corrected:** the .NET Change Case implementation formerly treated *every non-uppercase code unit* as a candidate for uppercasing. Unicode titlecase letters are neither lowercase nor uppercase; the algorithm now explicitly checks `char.IsLower` before calling `ToUpperInvariant`. Uncased symbols remain no-ops. The code comments document the source rule, modern Unicode policy and Undo reasoning. The Delete To End Of Line implementation needed no code change; its existing behavior gained stronger portable regression evidence.

This reviews **two of the 84 implemented-classification responsibilities in this particular audit slice**; it does not assert that the other 82 have been manually signed off. The inventory remains **103 of 123** routine rows linked to portable tests. Six new fixtures bring the portable V1 suite from 232 to **238 cases**. Headless CI is necessary but real Windows/Linux terminal smoke tests and final release-readiness review remain open.

## V1 handbook acceptance: joined-line Wrapped boundary

The direct handbook review of `EditDeleteLeftChar` (printed p. 161), `EditDeleteRightChar` (p. 163), `EditDeleteRightWord` (p. 164), and `EditJoinLine` (p. 184) confirmed that a join removes one logical line descriptor and its intervening separator; window/marker/Undo repair remains required. Cross-checking the **normative V1 outgoing-boundary model** exposed a concrete bug: the existing .NET joins retained the surviving *upper* line's `Wrapped` bit rather than the former *lower* line's bit. The wrong bit can change the historical file separator emitted by the 8-bit writer without changing the visible joined text.

The corrected compatibility helper now inherits only the lower line's outgoing Wrapped flag and retains the upper line's unrelated presentation bits. Six **new** portable V1 cases check both soft-to-hard and hard-to-soft boundary transitions, Delete Left Character, Delete Right Character, Delete Right Word, and Undo. Direct .NET tests also inspect emitted legacy byte streams and linked-window state, so the specific persistence risk is covered by executable evidence rather than by conceptual mapping.

These four **already linked** handbook entries now reference their new cases. The number of linked rows therefore remains **103/123**, while portable fixtures increase **238 → 244**. This audits an additional coherent historical routine group; it does not certify all 84 implemented classifications, prove every DOS edge case, or replace manual Windows/Linux terminal checks.


## V1 routine-by-routine checkpoint: InsertLine, NewLine, DeleteLine

The original handbook describes `EditInsertLine` (p. 183), `EditNewLine` (p. 191) and `EditDeleteLine` (p. 162). This audit compared the actual .NET compatibility implementation and tests against those descriptions and the normative V1 outgoing-`Wrapped` rule.

- **InsertLine:** three documented split positions (column one / inside text / after last nonblank) work structurally. A genuine flag-location defect was found: the old outgoing `Wrapped` bit remained on the upper line after splitting, rather than following the newly inserted lower line. The shared splitter now creates a hard upper-to-lower boundary while retaining the old outgoing boundary behind the lower line. The surviving upper `UserColored` flag is unchanged; anchors are realigned centrally.
- **NewLine:** Insert mode reuses the corrected splitter but moves the cursor to the new lower line; Autoindent remains derived from the former line's leading blanks. Overtype continues moving down without splitting except on the final line; clearing the previous line's outgoing `Wrapped` bit remains an Undoable mutation. No separate flag algorithm was added to the generic editor core.
- **DeleteLine:** the historical sole-line exception still preserves the one descriptor and its stored character count by filling it with spaces. New direct tests additionally check clearing the former outgoing `Wrapped` bit, preserving `UserColored`, marker invalidation, linked-view stability and Undo. Multi-line deletion/marker/block realignment already resides in `EditorLineTopology`; this step did not change its established rules.

Seven new portable `core-smoke.json` cases plus direct .NET byte-level and linked-view regression tests provide focused executable evidence. The 123 historical routines remain classified, and the **existing** directly linked row count remains 103; seven new cases increase portable coverage from **244 to 251**. These tests demonstrate specific byte/Undo/topology outcomes, not complete historical Pascal implementation identity. Manual Windows/Linux terminal acceptance and the remaining one-by-one V1 review are still required.


## V1 focused block-family audit: Copy, Delete, Move (21 October 2026)

Compared handbook p. 127 (`EditBlockCopy`), p. 128 (`EditBlockDelete`) and p. 131 (`EditBlockMove`) against the current compatibility processor and topology/Undo tests. The historical Move prohibition on inserting into its own block remains enforced **before mutation**. The manual is silent about Dirty/version/Undo effects when moving a block immediately before the already-following line. SASD now handles this as a **successful modern no-op**: the contiguous block has not changed location, so a remove-and-reinsert must not manufacture Dirty state or an Undo item.

New headless tests cover hidden/visible no-op moves; real upwards relocation and linked-window reattachment of a deleted top-line anchor; a cross-document move of the **entire source stream**, which retains one blank source descriptor and undoes both documents atomically; and copy within the source selection, whose inserted lines become part of its contiguous highlight via original endpoints. These are observable SASD policies, not unverified Pascal-internal behaviors.

Two portable `block-topology-and-undo.json` cases assert adjacent no-op versus actual upward move. They are linked to the already evidenced `EditBlockMove` routine. **253 portable V1 cases**, **103/123 linked historical rows**. This remains a focused acceptance slice: manual Windows/Linux terminals and the remaining routine-by-routine historical acceptance are outstanding.


## V1 checkpoint: EditUndo and EditWindowLink identity after relinking

Review of the 1985 `EditUndo` and `EditWindowLink` responsibilities against SASD's explicit modern snapshot Undo policy exposed an issue in the retained **low-level legacy Capture API**: its cursor restore selected a view by `WindowId` even after that view was reattached to another `DocumentId`. The text snapshot already restored the correct document, but the unrelated window could receive the old cursor and unexpectedly gain focus.

The fix requires the original view to still display its captured document before replaying that legacy cursor. A **relinked** original view preserves its current cursor and user-selected focus; a **closed** original view retains the older fallback to a surviving linked sibling. Modern compound transactions already validate both identities. Four new direct regression tests guard these cases. One new portable V1 case, `windows.undo-relinked-view-preserves-unrelated-current-window`, confirms the modern semantic undo/navigation outcome and is mapped to both indexed historical responsibilities.

The portable manifest now has **254 cases**; **103/123 handbook rows** remain directly linked (the new evidence strengthens existing linked rows). Historic Pascal Undo stack/pointer internals and real terminal key delivery are not claimed as verified; release sign-off remains outstanding.


## V1 acceptance: EditInsertCtrlChar (printed p. 182)

The original manual describes a two-step behavior: read one character, translate it to an ASCII control code, then insert that code. A direct code review found a genuine gap **in the FIRST-ED console sample**: after Ctrl-P, the prompt forwarded printable A to the core, which could insert ordinary 'A' rather than 0x01. The new host-only control-character prompt policy performs ASCII normalization before semantic dispatch. It maps A-Z (both cases), the standard six ASCII control punctuation keys, and already-translated physical control keys; Escape/Ctrl-U and unsupported input do not issue a command. The code and headless tests distinguish plain U (valid 0x15) from physical Ctrl-U (abort).

The original portable case `kernel.insert-control-character-writes-literal-control-into-line` already validates **literal semantic control insertion** and remains attached to `EditInsertCtrlChar`. New xUnit sample tests additionally exercise physical key normalization, invalid input, dispatcher insertion and Undo. **These are host tests, not additional portable cases.** The inventory remains **103/123 linked routines**, **254 portable cases**. This pass does not verify original DOS scan codes, terminal typeahead, or the broader EditAskfor line-prompt responsibilities.


## V1 command-line helper audit: EditAppchar, EditAppcmdnam, EditAskfor, EditZapcmdnam

A direct review of the original handbook pages 120–122 and 237 found an important **intentional host deviation**: historical `EditAskfor` uses Ctrl-U to preserve/recall a previous Varstring and stops accepting text when the command row is full, whereas the modern console sample treats Ctrl-U as **cancel** and preserves arbitrary-length input while right-scrolling the display. `EditAppcmdnam` historically updates global Cmdcol and could overwrite the top status line; modern prompts use a dedicated row. `EditZapcmdnam` resets Cmdcol; the host starts a new independent row for every prompt. `EditAppchar`'s Pascal Defnocols overflow is replaced by managed string accumulation.

One real bug was fixed within that modern policy: Ctrl-U or Escape may arrive as raw 0x15/0x1B bytes without a named ConsoleKey on some terminals. A shared host-only cancellation policy now recognizes both. Additionally, Backspace and right-scrolled display avoid splitting supplementary Unicode or combining sequences. Targeted .NET regression tests cover the changes without introducing terminal dependencies into the reusable Core.

See `docs/en/HOST-COMMAND-LINE-V1-AUDIT.md` for the per-routine matrix and unresolved gaps. All four entries **remain host-sample** and unlinked to portable per-routine conformance; the previously established **103/123 mapped rows and 254 portable V1 fixtures remain unchanged**. Actual terminal smoke checks and an optional distinct classic-recall host mode remain open. This is not historical typeahead or physical DOS screen acceptance.


## V1 physical-output safety: EditUpdphyscr / EditUpdwindow host replacement

A follow-up terminal acceptance pass identified a concrete safety flaw in the .NET FIRST-ED renderer: it wrote stored document content directly to `Console.Write`, including historical `EditInsertCtrlChar` bytes. A saved literal ESC followed by `[2J` could therefore act as a terminal instruction. A single host-only `ConsoleFirstEdRowProjection` now renders C0/DEL/C1 control bytes as inert text, keeps complete Unicode text elements when clipping, estimates common wide-glyph cell widths, and maps the physical cursor after expanded controls. This is **not** a change to stored editor text, the core viewport contract or historical file formats.

The 1985 video-memory screen update routines are intentionally handled by the modern renderer rather than reimplemented with BIOS/assembly calls. `EditUpdphyscr` remains a `modern-replacement` responsibility (printed p. 221); `EditUpdwindow` remains `host-sample` (p. 223). Neither receives an invented portable routine mapping from these host-only xUnit tests. The index therefore remains **103/123 linked**, with **254 portable cases**. Concrete Windows/Linux smoke checks and remaining terminal-width uncertainty are documented in `docs/en/TERMINAL-OUTPUT-V1-ACCEPTANCE.md`; they are **not yet signed off**.


## V1 codec acceptance: explicit empty final logical descriptor (21 October 2026)

An executable round-trip audit of the `EditFileWrite` / `EditFileRead` / `EditReatxtfil` compatibility path found a real ambiguity in the .NET reference codec: `["alpha", ""]` previously encoded as `alpha CR LF`, while the decoder (deliberately) treated this as one terminated line `["alpha"]`. A stored blank final logical descriptor was therefore lost on reload.

`FirstEdLegacyFileCodec.Encode` now emits an additional outgoing **CR/LF** for an explicitly blank last descriptor; an explicitly final blank `Wrapped` descriptor still emits 0x8D instead. A foreign file ending with only a single CR/LF continues to decode without a fabricated extra blank line, and an empty physical file continues to import zero lines. The distinction is an explicitly documented **SASD round-trip policy** for ambiguous 8-bit data, **not a claim about undocumented Pascal writer internals**.

Seven new direct .NET cases test exact legacy bytes, repeated trailing blanks, sole blank versus zero physical input, trailing soft boundary, unchanged externally terminated text, full Read File/Undo, and rejection before writing malformed trailing content. These strengthen already implemented and linked file operations; **254 portable V1 fixtures and 103/123 directly linked handbook routines** remain unchanged. Manual Windows/Linux terminal work and the remaining historical behavioral audits are still separate V1 gates.
