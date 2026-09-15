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
    public async ValueTask<bool> ExecuteAsync(EditorCommandRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        var command = session.Hooks.FilterCommand(session, request);

        try
        {
            switch (command.Id)
            {
                case EditorCommandId.CursorLeft: session.Engine.MoveLeft(); break;
                case EditorCommandId.CursorRight: session.Engine.MoveRight(); break;
                case EditorCommandId.CursorUp: session.Engine.MoveUp(); break;
                case EditorCommandId.CursorDown: session.Engine.MoveDown(); break;
                case EditorCommandId.PageUp: session.Engine.MovePageUp(command.PageSize ?? 20); break;
                case EditorCommandId.PageDown: session.Engine.MovePageDown(command.PageSize ?? 20); break;
                case EditorCommandId.WordLeft: session.Engine.MoveLeftWord(); break;
                case EditorCommandId.WordRight: session.Engine.MoveRightWord(); break;
                case EditorCommandId.BeginningOfLine: session.Engine.MoveBeginningOfLine(); break;
                case EditorCommandId.EndOfLine: session.Engine.MoveEndOfLine(); break;
                case EditorCommandId.TopOfFile: session.Engine.MoveTopOfFile(); break;
                case EditorCommandId.BottomOfFile: session.Engine.MoveBottomOfFile(); break;
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
                case EditorCommandId.CreateWindow: session.CreateDocument(); break;
                case EditorCommandId.LinkWindow: session.LinkWindow(session.CurrentWindow); break;
                case EditorCommandId.NextWindow: return session.NextWindow() is not null;
                case EditorCommandId.DeleteWindow: return session.CloseWindow(session.CurrentWindow.WindowId);
                case EditorCommandId.SetMarker: session.SetMarker(RequireNumber(command)); break;
                case EditorCommandId.JumpMarker: return session.JumpToMarker(RequireNumber(command));
                case EditorCommandId.FindNext: return session.Search.FindNext(RequireText(command), new SearchOptions()) is not null;
                case EditorCommandId.ReplaceNext:
                {
                    var parts = RequireText(command).Split('\0', 2);
                    if (parts.Length != 2)
                    {
                        throw new ArgumentException("ReplaceNext expects Text to contain pattern + NUL + replacement.", nameof(request));
                    }

                    return await session.Search.ReplaceNextAsync(parts[0], parts[1], cancellationToken: cancellationToken).ConfigureAwait(false);
                }
                default: return false;
            }

            return true;
        }
        catch (Exception exception) when (exception is ArgumentException or InvalidOperationException)
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
}
