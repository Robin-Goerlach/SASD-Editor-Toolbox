using Sasd.Editor.Commands;
using Sasd.Editor.Editing;
using Sasd.Editor.Hooks;
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
    private readonly ConsoleTerminalSizeTracker _terminalSize = new();
    private readonly ConsoleFirstEdRenderer _renderer;
    private readonly ConsoleFirstEdPromptService _prompts;
    private readonly MicroStarPrintQueue _printQueue;

    public ConsoleFirstEdHost(EditorSession session, ConsoleFirstEdHooks hooks)
    {
        _session = session ?? throw new ArgumentNullException(nameof(session));
        _hooks = hooks ?? throw new ArgumentNullException(nameof(hooks));
        _dispatcher = new EditorCommandDispatcher(session);
        _renderer = new ConsoleFirstEdRenderer(session);
        _prompts = new ConsoleFirstEdPromptService(_renderer, session);
        _printQueue = new MicroStarPrintQueue(session);
    }

    public void Render(string? message = null)
    {
        _renderer.Render(message);
        // Remember the dimensions used for the last successful full paint.
        // The idle input loop can then detect terminal resize without requiring
        // a key press. This state is host-only, never part of EditorSession.
        _terminalSize.Observe(Console.WindowWidth, Console.WindowHeight);
    }

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
                // Windows Terminal does not necessarily deliver a keystroke
                // on resize. Poll its dimensions while idle and repaint the
                // entire model before accepting further document input.
                // This is a host presentation event, not an editing command.
                if (_terminalSize.Observe(Console.WindowWidth, Console.WindowHeight))
                {
                    Render();
                    return true;
                }

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

                // Outside a long-running operation the input pump itself is the
                // consumer of Ctrl-U. Clear the one-shot abort flag here so a
                // later unrelated interruptible command is not aborted by stale
                // state.
                session.Typeahead.ConsumeAbortRequest();

                await session.Hooks.OnErrorAsync(
                    session,
                    EditorErrorCatalog.OperationAborted(),
                    cancellationToken).ConfigureAwait(false);
                Render(_hooks.TakeMessage());
                return true;
            }

            if (writeResult == EditorTypeaheadWriteResult.Overflow)
            {
                _keyMap.Reset();
                await session.Hooks.OnErrorAsync(
                    session,
                    EditorErrorCatalog.TypeaheadOverflow(),
                    cancellationToken).ConfigureAwait(false);
                Render(_hooks.TakeMessage());
                return true;
            }

            if (!session.Typeahead.TryRead(out keyStroke))
            {
                return true;
            }
        }

        // MicroStar's F10 application menu is a host feature, not part of
        // the historical FIRST-ED command vocabulary. Intercept it before
        // handing input to the generic key map; a pending Ctrl-K/O/Q prefix
        // must not leak into the menu's eventual semantic command.
        if (keyStroke.Key == EditorKey.F10
            && keyStroke.Modifiers == EditorKeyModifiers.None)
        {
            _keyMap.Reset();
            var menuMessage = await ExecuteMicroStarMenuAsync(cancellationToken)
                .ConfigureAwait(false);
            Render(_hooks.TakeMessage() ?? menuMessage);
            return true;
        }

        var action = _keyMap.Translate(keyStroke);
        var message = await ExecuteActionAsync(action, cancellationToken).ConfigureAwait(false);
        message = _hooks.TakeMessage() ?? message;
        Render(message);
        return true;
    }

    /// <summary>
    /// Executes a menu action through precisely the same prompt resolver and
    /// command dispatcher as a keyboard action. F10 never edits the document
    /// merely by opening the menu; cancellation is a strict no-op.
    /// </summary>
    private async ValueTask<string?> ExecuteMicroStarMenuAsync(
        CancellationToken cancellationToken)
    {
        var choice = _renderer.ReadMicroStarMenu();
        if (string.Equals(choice, "T", StringComparison.OrdinalIgnoreCase))
            return await ExportSelectedBlockAsync(cancellationToken)
                .ConfigureAwait(false);
        if (string.Equals(choice, "L", StringComparison.OrdinalIgnoreCase))
            return await ImportSelectedBlockAsync(cancellationToken)
                .ConfigureAwait(false);

        if (string.Equals(choice, "P", StringComparison.OrdinalIgnoreCase))
            return await SwitchMicroStarFileAsync(
                MicroStarOpenDisposition.SaveThenOpen, cancellationToken).ConfigureAwait(false);
        if (string.Equals(choice, "A", StringComparison.OrdinalIgnoreCase))
            return await SwitchMicroStarFileAsync(
                MicroStarOpenDisposition.AbandonThenOpen, cancellationToken).ConfigureAwait(false);

        if (string.Equals(choice, "H", StringComparison.OrdinalIgnoreCase))
            return BeginMicroStarPrint();
        if (string.Equals(choice, "J", StringComparison.OrdinalIgnoreCase))
            return MicroStarPrintStatus.Describe(_printQueue.Active);
        if (string.Equals(choice, "K", StringComparison.OrdinalIgnoreCase))
        {
            _printQueue.Cancel();
            return "Background print cancelled.";
        }

        var binding = ConsoleMicroStarMenuPolicy.Resolve(choice);
        if (binding is null)
            return "Menu cancelled.";

        var request = _prompts.Resolve(binding, _renderer.VisibleTextRows);
        if (request is null)
            return "Menu command cancelled.";

        var executed = await _dispatcher.ExecuteAsync(request, cancellationToken)
            .ConfigureAwait(false);
        return executed ? null : "Menu command was not executed.";
    }

    /// <summary>
    /// Start print-to-file without blocking on the document length. A new
    /// physical output file is exclusively created before scheduling any
    /// slices. The queue snapshots document lines; edits after this point
    /// cannot redirect content already committed for printing.
    /// </summary>
    private string BeginMicroStarPrint()
    {
        if (_printQueue.Active is { IsFinished: false, Failure: null })
            return "A background print job is already active.";

        var path = _renderer.ReadPrompt("Print to NEW UTF-8 file path: ");
        if (string.IsNullOrWhiteSpace(path))
            return "Print cancelled.";

        try
        {
            // Pagination is performed on a stable document snapshot BEFORE
            // scheduling, so subsequent edits cannot change page boundaries.
            // The 60-row default is explicitly a modern SASD policy, not an
            // assertion about undocumented historical printer settings.
            var lines = MicroStarTypedPrintPagination.Prepare(
                _session.CurrentWindow.Document.Buffer.Snapshot()
                    .Select(line => line.Text));
            var sink = new MicroStarPrintFileSink(path);
            if (!_printQueue.TryStartTyped(lines, sink))
            {
                sink.Dispose();
                return "A background print job is already active.";
            }
            return $"Printing {lines.Count} output record(s) in background.";
        }
        catch (IOException error)
        {
            return "Print not started: " +
                MicroStarBrowserDisplay.SafeLabel(error.Message, 60);
        }
        catch (UnauthorizedAccessException)
        {
            return "Print not started: access denied.";
        }
    }

    /// <summary>
    /// Ask for the replacement path without modifying document text.
    /// Saving and explicit discard confirmation remain mutually exclusive
    /// host-level choices; Core owns staged decoding and final reattachment.
    /// </summary>
    private async ValueTask<string?> SwitchMicroStarFileAsync(
        MicroStarOpenDisposition disposition, CancellationToken cancellationToken)
    {
        var path = _renderer.BrowseMicroStarFile(Environment.CurrentDirectory);
        if (string.IsNullOrWhiteSpace(path))
            return "File switch cancelled.";

        var window = _session.CurrentWindow;
        var source = window.Document;
        var version = source.Version;
        var discardConfirmed = disposition == MicroStarOpenDisposition.AbandonThenOpen
            && source.IsDirty && _renderer.ReadPrompt(
                "Discard unsaved changes? Type DISCARD: ") == "DISCARD";

        var plan = MicroStarOpenPolicy.Plan(
            disposition, source.IsDirty, discardConfirmed);
        if (!plan.MayOpen)
            return "File switch cancelled; original preserved.";

        var saved = false;
        if (plan.MustSave)
        {
            // Reuse established Save File prompting and its overwrite guard.
            var request = _prompts.Resolve(
                new EditorCommandBinding(EditorCommandId.SaveFile),
                _renderer.VisibleTextRows);
            if (request is null)
                return "File switch cancelled; save not approved.";
            saved = await _dispatcher.ExecuteAsync(request, cancellationToken)
                .ConfigureAwait(false);
            if (!saved)
                return "File switch cancelled; saving failed.";
        }

        // A different focused window or a newer source edit invalidates
        // consent to switch. Core will perform its own post-read recheck.
        if (_session.CurrentWindow.WindowId != window.WindowId ||
            !ReferenceEquals(window.Document, source) ||
            !MicroStarOpenPolicy.CanCommit(plan, version, source.Version,
                source.IsDirty, saved))
            return "File switch cancelled; document changed.";

        var opened = await _session.Files.OpenReplacingCurrentWindowAsync(
            path, allowDiscardDirty:
                disposition == MicroStarOpenDisposition.AbandonThenOpen && discardConfirmed,
            cancellationToken: cancellationToken).ConfigureAwait(false);
        return opened ? "File opened." : "File switch cancelled; original preserved.";
    }

    /// <summary>
    /// MicroStar's block-only Write is distinct from Ctrl-K W, which writes
    /// the entire document. The exporter is a sample application service;
    /// the editor's reusable file/undo semantics remain unchanged.
    /// </summary>
    private async ValueTask<string?> ExportSelectedBlockAsync(
        CancellationToken cancellationToken)
    {
        if (!ConsoleMicroStarBlockFileService.HasExportableBlock(_session))
            return "Define a valid block before exporting it.";

        var path = _renderer.ReadPrompt("Write marked block to file: ");
        if (string.IsNullOrWhiteSpace(path))
            return "Block export cancelled.";

        // Match the existing write-file host safety policy: a pre-existing
        // destination needs explicit YES, not the default Enter. Like normal
        // File.Exists checks this is a UX safeguard, not a race-free lock.
        if (File.Exists(path) && !_renderer.ConfirmMicroStarOverwrite())
            return "Block export cancelled.";

        var exported = await ConsoleMicroStarBlockFileService.ExportAsync(
            _session, path, cancellationToken).ConfigureAwait(false);
        return exported ? "Marked block exported." : "Block export was not executed.";
    }

    /// <summary>
    /// Only prompting and user feedback belong here. Actual block insertion,
    /// all integrity checks and a single Undo step belong to Core's file
    /// service, shared with the already-tested Read File implementation.
    /// </summary>
    private async ValueTask<string?> ImportSelectedBlockAsync(
        CancellationToken cancellationToken)
    {
        // Prefer a navigable, read-only picker. It returns only a path:
        // the existing Core file service still owns decoding, validating,
        // topology realignment and Undo as a single transaction.
        var path = _renderer.BrowseMicroStarFile(Environment.CurrentDirectory);
        if (string.IsNullOrWhiteSpace(path))
            return "Block import cancelled.";

        var count = await _session.Files.ReadBlockIntoCurrentWindowAsync(
            path, cancellationToken).ConfigureAwait(false);
        return count == 0 ? "Empty file; block selection unchanged."
            : $"Imported and selected {count} line(s).";
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
