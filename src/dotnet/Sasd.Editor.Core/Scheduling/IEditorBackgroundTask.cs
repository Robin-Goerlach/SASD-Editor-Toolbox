using Sasd.Editor.Editing;

namespace Sasd.Editor.Scheduling;

/// <summary>
/// Cooperative background task. One call must perform a bounded unit of work
/// and return quickly; the host decides when the next idle cycle occurs.
/// </summary>
public interface IEditorBackgroundTask
{
    ValueTask ExecuteSliceAsync(EditorSession session, CancellationToken cancellationToken);
}
