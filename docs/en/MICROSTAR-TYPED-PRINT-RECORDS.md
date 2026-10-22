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
