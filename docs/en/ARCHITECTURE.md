# Architecture

## Purpose

SASD Editor Toolbox is an embeddable editor kernel, not a single desktop editor. The architecture therefore separates **text**, **views**, **editing operations**, **commands**, **persistence**, **rendering**, **hooks** and **background work**.

This separation lets WPF, WinForms, terminal and web hosts reuse the same core without importing another UI framework.

## Dependency direction

```text
Host/UI
  |
  +--> Commands / key mapping
  |        |
  |        v
  +--> EditorSession --> EditorEngine --> ITextBuffer
  |        |                |
  |        |                +--> Undo
  |        +--> Search
  |        +--> Hooks
  |        +--> Scheduler
  |
  +--> ViewportBuilder --> platform-specific renderer
  |
  +--> ITextStorage --> file/database/cloud provider
```

## Text storage

The historical toolbox used linked line descriptors. The first SASD implementation keeps the line-oriented semantics and supplies `LinkedLineTextBuffer`, but callers only know `ITextBuffer`. A future piece table or rope can therefore replace the implementation without rewriting editing commands or hosts.

## Document and window model

`EditorDocument` owns text, persistence metadata, dirty state and version state. `EditorWindow` is a view onto a document and owns cursor/scroll/mode state. `EditorSession` coordinates all open windows.

This maps cleanly to the historical concept of multiple windows and linked text streams while removing screen-coordinate and DOS-memory assumptions.

## Editing engine

`EditorEngine` contains mutation/navigation primitives. It does not know which key was pressed. Keyboard conventions belong in a host/keymap layer.

## Undo

The first implementation uses snapshots because they are easy to reason about and test. It can later be replaced by operation deltas or a persistent text structure without changing host contracts.

## Hooks

`IEditorHooks` preserves the spirit of historical command, error, status, replace and background-task hooks through typed, async-compatible APIs.

## Rendering

Instead of writing to an 80x25 physical screen image, SASD exposes `EditorViewport`. The renderer decides fonts, colors, selection styling, cursor shape and actual screen coordinates.

## Multi-language architecture

Language-specific source trees are peers under `src/<language>`, with matching `tests/<language>` and `samples/<language>` lanes. Implementations do not call into one another. The shared `spec/` contract defines portable observable behavior, while `conformance/` holds implementation-neutral behavior cases that can be executed by language-specific test adapters.

The .NET implementation is the current executable reference because it is furthest along, not because its class layout or API names are normative. C++, Java and later ports should use idiomatic ownership, collections, error models and build systems while preserving the same editor invariants.

See [MULTI-LANGUAGE-DEVELOPMENT.md](MULTI-LANGUAGE-DEVELOPMENT.md) for source-of-truth, synchronization, CI and porting rules.

## Deliberately postponed

The next V1 steps include the historical WordStar-compatible key map and prefixed dispatcher, a full interactive FIRST-ED-style host, remaining compatibility details, richer search, MicroStar-style samples, and performance backends such as a piece table or rope.
