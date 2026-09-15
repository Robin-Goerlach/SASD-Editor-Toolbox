using Sasd.Editor.Input;

namespace Sasd.Editor.FirstEd.Sample;

/// <summary>
/// Converts <see cref="ConsoleKeyInfo"/> into the platform-neutral key model
/// consumed by <see cref="FirstEdKeyMap"/>.
/// </summary>
internal static class ConsoleKeyTranslator
{
    public static EditorKeyStroke? Translate(ConsoleKeyInfo keyInfo)
    {
        var modifiers = TranslateModifiers(keyInfo.Modifiers);

        // Prefer the ConsoleKey value for Ctrl+A..Z. Terminal implementations
        // often expose a control code in KeyChar rather than the original
        // printable letter.
        if ((keyInfo.Modifiers & ConsoleModifiers.Control) != 0 &&
            (int)keyInfo.Key >= (int)ConsoleKey.A &&
            (int)keyInfo.Key <= (int)ConsoleKey.Z)
        {
            var letter = (char)('A' + ((int)keyInfo.Key - (int)ConsoleKey.A));
            return EditorKeyStroke.ForCharacter(letter, modifiers);
        }

        var special = keyInfo.Key switch
        {
            ConsoleKey.Enter => EditorKey.Enter,
            ConsoleKey.Escape => EditorKey.Escape,
            ConsoleKey.Backspace => EditorKey.Backspace,
            ConsoleKey.Delete => EditorKey.Delete,
            ConsoleKey.Tab => EditorKey.Tab,
            ConsoleKey.LeftArrow => EditorKey.ArrowLeft,
            ConsoleKey.RightArrow => EditorKey.ArrowRight,
            ConsoleKey.UpArrow => EditorKey.ArrowUp,
            ConsoleKey.DownArrow => EditorKey.ArrowDown,
            ConsoleKey.PageUp => EditorKey.PageUp,
            ConsoleKey.PageDown => EditorKey.PageDown,
            ConsoleKey.Home => EditorKey.Home,
            ConsoleKey.End => EditorKey.End,
            ConsoleKey.F1 => EditorKey.F1,
            ConsoleKey.F2 => EditorKey.F2,
            ConsoleKey.F3 => EditorKey.F3,
            ConsoleKey.F4 => EditorKey.F4,
            ConsoleKey.F5 => EditorKey.F5,
            ConsoleKey.F6 => EditorKey.F6,
            ConsoleKey.F7 => EditorKey.F7,
            ConsoleKey.F8 => EditorKey.F8,
            ConsoleKey.F9 => EditorKey.F9,
            ConsoleKey.F10 => EditorKey.F10,
            ConsoleKey.F11 => EditorKey.F11,
            ConsoleKey.F12 => EditorKey.F12,
            _ => (EditorKey?)null
        };

        if (special.HasValue)
        {
            return new EditorKeyStroke(special.Value, modifiers: modifiers);
        }

        if (keyInfo.KeyChar == '\0' || char.IsControl(keyInfo.KeyChar))
        {
            return null;
        }

        return EditorKeyStroke.ForCharacter(keyInfo.KeyChar, modifiers);
    }

    private static EditorKeyModifiers TranslateModifiers(ConsoleModifiers modifiers)
    {
        var result = EditorKeyModifiers.None;
        if ((modifiers & ConsoleModifiers.Control) != 0) result |= EditorKeyModifiers.Control;
        if ((modifiers & ConsoleModifiers.Shift) != 0) result |= EditorKeyModifiers.Shift;
        if ((modifiers & ConsoleModifiers.Alt) != 0) result |= EditorKeyModifiers.Alt;
        return result;
    }
}
