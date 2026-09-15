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

The current foundation contains:

- a line-oriented text-buffer abstraction with a linked-line implementation;
- document and multi-window/session models, including linked windows over one document;
- cursor movement, insertion/overtype, newline, auto-indent, word-wrap and tab handling;
- historical FIRST-ED begin/end/goto, top/bottom-file and viewport-aware movement semantics isolated behind a compatibility processor;
- display-row-aware line scrolling and page movement supplied with a host-visible row count;
- deletion commands, change-case, centering and paragraph reformatting;
- whole-line block begin/end/copy/move/delete/hide operations plus block-boundary navigation;
- numbered markers;
- snapshot-based undo with a configurable limit and replaceable backend boundary;
- literal forward find/replace services plus remembered FIRST-ED Find Again state;
- modern asynchronous UTF-8 document persistence through `ITextStorage` / `FileTextStorage`;
- historical read/write command semantics through `EditorFileService`;
- a dedicated `FirstEdLegacyFileCodec` preserving the historical `0x8D` wrapped-line marker without imposing that format on modern storage;
- semantic command requests and a dispatcher separated from editing primitives;
- a UI-neutral key-stroke model plus the FIRST-ED Ctrl-K/Ctrl-O/Ctrl-Q and primary-key mapping contract;
- typed prompt metadata so keyboard mapping remains independent of UI prompting;
- window up/down/goto semantics and historical stream linking by reattaching an existing view to a shared document;
- an explicit rundown state corresponding to the historical `Rundown` flag;
- `EditorScheduler` and `EditorSystemLoop` implementing input-first cooperative scheduling and the repeated schedule-until-rundown main-loop model;
- `IEditorInputPump` as the host boundary for keyboard, terminal, scripted or other input sources;
- extension hooks inspired by the historical `UserCommand`, `UserError`, `UserStatusLine`, `UserReplace` and `UserTask` integration points;
- a UI-neutral viewport/status model rather than direct screen-memory access;
- English and German architecture/compatibility documentation;
- automated unit tests and a small FIRST-ED-style sample.

The command map is intentionally one layer ahead only where behavior still belongs to later host/layout work, especially physical window sizing and interactive prompt presentation. Search, file, lifecycle and viewport movement commands now have executable core processors behind their bindings.

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
- Historical input/value conventions live in the compatibility layer instead of leaking into the general text engine.
- Historical names are documented as compatibility references, not copied as a 1:1 public API.
- Platform-specific rendering stays outside the core; keyboard events are normalized before entering the compatibility key map.
- Key maps declare required prompt arguments but never display prompts themselves.
- Historical byte-level file compatibility is isolated behind a codec and does not replace modern UTF-8 storage.
- Scheduler/background work remains cooperative and bounded; platform input stays behind `IEditorInputPump`.
- Expensive optimizations (rope/piece-table buffers, SIMD search, native backends) are introduced only behind stable interfaces and after profiling.
- The first implementation favors correctness, reviewability and tests over premature micro-optimization.

## Documentation

- English architecture: [`docs/en/ARCHITECTURE.md`](docs/en/ARCHITECTURE.md)
- Deutsche Architektur: [`docs/de/ARCHITECTURE.md`](docs/de/ARCHITECTURE.md)
- V1 compatibility matrix: [`docs/en/BORLAND-V1-COMPATIBILITY.md`](docs/en/BORLAND-V1-COMPATIBILITY.md)
- Deutsche V1-Matrix: [`docs/de/BORLAND-V1-KOMPATIBILITAET.md`](docs/de/BORLAND-V1-KOMPATIBILITAET.md)
- FIRST-ED command map: [`docs/en/FIRST-ED-COMMAND-MAP.md`](docs/en/FIRST-ED-COMMAND-MAP.md)
- FIRST-ED-Befehle: [`docs/de/FIRST-ED-BEFEHLE.md`](docs/de/FIRST-ED-BEFEHLE.md)
- Command-processor transfer: [`docs/en/COMMAND-PROCESSOR-TRANSFER.md`](docs/en/COMMAND-PROCESSOR-TRANSFER.md)
- Command-Prozessor-Übertragung: [`docs/de/COMMAND-PROCESSOR-UEBERTRAGUNG.md`](docs/de/COMMAND-PROCESSOR-UEBERTRAGUNG.md)
- Search and file commands: [`docs/en/SEARCH-AND-FILE-COMMANDS.md`](docs/en/SEARCH-AND-FILE-COMMANDS.md)
- Suchen und Datei-Befehle: [`docs/de/SUCHEN-UND-DATEI-BEFEHLE.md`](docs/de/SUCHEN-UND-DATEI-BEFEHLE.md)
- Lifecycle and scrolling: [`docs/en/LIFECYCLE-AND-SCROLLING.md`](docs/en/LIFECYCLE-AND-SCROLLING.md)
- Lifecycle und Scrollen: [`docs/de/LIFECYCLE-UND-SCROLLEN.md`](docs/de/LIFECYCLE-UND-SCROLLEN.md)
- Language-neutral V1 contract: [`spec/editor-v1.md`](spec/editor-v1.md)

## License

MIT. See [`LICENSE`](LICENSE).
