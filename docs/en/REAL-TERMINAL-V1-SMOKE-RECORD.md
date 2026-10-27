# FIRST-ED and MicroStar — real-terminal V1 smoke record

Status: **manual verification required; not a historical parity certificate**.

## Confirmed hands-on state (2026-10-23)

Development feedback confirms that the interactive FIRST-ED/MicroStar
sample host has been started and tried on **Windows** and **Debian**.
Debian 12 machine `dev102` also reports .NET SDK `10.0.402`.
A real Windows editor screenshot confirmed the printable AltGr-character
fix; a two-window layout and linked document views were observed with
Ctrl-O/O followed by Ctrl-O/J.

These observations **do not imply** that all 11 FIRST-ED or all 10
MicroStar checklist entries have passed. Per-command Save/Open cancel paths,
file operations, print, resize and other detailed cases require individual
evidence. Record whether the Linux session was native or WSL explicitly;
a reported successful launch does not establish every terminal-specific
interaction in either environment.

## Explicit Debian 12 launch

Install the **.NET 10 SDK** for Debian 12 (Bookworm) from Microsoft's
official APT package feed if `dotnet --version` is not available. The
`packages-microsoft-prod.deb` repository package must be installed
before `dotnet-sdk-10.0` can be resolved by APT. Install system packages
as administrator, but perform restore/build/test/run as the ordinary user
to avoid root-owned build outputs.

Run in the existing repository root:

```bash
dotnet --version
git switch main
git pull --ff-only origin main
dotnet restore Sasd.Editor.Toolbox.slnx
dotnet build Sasd.Editor.Toolbox.slnx -c Release
dotnet test Sasd.Editor.Toolbox.slnx -c Release --no-build
dotnet run --project samples/dotnet/Sasd.Editor.FirstEd.Sample -c Release --no-build -- --demo
```

Use a **real interactive terminal**, not redirected input/output. Press
F10 for MicroStar inside the **same** editor executable. Exit with Ctrl-K,
then X and the explicit `YES` confirmation. SDK installation alone does
not count as a successful terminal smoke test.

## Historical V1 release gate

The printed historical handbook index has **123 mapped procedures**;
**103 have links to specific portable conformance cases** (see
`docs/en/HANDBOOK-PROCEDURE-INDEX-AUDIT.md`). The remaining **20 index
entries without direct portable case links** are evidence gaps, not
necessarily missing implementations. The separately catalogued
supplementary routines and the MicroStar feature inventory require
their own acceptance decisions. A successful build, green CI and the
confirmed Windows/Debian starts **must not** be represented as an
entire V1.0 historical parity sign-off.

## Purpose and safety

Run the *same* `Sasd.Editor.FirstEd.Sample` executable for FIRST-ED and for the MicroStar-inspired F10 menu. The menu is sample-host behavior; it is not an independent executable or an exact reproduction of the 1985 menu layout. Use disposable files only: the historical save codec uses an 8-bit format and 0x8D soft-line markers, not general UTF-8 persistence. Never test overwriting important data.

Run each session in a real interactive Windows Terminal/PowerShell window and a native Linux PTY (e.g. GNOME Terminal, Konsole or xterm). WSL may be tested additionally but does not replace the native Linux run. Suggested dimensions: at least 100 columns by 35 rows; record the actual dimensions.

## Build and launch (repository root)

```sh
dotnet --info
dotnet restore Sasd.Editor.Toolbox.slnx
dotnet build Sasd.Editor.Toolbox.slnx -c Release
dotnet test Sasd.Editor.Toolbox.slnx -c Release --no-build
dotnet run --project samples/dotnet/Sasd.Editor.FirstEd.Sample -c Release --no-build -- --demo
```

Do not pipe or redirect the interactive editor's standard streams; `--help` may be used without a terminal. `Ctrl-K, X` means Ctrl-K followed by X, not a simultaneous chord. Exit using Ctrl-K, X and answer `YES`.

## Record this environment for each run

- Date / tester:
- OS and version:
- Native or WSL:
- Terminal emulator and version:
- Font / encoding / keyboard layout:
- Width x height:
- `dotnet --version`:
- Git `rev-parse HEAD`:
- Build/test result:
- Evidence reference (screenshots/logs stored outside the repository unless deliberately reviewed):

## FIRST-ED observation checklist

Mark each **PASS / FAIL / BLOCKED**, and attach exact keystrokes plus expected/actual result for failures.

1. Demo starts with editable NONAME content and intact status/cursor.
2. Arrow, Home, End, Page Up/Down navigation and viewport/status updates remain aligned.
3. Text insertion followed by Escape restores the previous text via Undo.
4. Ctrl-Q, F, enter `WIDGET`, Enter: a matching occurrence is selected; Ctrl-L repeats the search.
5. Ctrl-K, B and Ctrl-K, K mark a whole-line block; Ctrl-K, C copies it; Undo restores the prior state.
6. Ctrl-K, 1 sets a marker; Ctrl-Q, 1 navigates to it.
7. In a tall terminal, Ctrl-O, O creates a stacked window; Ctrl-O, J links views of one document. Different cursors must not imply different document text.
8. The demo line containing a literal ESC followed by `[2J` is displayed inertly as `^[[2J`: screen/status are not cleared. Optionally test Ctrl-P then `[` followed by printable `[2J` in disposable text.
9. Resize with stacked windows; report missing or overwritten status lines, cursor displacement or unexpected terminal scroll.
10. With a disposable filename, Ctrl-K, S saves; Ctrl-K, W writes without associating the export path with the document. Verify overwrite prompts and cancel paths before confirming writes.
11. Ctrl-K, X exits only after `YES`; negative/cancel responses do not end the session.

For C0/C1 control symbols, grapheme-width and Unicode projection, also use `docs/en/TERMINAL-OUTPUT-V1-ACCEPTANCE.md`.

## HP-03: manual tab-width and linked-view acceptance (separate result per OS)

This protocol verifies the **chosen modern per-window** tab-width policy,
not byte-for-byte fidelity to Borland's global `Tabsize`. Use the normal
FIRST-ED terminal application and a disposable demo document. Record exact
keypresses because terminal emulators may handle control sequences differently.

1. Run `--demo` and note the original tab interval and active window.
   Press **Ctrl-K, T** to open the tab-width prompt. Enter **3** and confirm.
   Expected: the active window now uses width 3; the document text and Undo
   history are unchanged. Note any visible difference in tab-stop behavior.
2. In a sufficiently tall terminal, create a second displayed window using
   **Ctrl-O, O**, then link it to the first document using **Ctrl-O, J**
   according to the host's prompts. Make sure the windows display the same
   document but have individually identifiable focus.
3. In the linked active window, press **Ctrl-K, T** and enter **6**.
   Expected: that view uses width 6; the other view still uses width 3.
   Switch focus between views several times and record how you switched.
   Expected: neither view silently inherits the other's tab width.
4. Open **Ctrl-K, T** again and cancel the prompt with its documented
   cancel key. Repeat with an invalid non-numeric answer if permitted by
   the input UI. Expected: no change to either tab width, document content,
   Dirty state or Undo history.
5. Test **Ctrl-K, T** followed by an **empty Enter** while the cursor is
   at a nonzero, known column. Expected: the selected view receives that
   zero-based cursor column as its interval; the other view does not change.
   At the first column, an empty Enter must **not** install interval zero.
6. Exit normally with **Ctrl-K, X** and the confirmation described above.
   Reopen only if necessary to inspect saved files. Tab-width changes are
   configuration operations, not text edits; do not assume they are persisted
   across process restarts.

| Field | Windows run | Native Debian/Linux run |
| --- | --- | --- |
| Date, OS, terminal, dimensions, Git SHA | NOT RUN | NOT RUN |
| Numeric width 3 in first view | NOT RUN | NOT RUN |
| Linked view retains width 6 while first retains 3 | NOT RUN | NOT RUN |
| Focus changes preserve separate widths | NOT RUN | NOT RUN |
| Cancel / invalid input preserves state | NOT RUN | NOT RUN |
| Empty Enter and first-column guard | NOT RUN | NOT RUN |
| Reviewer verdict (PASS / FAIL / BLOCKED) and evidence | NOT RUN | NOT RUN |

Automated evidence is in `ConsoleFirstEdTabWidthPromptTests`, including
linked views, repeated focus changes and prompt cancellation. It does **not**
replace this manual observation. Leave HP-03 release acceptance **OPEN** until
actual results for both platforms are recorded and assessed.

## MicroStar F10 observation checklist

1. F10 opens the popup without altering document text or Undo.
2. Arrow navigation changes highlight only; Home/End, Enter and directly typed command letters act as indicated.
3. Escape, Ctrl-U, or menu Q cancels without dispatching a destructive command.
4. F=Find, R=Replace and G=Goto show prompts and perform their documented semantic commands; cancel must leave document unchanged.
5. S=Save, W=Write and I=Read use only disposable paths; verify overwrite confirmation and cancel/recovery.
6. B/E/C/M/D exercise block marking, copying, moving and deleting only on disposable in-memory text; Undo restores expected edits.
7. T=Write Block and L=Read Block round-trip a disposable block file; verify selection and cursor state.
8. P=Save & Open and A=Abandon & Open: test both cancel and confirmed paths, with special attention to modified documents. Do not point either command at valuable files.
9. H=Print, J=Status and K=Stop Print: use print-to-file and verify output/status/cancellation; this does **not** prove physical printer or exact historical PrintNext parity.
10. Check that popup redraw, focus and status recover after each action, especially cancellation and window changes.

## Result and release gate

For every step capture: **PASS / FAIL / BLOCKED**, actual behavior, target behavior and environment. Attach a screenshot only when useful; never include sensitive user data. Record differences as one of: implementation defect, terminal-specific behavior, intentional modern divergence, or historical behavior not established by the handbook.

A successful automated CI build, or passing this small manual checklist, does **not** by itself mean that all 123 indexed historical procedures, 20 supplemental names, or complete MicroStar behavior are accepted. No V1.0 release/tag until the separate historical acceptance review.
