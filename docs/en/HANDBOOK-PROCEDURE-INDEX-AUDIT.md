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

## Next review and acceptance criteria

For each *needs-review* row, examine its individual handbook description, check the existing architecture and tests, add focused regression coverage for genuine behavior gaps, then independently implement only those gaps. Revisit the conceptual *implemented* mappings and identify any missing tests without fabricating Pascal-shaped APIs. Keep the English and German audit, compatibility matrix, and any affected shared specification/conformance cases synchronized.

The Python repository-contract gate validates **123 distinct names**, sequential printed pages **118–240**, classification vocabulary and existence of evidence paths. This guarantees **inventory integrity**, not runtime equivalence. V1 remains open until the wider behavioral and regression-coverage release audit is signed off.

