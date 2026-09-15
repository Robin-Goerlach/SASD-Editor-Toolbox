using Sasd.Editor.Commands;

namespace Sasd.Editor.Input;

/// <summary>
/// WordStar-style command prefixes used by the historical FIRST-ED default
/// editor supplied with Turbo Editor Toolbox 1.0.
/// </summary>
public enum FirstEdCommandPrefix
{
    None,
    CtrlK,
    CtrlO,
    CtrlQ
}

/// <summary>
/// Stateful translation of FIRST-ED keystrokes into SASD semantic commands.
///
/// The historical editor first classified keyboard input and then delegated
/// Ctrl-K, Ctrl-O and Ctrl-Q to separate prefix dispatchers. This class keeps
/// the same two-stage behavior without coupling the core to DOS scan codes.
/// Prompting is intentionally outside the key map: bindings state which value a
/// host must collect, and the host then creates an <see cref="EditorCommandRequest"/>.
/// </summary>
public sealed class FirstEdKeyMap
{
    public FirstEdCommandPrefix PendingPrefix { get; private set; }

    public void Reset() => PendingPrefix = FirstEdCommandPrefix.None;

    public EditorInputAction Translate(EditorKeyStroke keyStroke)
    {
        if (PendingPrefix != FirstEdCommandPrefix.None)
        {
            return TranslatePrefixed(keyStroke);
        }

        var special = TranslateSpecialKey(keyStroke);
        if (special is not null)
        {
            return special;
        }

        if (keyStroke.Key != EditorKey.Character)
        {
            return EditorInputAction.Ignored;
        }

        if (keyStroke.HasModifier(EditorKeyModifiers.Alt))
        {
            return EditorInputAction.Ignored;
        }

        if (keyStroke.HasModifier(EditorKeyModifiers.Control))
        {
            var letter = char.ToUpperInvariant(keyStroke.Character);
            if (letter is 'K' or 'O' or 'Q')
            {
                PendingPrefix = letter switch
                {
                    'K' => FirstEdCommandPrefix.CtrlK,
                    'O' => FirstEdCommandPrefix.CtrlO,
                    _ => FirstEdCommandPrefix.CtrlQ
                };

                return EditorInputAction.AwaitPrefix($"Ctrl-{letter}");
            }

            return TranslatePrimaryControl(letter);
        }

        // Shift is already reflected in Character by the host. Ordinary
        // printable characters therefore flow to the text processor unchanged.
        return char.IsControl(keyStroke.Character)
            ? EditorInputAction.Ignored
            : EditorInputAction.Insert(keyStroke.Character.ToString());
    }

    private EditorInputAction TranslatePrefixed(EditorKeyStroke keyStroke)
    {
        if (keyStroke.Key == EditorKey.Escape)
        {
            Reset();
            return EditorInputAction.Ignored;
        }

        var prefix = PendingPrefix;
        Reset();

        if (!keyStroke.TryGetCommandCharacter(out var commandCharacter))
        {
            return EditorInputAction.Ignored;
        }

        return prefix switch
        {
            FirstEdCommandPrefix.CtrlK => TranslateCtrlK(commandCharacter),
            FirstEdCommandPrefix.CtrlO => TranslateCtrlO(commandCharacter),
            FirstEdCommandPrefix.CtrlQ => TranslateCtrlQ(commandCharacter),
            _ => EditorInputAction.Ignored
        };
    }

    private static EditorInputAction? TranslateSpecialKey(EditorKeyStroke keyStroke) => keyStroke.Key switch
    {
        EditorKey.Enter => Command(EditorCommandId.InsertLine),
        EditorKey.Escape => Command(EditorCommandId.Undo),
        EditorKey.Backspace => Command(EditorCommandId.DeleteLeftCharacter),
        EditorKey.Delete => Command(EditorCommandId.DeleteRightCharacter),
        EditorKey.Tab => Command(EditorCommandId.Tab),
        EditorKey.ArrowLeft => Command(EditorCommandId.CursorLeft),
        EditorKey.ArrowRight => Command(EditorCommandId.CursorRight),
        EditorKey.ArrowUp => Command(EditorCommandId.CursorUp),
        EditorKey.ArrowDown => Command(EditorCommandId.CursorDown),
        EditorKey.PageUp => Command(EditorCommandId.PageUp),
        EditorKey.PageDown => Command(EditorCommandId.PageDown),
        EditorKey.Home => Command(EditorCommandId.BeginningOfLine),
        EditorKey.End => Command(EditorCommandId.EndOfLine),
        _ => null
    };

    private static EditorInputAction TranslatePrimaryControl(char letter) => letter switch
    {
        'A' => Command(EditorCommandId.WordLeft),
        'S' => Command(EditorCommandId.CursorLeft),
        'D' => Command(EditorCommandId.CursorRight),
        'F' => Command(EditorCommandId.WordRight),
        'E' => Command(EditorCommandId.CursorUp),
        'X' => Command(EditorCommandId.CursorDown),
        'C' => Command(EditorCommandId.PageDown),
        'W' => Command(EditorCommandId.ScrollUp),
        'Z' => Command(EditorCommandId.ScrollDown),
        'P' => Command(EditorCommandId.InsertControlCharacter, EditorCommandArgumentKind.Character),
        'J' => Command(EditorCommandId.BeginningOrEndOfLine),
        'N' => Command(EditorCommandId.InsertLine),
        'G' => Command(EditorCommandId.DeleteRightCharacter),
        'H' => Command(EditorCommandId.DeleteLeftCharacter),
        'R' => Command(EditorCommandId.PageUp),
        'T' => Command(EditorCommandId.DeleteRightWord),
        'Y' => Command(EditorCommandId.DeleteLine),
        'B' => Command(EditorCommandId.ReformatParagraph),
        'V' => Command(EditorCommandId.ToggleInsert),
        'L' => Command(EditorCommandId.FindAgain),
        _ => EditorInputAction.Ignored
    };

    private static EditorInputAction TranslateCtrlK(char commandCharacter)
    {
        if (TryDigit(commandCharacter, out var number))
        {
            return Command(EditorCommandId.SetMarker, fixedNumber: number);
        }

        return commandCharacter switch
        {
            'B' => Command(EditorCommandId.BeginBlock),
            'K' => Command(EditorCommandId.EndBlock),
            'C' => Command(EditorCommandId.CopyBlock),
            'V' => Command(EditorCommandId.MoveBlock),
            'Y' => Command(EditorCommandId.DeleteBlock),
            'H' => Command(EditorCommandId.HideBlock),
            'R' => Command(EditorCommandId.ReadFile, EditorCommandArgumentKind.FilePath),
            'W' => Command(EditorCommandId.WriteFile, EditorCommandArgumentKind.FilePath),
            'S' => Command(EditorCommandId.SaveFile),
            'T' => Command(EditorCommandId.SetTabWidth, EditorCommandArgumentKind.Number),
            'X' => Command(EditorCommandId.Exit, EditorCommandArgumentKind.Confirmation),
            'M' => Command(EditorCommandId.SetMarker, EditorCommandArgumentKind.Number),
            _ => EditorInputAction.Ignored
        };
    }

    private static EditorInputAction TranslateCtrlO(char commandCharacter)
    {
        if (TryDigit(commandCharacter, out var number))
        {
            return Command(EditorCommandId.GoToWindow, fixedNumber: number);
        }

        return commandCharacter switch
        {
            'X' => Command(EditorCommandId.NextWindow),
            'G' => Command(EditorCommandId.GoToWindow, EditorCommandArgumentKind.Number),
            'E' => Command(EditorCommandId.PreviousWindow),
            'J' => Command(EditorCommandId.LinkWindow, EditorCommandArgumentKind.TwoNumbers),
            'Y' => Command(EditorCommandId.DeleteWindow, EditorCommandArgumentKind.Number),
            'O' => Command(EditorCommandId.CreateWindow, EditorCommandArgumentKind.TwoNumbers),
            'W' => Command(EditorCommandId.ToggleWordWrap),
            'C' => Command(EditorCommandId.CenterLine),
            'I' => Command(EditorCommandId.GoToColumn, EditorCommandArgumentKind.Number),
            'N' => Command(EditorCommandId.GoToLine, EditorCommandArgumentKind.Number),
            'K' => Command(EditorCommandId.ChangeCase),
            'L' => Command(EditorCommandId.SetLeftMargin, EditorCommandArgumentKind.Number),
            'R' => Command(EditorCommandId.SetRightMargin, EditorCommandArgumentKind.Number),
            'S' => Command(EditorCommandId.SetUndoLimit, EditorCommandArgumentKind.Number),
            _ => EditorInputAction.Ignored
        };
    }

    private static EditorInputAction TranslateCtrlQ(char commandCharacter)
    {
        if (TryDigit(commandCharacter, out var number))
        {
            return Command(EditorCommandId.JumpMarker, fixedNumber: number);
        }

        return commandCharacter switch
        {
            'C' => Command(EditorCommandId.BottomOfFile),
            'R' => Command(EditorCommandId.TopOfFile),
            'I' => Command(EditorCommandId.ToggleAutoIndent),
            'B' => Command(EditorCommandId.TopOfBlock),
            'K' => Command(EditorCommandId.BottomOfBlock),
            'J' => Command(EditorCommandId.JumpMarker, EditorCommandArgumentKind.Number),
            'A' => Command(EditorCommandId.ReplaceNext, EditorCommandArgumentKind.FindReplace),
            'F' => Command(EditorCommandId.FindNext, EditorCommandArgumentKind.Text),
            'D' => Command(EditorCommandId.EndOfLine),
            'S' => Command(EditorCommandId.BeginningOfLine),
            'Y' => Command(EditorCommandId.DeleteToEndOfLine),
            _ => EditorInputAction.Ignored
        };
    }

    private static bool TryDigit(char value, out int number)
    {
        if (value is >= '1' and <= '9')
        {
            number = value - '0';
            return true;
        }

        number = 0;
        return false;
    }

    private static EditorInputAction Command(
        EditorCommandId commandId,
        EditorCommandArgumentKind argumentKind = EditorCommandArgumentKind.None,
        int? fixedNumber = null) =>
        EditorInputAction.Execute(new EditorCommandBinding(commandId, argumentKind, fixedNumber));
}
