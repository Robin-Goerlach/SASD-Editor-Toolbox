# MicroStar safe file copy and rename

The terminal sample now has a dedicated `MicroStarFileOperations` service
with two explicit operations: `Copy(source, destination)` and
`Rename(source, destination)`. This is **application filesystem behavior**,
not reusable Editor Core or editor Undo.

Both require an existing regular-file source and different paths. The
destination must be new: the service uses the operating system's
`File.Copy(..., overwrite: false)` and `File.Move(..., overwrite: false)`
to reject an existing target at the point of mutation. No overwrite
confirmation can weaken that invariant.

The service does not recursively manipulate directories, silently emulate
cross-volume moves, or guarantee atomicity against concurrent filesystem
changes. Native exceptions are surfaced to the calling host.

Headless integration tests cover copy, rename, protected destinations,
missing files, self-targeting and rejecting directory sources on Linux
and Windows. File-manager UI integration is a separate next step.
