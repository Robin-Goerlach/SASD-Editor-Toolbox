using Sasd.Editor.Editing;

namespace Sasd.Editor.Scheduling;

/// <summary>
/// Coordinates modern cancellation with the historical immediate Ctrl-U abort
/// state without introducing a second host-specific cancellation mechanism.
/// </summary>
/// <remarks>
/// External <see cref="CancellationToken"/> cancellation remains normal platform
/// control flow and is never translated into an editor error. A physical Ctrl-U
/// is different: the host feeds it through <see cref="Input.EditorTypeaheadBuffer"/>,
/// which clears pending typeahead and raises the session abort flag. Interruptible
/// editor operations call <see cref="CheckpointAsync"/> at safe points, or wrap
/// awaited I/O in <see cref="RunInterruptibleAsync{T}"/>.
/// </remarks>
public sealed class EditorInterruptionService(EditorSession session)
{
    private static readonly TimeSpan PollInterval = TimeSpan.FromMilliseconds(25);

    /// <summary>
    /// Performs one EditBreathe-style checkpoint.
    /// </summary>
    /// <remarks>
    /// The host receives one bounded opportunity to collect physical input. Any
    /// queued Ctrl-U is then consumed exactly once and converted to
    /// <see cref="EditorOperationAbortedException"/>. Ordinary queued input stays
    /// in typeahead for normal command processing after the long operation ends.
    /// </remarks>
    public async ValueTask CheckpointAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        await session.Hooks
            .PollInputDuringOperationAsync(session, cancellationToken)
            .ConfigureAwait(false);
        cancellationToken.ThrowIfCancellationRequested();

        if (session.Typeahead.ConsumeAbortRequest())
        {
            throw new EditorOperationAbortedException();
        }
    }

    /// <summary>
    /// Runs an awaited operation while periodically giving the host a chance to
    /// detect Ctrl-U. The operation receives a linked cancellation token so a
    /// user abort can cancel built-in async I/O promptly.
    /// </summary>
    /// <remarks>
    /// This helper is intended for operations that spend most of their time
    /// awaiting cancellable I/O. CPU-bound editor loops should call
    /// <see cref="CheckpointAsync"/> directly at well-defined pre-commit points.
    ///
    /// Implementations passed here are expected to honor their cancellation
    /// token. The built-in file codecs do so. This expectation prevents a
    /// supposedly aborted I/O task from continuing invisibly after the editor
    /// has reported the abort.
    /// </remarks>
    public async ValueTask<T> RunInterruptibleAsync<T>(
        Func<CancellationToken, ValueTask<T>> operation,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(operation);
        cancellationToken.ThrowIfCancellationRequested();

        using var linkedCancellation = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);

        // Catch an already pending Ctrl-U before starting external side effects.
        await CheckpointAsync(cancellationToken).ConfigureAwait(false);

        var operationTask = operation(linkedCancellation.Token).AsTask();

        try
        {
            while (!operationTask.IsCompleted)
            {
                var delayTask = Task.Delay(PollInterval, cancellationToken);
                var completed = await Task.WhenAny(operationTask, delayTask).ConfigureAwait(false);
                if (completed == operationTask)
                {
                    break;
                }

                await CheckpointAsync(cancellationToken).ConfigureAwait(false);
            }

            var result = await operationTask.ConfigureAwait(false);

            // A Ctrl-U that arrived immediately as the I/O completed still wins
            // before the caller is allowed to commit the returned data.
            await CheckpointAsync(cancellationToken).ConfigureAwait(false);
            return result;
        }
        catch (EditorOperationAbortedException)
        {
            linkedCancellation.Cancel();
            await ObserveCancellationAsync(operationTask, linkedCancellation.Token).ConfigureAwait(false);
            throw;
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            // External cancellation is not an editor error, but the in-flight
            // operation still needs to be observed before this service returns.
            linkedCancellation.Cancel();
            await ObserveCancellationAsync(operationTask, linkedCancellation.Token).ConfigureAwait(false);
            throw;
        }
    }

    private static async ValueTask ObserveCancellationAsync<T>(
        Task<T> operationTask,
        CancellationToken linkedCancellationToken)
    {
        try
        {
            await operationTask.ConfigureAwait(false);
        }
        catch (OperationCanceledException) when (linkedCancellationToken.IsCancellationRequested)
        {
            // Expected cancellation path. Other failures still propagate because
            // they may indicate a real I/O/provider error that must not be hidden.
        }
    }

    /// <summary>
    /// Void-returning convenience form for interruptible writes.
    /// </summary>
    public async ValueTask RunInterruptibleAsync(
        Func<CancellationToken, ValueTask> operation,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(operation);

        await RunInterruptibleAsync(
            async token =>
            {
                await operation(token).ConfigureAwait(false);
                return true;
            },
            cancellationToken).ConfigureAwait(false);
    }
}

/// <summary>
/// Signals a user-originated editor abort (historically Ctrl-U).
/// </summary>
/// <remarks>
/// This deliberately does not derive from <see cref="OperationCanceledException"/>.
/// External cancellation and a user abort have different host semantics: external
/// cancellation propagates to the caller, whereas a Ctrl-U abort is translated
/// by the command dispatcher into the typed OperationAborted editor error.
/// </remarks>
public sealed class EditorOperationAbortedException : Exception
{
    public EditorOperationAbortedException()
        : base("The editor operation was aborted by the user.")
    {
    }
}
