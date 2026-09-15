using Sasd.Editor.Editing;

namespace Sasd.Editor.Scheduling;

/// <summary>
/// Host-neutral editor main loop corresponding to the historical EditSystem
/// routine. It repeatedly executes scheduler cycles until rundown is requested.
/// </summary>
public sealed class EditorSystemLoop(EditorSession session, EditorScheduler scheduler)
{
    /// <summary>
    /// Runs the editor loop until <see cref="EditorSession.RundownRequested"/>
    /// becomes true or the supplied cancellation token is cancelled.
    /// </summary>
    public async ValueTask RunAsync(
        IEditorInputPump inputPump,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(inputPump);

        while (!session.RundownRequested)
        {
            cancellationToken.ThrowIfCancellationRequested();
            await scheduler.RunCycleAsync(inputPump, cancellationToken).ConfigureAwait(false);
        }
    }
}
