using Sasd.Editor.Editing;

namespace Sasd.Editor.Scheduling;

/// <summary>
/// Cooperative scheduler corresponding to the historical EditSchedule concept.
/// Pending editor input always wins over background work.
/// </summary>
public sealed class EditorScheduler(EditorSession session)
{
    private readonly List<IEditorBackgroundTask> _tasks = [];

    public IReadOnlyList<IEditorBackgroundTask> Tasks => _tasks;

    public void Add(IEditorBackgroundTask task)
    {
        ArgumentNullException.ThrowIfNull(task);
        _tasks.Add(task);
    }

    public bool Remove(IEditorBackgroundTask task) => _tasks.Remove(task);

    /// <summary>
    /// Executes one scheduler cycle. If input is available, exactly one input
    /// unit is processed and background work is skipped for this cycle. If no
    /// input is available, one cooperative idle/background slice is executed.
    /// </summary>
    public async ValueTask<EditorScheduleResult> RunCycleAsync(
        IEditorInputPump inputPump,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(inputPump);
        cancellationToken.ThrowIfCancellationRequested();

        if (await inputPump.TryProcessInputAsync(session, cancellationToken).ConfigureAwait(false))
        {
            return EditorScheduleResult.InputProcessed;
        }

        await RunIdleCycleAsync(cancellationToken).ConfigureAwait(false);
        return EditorScheduleResult.BackgroundProcessed;
    }

    /// <summary>
    /// Executes one bounded background slice. Background tasks are deliberately
    /// cooperative: each task must retain its own state and return quickly.
    /// </summary>
    public async ValueTask RunIdleCycleAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (session.RundownRequested)
        {
            // An input pump may have requested Exit even when it returned
            // false (no edit command processed). Do not run idle work then.
            return;
        }

        await session.Hooks.OnIdleAsync(session, cancellationToken).ConfigureAwait(false);
        cancellationToken.ThrowIfCancellationRequested();
        if (session.RundownRequested)
        {
            // Exit requested by a host idle callback takes precedence over
            // all background tasks in the current scheduler cycle.
            return;
        }

        // Snapshot task registration so callbacks may add/remove tasks without
        // invalidating enumeration. Verify a task is still registered before
        // dispatch: removal by an earlier slice takes effect immediately.
        foreach (var task in _tasks.ToArray())
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (session.RundownRequested)
            {
                break;
            }

            if (!_tasks.Contains(task))
            {
                continue;
            }

            await task.ExecuteSliceAsync(session, cancellationToken).ConfigureAwait(false);
        }
    }
}

public enum EditorScheduleResult
{
    InputProcessed,
    BackgroundProcessed
}
