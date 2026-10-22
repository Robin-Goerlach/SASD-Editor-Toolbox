# MicroStar browser F5/F6 file actions

The navigable F10 → L browser now exposes **F5 Copy** and **F6 Rename**.
These shortcuts are a modern SASD host policy, not claimed historical
MicroStar scan codes.

The browser selects a **regular file** by its full filesystem identity,
not the sanitized display caption. The host requests an explicit new
destination path; a blank/cancelled prompt performs no action. Relative
paths are resolved against the current browser directory. The separate
`MicroStarFileOperations` service performs the operation with
`overwrite: false`, so existing targets cannot be silently replaced.
After success the browser refreshes its read-only directory snapshot.

Only the browser owns F5/F6; the reusable editor Core and Undo are
unchanged. The UI displays failures rather than assuming success.
Directories, modified function keys and missing selections are ignored.
Additional headless tests cover action eligibility, destination resolution
and refreshed listings after a real copy.

Directory operations, destructive delete and atomic cross-volume
move are intentionally NOT included.
