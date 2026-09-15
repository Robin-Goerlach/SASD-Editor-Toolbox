using Sasd.Editor.Editing;

namespace Sasd.Editor.Scheduling;

/// <summary>
/// Host-facing input boundary used by the compatibility scheduler.
/// </summary>
/// <remarks>
/// One call should process at most one pending input unit and return quickly.
/// Returning <see langword="true"/> means input was available and processed;
/// returning <see langword="false"/> gives the scheduler permission to run one
/// cooperative background slice. Platform keyboard APIs, terminal readers and
/// scripted hosts stay outside the editor core behind this interface.
/// </remarks>
public interface IEditorInputPump
{
    ValueTask<bool> TryProcessInputAsync(
        EditorSession session,
        CancellationToken cancellationToken = default);
}
