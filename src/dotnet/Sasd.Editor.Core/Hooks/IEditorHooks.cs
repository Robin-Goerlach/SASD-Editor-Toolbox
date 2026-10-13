using Sasd.Editor.Commands;
using Sasd.Editor.Editing;
using Sasd.Editor.Rendering;

namespace Sasd.Editor.Hooks;

/// <summary>
/// Host extension points corresponding conceptually to the historical user
/// command, error, status, replace and idle-task hooks.
/// </summary>
public interface IEditorHooks
{
    EditorCommandRequest FilterCommand(EditorSession session, EditorCommandRequest command) => command;

    ValueTask OnErrorAsync(EditorSession session, EditorError error, CancellationToken cancellationToken) =>
        ValueTask.CompletedTask;

    EditorStatus TransformStatus(EditorSession session, EditorStatus status) => status;

    ValueTask<ReplaceDecision> BeforeReplaceAsync(
        EditorSession session,
        ReplaceCandidate candidate,
        CancellationToken cancellationToken) => ValueTask.FromResult(ReplaceDecision.Replace);

    /// <summary>
    /// Gives a host one bounded opportunity to collect physical input while a
    /// long-running editor operation is in progress.
    /// </summary>
    /// <remarks>
    /// This is the host-neutral counterpart of the historical EditBreathe call.
    /// Implementations should normalize at most a small bounded amount of input
    /// and enqueue it through <see cref="Input.EditorTypeaheadBuffer"/>. A
    /// host-originated Ctrl-U is therefore converted into the existing immediate
    /// abort state without teaching the reusable core about Console/WPF/browser
    /// input APIs. The default hook has no physical input source and does nothing.
    /// </remarks>
    ValueTask PollInputDuringOperationAsync(
        EditorSession session,
        CancellationToken cancellationToken) => ValueTask.CompletedTask;

    ValueTask OnIdleAsync(EditorSession session, CancellationToken cancellationToken) => ValueTask.CompletedTask;
}
