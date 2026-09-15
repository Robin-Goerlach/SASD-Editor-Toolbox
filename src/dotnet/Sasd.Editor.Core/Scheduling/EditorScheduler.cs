using Sasd.Editor.Editing;

namespace Sasd.Editor.Scheduling;

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
