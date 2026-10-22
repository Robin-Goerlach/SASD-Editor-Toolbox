using System.Globalization;
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
    /// Clear the editable response when a host starts a new prompt or
    /// explicitly dismisses the old command-line contents.
    /// </summary>
    /// <remarks>
    /// This is an in-memory reset only. It deliberately does not move the
    /// terminal cursor, overwrite the physical command row or mutate the
    /// editor document. Those are separate presentation responsibilities.
    /// Because cancellation is returned as a per-key result rather than
    /// stored in this model, resetting cannot leak an old abort into a
    /// subsequent prompt.
    /// </remarks>
    public void Reset()
    {
        _answer.Clear();
    }

    /// <summary>
    /// Handles one physical prompt key without performing console operations.
    /// Ctrl-U cancels an active prompt as a host-level interruption; it does not
    /// leave an editor-wide abort flag that could affect a later command.
    /// </summary>
    public ConsolePromptInputResult Process(ConsoleKeyInfo key)
    {
        // Named control keys are not portable: some Unix terminal drivers
        // report Ctrl-U only as KeyChar 0x15 with an unspecified ConsoleKey.
        // Share the host's cancellation policy with the Ctrl-P prompt.
        if (ConsolePromptKeyPolicy.IsCancellation(key))
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
                // A .NET char is a UTF-16 code unit, not necessarily one
                // user-visible character. A surrogate pair (emoji) or a
                // base letter plus combining accent must disappear as one
                // text element when Backspace is pressed. Removing only
                // the last code unit could leave an invalid surrogate or
                // orphaned combining mark in a filename/search argument.
                //
                // StringInfo is used only in this sample-host input model.
                // The reusable editor's text-storage representation and
                // semantic commands remain completely unaffected.
                var textElements = StringInfo.ParseCombiningCharacters(
                    _answer.ToString());
                _answer.Length = textElements[^1];
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
        var firstVisibleCodeUnit = Math.Max(0, complete.Length - visibleColumns);

        if (firstVisibleCodeUnit > 0)
        {
            // Right-scrolling by raw UTF-16 offsets can start in the
            // middle of a surrogate pair or combining sequence. Choose
            // the NEXT whole text-element boundary instead of painting
            // broken Unicode onto the command/status row. It is safer
            // to show one fewer code unit than to output half a symbol.
            //
            // VisibleColumns is the current conservative UTF-16 budget.
            // Exact cell widths of wide/emoji glyphs are terminal-specific,
            // and still require platform smoke tests.
            var alignedStart = complete.Length;
            foreach (var boundary in StringInfo.ParseCombiningCharacters(complete))
            {
                if (boundary >= firstVisibleCodeUnit)
                {
                    alignedStart = boundary;
                    break;
                }
            }

            firstVisibleCodeUnit = alignedStart;
        }

        var visible = complete[firstVisibleCodeUnit..];

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
