# Typed error and resource model

## Purpose

The editor core must report failures without deciding how a terminal, desktop application, web host or future C++/Java implementation displays them. V1 therefore uses a small typed error contract instead of copying the historical numeric/message catalog.

This is a modernization boundary: the handbook establishes that several commands report errors, but the original wording and numeric catalog are presentation artifacts rather than portable editor behavior.

## Error contract

The .NET reference implementation exposes `EditorError` through `IEditorHooks.OnErrorAsync`. Every catalog-backed error carries:

- `Id` — a typed semantic `EditorErrorId`;
- `Code` — a stable machine-readable code for logs, automation and conformance tests;
- `ResourceKey` — a stable lookup key that a host can localize;
- `Message` — an English fallback diagnostic, not normative UI wording;
- optional `DetailCode` and `ParameterName`;
- the original `Exception` when one exists, for diagnostics rather than blind user display.

The catalog currently defines these portable categories:

| ID | Machine code | Resource key |
|---|---|---|
| `InvalidCommandArgument` | `EDITOR_COMMAND_INVALID_ARGUMENT` | `Editor.Errors.Command.InvalidArgument` |
| `CommandRejected` | `EDITOR_COMMAND_REJECTED` | `Editor.Errors.Command.Rejected` |
| `OperationFailed` | `EDITOR_OPERATION_FAILED` | `Editor.Errors.Operation.Failed` |
| `FileIoFailed` | `EDITOR_FILE_IO_FAILED` | `Editor.Errors.File.IoFailed` |
| `AccessDenied` | `EDITOR_ACCESS_DENIED` | `Editor.Errors.Access.Denied` |
| `UnsupportedOperation` | `EDITOR_OPERATION_UNSUPPORTED` | `Editor.Errors.Operation.Unsupported` |
| `TypeaheadOverflow` | `EDITOR_TYPEAHEAD_OVERFLOW` | `Editor.Errors.Input.TypeaheadOverflow` |
| `OperationAborted` | `EDITOR_OPERATION_ABORTED` | `Editor.Errors.Operation.Aborted` |

## Command failures

Request-shape/range failures are converted from `EditorCommandValidationFailure` into `InvalidCommandArgument`. The validation category and request field remain available as detail metadata.

State-dependent compatibility failures that the handbook describes as actual errors use `CommandRejected`. The first audited examples are Create Window when the donor cannot be compressed, linking a window to itself/already shared text, deleting the only displayed window, and an impossible left/right margin change.

Ordinary navigation no-ops remain normal `false` results rather than becoming errors. Reaching the beginning of a file, for example, is not equivalent to a command rejection.

## Operational exceptions

The dispatcher maps expected exception families without leaking their text into the UI contract:

- `IOException` and `EncoderFallbackException` -> `FileIoFailed`;
- `UnauthorizedAccessException` and `SecurityException` -> `AccessDenied`;
- `NotSupportedException` -> `UnsupportedOperation`;
- residual `ArgumentException` -> `InvalidCommandArgument`;
- `InvalidOperationException` -> `OperationFailed`.

Cancellation is intentionally not converted here. `OperationCanceledException` remains control flow for `CancellationToken`. The completed interruptibility bridge treats historical host Ctrl-U separately: `EditorOperationAbortedException` is caught by the semantic dispatcher and reported as `EditorErrorId.OperationAborted`, while external cancellation continues to propagate to the caller.

## Input errors

Typeahead overflow and host-originated Ctrl-U now use the same error catalog as command failures. The terminal sample sends those errors through the normal hook and then decides how to render them. The core typeahead buffer remains host-independent.

## Localization and diagnostics

Hosts should prefer `ResourceKey` for localized presentation and may fall back to `Message`. Logs and automation should prefer `Code` and `Id`.

The original exception remains attached for diagnostics. A host should not automatically display `Exception.Message`, because file and platform exceptions can contain paths or environment details that should not become user-facing contract text.

## Cross-language conformance

Shared conformance cases may assert an error `id`, `code` and `resourceKey`. The .NET adapter captures errors through `IEditorHooks`; C++ and Java should map the same portable values to idiomatic native enums/resources rather than reproducing the C# type layout.

This keeps error semantics portable while allowing each host and language to choose its own presentation and localization mechanism.

## Optional host-owned message overrides (modern replacement)

The FIRST-ED console presentation adapter accepts an optional read-only
mapping from **stable `ResourceKey` to a one-line display message**. This is
an explicit injection point for future localization or configuration; it
does not parse, load or claim binary/text compatibility with Borland's
`EDITERR.MSG`. The semantic `EditorErrorId` and `Code` are unchanged.
A missing, empty, or terminal-control-containing translation falls back
to the safe English catalog message. Encoder failures use a specific safe
diagnostic instead of a generic translated I/O message, and exception
messages are never used as display resources. Loading an external resource
file or choosing a language remains a separate host responsibility.

Covered by `ConsoleFirstEdErrorPresentationTests` for normal overrides,
missing entries, unsafe control characters and unchanged error identity.
This is not historical numeric-message-file parity or a completed release
sign-off for HP-02.

## HP-02 implementation verification

The optional UTF-8 JSON resource file is now wired through the FIRST-ED
host's real error hook at startup. The headless integration test
`LoadedResourceFlowsThroughActualHostHookAndKeepsFallback` verifies that a
loaded German message reaches the pending terminal message and that another
untranslated error retains its typed English fallback. It also verifies that
consuming a pending message clears it, without changing the editor core.
The separate HP-02 decision register chooses this **modern substitute** for
V1; it does not imply compatibility with original `EDITERR.MSG` files.
