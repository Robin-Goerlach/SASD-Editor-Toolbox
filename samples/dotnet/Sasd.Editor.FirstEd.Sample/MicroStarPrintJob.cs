using Sasd.Editor.Editing;
using Sasd.Editor.Scheduling;

namespace Sasd.Editor.FirstEd.Sample;

/// <summary>
/// A deliberately small port boundary for one output line. A future host
/// may implement this with a spooler, print file, serial device, or mock.
/// Printing logic must never assume that the system has a DOS LPT port.
/// </summary>
internal interface IMicroStarPrintSink
{
    ValueTask WriteLineAsync(string text, CancellationToken cancellationToken);
}

/// <summary>
/// A cooperative, snapshot-based print job compatible with the existing
/// input-first EditorScheduler. Each idle slice advances by AT MOST one
/// logical line, so printing does not monopolize keyboard responsiveness.
/// </summary>
/// <remarks>
/// The historical MicroStar background-print description motivates this
/// behavior. The exact original PrintNext timing, printer control bytes and
/// page formatting are not established here and are NOT claimed as reproduced.
///
/// Document lines are copied when the job is created; later edits, Undo, view
/// changes and even document closure cannot silently redirect a print job.
/// The actual hardware operation is delegated to IMicroStarPrintSink.
/// </remarks>
internal sealed class MicroStarPrintJob : IEditorBackgroundTask
{
    private readonly string[] _lines;
    private readonly MicroStarPrintRecord[]? _typedRecords;
    private readonly IMicroStarPrintSink _sink;
    private int _next;
    private bool _sinkClosed;

    internal MicroStarPrintJob(IEnumerable<string> lines, IMicroStarPrintSink sink)
    {
        ArgumentNullException.ThrowIfNull(lines);
        _sink = sink ?? throw new ArgumentNullException(nameof(sink));
        _lines = lines.Select(line => line ??
            throw new ArgumentException("Print lines must not be null.", nameof(lines))).ToArray();

        // Even an empty job owns its supplied sink. No scheduler callback
        // will ever run for an empty sequence, so release the output here
        // instead of leaving an empty print file exclusively locked.
        if (_lines.Length == 0)
            CloseSink();
    }

    /// <summary>
    /// Typed records are deliberately stored independently of legacy text.
    /// No control-byte detection is performed on actual document text.
    /// </summary>
    internal MicroStarPrintJob(
        IEnumerable<MicroStarPrintRecord> records, MicroStarPrintFileSink sink)
    {
        ArgumentNullException.ThrowIfNull(records);
        _sink = sink ?? throw new ArgumentNullException(nameof(sink));
        _typedRecords = records.ToArray();
        _lines = [];
        if (_typedRecords.Length == 0)
            CloseSink();
    }

    internal int TotalLines => _typedRecords?.Length ?? _lines.Length;
    internal int CompletedLines => _next;
    internal bool IsFinished => _next == TotalLines || IsCancelled;
    internal bool IsCancelled { get; private set; }
    internal Exception? Failure { get; private set; }

    /// <summary>
    /// Cancellation is local to this job, not to the EditorSession. The next
    /// scheduler callback becomes a no-op and cannot print an extra line.
    /// </summary>
    internal void Cancel()
    {
        if (IsFinished || Failure is not null) return;
        IsCancelled = true;
        CloseSink();
    }

    /// <summary>
    /// Relinquish an owned physical print-file handle at completion, failure
    /// or explicit cancellation. Non-owning mock sinks remain unaffected.
    /// This lets another process inspect or rename the completed output.
    /// </summary>
    private void CloseSink()
    {
        // Cancellation, failure and completion all share this cleanup path.
        // Disposal must happen only once even if a host calls Cancel after
        // observing a failed job or the scheduler reports cancellation.
        if (_sinkClosed) return;
        _sinkClosed = true;
        if (_sink is IDisposable disposable)
            disposable.Dispose();
    }

    public async ValueTask ExecuteSliceAsync(
        EditorSession session, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(session);
        cancellationToken.ThrowIfCancellationRequested();
        if (IsFinished || Failure is not null)
            return;

        // Do not advance the cursor until the sink has accepted the line.
        // On failure we retain the index and record an error for the host,
        // rather than throwing from a background cycle and ending editing.
        // Automatic retries are intentionally avoided: an output device can
        // have printed a line immediately before reporting an I/O failure.
        try
        {
            if (_typedRecords is not null)
            {
                // Only the explicitly typed path can express a page break.
                // A literal form-feed line stays text, regardless of content.
                await ((MicroStarPrintFileSink)_sink)
                    .WriteRecordAsync(_typedRecords[_next], cancellationToken)
                    .ConfigureAwait(false);
            }
            else
            {
                await _sink.WriteLineAsync(_lines[_next], cancellationToken)
                    .ConfigureAwait(false);
            }
            _next++;
            if (IsFinished) CloseSink();
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            // Cancellation can reach us AFTER an output slice has begun.
            // A file-backed sink keeps an exclusive handle open until its
            // owning job releases it. Without cleanup here, cancelling the
            // editor loop could strand a partially written print file until
            // process exit. Preserve cancellation as caller control flow,
            // but terminate this job and release its owned output first.
            //
            // Do NOT increment _next: a cancelled write may have produced
            // a partial record. Do NOT automatically retry: the destination
            // may already have accepted those bytes.
            IsCancelled = true;
            CloseSink();
            throw;
        }
        catch (Exception error) when (error is IOException or UnauthorizedAccessException
            or InvalidOperationException)
        {
            Failure = error;
            CloseSink();
        }
        finally
        {
            // Scheduler registration is an execution concern, not part of
            // durable job status: the queue keeps the last job for display,
            // while the scheduler should only retain work that can progress.
            // Removing ourselves here is safe because EditorScheduler
            // traverses a snapshot and checks current registration.
            //
            // This also runs while cancellation propagates to the caller;
            // merely closing a sink must not leave a terminal task in the
            // idle-task list. Keep unfinished jobs registered for their next
            // cooperative slice.
            if (IsFinished || Failure is not null)
                session.Scheduler.Remove(this);
        }
    }
}

/// <summary>
/// Manages one active sample print job. The scheduler owns task execution;
/// this manager owns registration and replacement policy only.
/// </summary>
internal sealed class MicroStarPrintQueue(EditorSession session)
{
    private MicroStarPrintJob? _active;

    internal MicroStarPrintJob? Active => _active;

    internal bool TryStart(IEnumerable<string> lines, IMicroStarPrintSink sink)
    {
        // Explicitly reject a second live job instead of silently replacing
        // work or interleaving separate documents on the printer.
        if (_active is { IsFinished: false, Failure: null })
            return false;

        if (_active is not null)
            session.Scheduler.Remove(_active);
        var job = new MicroStarPrintJob(lines, sink);
        _active = job;
        if (!job.IsFinished)
            session.Scheduler.Add(job);
        return true;
    }

    /// <summary>
    /// Register a typed print-file job without converting control records
    /// back to ambiguous strings. Existing callers of the legacy overload
    /// continue to work until they can be migrated independently.
    /// </summary>
    internal bool TryStartTyped(
        IEnumerable<MicroStarPrintRecord> records, MicroStarPrintFileSink sink)
    {
        if (_active is { IsFinished: false, Failure: null })
            return false;
        if (_active is not null)
            session.Scheduler.Remove(_active);
        var job = new MicroStarPrintJob(records, sink);
        _active = job;
        if (!job.IsFinished)
            session.Scheduler.Add(job);
        return true;
    }

    internal void Cancel()
    {
        _active?.Cancel();
        if (_active is not null)
            session.Scheduler.Remove(_active);
    }
}
