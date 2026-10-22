# Typed MicroStar print records

The console F10 → H print-to-file path now uses **typed** output records:
`Text` and `PageBreak`. The distinction is independent of record contents.
A document line containing only the form-feed character remains a text row,
while an inserted page boundary emits a bare form feed byte without a newline.

`MicroStarTypedPrintPagination` snapshots logical editor lines before
formatting. `MicroStarPrintJob` dispatches one typed record per cooperative
scheduler slice and `MicroStarPrintFileSink.WriteRecordAsync` owns encoding.

The earlier string-based output interface and pagination helper remain for
compatibility with tests and existing callers; new file output does not infer
control intent from strings. Real printer devices, page geometry and exact
historical Borland `PrintNext` behavior remain outside this milestone.

## Whole-job instruction validation

Directly constructed typed records are validated as a complete snapshot
before a job is registered. Text records require non-null text; page-break
records may not carry text; unknown record kinds are rejected. This prevents
a malformed late instruction from causing avoidable partial print output.
If validation fails, ownership of the supplied sink remains with the caller,
which must dispose it. A regression test verifies rejection before any
scheduler work or output bytes are produced.
