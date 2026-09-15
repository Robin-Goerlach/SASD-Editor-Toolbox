# SASD Editor Toolbox

**SASD Editor Toolbox** is a reusable, UI-agnostic editing foundation for SASD applications and tools. The first implementation is written in C#/.NET and is structured so equivalent implementations can later be added without redesigning the repository.

The long-term goal is broader than a stand-alone text editor: the toolbox should provide reusable document, buffer, command, search, block, window, undo, rendering-model and extension infrastructure that can be embedded in SASD Workbench applications.

## V1 target: Turbo Editor Toolbox compatibility set

Version 1 uses the functional scope of Borland's historical **Turbo Editor Toolbox 1.0 (1985)** as a compatibility/reference milestone. This is a **clean-room reimplementation**: the historical handbook is treated as a behavioral requirements source; historical source code is not copied.

The historical design is especially useful because it separates the editor into text streams, windows, command dispatchers/processors, hooks, file operations, screen updating and a cooperative background task mechanism. SASD keeps those responsibilities, but exposes them through modern, testable abstractions rather than DOS/video-memory-specific code.

## Repository architecture

```text
SASD-Editor-Toolbox/
├── src/
│   └── dotnet/
│       └── Sasd.Editor.Core/          # first implementation
├── tests/
│   └── dotnet/
├── samples/
│   └── dotnet/
│       └── Sasd.Editor.FirstEd.Sample/
├── spec/                              # language-neutral contracts
├── docs/
│   ├── en/                            # primary documentation
│   └── de/                            # German documentation
└── .github/workflows/
```

Future language implementations should be peers of `dotnet` (for example `cpp`, `java`, `javascript`). Language-neutral behavior belongs in `spec/`.

## Implemented foundation

The current first milestone already contains:

- a line-oriented text-buffer abstraction with a linked-line implementation;
- document and multi-window/session models, including linked windows over one document;
- cursor movement, insertion/overtype, newline, auto-indent, word-wrap and tab handling;
- deletion commands, change-case, centering and paragraph reformatting;
- whole-line block begin/end/copy/move/delete/hide operations;
- numbered markers;
- snapshot-based undo with a replaceable undo boundary;
- literal find/replace services with case, whole-word and wrap options;
- asynchronous file load/save through a storage abstraction;
- command requests and a dispatcher separated from editing primitives;
- extension hooks inspired by the historical `UserCommand`, `UserError`, `UserStatusLine`, `UserReplace` and `UserTask` integration points;
- a cooperative idle scheduler for background work;
- a UI-neutral viewport/status model rather than direct screen-memory access;
- English and German architecture/compatibility documentation;
- automated unit-test scaffolding and a small FIRST-ED-style sample.

See [`docs/en/BORLAND-V1-COMPATIBILITY.md`](docs/en/BORLAND-V1-COMPATIBILITY.md) for the V1 matrix.

## Build

```bash
dotnet restore Sasd.Editor.Toolbox.slnx
dotnet build Sasd.Editor.Toolbox.slnx --configuration Release
dotnet test Sasd.Editor.Toolbox.slnx --configuration Release
```

Target framework: **.NET 10**.

## Design rules

- Core editing logic has no WinForms, WPF, console, terminal or browser dependency.
- A document and its views/windows are separate concepts; multiple windows may share one document.
- Mutation APIs own dirty-state and undo integration.
- Historical names are documented as compatibility references, not copied as a 1:1 public API.
- Platform-specific rendering and keyboard mapping stay outside the core library.
- Expensive optimizations (rope/piece-table buffers, SIMD search, native backends) are introduced only behind stable interfaces and after profiling.
- The first implementation favors correctness, reviewability and tests over premature micro-optimization.

## Documentation

- English architecture: [`docs/en/ARCHITECTURE.md`](docs/en/ARCHITECTURE.md)
- Deutsche Architektur: [`docs/de/ARCHITECTURE.md`](docs/de/ARCHITECTURE.md)
- V1 compatibility matrix: [`docs/en/BORLAND-V1-COMPATIBILITY.md`](docs/en/BORLAND-V1-COMPATIBILITY.md)
- Deutsche V1-Matrix: [`docs/de/BORLAND-V1-KOMPATIBILITAET.md`](docs/de/BORLAND-V1-KOMPATIBILITAET.md)
- Language-neutral V1 contract: [`spec/editor-v1.md`](spec/editor-v1.md)

## License

MIT. See [`LICENSE`](LICENSE).
