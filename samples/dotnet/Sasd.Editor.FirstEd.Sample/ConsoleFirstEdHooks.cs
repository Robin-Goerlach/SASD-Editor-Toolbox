using Sasd.Editor.Editing;
using Sasd.Editor.Hooks;

namespace Sasd.Editor.FirstEd.Sample;

/// <summary>
/// Minimal host hook implementation. Errors are captured and rendered on the
/// sample's command/message line instead of making the reusable core write to
/// the terminal directly.
/// </summary>
internal sealed class ConsoleFirstEdHooks : IEditorHooks
{
    private string? _pendingMessage;

    public ValueTask OnErrorAsync(
        EditorSession session,
        EditorError error,
        CancellationToken cancellationToken)
    {
        _pendingMessage = $"{error.Code}: {error.Message}";
        return ValueTask.CompletedTask;
    }

    public string? TakeMessage()
    {
        var message = _pendingMessage;
        _pendingMessage = null;
        return message;
    }
}
