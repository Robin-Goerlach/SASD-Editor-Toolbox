using Sasd.Editor.Hooks;
using Sasd.Editor.Model;

namespace Sasd.Editor.Search;

/// <summary>
/// Literal search and replace service. Regex and backwards search can be added
/// behind this service without coupling them to the editor engine.
/// </summary>
public sealed class EditorSearchService
{
    private readonly Editing.EditorSession _session;

    internal EditorSearchService(Editing.EditorSession session)
    {
        _session = session;
    }

    public TextPosition? FindNext(string pattern, SearchOptions? options = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(pattern);
        options ??= new SearchOptions();
        var window = _session.CurrentWindow;
        var buffer = window.Document.Buffer;
        var comparison = options.CaseSensitive ? StringComparison.Ordinal : StringComparison.OrdinalIgnoreCase;

        var match = FindInRange(window.Cursor.Line, buffer.LineCount - 1, window.Cursor.Column, pattern, options, comparison);
        if (match is null && options.WrapAround)
        {
            match = FindInRange(0, window.Cursor.Line, 0, pattern, options, comparison, stopColumnExclusive: window.Cursor.Column);
        }

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
            _session.CurrentWindow.Cursor = _session.CurrentWindow.Cursor with
            {
                Column = _session.CurrentWindow.Cursor.Column + Math.Max(1, pattern.Length)
            };
            return false;
        }

        _session.Engine.ReplaceSingleLineRange(match.Value, pattern.Length, replacement);
        return true;
    }

    public int ReplaceAll(string pattern, string replacement, SearchOptions? options = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(pattern);
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

    private static bool IsWholeWord(string text, int index, int length)
    {
        var leftBoundary = index == 0 || !IsWordCharacter(text[index - 1]);
        var rightIndex = index + length;
        var rightBoundary = rightIndex >= text.Length || !IsWordCharacter(text[rightIndex]);
        return leftBoundary && rightBoundary;
    }

    private static bool IsWordCharacter(char ch) => char.IsLetterOrDigit(ch) || ch == '_';
}
