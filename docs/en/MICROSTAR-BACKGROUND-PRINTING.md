# MicroStar cooperative background printing — first slice

The MicroStar sample now has `MicroStarPrintJob` and
`MicroStarPrintQueue`. This is a **hardware-neutral** foundation for the
historical background printing idea, not a complete `PrintNext` clone.

A print job snapshots the source lines before dispatch. Each invocation
of the existing input-first `EditorScheduler` emits **at most one line**
to `IMicroStarPrintSink`, an interface whose implementation can later
target a real spooler, file or test recorder. No DOS-specific LPT port or
thread pool dependency is introduced.

An active unfinished job cannot be silently replaced. Explicit cancellation
prevents later lines. Output errors are recorded without blind retries;
repeating a line after an ambiguous printer failure can duplicate paper
output. External scheduler cancellation propagates distinctly.

Headless tests cover bounded slices, immutable source snapshots,
cancellation, output failure, exclusive active job and empty jobs.
**Still open:** user-facing F10 commands, a real output adapter,
historical page formatting and exact PrintNext compatibility.
