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


## Cancellation during an active output slice

A cancellation request can occur **after** a sink's `WriteLineAsync`
has started, unlike cancellation before a scheduler cycle. The print
job now terminates and disposes an owned output sink before propagating
the caller's `OperationCanceledException`. It deliberately does not
advance its completed-record counter or retry the ambiguous record:
a physical device or filesystem may already have accepted some bytes.

A deterministic test simulates cancellation from inside the sink and
asserts handle cleanup, zero additional output records and no
automatic retry. This is a modern resource-safety guarantee, not
historical DOS printer compatibility.
