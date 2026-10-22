# .NET samples

This directory contains executable .NET hosts and developer examples for the
[SASD Editor Toolbox](../../README.md). The samples demonstrate how an
application can connect user input, prompts, rendering, persistence, and
application-specific workflows to the reusable editor core.

The samples are intentionally **hosts, not alternative editor
implementations**. Reusable editing behavior belongs in
[`src/dotnet/Sasd.Editor.Core`](../../src/dotnet/Sasd.Editor.Core/); console,
desktop, and other UI-specific code belongs here or in another host-facing
project.

## Available sample

| Project | Type | Purpose |
| --- | --- | --- |
| [`Sasd.Editor.FirstEd.Sample`](./Sasd.Editor.FirstEd.Sample/) | Interactive console application | Demonstrates the public .NET editor API through a FIRST-ED-compatible terminal host, including editing, search, blocks, markers, stacked windows, historical file handling, a MicroStar-inspired menu, and cooperative background printing. |

The FIRST-ED sample is currently the only executable .NET host in this
directory. A graphical sample may be added later, but no WinForms, WPF, MAUI,
or web host is implemented here yet.

## Requirements

- [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0)
- An interactive terminal for the editor itself
- Windows, Linux, or another platform with a compatible .NET console

Run all commands below from the repository root.

## Quick start

Start with a prefilled, editable demonstration:

```bash
dotnet run --project samples/dotnet/Sasd.Editor.FirstEd.Sample -- --demo
```

Start with an empty in-memory document:

```bash
dotnet run --project samples/dotnet/Sasd.Editor.FirstEd.Sample
```

Show the supported launch options:

```bash
dotnet run --project samples/dotnet/Sasd.Editor.FirstEd.Sample -- --help
```

The `--help` option also works when input or output is redirected. The editor
modes require a real interactive terminal.

## Suggested first session

The `--demo` mode uses a genuine `EditorSession` and a normal editable
document; it is not a mock-up. It starts without a filename, without disk I/O,
and with an empty Undo history.

Try this short walkthrough:

1. Move with the arrow keys, Home, End, Page Up, and Page Down.
2. Type some text and press **Escape** to undo the last editing transaction.
3. Press **Ctrl-Q**, then **F**, enter `WIDGET`, and press Enter to search.
4. Press **Ctrl-K**, then **B** to mark the beginning of a whole-line block.
   Move the cursor, press **Ctrl-K**, then **K** to mark the end, and use
   **Ctrl-K**, then **C** to copy it.
5. Press **Ctrl-K**, then **1** to set marker 1; use **Ctrl-Q**, then **1** to
   return to it.
6. In a sufficiently tall terminal, press **Ctrl-O**, then **O** to create a
   stacked window.
7. Press **F10** to open the MicroStar-inspired sample menu. Navigate with the
   arrow keys and confirm with Enter, or use the displayed command letter.
8. Press **Ctrl-K**, then **X**, and enter `YES` to leave the editor.

The demo includes a stored ESC control character followed by `[2J`. The
renderer must display it harmlessly as caret notation; it must never execute it
as a terminal clear-screen sequence.

## Essential commands

FIRST-ED commands use sequential control-key prefixes. For example,
**Ctrl-K, X** means press Ctrl-K, release it, and then press X.

| Keys | Action |
| --- | --- |
| Arrow keys, Home, End, Page Up, Page Down | Modern navigation aliases |
| Enter | New line or overtype-mode movement |
| Ctrl-N | Insert line |
| Escape | Undo |
| Ctrl-Q, F | Find |
| Ctrl-L | Find next occurrence |
| Ctrl-K, B / Ctrl-K, K | Set block beginning / end |
| Ctrl-K, C / Ctrl-K, V / Ctrl-K, Y | Copy / move / delete block |
| Ctrl-K, 1..9 / Ctrl-Q, 1..9 | Set / visit a numbered marker |
| Ctrl-O, O / Ctrl-O, Y | Create / delete a window |
| Ctrl-O, J | Link two windows to one document |
| Ctrl-K, S | Save the current document |
| Ctrl-K, W / Ctrl-K, R | Write / read a file |
| Ctrl-U | Request cancellation or abort where supported |
| F10 | Open the MicroStar-inspired host menu |
| Ctrl-K, X | Exit after `YES` confirmation |

See the complete
[FIRST-ED command map](../../docs/en/FIRST-ED-COMMAND-MAP.md) for all primary,
Ctrl-K, Ctrl-O, and Ctrl-Q bindings.

## File-format warning

The console sample deliberately exercises the historical FIRST-ED 8-bit file
codec. In particular, it preserves the `0x8D` wrapped-line marker. This is
different from the modern UTF-8 persistence API exposed by
`FileTextStorage`.

When experimenting with **Save**, **Write File**, block export, or the F10
file commands:

- use disposable test files;
- do not assume that arbitrary Unicode text can be represented by the legacy
  codec;
- remember that **Write File** exports without associating the document with
  the target path;
- remember that **Save** associates an unnamed document with the selected path
  and subsequently saves directly to it;
- review every overwrite confirmation before accepting it.

The `--demo` document itself is not written automatically.

## Architecture

The sample follows the same host-neutral pipeline expected of another terminal
or graphical application:

```text
ConsoleKeyInfo
    -> ConsoleKeyTranslator
    -> EditorKeyStroke
    -> editor-owned typeahead buffer
    -> FirstEdKeyMap
    -> semantic EditorCommandRequest
    -> EditorCommandDispatcher
    -> EditorSession and editor services
    -> viewport/status projection
    -> ConsoleFirstEdRenderer
```

Important boundaries:

- `Sasd.Editor.Core` owns documents, windows, editing services, semantic
  commands, Undo, topology, scheduling, and persistence abstractions.
- The sample owns `Console` access, terminal key translation, prompts, colors,
  cursor placement, safe row projection, and application menus.
- Prompt collection is separate from command mapping. The key map declares
  required arguments but never displays a prompt.
- Both keyboard input and injected macro/typeahead input pass through the same
  normalized editor pipeline.
- MicroStar-specific menus, file browsing, block-file commands, and print-to-file
  workflows remain sample-host features; they are not dependencies of the
  reusable core.
- Multiple windows may share one document while retaining independent cursor and
  viewport state.

Do not reference the sample executable as though it were a reusable library.
Applications should reference
[`Sasd.Editor.Core`](../../src/dotnet/Sasd.Editor.Core/) and implement their
own host adapter.

## Project guide

The most useful starting points in
[`Sasd.Editor.FirstEd.Sample`](./Sasd.Editor.FirstEd.Sample/) are:

| File or group | Responsibility |
| --- | --- |
| `Program.cs` | Parses startup mode, creates the public editor session, and starts the editor system loop. |
| `FirstEdSampleStartup.cs` | Defines and parses the small command-line contract. |
| `FirstEdDemoScenario.cs` | Creates the safe, editable in-memory demonstration. |
| `ConsoleFirstEdHost.cs` | Coordinates input, prompts, semantic dispatch, menus, file actions, and background printing. |
| `ConsoleKeyTranslator.cs` | Converts platform console events into host-neutral editor key strokes. |
| `ConsoleFirstEdPromptService.cs` and prompt policies | Resolve terminal answers into typed semantic command arguments. |
| `ConsoleFirstEdRenderer.cs` | Renders stacked windows, status rows, prompts, and the active caret. |
| `ConsoleFirstEdRowProjection.cs` | Projects controls and Unicode safely to physical terminal rows. |
| `ConsoleMicroStar*.cs` and `MicroStar*.cs` | Implement sample-only menus, file browsing, block-file operations, and cooperative printing. |

All sample types are kept internal unless a genuine host-facing public contract
is required. Public reusable contracts belong in the core project.

## Build and test

Restore, build, and test the complete .NET reference implementation:

```bash
dotnet restore Sasd.Editor.Toolbox.slnx
dotnet build Sasd.Editor.Toolbox.slnx --configuration Release --no-restore
dotnet test Sasd.Editor.Toolbox.slnx --configuration Release --no-build
```

Validate the repository-wide, language-neutral layout and metadata:

```bash
python3 tools/verify_repository_contract.py
```

The automated tests cover the sample's headless policies and its integration
with real semantic editor commands. They cannot certify physical terminal key
delivery, font cell widths, colors, or interactive cursor placement. Those
areas still require manual smoke testing on representative terminals.

## Adding another .NET sample

A new sample should:

1. reference `src/dotnet/Sasd.Editor.Core` rather than another sample;
2. keep UI-framework and platform APIs outside the core;
3. reuse `EditorSession`, semantic command dispatch, and public persistence
   abstractions instead of implementing another editor engine;
4. keep prompts, dialogs, rendering, and application-specific workflows in the
   host;
5. add deterministic headless tests for host policies wherever practical;
6. document whether a behavior is historical compatibility, modern host policy,
   or application-specific behavior;
7. be added to `Sasd.Editor.Toolbox.slnx` only when it is intended to take
   part in the normal .NET build and test gate.

## Further reading

- [Interactive FIRST-ED terminal host](../../docs/en/INTERACTIVE-FIRST-ED-HOST.md)
- [Developer demo and public API acceptance](../../docs/en/DEMO-EDITOR-AND-PUBLIC-API-ACCEPTANCE.md)
- [FIRST-ED command map](../../docs/en/FIRST-ED-COMMAND-MAP.md)
- [Architecture](../../docs/en/ARCHITECTURE.md)
- [Language-neutral V1 contract](../../spec/editor-v1.md)
- [Repository contribution instructions](../../AGENTS.md)

## Status

This sample is an executable developer reference and acceptance aid for the
current .NET implementation. It does not by itself claim complete historical
parity, final manual terminal acceptance, or a released V1.0 package.
