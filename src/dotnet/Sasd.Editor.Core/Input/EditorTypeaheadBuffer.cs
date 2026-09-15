namespace Sasd.Editor.Input;

/// <summary>
/// Result of adding normalized input to <see cref="EditorTypeaheadBuffer"/>.
/// </summary>
public enum EditorTypeaheadWriteResult
{
    /// <summary>The input unit was queued successfully.</summary>
    Queued,

    /// <summary>No input was supplied, so the buffer was left unchanged.</summary>
    Ignored,

    /// <summary>
    /// A host-originated Ctrl-U requested the historical immediate abort behavior.
    /// The pending typeahead buffer was cleared and the Ctrl-U itself was not queued.
    /// </summary>
    Aborted,

    /// <summary>
    /// The bounded buffer could not accept the input. Historical compatibility
    /// requires the pending buffer to be cleared rather than partially retained.
    /// </summary>
    Overflow
}

/// <summary>
/// Bounded, host-independent typeahead queue inspired by the Turbo Editor
/// Toolbox INPUT.ED/USER.ED input buffer.
/// </summary>
/// <remarks>
/// The 1985 implementation stored raw bytes in a circular DOS-era buffer. The
/// modern core stores <see cref="EditorKeyStroke"/> values instead, so terminal,
/// WPF, WinForms and browser hosts share the same semantics without sharing scan
/// codes. Back insertion models physical keyboard/typeahead input; front insertion
/// models EditPushtbf/EditUserpush-style macro injection.
///
/// This class is deliberately simple and session-loop oriented. It is not a
/// concurrent producer/consumer collection; hosts that feed it from another
/// thread must marshal access to their editor session.
/// </remarks>
public sealed class EditorTypeaheadBuffer
{
    /// <summary>Historical default typeahead capacity (<c>Deftypahd</c>).</summary>
    public const int DefaultCapacity = 500;

    private readonly LinkedList<EditorKeyStroke> _buffer = new();

    public EditorTypeaheadBuffer(int capacity = DefaultCapacity)
    {
        Capacity = capacity > 0
            ? capacity
            : throw new ArgumentOutOfRangeException(nameof(capacity), "Typeahead capacity must be positive.");
    }

    public int Capacity { get; }

    public int Count => _buffer.Count;

    /// <summary>
    /// Equivalent to the historical Abortcmd state. Consumers that implement a
    /// long-running interruptible operation can observe and consume this flag.
    /// </summary>
    public bool AbortRequested { get; private set; }

    /// <summary>
    /// Adds normalized physical/host input to the back of the queue, corresponding
    /// to Pokechr's queue-style insertion. Ctrl-U is handled immediately and is
    /// never placed behind already buffered commands.
    /// </summary>
    public EditorTypeaheadWriteResult EnqueueFromHost(EditorKeyStroke keyStroke)
    {
        if (keyStroke.IsControlLetter('U'))
        {
            Clear();
            AbortRequested = true;
            return EditorTypeaheadWriteResult.Aborted;
        }

        if (_buffer.Count >= Capacity)
        {
            Clear();
            return EditorTypeaheadWriteResult.Overflow;
        }

        _buffer.AddLast(keyStroke);
        return EditorTypeaheadWriteResult.Queued;
    }

    /// <summary>
    /// Pushes one input unit onto the front of the queue so it becomes the next
    /// unit returned by <see cref="TryRead"/>. This corresponds to EditPushtbf.
    /// Unlike physical input, a pushed Ctrl-U is not interpreted as an immediate
    /// abort because the historical EditPushtbf path did not call EditAbort.
    /// </summary>
    public EditorTypeaheadWriteResult PushNext(EditorKeyStroke keyStroke)
    {
        if (_buffer.Count >= Capacity)
        {
            Clear();
            return EditorTypeaheadWriteResult.Overflow;
        }

        _buffer.AddFirst(keyStroke);
        return EditorTypeaheadWriteResult.Queued;
    }

    /// <summary>
    /// Pushes a sequence onto the front while preserving the sequence's natural
    /// read order. Internally this is the modern equivalent of EditUserpush's
    /// reverse insertion loop.
    /// </summary>
    public EditorTypeaheadWriteResult PushSequence(IEnumerable<EditorKeyStroke> keyStrokes)
    {
        ArgumentNullException.ThrowIfNull(keyStrokes);
        var items = keyStrokes.ToArray();
        if (items.Length == 0)
        {
            return EditorTypeaheadWriteResult.Ignored;
        }

        if (_buffer.Count + items.Length > Capacity)
        {
            // The Pascal routine would discover overflow while pushing elements
            // one by one and then clear the whole circular buffer. Preflight is
            // simpler but deliberately preserves the same final observable state.
            Clear();
            return EditorTypeaheadWriteResult.Overflow;
        }

        for (var index = items.Length - 1; index >= 0; index--)
        {
            _buffer.AddFirst(items[index]);
        }

        return EditorTypeaheadWriteResult.Queued;
    }

    /// <summary>
    /// Convenience form of <see cref="PushSequence"/> for macro strings. Classic
    /// ASCII control characters are normalized into semantic key strokes so a
    /// string containing Ctrl-K/Ctrl-O/Ctrl-Q can drive the same key map as a
    /// physical keyboard. CR/LF, Tab, Backspace, Escape and Delete receive their
    /// corresponding normalized special-key representation.
    /// </summary>
    public EditorTypeaheadWriteResult PushText(string? text)
    {
        if (string.IsNullOrEmpty(text))
        {
            return EditorTypeaheadWriteResult.Ignored;
        }

        var normalized = new List<EditorKeyStroke>(text.Length);
        for (var index = 0; index < text.Length; index++)
        {
            var character = text[index];
            if (character == '\r')
            {
                normalized.Add(new EditorKeyStroke(EditorKey.Enter));
                if (index + 1 < text.Length && text[index + 1] == '\n')
                {
                    index++;
                }

                continue;
            }

            if (character == '\n')
            {
                normalized.Add(new EditorKeyStroke(EditorKey.Enter));
                continue;
            }

            if (character == '\t')
            {
                normalized.Add(new EditorKeyStroke(EditorKey.Tab));
                continue;
            }

            if (character == '\b')
            {
                normalized.Add(new EditorKeyStroke(EditorKey.Backspace));
                continue;
            }

            if (character == '\u001b')
            {
                normalized.Add(new EditorKeyStroke(EditorKey.Escape));
                continue;
            }

            if (character == '\u007f')
            {
                normalized.Add(new EditorKeyStroke(EditorKey.Delete));
                continue;
            }

            if (character is >= '\u0001' and <= '\u001a')
            {
                normalized.Add(EditorKeyStroke.Control((char)('A' + character - 1)));
                continue;
            }

            // NUL has no meaningful normalized key representation. Treating it
            // as an empty macro element is safer than manufacturing a key event.
            if (character == '\0')
            {
                continue;
            }

            normalized.Add(EditorKeyStroke.ForCharacter(character));
        }

        return PushSequence(normalized);
    }

    /// <summary>Returns and removes the oldest/next logical input unit.</summary>
    public bool TryRead(out EditorKeyStroke keyStroke)
    {
        if (_buffer.First is null)
        {
            keyStroke = default;
            return false;
        }

        keyStroke = _buffer.First.Value;
        _buffer.RemoveFirst();
        return true;
    }

    /// <summary>
    /// Returns the current abort state and resets it. This mirrors the historical
    /// pattern where interruptible routines checked and then handled Abortcmd.
    /// </summary>
    public bool ConsumeAbortRequest()
    {
        var requested = AbortRequested;
        AbortRequested = false;
        return requested;
    }

    public void Clear() => _buffer.Clear();
}
