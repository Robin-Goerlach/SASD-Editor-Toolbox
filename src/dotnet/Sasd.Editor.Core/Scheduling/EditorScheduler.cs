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
        await session.Hooks.OnIdleAsync(session, cancellationToken).ConfigureAwait(false);
        foreach (var task in _tasks.ToArray())
        {
            cancellationToken.ThrowIfCancellationRequested();
            await task.ExecuteSliceAsync(session, cancellationToken).ConfigureAwait(false);
        }
    }
}

public enum EditorScheduleResult
{
    InputProcessed,
    BackgroundProcessed
}
