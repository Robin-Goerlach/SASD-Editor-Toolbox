# .NET implementation lane

This directory contains the active **C#/.NET reference implementation** of the SASD Editor Toolbox.

> **Status:** active reference implementation  
> **Target framework:** .NET 10 (`net10.0`)  
> **Reusable library:** `Sasd.Editor.Core`  
> **Portable behavior contract:** [`spec/editor-v1.md`](../../spec/editor-v1.md)

The .NET implementation is currently the most complete executable implementation in the repository. It is used to develop, exercise and verify the editor architecture, but it is **not** the language-neutral specification. Portable observable behavior belongs in `spec/` and shared conformance cases belong in `conformance/`.

## Purpose of this directory

`src/dotnet` contains **production source code only** for the .NET implementation lane.

The reusable editor core is intentionally independent from a concrete user interface. It is designed to be embedded in different hosts without making the editing engine depend on terminal, WinForms, WPF, ASP.NET, browser or other presentation APIs.

The corresponding .NET lane is split across three repository roots:

```text
src/dotnet/       reusable production code
tests/dotnet/     .NET regression and conformance tests
samples/dotnet/   executable reference/sample hosts
```

This separation is important. A host may depend on the Core; the Core must never depend on a host.

## Current layout

```text
src/dotnet/
└── Sasd.Editor.Core/
    ├── Abstractions/
    ├── Commands/
    ├── Document/
    ├── Editing/
    ├── Hooks/
    ├── IO/
    ├── Input/
    ├── Model/
    ├── Properties/
    ├── Rendering/
    ├── Scheduling/
    ├── Search/
    ├── Undo/
    ├── Windows/
    └── Sasd.Editor.Core.csproj
```

### Core areas

| Area | Responsibility |
| --- | --- |
| `Abstractions` | Small reusable contracts such as the text-buffer abstraction. |
| `Commands` | Semantic editor commands, command validation/dispatch and FIRST-ED compatibility command processors. |
| `Document` | Document state, snapshots and the current linked-line text-buffer implementation. |
| `Editing` | General editing operations, editor sessions, structural line topology and compatibility editing behavior. |
| `Hooks` | Host-neutral extension/error/status integration points inspired by the historical toolbox hooks. |
| `IO` | Modern storage abstractions, file services and the isolated historical FIRST-ED file codec. |
| `Input` | Normalized key strokes, typeahead handling and FIRST-ED key mapping. |
| `Model` | UI-neutral value/state models such as positions, markers, blocks and line flags. |
| `Rendering` | Host-neutral viewport projection. Rendering itself remains the host's responsibility. |
| `Scheduling` | Cooperative scheduling, input pumping and editor interruption/abort support. |
| `Search` | Find/replace behavior and search options. |
| `Undo` | Transactional editor undo state and restoration. |
| `Windows` | Editor views/windows, view options and host-neutral stacked-window layout. |

The directory names express editor responsibilities rather than UI technologies. New production code should normally fit one of these responsibilities before a new top-level Core area is introduced.

## Architectural boundaries

### The Core is UI-agnostic

`Sasd.Editor.Core` must not take dependencies on concrete presentation frameworks or directly perform user interaction.

In particular, do not put the following into the Core:

- `Console`-driven prompting or terminal rendering;
- WinForms controls/forms;
- WPF windows or controls;
- ASP.NET/browser-specific APIs;
- platform-specific input event types;
- dialogs, message boxes or other host-owned UI;
- sample-only behavior that is not reusable editor behavior.

A terminal, WinForms, WPF, web or future MAUI host should translate its native input into editor concepts, invoke the public Core API and render the resulting editor state.

### Documents and windows are different concepts

Text belongs to an `EditorDocument`.

An `EditorWindow` is a view over a document and owns view-specific state such as cursor position, viewport and editing options. Multiple windows may therefore refer to the same document.

Do not merge document identity and window identity merely because a simple host happens to display one document in one window.

### General editing and historical compatibility stay separated

The repository uses Borland's historical **Turbo Editor Toolbox 1.0 / FIRST-ED** behavior as a clean-room V1 compatibility/reference target.

Modern reusable editing behavior belongs in general services such as `EditorEngine`. Unusual historical behavior is isolated in explicitly named compatibility components such as the FIRST-ED processors, key map and legacy file codec.

This prevents DOS-era or FIRST-ED-specific conventions from becoming accidental requirements for modern editor hosts.

### Structural changes use topology realignment

Operations that insert, remove, join or otherwise restructure logical lines can affect much more than text. They may also affect:

- cursors in linked windows;
- top-line viewport anchors;
- markers;
- block boundaries;
- undo/navigation state.

`EditorLineTopology` is the coordination boundary for this class of changes. New structural operations should audit these relationships explicitly instead of repairing line numbers ad hoc in unrelated commands.

### Modern storage and legacy compatibility are separate

Modern document persistence uses the storage abstraction around `ITextStorage` / `FileTextStorage`.

Historical byte-level FIRST-ED compatibility is isolated behind `IEditorFileCodec` / `FirstEdLegacyFileCodec`.

The legacy format must not become the default storage model for modern applications.

### Input is normalized before command dispatch

The intended host-neutral flow is conceptually:

```text
native host input
    ↓
EditorKeyStroke
    ↓
EditorTypeaheadBuffer
    ↓
key mapping / FirstEdKeyMap
    ↓
EditorCommandRequest
    ↓
EditorCommandDispatcher
    ↓
editor services
```

A host is responsible for translating platform input into normalized editor input. The Core does not need to know whether the original event came from a terminal, WinForms, WPF, a test script or another input source.

### Scheduling is cooperative

The scheduling layer preserves the historical idea of short, cooperative background work while modernizing the host boundary.

Long-running work must remain interruptible and must not silently block the editor loop. Platform cancellation and the historical editor abort concept are intentionally modeled as separate concerns.

## Relationship to the multi-language project

SASD Editor Toolbox is designed as one behavioral product contract with independent language implementations.

The current implementation lanes are:

```text
src/dotnet/   active C#/.NET reference implementation
src/cpp/      C++ implementation scaffold
src/java/     Java implementation scaffold
```

The .NET implementation may be used as an executable reference when developing another language port, but another implementation should **not** mechanically translate the C# class hierarchy and must never depend on the .NET binary at runtime.

The cross-language rule is:

> **Share behavior, not implementation.**

When behavior is intended to be portable, establish it in [`spec/editor-v1.md`](../../spec/editor-v1.md) and, where practical, represent it in the shared [conformance cases](../../conformance/README.md).

## V1 compatibility scope

The V1 milestone uses the functional behavior described by the historical Turbo Editor Toolbox 1.0 documentation as a compatibility/reference set.

This project is a **clean-room reimplementation**:

- historical documentation may be used to identify observable behavior;
- behavior is described and implemented independently;
- historical source code is not copied or transliterated;
- DOS implementation details are replaced with modern abstractions where they are not part of observable editor behavior;
- historical names are used only where they are useful for compatibility traceability.

The .NET lane currently contains the broadest implementation and regression coverage, but “active reference” does not mean that every historical behavior has automatically passed the final V1 release-readiness review.

See the repository compatibility documentation for the current acceptance status.

## Building and testing

The canonical .NET build uses the repository solution from the repository root.

Prerequisite:

- .NET 10 SDK

Run:

```bash
dotnet restore Sasd.Editor.Toolbox.slnx

dotnet build Sasd.Editor.Toolbox.slnx \
  --configuration Release \
  --no-restore

dotnet test Sasd.Editor.Toolbox.slnx \
  --configuration Release \
  --no-build
```

The solution currently includes:

- `src/dotnet/Sasd.Editor.Core/Sasd.Editor.Core.csproj`
- `tests/dotnet/Sasd.Editor.Core.Tests/Sasd.Editor.Core.Tests.csproj`
- `samples/dotnet/Sasd.Editor.FirstEd.Sample/Sasd.Editor.FirstEd.Sample.csproj`

Repository-wide structural and conformance metadata can be validated separately:

```bash
python3 tools/verify_repository_contract.py
```

## Running the FIRST-ED reference sample

The current .NET sample is a terminal-based host built on the public Core API.

From the repository root:

```bash
dotnet run --project samples/dotnet/Sasd.Editor.FirstEd.Sample/Sasd.Editor.FirstEd.Sample.csproj
```

For the non-destructive developer demonstration:

```bash
dotnet run --project samples/dotnet/Sasd.Editor.FirstEd.Sample -- --demo
```

For available launch options:

```bash
dotnet run --project samples/dotnet/Sasd.Editor.FirstEd.Sample -- --help
```

The sample is deliberately outside `src/dotnet`: it demonstrates how a host consumes the Core without making terminal behavior a Core dependency.

## Where new work belongs

Use this rule of thumb when adding .NET code:

| Change | Location |
| --- | --- |
| Reusable editor behavior | `src/dotnet/Sasd.Editor.Core/` |
| .NET regression test | `tests/dotnet/Sasd.Editor.Core.Tests/` |
| Shared portable behavior | `spec/` |
| Shared machine-readable example | `conformance/` |
| Terminal/demo/host behavior | `samples/dotnet/` |
| Architecture or compatibility explanation | `docs/en/` and matching `docs/de/` document where applicable |
| Repository-wide development rules | `AGENTS.md` |

For a portable semantic change, the preferred order is:

1. clarify the intended observable behavior;
2. update the portable specification when the behavior is cross-language;
3. add or update a shared conformance case when practical;
4. add deterministic .NET regression coverage;
5. implement the behavior in the Core;
6. update relevant English and German documentation;
7. run the repository contract check and the full .NET Release build/test gate.

## Development principles

When working in this lane:

- keep production code, APIs and identifiers in English;
- keep nullable reference types and normal modern C# safety expectations intact;
- add XML documentation to public APIs when the contract is not self-evident;
- write comments that explain **why**, invariants, compatibility constraints and trade-offs rather than restating syntax;
- prefer clear correctness-first implementations over speculative optimization;
- keep UI and platform concerns outside the reusable Core;
- preserve document/window separation;
- audit topology, markers, blocks and undo whenever structural text changes;
- preserve valid virtual cursor columns unless an operation explicitly changes them;
- keep modern persistence independent from the historical codec;
- add deterministic, headless regression tests for behavioral changes;
- do not weaken tests merely to match an accidental implementation detail.

## Related locations

- [Repository overview](../../README.md)
- [Portable V1 editor contract](../../spec/editor-v1.md)
- [Shared conformance cases](../../conformance/README.md)
- [.NET tests](../../tests/dotnet/Sasd.Editor.Core.Tests)
- [.NET FIRST-ED sample](../../samples/dotnet/Sasd.Editor.FirstEd.Sample)
- [Architecture documentation](../../docs/en/ARCHITECTURE.md)
- [Multi-language development](../../docs/en/MULTI-LANGUAGE-DEVELOPMENT.md)
- [Borland V1 compatibility status](../../docs/en/BORLAND-V1-COMPATIBILITY.md)
- [Repository development rules](../../AGENTS.md)

## Summary

`src/dotnet` is the production home of the .NET implementation of SASD Editor Toolbox.

Its job is not to provide a particular editor window. Its job is to provide the reusable, testable editor machinery that many different windows and hosts can build upon.

That distinction is central to the project: **the editing engine belongs here; the user interface belongs around it.**
