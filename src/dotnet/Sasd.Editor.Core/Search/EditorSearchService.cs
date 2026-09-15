using Sasd.Editor.Hooks;
using Sasd.Editor.Model;

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

    internal EditorSearchService(Editing.EditorSession session)
    {
        _session = session;
    }

    public string? LastPattern => _lastPattern;

    public TextPosition? FindNext(string pattern, SearchOptions? options = null)
    {
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
            start = AdvancePastMatch(window.Cursor, _lastPattern.Length);
        }

        var match = FindFrom(start, _lastPattern, _lastOptions);
        Remember(_lastPattern, _lastOptions, match, window.Document.DocumentId);

        if (match is not null)
        {
            window.Cursor = match.Value;
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
        var match = FindNext(pattern, options);
        if (match is null)
        {
            return false;
        }

        var candidate = new ReplaceCandidate(match.Value, pattern, replacement);
        var decision = await _session.Hooks.BeforeReplaceAsync(_session, candidate, cancellationToken).ConfigureAwait(false);
        if (decision == ReplaceDecision.Cancel)
        {
            return false;
        }

        if (decision == ReplaceDecision.Skip)
        {
            _session.CurrentWindow.Cursor = AdvancePastMatch(match.Value, Math.Max(1, pattern.Length));
            return false;
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
        _session.Undo.Capture(window, "Replace all");

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

    private TextPosition AdvancePastMatch(TextPosition position, int length)
    {
        var buffer = _session.CurrentWindow.Document.Buffer;
        var line = Math.Clamp(position.Line, 0, buffer.LineCount - 1);
        var text = buffer.GetLine(line).Text;
        var column = Math.Clamp(position.Column + Math.Max(1, length), 0, text.Length);
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

    private void Remember(string pattern, SearchOptions options, TextPosition? match, Guid documentId)
    {
        _lastPattern = pattern;
        _lastOptions = options;
        _lastMatch = match;
        _lastDocumentId = documentId;
    }

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
