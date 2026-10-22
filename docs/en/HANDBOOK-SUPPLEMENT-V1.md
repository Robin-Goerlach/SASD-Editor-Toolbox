# Handbook supplement — symbols outside the printed 123-function index

The 1985 handbook's printed *Procedures and Functions Index* (pp. 243-244)
enumerates 123 pages of routine descriptions (pp. 118-240). That is NOT an
exhaustive catalog of every historical name used in the prose, code
illustrations, hooks, or MicroStar demonstration.

The companion machine-readable file is
`docs/handbook-supplemental-symbols-v1.json`. It currently records
**20 separately evidenced names**, including toolbox helpers,
five extension hooks, one composite code example and three MicroStar
sample helpers. Every name has a printed-page reference, a layer, status,
repo evidence and rationale. The repository validator rejects duplicates
against the original 123 and enforces required fields.

**Scope limitation:** this is an incremental, non-exhaustive symbol audit.
The manual does not establish a full declaration or signature for each
referenced helper. We do not invent missing signatures or assume that
historical source files are redistributable. The original 123 remain
unmodified in their own ordered inventory.

## Historical features: implementation and remaining acceptance gaps

| Handbook reference | Location | Current state |
| --- | --- | --- |
| FIRST-ED colors and block/user attribute projection | pp. 69-72, 137-138 | Console host implemented; actual Windows/Linux terminal smoke testing pending |
| FIRST-ED hard/soft/end-of-file right-edge markers | p. 69 | Console host implemented; option host-only |
| Find/Replace with options and `Optstr` | back cover, variable p. 113 | Core options now exposed via modern C/W/R host prompt; original `Optstr` grammar remains unverified |
| `EDITERR.MSG` customizable numeric message file | p. 54 | Deliberately replaced by typed/localizable error catalog; legacy file interchange not implemented |
| MicroStar F10 pull-down menus | pp. 42, 44-52 | Partial: F10 popup, keyboard navigation and commands in console sample; historical interaction and real terminals not fully accepted |
| MicroStar pop-ups `MakeWindow` / `RestoreWindow` | p. 53 | Partial: redraw-based overlays and restoration with deterministic tests; original DOS cell buffering not reproduced |
| MicroStar incremental background printing `PrintNext` | p. 53 | Partial: print-to-file, pagination and status implemented; exact historical printer behavior remains unverified |
| MicroStar directory browser and file rename/copy/delete | p. 42 | Partial: safe browse/copy/rename/delete sample workflows and tests; historic UI fidelity not accepted |
| MicroStar Read Block / Write Block | p. 45 | Partial: F10 T/L block-specific file workflows and tests; final historical acceptance open |
| MicroStar Save & Open / Abandon & Open | p. 45 | Partial: F10 P/A staged document replacement with save/discard policy and tests; interactive acceptance open |
| MicroStar Lightning spell-check command | p. 45 and disk file list p. 98 | Not implemented; historically a separate integration |

The MicroStar additions are **sample host/application features**, not
unimplemented primitives of FIRST-ED Core. They should be developed in
their own optional sample milestones and never tied to the mandatory core
public API.

## Suggested follow-up order

Validate the implemented MicroStar workflows in real Windows/Linux terminals;
check the original dialog and PrintNext behavior against available handbook
evidence; keep Lightning optional and establish its interface only from
supported facts. The code already includes F10 navigation, pop-ups, browser,
block file operations, combined Save/Open actions and a modern cooperative
print-to-file sample. **Partial implementation is not historical parity.**
Avoid pretending that missing historical UI sources can be reconstructed
solely from a short feature name in the handbook.

## Acceptance gates for the MicroStar sample

The table above distinguishes source-level implementation from original
observable behavior. A row may be called **historically accepted** only
when all applicable gates are satisfied:

1. **Source evidence:** cite the relevant handbook page and explicitly
   record details the handbook does not establish.
2. **Implementation trace:** point to the actual sample host, policy or
   core service rather than an assumed feature based on its name.
3. **Deterministic verification:** identify automated tests for ordinary,
   cancellation and failure paths where those paths exist.
4. **Interactive verification:** check keyboard, focus, overlay restore and
   file prompts on real Windows and Linux terminals where applicable.
5. **Divergence classification:** distinguish an intentional modern
   substitute (such as redraw-based popups) from claimed literal DOS
   behavior. Document unavoidable deviations.
6. **Parity review:** do not mark a row fully accepted merely because an
   implementation and tests exist; a final reviewer must compare the
   evidence with the observed historical contract.

This gate is a **review policy**, not an assertion that the listed
features have already passed historical acceptance. The separate
handbook procedure index still requires its own coverage audit.

## Machine-readable acceptance gap reporting

`MicroStarAcceptanceReport.Missing` returns named, ordered evidence gaps
(handbook, implementation, tests, conditional real-terminal check,
divergences and final reviewer sign-off). This pure function never executes
tests or assumes that green CI establishes historical fidelity. Its
`IsReady` decision mirrors `MicroStarAcceptancePolicy`, and unit tests
protect the absent-evidence and terminal-check cases. The report is a
building block for later per-feature acceptance records, **not** a
completed assessment of the original 123 indexed routines.

## Per-feature audit records

`MicroStarFeatureAudit` accepts explicitly named review entries containing
a handbook page/reference, implementation reference, test reference and
the independently reviewed evidence gates. The report returns missing
gates for **each** feature. Duplicate IDs (case insensitive) and absent
references are rejected. An empty review set can never be counted as
complete. The tests deliberately use synthetic evidence and do not assert
historical parity for any real feature.

**Scope:** a passing audit of a selected subset does not imply that all
MicroStar features, much less all 123 indexed FIRST-ED procedures, are
covered. The feature inventory must be independently reconciled before
a project-wide parity statement can be made.

## Defensive parsing of modern search options

The console sample's C/W/R options are a **modern** user-interface grammar,
not a reconstructed Borland `Optstr` format. Only ordinary ASCII spaces,
commas and semicolons separate option letters. Control characters, pasted
newlines and Unicode whitespace are rejected; the search command is not
created on invalid input. Headless tests guard these cases while preserving
existing case-sensitive, whole-word and wrap semantics. Exact historical
`Optstr` syntax and behavior remain unverified.
