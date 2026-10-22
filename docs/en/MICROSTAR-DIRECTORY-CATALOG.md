# MicroStar read-only directory catalog

`MicroStarDirectoryCatalog` is a separate, host-local **read-only**
filesystem discovery service that prepares the future file browser.

It snapshots the immediate children of a requested directory (no recursive
walk); directories sort before files and names are sorted deterministically.
Each entry carries the full path as its stable identity, its basename as
display text and a directory flag. `CreateList` adapts this data to
`MicroStarSelectionList`, keeping selection and scroll state separate
from filesystem operations.

Invalid or inaccessible directories report errors instead of appearing
empty. Nothing is created, overwritten, renamed or deleted by this
component. Temporary-directory tests exercise sorting, directory depth,
stable list IDs, missing paths and empty folders on Windows and Linux.

This is a foundation only: a complete interactive browser, constrained
filesystem action interface, confirmation policy and user-facing errors
remain to be implemented.
