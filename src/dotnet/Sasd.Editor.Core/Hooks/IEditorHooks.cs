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

    ValueTask OnIdleAsync(EditorSession session, CancellationToken cancellationToken) => ValueTask.CompletedTask;
}
