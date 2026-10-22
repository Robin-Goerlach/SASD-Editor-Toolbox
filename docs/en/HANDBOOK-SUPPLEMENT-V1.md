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

## Still-unimplemented features explicitly described in the book

| Handbook reference | Location | Current state |
| --- | --- | --- |
| FIRST-ED colors and block/user attribute projection | pp. 69-72, 137-138 | Console host implemented; actual Windows/Linux terminal smoke testing pending |
| FIRST-ED hard/soft/end-of-file right-edge markers | p. 69 | Console host implemented; option host-only |
| Find/Replace with options and `Optstr` | back cover, variable p. 113 | Core options now exposed via modern C/W/R host prompt; original `Optstr` grammar remains unverified |
| `EDITERR.MSG` customizable numeric message file | p. 54 | Deliberately replaced by typed/localizable error catalog; legacy file interchange not implemented |
| MicroStar F10 pull-down menus | pp. 42, 44-52 | Not implemented |
| MicroStar pop-ups `MakeWindow` / `RestoreWindow` | p. 53 | Not implemented |
| MicroStar incremental background printing `PrintNext` | p. 53 | Partial: print-to-file, pagination and status implemented; exact historical printer behavior remains unverified |
| MicroStar directory browser and file rename/copy/delete | p. 42 | Not implemented |
| MicroStar Read Block / Write Block | p. 45 | Not implemented as block-specific file commands |
| MicroStar Save & Open / Abandon & Open | p. 45 | Not implemented as combined workflows |
| MicroStar Lightning spell-check command | p. 45 and disk file list p. 98 | Not implemented; historically a separate integration |

The MicroStar additions are **sample host/application features**, not
unimplemented primitives of FIRST-ED Core. They should be developed in
their own optional sample milestones and never tied to the mandatory core
public API.

## Suggested follow-up order

Finish real terminal smoke tests, then add an optional MicroStar host layer
with a documented command map, prompts and pop-ups; integrate block
file operations and a safe filesystem-view model; add cooperative print
integration; leave Lightning as an optional integration boundary. Avoid
pretending that missing historical UI sources can be reconstructed solely
from a short feature name in the handbook.
