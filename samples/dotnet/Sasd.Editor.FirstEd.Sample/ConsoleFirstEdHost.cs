using Sasd.Editor.Commands;
using Sasd.Editor.Editing;
using Sasd.Editor.Input;
using Sasd.Editor.Scheduling;

namespace Sasd.Editor.FirstEd.Sample;

/// <summary>
/// Interactive terminal adapter that wires normalized console input, the
/// FIRST-ED key map, host prompts, semantic commands and stacked-window viewport
/// rendering into the reusable editor system loop.
/// </summary>
internal sealed class ConsoleFirstEdHost : IEditorInputPump
{
    private readonly EditorSession _session;
    private readonly ConsoleFirstEdHooks _hooks;
    private readonly EditorCommandDispatcher _dispatcher;
    private readonly FirstEdKeyMap _keyMap = new();
    private readonly ConsoleFirstEdRenderer _renderer;
    private readonly ConsoleFirstEdPromptService _prompts;

    public ConsoleFirstEdHost(EditorSession session, ConsoleFirstEdHooks hooks)
    {
        _session = session ?? throw new ArgumentNullException(nameof(session));
        _hooks = hooks ?? throw new ArgumentNullException(nameof(hooks));
        _dispatcher = new EditorCommandDispatcher(session);
        _renderer = new ConsoleFirstEdRenderer(session);
        _prompts = new ConsoleFirstEdPromptService(_renderer);
    }

    public void Render(string? message = null) => _renderer.Render(message);

    public void Shutdown() => _renderer.Shutdown();

    public async ValueTask<bool> TryProcessInputAsync(
        EditorSession session,
        CancellationToken cancellationToken = default)
    {
        // The historical kernel consumed the editor's own typeahead buffer before
        // doing background work. Macro-injected input therefore has exactly the
        // same path through the key map and command dispatcher as keyboard input.
        if (!session.Typeahead.TryRead(out var keyStroke))
        {
            if (!Console.KeyAvailable)
            {
                await Task.Delay(15, cancellationToken).ConfigureAwait(false);
                return false;
            }

            var keyInfo = Console.ReadKey(intercept: true);
            var translated = ConsoleKeyTranslator.Translate(keyInfo);
            if (!translated.HasValue)
            {
                Render("This terminal key could not be normalized.");
                return true;
            }

            var writeResult = session.Typeahead.EnqueueFromHost(translated.Value);
            if (writeResult == EditorTypeaheadWriteResult.Aborted)
            {
                _keyMap.Reset();
                Render("Ctrl-U: pending typeahead input aborted.");
                return true;
            }

            if (writeResult == EditorTypeaheadWriteResult.Overflow)
            {
                _keyMap.Reset();
                Render("Typeahead buffer overflow: pending input was cleared.");
                return true;
            }

            if (!session.Typeahead.TryRead(out keyStroke))
            {
                return true;
            }
        }

        var action = _keyMap.Translate(keyStroke);
        var message = await ExecuteActionAsync(action, cancellationToken).ConfigureAwait(false);
        message = _hooks.TakeMessage() ?? message;
        Render(message);
        return true;
    }

    private async ValueTask<string?> ExecuteActionAsync(
        EditorInputAction action,
        CancellationToken cancellationToken)
    {
        switch (action.Kind)
        {
            case EditorInputActionKind.Ignored:
                return null;

            case EditorInputActionKind.PrefixPending:
                return $"{action.PrefixDisplay} prefix: waiting for command key";

            case EditorInputActionKind.InsertText:
                await _dispatcher.ExecuteAsync(
                    new EditorCommandRequest(
                        EditorCommandId.InsertText,
                        Text: action.Text,
                        PageSize: _renderer.VisibleTextRows),
                    cancellationToken).ConfigureAwait(false);
                return null;

            case EditorInputActionKind.Command:
            {
                var binding = action.Binding ?? throw new InvalidOperationException("Command action has no binding.");
                var request = _prompts.Resolve(binding, _renderer.VisibleTextRows);
                if (request is null)
                {
                    return "Command cancelled.";
                }

                var executed = await _dispatcher.ExecuteAsync(request, cancellationToken).ConfigureAwait(false);
                return executed ? null : "Command was not executed.";
            }

            default:
                return null;
        }
    }
}
