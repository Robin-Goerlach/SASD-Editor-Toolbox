namespace Sasd.Editor.FirstEd.Sample;

/// <summary>
/// Recognizes terminal-level cancellation of a single interactive prompt.
/// </summary>
/// <remarks>
/// ConsoleKeyInfo is not guaranteed to carry the same logical ConsoleKey on
/// every terminal. For example, Ctrl-U may arrive as character 0x15 with
/// ConsoleKey.NoName on a Unix pseudo-terminal. The keyboard-facing sample
/// uses one shared rule for both text and Ctrl-P character prompts; Core has
/// no dependency on physical keys or keyboard layouts.
///
/// The FIRST-ED sample's modern policy reserves Ctrl-U for *cancel*. The
/// original EditAskfor (1985 manual p. 122) instead documented using Ctrl-U
/// to recall the previous Varstring. We record that divergence explicitly
/// rather than silently changing existing Save/Find cancellation semantics.
/// </remarks>
internal static class ConsolePromptKeyPolicy
{
    internal static bool IsCancellation(ConsoleKeyInfo key) =>
        IsCancellation(key.Key, key.KeyChar, key.Modifiers);

    /// <summary>
    /// Shared cancellation decision for modal prompts and the MicroStar F10
    /// menu. Keeping the three primitive console fields in this overload lets
    /// pure menu-navigation tests exercise terminal-specific encodings without
    /// constructing a console or duplicating the interpretation policy.
    /// </summary>
    internal static bool IsCancellation(
        ConsoleKey key, char character, ConsoleModifiers modifiers)
    {
        // Escape may be reported as a named virtual key or as the raw ESC
        // byte by a pseudo-terminal; either form always cancels the dialog.
        if (key == ConsoleKey.Escape || character == '\u001b')
            return true;

        // Windows AltGr may appear as Ctrl+Alt. Even when another terminal
        // supplies a raw Ctrl-U byte together with Alt, do not turn an
        // Alt-modified gesture into an unintended cancellation.
        if ((modifiers & ConsoleModifiers.Alt) != 0)
            return false;

        // Both the physical Ctrl-U chord and its raw byte representation are
        // accepted. This is a deliberate modern host policy; it does not
        // reconstruct the historical EditAskfor Ctrl-U recall behavior.
        return character == '\u0015'
            || (key == ConsoleKey.U
                && (modifiers & ConsoleModifiers.Control) != 0);
    }
}
