namespace Sasd.Editor.FirstEd.Sample;

/// <summary>
/// Converts the second key of the historical Ctrl-P / EditInsertCtrlChar
/// sequence into a literal ASCII control character.
/// </summary>
/// <remarks>
/// The handbook (EditInsertCtrlChar, printed p. 182) separates collecting one
/// typeahead character from translating that character to its ASCII control
/// value. ConsoleKeyInfo is a host detail, so this conversion belongs to the
/// FIRST-ED sample, not to the reusable EditorCommandDispatcher or document.
///
/// The portable semantic command already receives a literal control character;
/// translating there would make callers supplying '\u0001' and callers supplying
/// 'A' observe different, host-dependent rules. This small policy maintains a
/// single, testable host boundary.
/// </remarks>
internal static class ConsoleFirstEdControlCharacterPromptPolicy
{
    /// <summary>
    /// Resolves one physical key to a control character or null for cancellation
    /// or unsupported input. A null result must never be dispatched as a command.
    /// </summary>
    internal static char? Resolve(ConsoleKeyInfo key)
    {
        // Escape and the editor's interrupt chord cancel the pending Ctrl-P
        // prompt. In particular Ctrl-U must NOT insert U / 0x15 by mistake.
        // On some terminals Ctrl+U arrives as the literal control code even
        // when the corresponding ConsoleKey is unavailable.
        // Use precisely the same host-level cancellation decision as the
        // multi-character Askfor prompt; raw Ctrl-U and raw ESC may arrive
        // without a named ConsoleKey on some terminals.
        if (ConsolePromptKeyPolicy.IsCancellation(key))
        {
            return null;
        }

        // An Enter key is prompt navigation, not an instruction to append
        // carriage return to a logical line. The existing text editor treats
        // CR specially; passing it on would acknowledge a command with no
        // stored control character.
        if (key.Key is ConsoleKey.Enter or ConsoleKey.Backspace
            || key.KeyChar is '\0' or '\r' or '\n')
        {
            return null;
        }

        var character = key.KeyChar;

        // Typical physical Ctrl+A..Z keys are delivered already translated
        // (0x01..0x1A). Preserve those actual control bytes rather than
        // reinterpreting the key's printable letter or applying a second
        // conversion. Ctrl+U above is reserved for interruption.
        if (character is >= '\u0001' and <= '\u001a')
        {
            return character;
        }

        // The ordinary prompt takes a printable A..Z key, without requiring
        // the user to hold Ctrl twice. Letter case must not affect the result.
        // ASCII's standard control mapping is uppercase letter minus '@'.
        // Test the ASCII ranges *before* mapping. Unicode case folding can
        // turn non-ASCII letters (for example long s) into ASCII 'S'; those
        // characters are not part of the original ASCII control alphabet.
        if (character is >= 'A' and <= 'Z')
        {
            return (char)(character - '@');
        }

        if (character is >= 'a' and <= 'z')
        {
            return (char)(character - 'a' + 1);
        }

        // The six punctuation keys adjacent to the alphabetic range form
        // the remaining ASCII controls. A printable '[' is therefore a way
        // to request ESC even though the physical Escape key cancels the
        // prompt. No general Unicode/control mapping is claimed here.
        return character switch
        {
            '@' => '\u0000',
            '[' => '\u001b',
            '\\' => '\u001c',
            ']' => '\u001d',
            '^' => '\u001e',
            '_' => '\u001f',
            _ => null
        };
    }
}
