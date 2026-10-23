# V1 handbook index: 20 entries without direct portable case links

Status: **Evidence triage, not a claim of completed V1 acceptance.**

## Ground truth and scope

This report is derived from `docs/handbook-procedure-index-v1.json` at
`0823cec2cb64261ea9a71c118d1687c9a2fd72ab` and covers precisely those index rows for which
`conformanceCases` is absent or empty. The printed 1985 index contains 123
entries. **103** have links to named portable cases; the **20** rows below do
not. A missing portable link is **not** a missing implementation, nor does an
existing conceptual mapping establish exact historical behavior.

| Missing-link classification | Count | Required treatment |
|---|---:|---|
| Modern replacement | 7 | Review observable invariant and existing portable/.NET evidence; link only a genuinely representative portable case |
| Host/sample | 9 | Review headless console tests and named real-terminal scenarios; do not invent portable Core cases for UI-only behavior |
| Documented divergence | 1 | Explicitly approve or reject the known difference against handbook behavior |
| Not applicable (DOS) | 3 | Review rationale and modern redraw safety, not impossible BIOS/video-memory parity |
| **Total** | **20** | No automatic release sign-off |

## Per-routine review worklist

Each evidence path and historical page comes directly from the checked-in
machine-readable index. The last column is a **proposed next verification
action**, not a passed test or a newly established historical requirement.

| Historical routine | Handbook page | Current mapping | Existing evidence reference | Next acceptance action |
|---|---:|---|---|---|
| `Advance` | 118 | Modern replacement | `docs/en/KERNEL-COMPATIBILITY-AUDIT.md` | Verify word-left/right classification against direct compatibility tests; do not require a standalone Pascal helper API. |
| `EditAppchar` | 120 | Host/sample | `docs/en/HOST-COMMAND-LINE-V1-AUDIT.md` | Check terminal prompt rendering and keyboard entry in the existing headless host tests; record historical display differences. |
| `EditAppcmdnam` | 121 | Host/sample | `docs/en/HOST-COMMAND-LINE-V1-AUDIT.md` | Check command-line name projection and host prompt width behavior; record DOS Cmdcol divergence. |
| `EditAskfor` | 122 | Host/sample | `docs/en/HOST-COMMAND-LINE-V1-AUDIT.md` | Verify cancellation, Unicode editing and answer handling in host tests; historical Ctrl-U recall remains divergent. |
| `EditClsinp` | 136 | Modern replacement | `docs/en/TYPEAHEAD-AND-WINDOW-TEXT.md` | Trace normalized host input and the typeahead/abort fixtures; preserve origin distinction. |
| `EditCpFileSave` | 143 | Host/sample | `docs/en/COMMAND-PROCESSOR-TRANSFER.md` | Trace Ctrl-K/S and F10 Save through prompt resolver, dispatcher and actual file tests; run explicit cancel/overwrite smoke case. |
| `EditDecline` | 159 | Modern replacement | `docs/en/LINE-TOPOLOGY-AND-REALIGNMENT.md` | Verify line deletion and renumbering/anchor integrity via topology fixtures; no separate mutable Pascal count. |
| `EditDefineTab` | 160 | Documented divergence | `docs/en/KERNEL-COMPATIBILITY-AUDIT.md` | Verify per-window tab-size independence and annotate the historical global Tabsize deviation; require explicit acceptance. |
| `EditDestxtdes` | 167 | Modern replacement | `docs/en/ARCHITECTURE.md` | Review document/view lifetime, last-view cleanup and Undo invalidation tests; no manual descriptor free. |
| `EditErrormsg` | 171 | Modern replacement | `docs/en/ERROR-MODEL.md` | Verify typed error-to-host message mapping and abort presentation; original numeric error code UI not claimed. |
| `EditIncline` | 180 | Modern replacement | `docs/en/LINE-TOPOLOGY-AND-REALIGNMENT.md` | Verify insert/split topology and anchor updates in portable fixtures; no separate mutable Pascal count. |
| `EditInitialize` | 181 | Host/sample | `docs/en/INTERACTIVE-FIRST-ED-HOST.md` | Verify sample startup, terminal restoration on exit and input modes on Windows/Debian; host-specific sign-off. |
| `EditUpcase` | 220 | Host/sample | `docs/en/KERNEL-COMPATIBILITY-AUDIT.md` | Verify ASCII shortcut normalization/uppercase helper semantics and distinguish Unicode extensions; no portable Pascal Varstring contract. |
| `EditUpdphyscr` | 221 | Modern replacement | `docs/en/ARCHITECTURE.md` | Review sanitized physical terminal projection and resize tests; DOS memory writes not reconstructed. |
| `EditUpdrowasm` | 222 | Not applicable (DOS) | `docs/en/ARCHITECTURE.md` | Classify nonportable inline DOS assembly as not applicable; verify modern renderer has no dependency on it. |
| `EditUpdwindow` | 223 | Host/sample | `docs/en/ARCHITECTURE.md` | Check multiwindow repaint, status rows and focus recovery on real terminals; use renderer tests. |
| `EditUpdwinsl` | 224 | Host/sample | `docs/en/ARCHITECTURE.md` | Check per-window status-line rendering after link/split/resize; historical direct display calls not claimed. |
| `EditZapcmdnam` | 237 | Host/sample | `docs/en/HOST-COMMAND-LINE-V1-AUDIT.md` | Check clearing/redrawing of prompt rows after cancellation and completion; capture Windows/Debian observations. |
| `MoveFromScreen` | 238 | Not applicable (DOS) | `docs/en/ARCHITECTURE.md` | Classify DOS video-memory readback as not applicable; modern repaint uses document model. |
| `MoveToScreen` | 239 | Not applicable (DOS) | `docs/en/ARCHITECTURE.md` | Classify DOS video-memory restore as not applicable; modern overlay stack re-renders model. |

## Prioritized V1 acceptance slices

1. **Safety-critical interactive file path:** start with
   `EditCpFileSave` and the actual Save/Open/overwrite/cancel paths.
   Trace host prompts to the semantic dispatcher and file service;
   confirm that cancellation cannot mutate the document or destination.
2. **Terminal presentation:** verify `EditInitialize`,
   `EditUpdwindow`, `EditUpdwinsl`, `EditAppchar`,
   `EditAppcmdnam`, `EditAskfor` and `EditZapcmdnam` with
   headless tests and individually recorded real-terminal behavior.
3. **Model/compatibility invariants:** review portable command fixtures
   for `Advance`, `EditClsinp`, `EditDecline`, `EditIncline`,
   `EditDestxtdes` and the typed error behavior of `EditErrormsg`.
4. **Decisions rather than unsupported recreation:** explicitly approve
   the `EditDefineTab` global-to-per-window deviation, the
   `EditUpcase` host helper choice, and the non-applicability of
   `EditUpdrowasm`, `MoveFromScreen`, `MoveToScreen`.
   `EditUpdphyscr` requires modern renderer evidence, not DOS API parity.

## Acceptance discipline

- Only attach `conformanceCases` when the manifest contains an existing,
  accurately scoped portable case ID and the case actually tests the mapped
  observable responsibility. Never add links merely to increase coverage.
- Host-only functions may be fully reviewable using documented host unit tests
  and real-terminal acceptance; a portable case link is not a universal gate.
- The confirmed Windows/Debian editor launches and Windows AltGr/window
  observations are **smoke evidence**, not blanket PASS entries for these
  20 historical routines.
- Keep `HANDBOOK-PROCEDURE-INDEX-AUDIT.md` as the conceptual index
  summary. This worklist records *unresolved direct-link evidence*, not
  20 planned feature implementations.
- The separate supplemental handbook inventory and MicroStar acceptance
  record remain independent release gates. No V1.0 tag is justified by
  closing this table alone.
