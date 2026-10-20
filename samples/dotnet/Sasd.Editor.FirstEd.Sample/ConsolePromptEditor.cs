using System.Text;

namespace Sasd.Editor.FirstEd.Sample;

/// <summary>
/// Holds a single line of interactive terminal-prompt input, independently of
/// console painting and of the reusable editor command dispatcher.
/// </summary>
/// <remarks>
/// This is deliberately sample-host state, not an editor document. In
/// particular, an empty submitted answer and a cancelled prompt are different
/// outcomes: the command resolver must receive a value only for submission.
/// </remarks>
internal sealed class ConsolePromptEditor
{
    private readonly StringBuilder _answer = new();

    /// <summary>The complete, untruncated response to pass to the host resolver.</summary>
    public string Answer => _answer.ToString();

    /// <summary>
    /// Handles one physical prompt key without performing console operations.
    /// Ctrl-U cancels an active prompt as a host-level interruption; it does not
    /// leave an editor-wide abort flag that could affect a later command.
    /// </summary>
    public ConsolePromptInputResult Process(ConsoleKeyInfo key)
    {
        if (key.Key == ConsoleKey.Escape ||
            (key.Key == ConsoleKey.U &&
             (key.Modifiers & ConsoleModifiers.Control) != 0 &&
             (key.Modifiers & ConsoleModifiers.Alt) == 0))
        {
            return ConsolePromptInputResult.Cancelled;
        }

        if (key.Key == ConsoleKey.Enter)
        {
            return ConsolePromptInputResult.Submitted;
        }

        if (key.Key == ConsoleKey.Backspace)
        {
            if (_answer.Length > 0)
            {
                _answer.Length--;
            }

            return ConsolePromptInputResult.Continue;
        }

        // Keep printable characters (including AltGr-generated characters on
        // European keyboards) exactly as delivered by the host. A control key
        // with no printable result must not end up in a filename or search.
        if (!char.IsControl(key.KeyChar))
        {
            _answer.Append(key.KeyChar);
        }

        return ConsolePromptInputResult.Continue;
    }

    /// <summary>
    /// Projects the prompt and answer onto a fixed-width console row, always
    /// retaining the newest input at the right edge when it no longer fits.
    /// The return value is visual only; <see cref="Answer"/> is never clipped.
    /// </summary>
    public ConsolePromptDisplay Project(string prompt, int visibleColumns)
    {
        ArgumentNullException.ThrowIfNull(prompt);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(visibleColumns);

        var complete = prompt + _answer;
        var visible = complete.Length > visibleColumns
            ? complete[^visibleColumns..]
            : complete;

        // The host deliberately reserves the last physical column to avoid
        // terminal auto-wrap; position the cursor inside the visible range.
        return new ConsolePromptDisplay(
            visible,
            Math.Min(visible.Length, visibleColumns - 1));
    }
}

/// <summary>Outcome of one prompt key, before semantic command construction.</summary>
internal enum ConsolePromptInputResult
{
    Continue,
    Submitted,
    Cancelled
}

/// <summary>One console-row projection and its zero-based caret column.</summary>
internal readonly record struct ConsolePromptDisplay(string Text, int CaretColumn);
