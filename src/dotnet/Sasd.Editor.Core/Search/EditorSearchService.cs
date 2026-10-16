using Sasd.Editor.Document;
using Sasd.Editor.Hooks;
using Sasd.Editor.Model;
using Sasd.Editor.Windows;

namespace Sasd.Editor.Search;

/// <summary>
/// Literal search and replace service. It also owns the remembered search state
/// required by the historical Find Again command. Regex and backwards search
/// can be added later without coupling them to the editor engine.
/// </summary>
public sealed class EditorSearchService
{
    private readonly Editing.EditorSession _session;
    private string? _lastPattern;
    private SearchOptions _lastOptions = new();
    private TextPosition? _lastMatch;
    private Guid? _lastDocumentId;
    private long _searchRevision;

    internal EditorSearchService(Editing.EditorSession session)
    {
        _session = session;
    }

    public string? LastPattern => _lastPattern;

    public TextPosition? FindNext(string pattern, SearchOptions? options = null)
    {
        // FIRST-ED EditFind interprets an empty *entered* search string as
        // "find again". Preserve the last pattern and its previous options;
        // do not overwrite them with defaults from an empty prompt.
        if (pattern == string.Empty)
        {
            return FindAgain();
        }

        ValidatePattern(pattern);
        options ??= new SearchOptions();

        var window = _session.CurrentWindow;
        var start = window.Cursor;
        var match = FindFrom(start, pattern, options);
        Remember(pattern, options, match, window.Document.DocumentId);

        if (match is not null)
        {
            window.Cursor = match.Value;
        }

        return match;
    }

    /// <summary>
    /// Interruptible semantic-command form of <see cref="FindNext"/>.
    /// </summary>
    /// <remarks>
    /// Search state and cursor position are committed only after the scan
    /// completes. A Ctrl-U or external cancellation therefore leaves both the
    /// remembered Find Again state and the current cursor unchanged.
    /// </remarks>
    public async ValueTask<TextPosition?> FindNextAsync(
        string pattern,
        SearchOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        // This is the same historical empty-prompt path as synchronous
        // FindNext, but repeat through the interruptible async scan.
        if (pattern == string.Empty)
        {
            return await FindAgainAsync(cancellationToken).ConfigureAwait(false);
        }

        ValidatePattern(pattern);
        options ??= new SearchOptions();

        var context = CaptureContext();
        var match = await FindFromAsync(
            context.Lines, context.Cursor, pattern, options, cancellationToken).ConfigureAwait(false);

        // An EditBreathe-style host callback can switch windows, change the
        // document or perform another search while this scan is suspended.
        // Never apply an obsolete result to the newly selected window.
        EnsureContextUnchanged(context);
        Remember(pattern, options, match, context.Document.DocumentId);
        if (match is not null)
        {
            context.Window.Cursor = match.Value;
        }

        return match;
    }

    /// <summary>
    /// Repeats the most recent search. When the cursor is still on the previous
    /// match, scanning resumes after that match instead of returning it again.
    /// </summary>
    public TextPosition? FindAgain()
    {
        if (string.IsNullOrEmpty(_lastPattern))
        {
            return null;
        }

        var window = _session.CurrentWindow;
        var start = window.Cursor;

        if (_lastDocumentId == window.Document.DocumentId
            && _lastMatch.HasValue
            && _lastMatch.Value == window.Cursor)
        {
            start = AdvancePastMatch(window.Document, window.Cursor, _lastPattern.Length);
        }

        var match = FindFrom(start, _lastPattern, _lastOptions);
        Remember(_lastPattern, _lastOptions, match, window.Document.DocumentId);

        if (match is not null)
        {
            window.Cursor = match.Value;
        }

        return match;
    }

    /// <summary>
    /// Interruptible semantic-command form of <see cref="FindAgain"/>.
    /// </summary>
    public async ValueTask<TextPosition?> FindAgainAsync(
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(_lastPattern))
        {
            return null;
        }

        var context = CaptureContext();
        var pattern = _lastPattern;
        var options = _lastOptions;
        var start = context.Cursor;
        if (_lastDocumentId == context.Document.DocumentId
            && _lastMatch.HasValue
            && _lastMatch.Value == context.Cursor)
        {
            start = AdvancePastMatch(context.Document, context.Cursor, pattern.Length);
        }

        var match = await FindFromAsync(
            context.Lines, start, pattern, options, cancellationToken).ConfigureAwait(false);

        // Repeated search must not overwrite a newer Find/Find Again request
        // that completed while this one was waiting at a host checkpoint.
        EnsureContextUnchanged(context);
        Remember(pattern, options, match, context.Document.DocumentId);
        if (match is not null)
        {
            context.Window.Cursor = match.Value;
        }

        return match;
    }

    public async ValueTask<bool> ReplaceNextAsync(
        string pattern,
        string replacement,
        SearchOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(replacement);
        ValidatePattern(pattern);

        // Find can yield to the host before the replacement confirmation even
        // starts. Retain that initiating view, not the potentially different
        // CurrentWindow selected when FindNextAsync returns.
        var initiatingWindow = _session.CurrentWindow;
        var match = await FindNextAsync(pattern, options, cancellationToken).ConfigureAwait(false);
        if (match is null)
        {
            return false;
        }

        if (!ReferenceEquals(_session.CurrentWindow, initiatingWindow))
        {
            throw new InvalidOperationException(
                "The replacement window changed while searching for the match.");
        }

        // The Find result is already committed to its originating view.
        // Capture that exact document and matched position before asking the
        // host for permission to replace. The hook is awaited and can change
        // the selection, text or cursor in the meantime.
        var context = CaptureContext();
        var candidate = new ReplaceCandidate(match.Value, pattern, replacement);
        var decision = await _session.Hooks.BeforeReplaceAsync(_session, candidate, cancellationToken).ConfigureAwait(false);
        if (decision == ReplaceDecision.Cancel)
        {
            return false;
        }

        EnsureContextUnchanged(context);
        if (decision == ReplaceDecision.Skip)
        {
            context.Window.Cursor = AdvancePastMatch(context.Document, match.Value, pattern.Length);
            return false;
        }

        // EditorEngine's synchronous mutation API acts on CurrentWindow.
        // Do not redirect it to a different view just because the host
        // changed focus while it was displaying a replace confirmation.
        if (!ReferenceEquals(_session.CurrentWindow, context.Window))
        {
            throw new InvalidOperationException(
                "The replacement window changed while awaiting host confirmation.");
        }

        _session.Engine.ReplaceSingleLineRange(match.Value, pattern.Length, replacement);
        return true;
    }

    public int ReplaceAll(string pattern, string replacement, SearchOptions? options = null)
    {
        ValidatePattern(pattern);
        ArgumentNullException.ThrowIfNull(replacement);
        options ??= new SearchOptions();

        var window = _session.CurrentWindow;
        var buffer = window.Document.Buffer;
        var comparison = options.CaseSensitive ? StringComparison.Ordinal : StringComparison.OrdinalIgnoreCase;
        var replacements = 0;
        var undo = _session.Undo.BeginTransaction(_session, window, "Replace all");

        for (var lineIndex = 0; lineIndex < buffer.LineCount; lineIndex++)
        {
            var text = buffer.GetLine(lineIndex).Text;
            var cursor = 0;
            var changed = false;
            var builder = new System.Text.StringBuilder();

            while (cursor <= text.Length - pattern.Length)
            {
                var index = text.IndexOf(pattern, cursor, comparison);
                if (index < 0)
                {
                    break;
                }

                if (options.WholeWord && !IsWholeWord(text, index, pattern.Length))
                {
                    builder.Append(text, cursor, index - cursor + 1);
                    cursor = index + 1;
                    continue;
                }

                builder.Append(text, cursor, index - cursor);
                builder.Append(replacement);
                cursor = index + pattern.Length;
                replacements++;
                changed = true;
            }

            if (changed)
            {
                builder.Append(text, cursor, text.Length - cursor);
                buffer.ReplaceLine(lineIndex, builder.ToString());
            }
        }

        if (replacements > 0)
        {
            window.Document.MarkChanged();
            undo.Commit();
        }

        return replacements;
    }

    private TextPosition? FindFrom(TextPosition start, string pattern, SearchOptions options)
    {
        var window = _session.CurrentWindow;
        var buffer = window.Document.Buffer;
        var comparison = options.CaseSensitive ? StringComparison.Ordinal : StringComparison.OrdinalIgnoreCase;
        var startLine = Math.Clamp(start.Line, 0, buffer.LineCount - 1);
        var startColumn = Math.Max(0, start.Column);

        var match = FindInRange(
            startLine,
            buffer.LineCount - 1,
            startColumn,
            pattern,
            options,
            comparison);

        if (match is null && options.WrapAround)
        {
            match = FindInRange(
                0,
                startLine,
                0,
                pattern,
                options,
                comparison,
                stopColumnExclusive: startColumn);
        }

        return match;
    }

    private async ValueTask<TextPosition?> FindFromAsync(
        IReadOnlyList<EditorLineSnapshot> lines,
        TextPosition start,
        string pattern,
        SearchOptions options,
        CancellationToken cancellationToken)
    {
        // Scan the initiating document's immutable logical-line snapshot.
        // Looking up CurrentWindow after a checkpoint would make the second
        // half of a search run in a different document after focus changes.
        var comparison = options.CaseSensitive ? StringComparison.Ordinal : StringComparison.OrdinalIgnoreCase;
        var startLine = Math.Clamp(start.Line, 0, lines.Count - 1);
        var startColumn = Math.Max(0, start.Column);

        var match = await FindInRangeAsync(
            lines,
            startLine,
            lines.Count - 1,
            startColumn,
            pattern,
            options,
            comparison,
            cancellationToken).ConfigureAwait(false);

        if (match is null && options.WrapAround)
        {
            match = await FindInRangeAsync(
                lines,
                0,
                startLine,
                0,
                pattern,
                options,
                comparison,
                cancellationToken,
                stopColumnExclusive: startColumn).ConfigureAwait(false);
        }

        return match;
    }

    private static TextPosition AdvancePastMatch(EditorDocument document, TextPosition position, int length)
    {
        var buffer = document.Buffer;
        var line = Math.Clamp(position.Line, 0, buffer.LineCount - 1);
        var text = buffer.GetLine(line).Text;
        // Virtual columns and very long patterns must not overflow int.
        var column = (int)Math.Clamp((long)position.Column + Math.Max(1, length), 0, text.Length);
        return new TextPosition(line, column);
    }

    private TextPosition? FindInRange(
        int startLine,
        int endLine,
        int startColumn,
        string pattern,
        SearchOptions options,
        StringComparison comparison,
        int? stopColumnExclusive = null)
    {
        var buffer = _session.CurrentWindow.Document.Buffer;
        for (var lineIndex = startLine; lineIndex <= endLine; lineIndex++)
        {
            var text = buffer.GetLine(lineIndex).Text;
            var column = lineIndex == startLine ? Math.Clamp(startColumn, 0, text.Length) : 0;
            var limit = lineIndex == endLine && stopColumnExclusive.HasValue
                ? Math.Clamp(stopColumnExclusive.Value, 0, text.Length)
                : text.Length;

            while (column <= limit - pattern.Length)
            {
                var index = text.IndexOf(pattern, column, comparison);
                if (index < 0 || index >= limit)
                {
                    break;
                }

                if (!options.WholeWord || IsWholeWord(text, index, pattern.Length))
                {
                    return new TextPosition(lineIndex, index);
                }

                column = index + 1;
            }
        }

        return null;
    }

    /// <summary>
    /// Scans one logical line at a time and yields to the host at each line
    /// boundary. This is the modern host-neutral counterpart of calling
    /// EditBreathe inside a potentially long loop.
    /// </summary>
    private async ValueTask<TextPosition?> FindInRangeAsync(
        IReadOnlyList<EditorLineSnapshot> lines,
        int startLine,
        int endLine,
        int startColumn,
        string pattern,
        SearchOptions options,
        StringComparison comparison,
        CancellationToken cancellationToken,
        int? stopColumnExclusive = null)
    {
        for (var lineIndex = startLine; lineIndex <= endLine; lineIndex++)
        {
            await _session.Interruption.CheckpointAsync(cancellationToken).ConfigureAwait(false);

            var text = lines[lineIndex].Text;
            var column = lineIndex == startLine ? Math.Clamp(startColumn, 0, text.Length) : 0;
            var limit = lineIndex == endLine && stopColumnExclusive.HasValue
                ? Math.Clamp(stopColumnExclusive.Value, 0, text.Length)
                : text.Length;

            while (column <= limit - pattern.Length)
            {
                var index = text.IndexOf(pattern, column, comparison);
                if (index < 0 || index >= limit)
                {
                    break;
                }

                if (!options.WholeWord || IsWholeWord(text, index, pattern.Length))
                {
                    return new TextPosition(lineIndex, index);
                }

                column = index + 1;
            }
        }

        return null;
    }

    private void Remember(string pattern, SearchOptions options, TextPosition? match, Guid documentId)
    {
        _lastPattern = pattern;
        _lastOptions = options;
        _lastMatch = match;
        _lastDocumentId = documentId;
        _searchRevision++;
    }

    /// <summary>
    /// Captures everything that gives an asynchronous search result meaning.
    /// Search/checkpoint hooks may run arbitrary host logic before completion.
    /// </summary>
    private SearchContext CaptureContext()
    {
        var window = _session.CurrentWindow;
        var document = window.Document;
        return new SearchContext(
            window, document, window.Cursor, document.Version,
            document.Buffer.Snapshot(), _searchRevision);
    }

    private void EnsureContextUnchanged(SearchContext context)
    {
        if (!_session.Windows.Any(view => ReferenceEquals(view, context.Window))
            || !ReferenceEquals(context.Window.Document, context.Document)
            || context.Window.Cursor != context.Cursor
            || context.Document.Version != context.Version
            || _searchRevision != context.SearchRevision
            || !context.Document.Buffer.Snapshot().SequenceEqual(context.Lines))
        {
            throw new InvalidOperationException(
                "The search destination or document changed during an awaited editor operation.");
        }
    }

    private sealed record SearchContext(
        EditorWindow Window,
        EditorDocument Document,
        TextPosition Cursor,
        long Version,
        IReadOnlyList<EditorLineSnapshot> Lines,
        long SearchRevision);

    private static void ValidatePattern(string pattern)
    {
        if (string.IsNullOrEmpty(pattern))
        {
            throw new ArgumentException("Search pattern cannot be empty.", nameof(pattern));
        }
    }

    private static bool IsWholeWord(string text, int index, int length)
    {
        var leftBoundary = index == 0 || !IsWordCharacter(text[index - 1]);
        var rightIndex = index + length;
        var rightBoundary = rightIndex >= text.Length || !IsWordCharacter(text[rightIndex]);
        return leftBoundary && rightBoundary;
    }

    private static bool IsWordCharacter(char ch) => char.IsLetterOrDigit(ch) || ch == '_';
}
