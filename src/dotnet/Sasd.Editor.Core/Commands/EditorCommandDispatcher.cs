using Sasd.Editor.Editing;
using Sasd.Editor.Hooks;
using Sasd.Editor.Search;

namespace Sasd.Editor.Commands;

/// <summary>
/// Maps semantic commands to editing primitives. Keyboard/UI mapping is a
/// separate responsibility, keeping the core usable from WPF, WinForms,
/// terminals, web front ends and tests.
/// </summary>
public sealed class EditorCommandDispatcher(EditorSession session)
{
    private const int DefaultVisibleLines = 20;

    public async ValueTask<bool> ExecuteAsync(EditorCommandRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        var command = session.Hooks.FilterCommand(session, request);

        try
        {
            var visibleLines = command.PageSize ?? DefaultVisibleLines;
            switch (command.Id)
            {
                case EditorCommandId.CursorLeft: session.Engine.MoveLeft(); break;
                case EditorCommandId.CursorRight: session.Engine.MoveRight(); break;
                case EditorCommandId.CursorUp: FirstEdCompatibilityProcessor.MoveUpLine(session, visibleLines); break;
                case EditorCommandId.CursorDown: FirstEdCompatibilityProcessor.MoveDownLine(session, visibleLines); break;
                case EditorCommandId.PageUp: FirstEdCompatibilityProcessor.PageUp(session, visibleLines); break;
                case EditorCommandId.PageDown: FirstEdCompatibilityProcessor.PageDown(session, visibleLines); break;
                case EditorCommandId.ScrollUp: FirstEdCompatibilityProcessor.ScrollUp(session, visibleLines); break;
                case EditorCommandId.ScrollDown: FirstEdCompatibilityProcessor.ScrollDown(session, visibleLines); break;
                case EditorCommandId.WordLeft: session.Engine.MoveLeftWord(); break;
                case EditorCommandId.WordRight: session.Engine.MoveRightWord(); break;
                case EditorCommandId.BeginningOfLine: session.Engine.MoveBeginningOfLine(); break;
                case EditorCommandId.EndOfLine: FirstEdCompatibilityProcessor.MoveEndOfLine(session); break;
                case EditorCommandId.BeginningOrEndOfLine: FirstEdCompatibilityProcessor.MoveBeginningOrEndOfLine(session); break;
                case EditorCommandId.TopOfFile: FirstEdCompatibilityProcessor.MoveWindowTopFile(session); break;
                case EditorCommandId.BottomOfFile: FirstEdCompatibilityProcessor.MoveWindowBottomFile(session); break;
                case EditorCommandId.TopOfBlock: return FirstEdCompatibilityProcessor.GoToBlockBoundary(session, end: false);
                case EditorCommandId.BottomOfBlock: return FirstEdCompatibilityProcessor.GoToBlockBoundary(session, end: true);
                case EditorCommandId.GoToLine: return FirstEdCompatibilityProcessor.GoToLine(session, RequireNumber(command));
                case EditorCommandId.GoToColumn: return FirstEdCompatibilityProcessor.GoToColumn(session, RequireNumber(command));
                case EditorCommandId.InsertText: session.Engine.InsertText(command.Text ?? string.Empty); break;
                case EditorCommandId.InsertLine: session.Engine.InsertNewLine(); break;
                case EditorCommandId.InsertControlCharacter: session.Engine.InsertControlCharacter(RequireText(command)[0]); break;
                case EditorCommandId.Tab: session.Engine.Tab(); break;
                case EditorCommandId.DeleteLeftCharacter: session.Engine.DeleteLeftCharacter(); break;
                case EditorCommandId.DeleteRightCharacter: session.Engine.DeleteRightCharacter(); break;
                case EditorCommandId.DeleteRightWord: session.Engine.DeleteRightWord(); break;
                case EditorCommandId.DeleteLine: session.Engine.DeleteLine(); break;
                case EditorCommandId.DeleteToEndOfLine: session.Engine.DeleteToEndOfLine(); break;
                case EditorCommandId.ChangeCase: session.Engine.ChangeCase(); break;
                case EditorCommandId.CenterLine: session.Engine.CenterLine(); break;
                case EditorCommandId.ReformatParagraph: session.Engine.ReformatParagraph(); break;
                case EditorCommandId.ToggleInsert: session.Engine.ToggleInsertMode(); break;
                case EditorCommandId.ToggleWordWrap: session.Engine.ToggleWordWrap(); break;
                case EditorCommandId.ToggleAutoIndent: session.Engine.ToggleAutoIndent(); break;
                case EditorCommandId.Undo: return session.Engine.Undo();
                case EditorCommandId.BeginBlock: session.BeginBlock(); break;
                case EditorCommandId.EndBlock: session.EndBlock(); break;
                case EditorCommandId.CopyBlock: session.Engine.CopyBlockToCursor(); break;
                case EditorCommandId.MoveBlock: session.Engine.MoveBlockToCursor(); break;
                case EditorCommandId.DeleteBlock: session.Engine.DeleteBlock(); break;
                case EditorCommandId.HideBlock: session.ToggleBlockHidden(); break;
                case EditorCommandId.CreateWindow:
                    return FirstEdCompatibilityProcessor.CreateWindow(
                        session,
                        RequireNumber(command),
                        RequireSecondNumber(command));
                case EditorCommandId.LinkWindow:
                    return FirstEdCompatibilityProcessor.LinkWindows(
                        session,
                        RequireNumber(command),
                        RequireSecondNumber(command));
                case EditorCommandId.PreviousWindow: return FirstEdCompatibilityProcessor.PreviousWindow(session);
                case EditorCommandId.NextWindow: return session.NextWindow() is not null;
                case EditorCommandId.GoToWindow: return FirstEdCompatibilityProcessor.GoToWindow(session, RequireNumber(command));
                case EditorCommandId.DeleteWindow:
                    return command.Number.HasValue
                        ? FirstEdCompatibilityProcessor.DeleteWindow(session, command.Number.Value)
                        : FirstEdCompatibilityProcessor.DeleteWindow(session, CurrentWindowNumber(session));
                case EditorCommandId.DeleteWindowText:
                    return session.DeleteCurrentWindowText();
                case EditorCommandId.SetLeftMargin: return FirstEdCompatibilityProcessor.SetLeftMargin(session, RequireNumber(command));
                case EditorCommandId.SetRightMargin: return FirstEdCompatibilityProcessor.SetRightMargin(session, RequireNumber(command));
                case EditorCommandId.SetTabWidth: return FirstEdCompatibilityProcessor.SetTabWidth(session, RequireNumber(command));
                case EditorCommandId.SetUndoLimit: return FirstEdCompatibilityProcessor.SetUndoLimit(session, RequireNumber(command));
                case EditorCommandId.SetMarker: session.SetMarker(RequireNumber(command)); break;
                case EditorCommandId.JumpMarker: return session.JumpToMarker(RequireNumber(command));
                case EditorCommandId.FindNext:
                    return session.Search.FindNext(RequireText(command), new SearchOptions()) is not null;
                case EditorCommandId.FindAgain:
                    return session.Search.FindAgain() is not null;
                case EditorCommandId.ReplaceNext:
                {
                    var parts = RequireText(command).Split('\0', 2);
                    if (parts.Length != 2)
                    {
                        throw new ArgumentException("ReplaceNext expects Text to contain pattern + NUL + replacement.", nameof(request));
                    }

                    return await session.Search.ReplaceNextAsync(parts[0], parts[1], cancellationToken: cancellationToken).ConfigureAwait(false);
                }
                case EditorCommandId.ReadFile:
                    await session.Files.ReadIntoCurrentWindowAsync(RequireText(command), cancellationToken).ConfigureAwait(false);
                    return true;
                case EditorCommandId.WriteFile:
                    await session.Files.WriteCurrentWindowAsync(RequireText(command), cancellationToken).ConfigureAwait(false);
                    return true;
                case EditorCommandId.SaveFile:
                    return await session.Files.SaveCurrentWindowAsync(command.Text, cancellationToken).ConfigureAwait(false);
                case EditorCommandId.Exit:
                    session.RequestRundown();
                    return true;
                default: return false;
            }

            return true;
        }
        catch (Exception exception) when (exception is ArgumentException
                                          or InvalidOperationException
                                          or IOException
                                          or UnauthorizedAccessException
                                          or NotSupportedException)
        {
            await session.Hooks.OnErrorAsync(
                session,
                new EditorError("EDITOR_COMMAND_FAILED", exception.Message, exception),
                cancellationToken).ConfigureAwait(false);
            return false;
        }
    }

    private static string RequireText(EditorCommandRequest command) =>
        !string.IsNullOrEmpty(command.Text)
            ? command.Text
            : throw new ArgumentException($"Command {command.Id} requires text.");

    private static int RequireNumber(EditorCommandRequest command) =>
        command.Number ?? throw new ArgumentException($"Command {command.Id} requires a numeric value.");

    private static int RequireSecondNumber(EditorCommandRequest command) =>
        command.Number2 ?? throw new ArgumentException($"Command {command.Id} requires a second numeric value.");

    private static int CurrentWindowNumber(EditorSession session)
    {
        for (var index = 0; index < session.Windows.Count; index++)
        {
            if (session.Windows[index].WindowId == session.CurrentWindow.WindowId)
            {
                return index + 1;
            }
        }

        throw new InvalidOperationException("The current window is not part of the editor session.");
    }
}
