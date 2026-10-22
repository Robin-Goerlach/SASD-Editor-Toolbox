# MicroStar print pagination (modern sample policy)

`MicroStarPrintPagination.Prepare` creates an immutable output sequence
before the cooperative print job is registered. Its default is **60 logical
text rows per page**, with a form-feed record between pages. Empty lines
count as rows; no trailing form feed is emitted for a full final page.
The print job still emits at most one record per scheduler idle slice.

This is an explicit modern sample policy, not proof of original
Borland/MicroStar PrintNext parity. Long-line wrapping, margins,
physical-device page eject, spooler integration and printer-specific
control sequences require separate design and acceptance.

Unit tests cover exact page boundaries, empty documents, empty lines,
input mutation and invalid settings.

## Form-feed file encoding

The print-to-file adapter writes pagination's stand-alone form-feed
record as a single `0x0C` byte, **without** an implicit newline.
Ordinary text rows still terminate using the host's normal line ending.
A cross-platform byte-level regression test validates the boundary on
Ubuntu and Windows. This remains a modern sample encoding rule rather
than a verified historic printer protocol. At present a document line
consisting solely of form feed is indistinguishable from a pagination
record in the string-based sink contract; introducing typed output
records remains future work.
