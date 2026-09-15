# Interactive FIRST-ED terminal host

## Purpose

The original FIRST-ED program is intentionally tiny because it initializes the Toolbox and then hands control to the Toolbox editor loop. The handbook describes an initial `NONAME` document in Window 1, a status line containing file name plus line/column, WordStar-like control commands, Escape as Undo, and Ctrl-K X as the exit path.

The .NET sample now follows the same architectural idea without importing DOS terminal assumptions into `Sasd.Editor.Core`.

## Host pipeline

```text
ConsoleKeyInfo
    -> ConsoleKeyTranslator
    -> EditorKeyStroke
    -> FirstEdKeyMap
    -> EditorInputAction / EditorCommandBinding
    -> ConsoleFirstEdPromptService (when required)
    -> EditorCommandDispatcher
    -> EditorSession / services
    -> EditorViewportBuilder
    -> ConsoleFirstEdRenderer
```

`ConsoleFirstEdHost` implements `IEditorInputPump`, so `EditorSystemLoop` remains the owner of the main loop. The terminal adapter only supplies one input unit at a time. When no key is waiting, it returns control to the cooperative scheduler.

## Prompt ownership

Prompts are deliberately a host concern. The core key map declares `Number`, `TwoNumbers`, `Text`, `FindReplace`, `Character`, `FilePath` or `Confirmation`; the terminal host turns that metadata into `Console.ReadLine` / `Console.ReadKey` interactions.

For Ctrl-K X, the host requires the word `YES` before dispatching `EditorCommandId.Exit`. This mirrors the handbook's `EditCpExit`/`EditExit` separation while keeping UI code out of the core.

A blank Find prompt is translated to `FindAgain`, preserving the remembered-search behavior already implemented in the core.

## Status and rendering

On startup the sample creates one empty unnamed document, so the status line begins with Window 1 / `NONAME`. The renderer shows one-based line and column values and current insert, word-wrap, auto-indent and dirty state.

Modern arrow, Home, End, Page Up and Page Down keys remain aliases because the core input model already supports them. Historical Ctrl-K / Ctrl-O / Ctrl-Q prefix sequences remain available.

## Terminal limitations

Console and terminal APIs do not expose every control-key combination identically on every operating system or terminal emulator. The adapter therefore normalizes what the host actually reports and keeps the core independent of those differences. On platforms that allow it, Ctrl-C is configured as ordinary input so the historical mapping can receive it.

## Still pending

The sample is now genuinely interactive, but it renders the **current window** rather than the historical vertically stacked physical window layout. `CreateWindow` already creates a logical editor window; exact screen-row splitting/compression and simultaneous multi-window rendering remain the next host/layout compatibility step.
