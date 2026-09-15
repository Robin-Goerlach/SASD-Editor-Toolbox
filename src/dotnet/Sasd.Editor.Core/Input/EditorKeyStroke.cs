namespace Sasd.Editor.Input;

/// <summary>
/// Modifier flags supplied by a UI host. They are deliberately independent of
/// WinForms, WPF, terminal and browser key types.
/// </summary>
[Flags]
public enum EditorKeyModifiers
{
    None = 0,
    Control = 1,
    Shift = 2,
    Alt = 4
}

/// <summary>
/// Normalized non-platform-specific keys used by editor key maps.
/// </summary>
public enum EditorKey
{
    Character,
    Enter,
    Escape,
    Backspace,
    Delete,
    Tab,
    ArrowLeft,
    ArrowRight,
    ArrowUp,
    ArrowDown,
    PageUp,
    PageDown,
    Home,
    End,
    F1,
    F2,
    F3,
    F4,
    F5,
    F6,
    F7,
    F8,
    F9,
    F10,
    F11,
    F12
}

/// <summary>
/// A normalized key stroke received from a host.
///
/// Character keys keep the original character so ordinary text input remains
/// distinct from command interpretation. Special keys carry no character.
/// </summary>
public readonly record struct EditorKeyStroke
{
    public EditorKeyStroke(
        EditorKey key,
        char character = '\0',
        EditorKeyModifiers modifiers = EditorKeyModifiers.None)
    {
        if (key == EditorKey.Character && character == '\0')
        {
            throw new ArgumentException("Character keys require a character value.", nameof(character));
        }

        if (key != EditorKey.Character && character != '\0')
        {
            throw new ArgumentException("Special keys cannot carry a character value.", nameof(character));
        }

        Key = key;
        Character = character;
        Modifiers = modifiers;
    }

    public EditorKey Key { get; }

    public char Character { get; }

    public EditorKeyModifiers Modifiers { get; }

    public static EditorKeyStroke ForCharacter(
        char character,
        EditorKeyModifiers modifiers = EditorKeyModifiers.None) =>
        new(EditorKey.Character, character, modifiers);

    public static EditorKeyStroke Control(char letter) =>
        new(EditorKey.Character, char.ToUpperInvariant(letter), EditorKeyModifiers.Control);

    public bool HasModifier(EditorKeyModifiers modifier) => (Modifiers & modifier) == modifier;

    public bool IsControlLetter(char letter) =>
        Key == EditorKey.Character &&
        HasModifier(EditorKeyModifiers.Control) &&
        char.ToUpperInvariant(Character) == char.ToUpperInvariant(letter);

    /// <summary>
    /// Returns the normalized command character used after a historical prefix.
    /// WordStar-style prefix sequences accept either a plain letter/digit or the
    /// corresponding Control+letter form. Alt-modified characters are excluded.
    /// </summary>
    public bool TryGetCommandCharacter(out char value)
    {
        value = '\0';
        if (Key != EditorKey.Character || HasModifier(EditorKeyModifiers.Alt))
        {
            return false;
        }

        if (!char.IsLetterOrDigit(Character))
        {
            return false;
        }

        value = char.ToUpperInvariant(Character);
        return true;
    }
}
