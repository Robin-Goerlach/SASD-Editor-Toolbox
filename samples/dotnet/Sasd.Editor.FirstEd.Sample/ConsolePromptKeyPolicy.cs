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
    internal static bool IsCancellation(ConsoleKeyInfo key)
    {
        // Physical Escape can also arrive as raw ESC rather than a named key.
        if (key.Key == ConsoleKey.Escape || key.KeyChar == '\u001b')
            return true;

        // Do not interpret Ctrl+Alt/AltGr-generated printable characters as
        // a Ctrl-U cancellation. A raw Ctrl-U code and a named Ctrl+U chord
        // are both valid independent indications when Alt is NOT present.
        if ((key.Modifiers & ConsoleModifiers.Alt) != 0)
            return false;

        return key.KeyChar == '\u0015'
            || (key.Key == ConsoleKey.U
                && (key.Modifiers & ConsoleModifiers.Control) != 0);
    }
}
