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
    private readonly IMicroStarPrintSink _sink;
    private int _next;

    internal MicroStarPrintJob(IEnumerable<string> lines, IMicroStarPrintSink sink)
    {
        ArgumentNullException.ThrowIfNull(lines);
        _sink = sink ?? throw new ArgumentNullException(nameof(sink));
        _lines = lines.Select(line => line ??
            throw new ArgumentException("Print lines must not be null.", nameof(lines))).ToArray();
    }

    internal int TotalLines => _lines.Length;
    internal int CompletedLines => _next;
    internal bool IsFinished => _next == _lines.Length || IsCancelled;
    internal bool IsCancelled { get; private set; }
    internal Exception? Failure { get; private set; }

    /// <summary>
    /// Cancellation is local to this job, not to the EditorSession. The next
    /// scheduler callback becomes a no-op and cannot print an extra line.
    /// </summary>
    internal void Cancel() => IsCancelled = true;

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
            await _sink.WriteLineAsync(_lines[_next], cancellationToken)
                .ConfigureAwait(false);
            _next++;
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            // External cancellation belongs to scheduler control flow.
            // It must not be mistaken for a failed printer.
            throw;
        }
        catch (Exception error) when (error is IOException or UnauthorizedAccessException
            or InvalidOperationException)
        {
            Failure = error;
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

    internal void Cancel()
    {
        _active?.Cancel();
        if (_active is not null)
            session.Scheduler.Remove(_active);
    }
}
