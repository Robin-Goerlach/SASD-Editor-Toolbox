# V1.0 acceptance worksheet — FIRST-ED, MicroStar and historical scope

**Status: TEMPLATE ONLY — NOT SIGNED OFF.** This worksheet is the
*execution record*, while [the release-gap analysis](V1-RELEASE-GAP-ANALYSIS.md)
explains why each gap matters. Never convert CI success, a previously reported
launch, or an unannotated screenshot into an individual manual PASS.

## How to use this worksheet

Copy the empty environment table and relevant test tables **for each run**;
record the reviewer, exact source SHA, version, operating system and terminal.
Run in the working tree checked out to the recorded SHA. A Windows Terminal
session running Debian under WSL is **WSL**, not native Debian; do not merge
their results. Only disposable file paths and synthetic test content are
allowed. For any FAIL or BLOCKED row, supply keystrokes, expected/observed
behavior and a linked issue or commit.

Result vocabulary: **PASS** = executed and observed; **FAIL** = reproducible
defect; **BLOCKED** = not executed / environment constraint; **N/A** = a
deliberately excluded feature with an approved reason; **NOT RUN** = no
recorded execution. A code-level test is evidence for automation, not a
substitute for these rows.

## Environment / reproducibility (copy per session)

| Field | Value |
| --- | --- |
| Date and reviewer | NOT RECORDED |
| Target | Windows Terminal / native Debian / Debian WSL (select one) |
| OS and terminal version | NOT RECORDED |
| Keyboard layout / encoding / terminal dimensions | NOT RECORDED |
| `git rev-parse HEAD` | NOT RECORDED |
| `git status --short` | NOT RECORDED |
| `dotnet --version` | NOT RECORDED |
| Restore/build/test result | NOT RECORDED |
| Screenshot/log evidence reference (outside repository) | NOT RECORDED |

## FIRST-ED interactive acceptance (RC-04, 11 cases)

| ID | Required observation | Result | Notes / evidence |
| --- | --- | --- | --- |
| FE-01 | Demo launch, editable NONAME and stable status/cursor | NOT RUN | |
| FE-02 | Arrow/Home/End/Page navigation and viewport consistency | NOT RUN | |
| FE-03 | Insert text then Escape/Undo restores original | NOT RUN | |
| FE-04 | Ctrl-Q/F WIDGET search and Ctrl-L repeat | NOT RUN | |
| FE-05 | Block marking/copy and Undo | NOT RUN | |
| FE-06 | Marker Ctrl-K/1 and navigation Ctrl-Q/1 | NOT RUN | |
| FE-07 | Ctrl-O/O stacked windows, Ctrl-O/J linked text, independent cursors | NOT RUN | |
| FE-08 | Embedded ESC+[2J is rendered inertly | NOT RUN | |
| FE-09 | Resize redraw with no key, then during queued typing; status preserved | NOT RUN | |
| FE-10 | Disposable Save vs Write and overwrite/cancel paths | NOT RUN | |
| FE-11 | Exit requires YES; denial and cancel keep session open | NOT RUN | |

## MicroStar interactive acceptance (RC-05, 10 cases)

| ID | Required observation | Result | Notes / evidence |
| --- | --- | --- | --- |
| MS-01 | F10 popup without buffer/Undo edits | NOT RUN | |
| MS-02 | Arrow/Home/End/Enter/direct-letter menu navigation | NOT RUN | |
| MS-03 | Escape, Ctrl-U and Q cancel safely | NOT RUN | |
| MS-04 | Find/Replace/Goto and safe prompt cancellation | NOT RUN | |
| MS-05 | Save/Write/Read with disposable paths, overwrite and cancel | NOT RUN | |
| MS-06 | Block operations and Undo | NOT RUN | |
| MS-07 | Write Block/Read Block disposable roundtrip | NOT RUN | |
| MS-08 | Save & Open / Abandon & Open, dirty-state confirmation | NOT RUN | |
| MS-09 | Print-to-file, status, cancellation, partial-output expectations | NOT RUN | |
| MS-10 | Popup redraw, focus restoration, status after cancel/resize | NOT RUN | |

## Per-view tab-width decision (RC-06 / HP-03)

| ID | Observation | Result | Notes / evidence |
| --- | --- | --- | --- |
| TAB-01 | Ctrl-K/T width 3 applies only to first view | NOT RUN | |
| TAB-02 | Linked second view width 6; first remains 3 | NOT RUN | |
| TAB-03 | Repeated focus changes preserve independent settings | NOT RUN | |
| TAB-04 | Cancel/invalid input changes neither view nor Undo | NOT RUN | |
| TAB-05 | Empty Enter uses active nonzero cursor column; column zero guarded | NOT RUN | |

## Legacy file safety (RC-08)

| ID | Observation using only disposable data | Result | Notes / evidence |
| --- | --- | --- | --- |
| IO-01 | 8-bit historical codec roundtrip, including Wrapped boundary | NOT RUN | |
| IO-02 | Cancelled Save/Write leaves destination and dirty state intact | NOT RUN | |
| IO-03 | Overwrite requires explicit confirmation; denial preserves bytes | NOT RUN | |
| IO-04 | Unrepresentable characters produce visible safe encoding failure | NOT RUN | |
| IO-05 | Successful Write export does not unexpectedly change document identity | NOT RUN | |

## Historical scope decision register (RC-07)

The product owner decides scope; an engineer must not mark these approved
from executable tests alone. See
[V1-HISTORICAL-PARITY-DECISION-REGISTER.md](V1-HISTORICAL-PARITY-DECISION-REGISTER.md)
for evidence and historical differences.

| Decision | Proposed disposition — requires owner approval | Owner verdict |
| --- | --- | --- |
| HP-01 | Accept modern search-option grammar, no original Optstr claim | OPEN |
| HP-02 | Accept typed errors/JSON resources, no EDITERR.MSG file parity | OPEN |
| HP-03 | Accept view-local tabs, not global historical Tabsize | OPEN |
| HP-04 | Accept renderer instead of DOS video memory/assembler | OPEN |
| HP-05 | Accept overlay model after popup terminal checks | OPEN |
| HP-06 | Accept print-to-file, no physical DOS printer | OPEN |
| HP-07 | Exclude separate Lightning spelling integration from V1 | OPEN |
| HP-08 | Accept modern host prompts after interactive checks | OPEN |
| HP-09 | Accept codec modernization after disposable IO checks | OPEN |
| HP-10 | Release scope is behavioral modernization, not total historical parity | OPEN |

## Candidate approval (RC-01, RC-02, RC-03, RC-09, RC-10)

| Final release item | Outcome / evidence |
| --- | --- |
| Proposed immutable source SHA / clean checkout | OPEN |
| Both Actions workflows successful for that **same** SHA | OPEN |
| Independent project- and package-reference consumer results for that SHA | OPEN |
| Packaged NuGet artifact identity, manifest and README | OPEN |
| Release notes and selected publication destination | OPEN |
| All terminal, I/O and HP decisions resolved or expressly waived | OPEN |
| Reviewer / date / APPROVE or BLOCK | OPEN |

**Do not tag V1.0, upload a production package, or declare historical
parity before the recorded owner decision and exact-SHA release gates.**

### Previously observed partial results (not transferred into PASS cells)

- Windows Terminal: multiple windows, linked text and automatic resize redraw
  were reported and screenshot-supported in the development conversation.
- Debian under WSL: application launch, linked windows and status bars after
  resize were reported. A prior local run recorded 1,063 passing tests.
- These observations relate to earlier source revisions and do **not** fill
  the complete final-SHA matrix. See
  [real-terminal smoke record](REAL-TERMINAL-V1-SMOKE-RECORD.md).
