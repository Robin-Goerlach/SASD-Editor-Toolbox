# MicroStar file delete — explicit approval

The MicroStar read-block file browser now offers **F8 Delete** for regular
files. Pressing F8 never deletes anything immediately. The host asks the user
to type the exact token `DELETE`; Enter, Escape, blank and every other
response cancel without invoking the mutation service.

`MicroStarFileOperations.Delete` is a separate application service, not an
editor command or an Undo transaction. It validates that the target is an
ordinary file, rejecting directories and reparse points/symbolic links.
After a successful operation the directory listing refreshes. Failures are
reported and never silently treated as success.

**Limitations:** deletion is permanent rather than a recycle-bin move; there
is no cross-process atomicity guarantee between checking attributes and
deletion. The user must not assume that a confirmation alone protects against
concurrent renames or hostile filesystem changes. Handle-based race-resistant
operations and interactive terminal acceptance tests remain future work.

New cross-platform temporary-file tests exercise single-file deletion,
directory rejection and missing targets. The existing F5/F6 behavior is
preserved and the editor Core remains UI- and filesystem-manager-neutral.
