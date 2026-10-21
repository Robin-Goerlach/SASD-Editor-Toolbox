# V1 developer demo editor and public API acceptance

**Status (October 2026):** First executable acceptance slice. No V1.0 release/tag/package, no claim of complete manual terminal sign-off.

## Decision: reuse the editor core, demonstrate the host

The existing interactive FIRST-ED sample at `samples/dotnet/Sasd.Editor.FirstEd.Sample` is our first **real demo editor and developer template**. It uses the public `EditorSession`, `EditorCommandDispatcher`, view/document model, file codec and Undo path. No sample-specific editing engine, buffer or persistence layer has been introduced.

The next graphical example should be a deliberately small **WinForms demo**, once the reusable V1 gate is sufficiently covered: basic text viewport, File/Edit/Search actions, linked views and diagnostic output. It should depend only on the core, never become a core dependency or pull WPF/MAUI/web into V1. This is a recommendation for a follow-up step, not a claim that WinForms is implemented.

## Running the demonstration

Run these commands in a **Windows PowerShell terminal** or Linux terminal with the .NET 10 SDK installed, from the repository root:

```powershell
dotnet restore Sasd.Editor.Toolbox.slnx
dotnet build Sasd.Editor.Toolbox.slnx -c Release
dotnet run --project samples/dotnet/Sasd.Editor.FirstEd.Sample -c Release --no-build -- --help
dotnet run --project samples/dotnet/Sasd.Editor.FirstEd.Sample -c Release --no-build -- --demo
```

Without options, the sample starts with an empty document. `--demo` starts a **genuine editable, in-memory** example. It creates no files, has no filename association and begins with empty Undo history. `--help` is available even with redirected standard input. Unknown/combined arguments fail instead of unexpectedly starting another mode.

**Warning:** this historical sample saves via the Latin-1/0x8D legacy file codec; modern Unicode storage is available separately through the public `FileTextStorage` API. The seeded demo is intentionally ASCII-compatible, with one literal ESC to demonstrate safe rendering. Choose disposable filenames when practicing Save.

## Manual demo walk-through (not yet signed off)

1. Launch `--demo`. Verify the first document is a normal editable NONAME stream, not a picture.
2. Navigate with arrows/Home/End. Insert characters, then press Escape for Undo.
3. Press Ctrl-Q then F; search for `WIDGET`, which appears in the sample. Try repeated searching.
4. Ctrl-K B starts a block; Ctrl-K K ends it; Ctrl-K C copies the selection. Undo afterward.
5. Ctrl-O O opens the two-number window-creation prompt. Ctrl-O J links two windows; check that their cursors move independently while sharing text. A tall enough terminal is necessary.
6. Ctrl-K 1 sets a marker, Ctrl-Q 1 jumps to it. Ctrl-P followed by a printable ASCII letter inserts a real control code.
7. The line `SAFE CONTROL DISPLAY:` contains an actual ESC followed by printable `[2J`. It must display as `^[[2J` **without clearing the terminal**.
8. Optionally Ctrl-K S saves after prompting for a path, or Ctrl-K W writes a separate target without renaming the current stream. Use only disposable test paths.
9. Ctrl-K X exits after an explicit `YES` confirmation.

For terminal-font/Unicode/key-chord checks, use `docs/en/TERMINAL-OUTPUT-V1-ACCEPTANCE.md`. A headless GitHub runner does not provide interactive terminal acceptance.

## V1 public API acceptance matrix

| Area | Required consumer-level evidence | Gate status |
| --- | --- | --- |
| Document/view ownership | Public create/load/attach; multiple windows share one document but keep separate cursors | Headless regression evidence, manual host pending |
| Semantic commands and Undo | `EditorCommandRequest` through dispatcher; no internal APIs or fake edit path | Public consumer tests, routine-by-routine historical sign-off pending |
| Modern UTF-8 persistence | Strict UTF-8 Load/Edit/Save/Reopen, newline preservation, dirty acknowledgment and external revision checks | Automated on Linux and Windows |
| Historical format | 0x8D Wrapped, distinct Write versus Save semantics, byte-level round trips and safe failure | Automated; historical audit continues |
| Failure/abort | Invalid Latin-1 save, absent read file, canceled save leave bytes/text/Undo/dirty state intact | Automated across public API |
| CLI/demo host | `--help`, `--demo`, real session, search/edit/Undo/linked windows | Headless; hands-on terminal pending |
| Public API freeze | Document exceptions, nullable contracts, host layering, versioning and allowed changes | Review before release still open |

**Test suites added:** `PublicEditorApiAcceptanceTests` exercises the actual public API from a separate compiled assembly. It covers UTF-8 load/edit/save, historical Write and Save distinction, invalid legacy output preserving existing bytes, missing-file read preserving linked views, and pre-cancelled save with no disk mutation. `FirstEdDeveloperDemoTests` covers CLI option parsing, genuinely clean in-memory demo state, real search/edit/Undo and shared documents with independent linked-window cursors. These do not rely on terminal hardware or privileged internal model APIs.

## Responsibilities and release gate

The assistant can implement and validate API guards, sample startup, regression tests, bilingual docs and CI. A developer/user should eventually run the demo manually in Windows Terminal, record the terminal version/dimensions and test results, and provide a screenshot or reproduction only for a failing case. No help is needed from the user for the current automated development.

Keep `SASD-Editor-Toolkit` separate; no integration, package publication or V1.0 tag without an explicit later decision.
