# Staged document replacement — reusable Core

`EditorFileService.OpenReplacingCurrentWindowAsync` implements the
non-destructive **load-validate-stage-commit** half of MicroStar file
switching. This is distinct from Read File, which inserts lines and
creates Undo history.

The initiating window/document, dirty state, version and complete lines
are captured before the asynchronous codec read. All decoded data is
validated before a detached `EditorDocument` is created. On completion,
the source snapshot and window attachment are rechecked; a stale or
failed load leaves the original untouched. On success only the initiating
view is reattached. Existing linked views retain the original document;
session lifecycle rules own Undo/marker cleanup. The new document starts
at line 1/column 1, clean and associated with the selected path.

The `allowDiscardDirty` parameter is **explicit authorization from
the caller**, not a prompt or implied permission. By default dirty
documents are not replaced. Saving and affirmative discard confirmation
remain host responsibilities; the MicroStar menu workflow is not
wired by this commit.

New headless tests cover linked views, explicit dirty opt-in, corrupted
codec output, empty-file behavior and unchanged source on failure.
