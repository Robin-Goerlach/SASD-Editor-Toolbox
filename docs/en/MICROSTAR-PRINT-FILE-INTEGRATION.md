# MicroStar F10 print-to-file integration

The F10 sample menu now offers `H` (start print-to-file) and `K`
(cancel printing). This is a modern keyboard choice, not an assertion
about the original MicroStar scan codes.

The host obtains a **new output filename** and creates an exclusive UTF-8
stream with FileMode.CreateNew. Existing files are never overwritten.
The already-tested MicroStarPrintQueue snapshots the current editor
document; the input-first scheduler then writes at most one line per idle
cycle. An active job cannot be replaced by another.

The new MicroStarPrintFileSink closes output on job completion,
explicit cancellation or caught print failure. Partial files are kept and
must not be confused with a successfully printed complete document.
There is no device-specific printer/spooler support, page layout or verified
historical PrintNext parity yet.

Tests cover complete UTF-8 output, no overwrite and cancelling a partial
job. The editor Core remains independent of filesystem printing.
