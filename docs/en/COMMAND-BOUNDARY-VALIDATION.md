# Command-boundary validation

## Purpose

Semantic commands form a trust boundary between hosts/automation and the reusable editor core. Keyboard prompts, menus, tests and future C++/Java adapters can all construct requests, so malformed arguments must be rejected consistently before mutation or file I/O.

The .NET reference implementation centralizes request-shape validation in `EditorCommandValidator`. Stateful compatibility rules remain in the service that owns the relevant state.

## Stable request rules

| Command family | Boundary rule |
|---|---|
| Goto line / column / window | one-based numeric value >= 1 |
| Delete Window with explicit number | window number >= 1; omitted number means current window |
| Create Window | size >= 3 and donor window number >= 1 |
| Link Window | destination and source numbers >= 1 |
| Left/right margin prompts | numeric value >= 1 before stateful margin-order checking |
| Tab width | >= 1 |
| Undo limit compatibility command | >= 1 |
| Set/Jump marker | marker number 1..20 |
| Display-dependent commands | supplied page size >= 1 |
| Insert control character | exactly one UTF-16 character in the .NET request |
| Find | non-empty pattern |
| Replace | non-empty pattern plus a replacement value; replacement may be empty |
| Read/Write file | non-blank resolved path |

Modern lower-level services may expose broader capabilities where that does not alter the compatibility command. In particular, the general undo manager may use a zero limit to disable capture even though the historical compatibility command rejects values below one.

## Replace argument modernization

The initial .NET dispatcher packed find and replacement strings into one value separated by NUL. That transport representation is unsuitable as a cross-language semantic contract.

`EditorCommandRequest` now exposes `Text2`. For `ReplaceNext`, `Text` is the pattern and `Text2` is the replacement. An empty replacement is valid and deletes the match.

The dispatcher still accepts the older NUL-packed payload as a .NET transition path so existing callers are not broken, but the terminal host no longer emits it and shared conformance cases never depend on it.

## Boundary versus stateful validation

The centralized validator checks only facts established by the request itself. It does not duplicate mutable editor state. Margin ordering, donor-window capacity, actual window resolution and whether a syntactically valid marker is currently defined remain with the services that own those states.

## Error behavior

A validation failure returns `false` from the dispatcher, performs no command mutation or file I/O, and is reported through `IEditorHooks.OnErrorAsync` with the host-neutral code `EDITOR_COMMAND_INVALID_ARGUMENT`.

`EditorCommandValidationFailure` carries a typed `EditorCommandValidationCode`, parameter name and English fallback diagnostic. The completed error-resource layer maps this into `EditorErrorId.InvalidCommandArgument`, the stable code `EDITOR_COMMAND_INVALID_ARGUMENT` and resource key `Editor.Errors.Command.InvalidArgument`; see [ERROR-MODEL.md](ERROR-MODEL.md).

## Tests and cross-language conformance

`CommandBoundaryValidationTests` covers missing/out-of-range numbers, page size, Create/Link arguments, marker limits, blank file paths, replace arguments, compatibility Undo Limit zero and control-character request shape.

Shared V1 conformance cases also cover invalid Goto Line and Undo Limit requests so later C++ and Java adapters can reproduce the same boundary result without depending on .NET exception types.
