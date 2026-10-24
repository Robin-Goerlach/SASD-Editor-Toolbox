using Sasd.Editor.Editing;
using Sasd.Editor.Hooks;
using Sasd.Editor.Input;

namespace Sasd.Editor.FirstEd.Sample;

/// <summary>
/// Minimal host hook implementation. Errors are captured and rendered on the
/// sample's command/message line instead of making the reusable core write to
/// the terminal directly.
/// </summary>
internal sealed class ConsoleFirstEdHooks : IEditorHooks
{
    private string? _pendingMessage;
    private readonly IReadOnlyDictionary<string, string> _resources;

    internal ConsoleFirstEdHooks(IReadOnlyDictionary<string, string>? resources = null)
    {
        _resources = resources ?? new Dictionary<string, string>();
    }

    public ValueTask OnErrorAsync(
        EditorSession session,
        EditorError error,
        CancellationToken cancellationToken)
    {
        _pendingMessage = ConsoleFirstEdErrorPresentation.Format(error, _resources);
        return ValueTask.CompletedTask;
    }

    /// <summary>
    /// EditBreathe-style polling used only while the reusable core is executing
    /// an interruptible operation. One physical key is collected per checkpoint
    /// so the hook remains bounded and cannot turn a CPU loop into an input loop.
    /// </summary>
    public async ValueTask PollInputDuringOperationAsync(
        EditorSession session,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (!Console.KeyAvailable)
        {
            return;
        }

        var translated = ConsoleKeyTranslator.Translate(Console.ReadKey(intercept: true));
        if (!translated.HasValue)
        {
            return;
        }

        var result = session.Typeahead.EnqueueFromHost(translated.Value);
        if (result == EditorTypeaheadWriteResult.Overflow)
        {
            await OnErrorAsync(
                session,
                EditorErrorCatalog.TypeaheadOverflow(),
                cancellationToken).ConfigureAwait(false);
        }

        // Aborted is deliberately not rendered here. The interruption service
        // consumes the abort flag and the dispatcher reports one typed abort
        // error, avoiding duplicate Ctrl-U messages.
    }

    public string? TakeMessage()
    {
        var message = _pendingMessage;
        _pendingMessage = null;
        return message;
    }
}
