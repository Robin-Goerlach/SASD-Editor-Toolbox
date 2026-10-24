using Sasd.Editor.Commands;
using Sasd.Editor.Search;

namespace Sasd.Editor.FirstEd.Sample;

/// <summary>
/// Small, pure input policy for options the modern search service already
/// supports. The handbook refers to Optstr and to "Find/Replace with options"
/// but does NOT specify these particular C/W/R terminal shortcuts.
/// This vocabulary belongs strictly to our modern console sample.
/// </summary>
internal static class ConsoleFirstEdSearchOptionsPolicy
{
    /// <summary>
    /// Null means a cancelled prompt, while an empty submitted response
    /// deliberately selects the FIRST-ED default forward, case-insensitive
    /// substring search (no wrap). Reject unknown characters rather than
    /// silently performing a different search than the operator requested.
    /// </summary>
    internal static EditorCommandRequest? Attach(
        EditorCommandRequest? request, string? response)
    {
        if (request is null || response is null)
            return null;

        // FindAgain already retains its earlier options inside SearchService.
        // Other commands have no search-option meaning and are left intact.
        if (request.Id is not (EditorCommandId.FindNext or EditorCommandId.ReplaceNext))
            return request;

        var caseSensitive = false;
        var wholeWord = false;
        var wrapAround = false;
        var seen = new HashSet<char>();
        foreach (var raw in response)
        {
            // Accept only the documented separators. char.IsWhiteSpace
            // also recognizes line breaks and other invisible Unicode
            // separators; silently swallowing those would turn pasted,
            // multiline input into a different search request. The prompt
            // is one line and its option grammar is deliberately ASCII.
            if (raw is ' ' or ',' or ';')
                continue;

            // Reject control/invisible formatting characters rather than
            // normalizing them. This keeps diagnostic failure safe: an
            // unexpected pasted option must never dispatch a search with
            // flags the user did not intend.
            if (char.IsControl(raw) || char.IsWhiteSpace(raw))
                return null;

            // This is a deliberately ASCII-only host vocabulary, not the
            // undocumented Borland Optstr grammar. Normalize lowercase ASCII
            // explicitly rather than applying Unicode case folding. A pasted
            // lookalike or non-ASCII letter must not silently authorize a
            // different search than the operator requested.
            var option = raw is >= 'a' and <= 'z'
                ? (char)(raw - ('a' - 'A'))
                : raw;
            if (option is not ('C' or 'W' or 'R') || !seen.Add(option))
                return null;

            switch (option)
            {
                case 'C': caseSensitive = true; break;
                case 'W': wholeWord = true; break;
                case 'R': wrapAround = true; break;
            }
        }

        return request with
        {
            Options = new SearchOptions(
                CaseSensitive: caseSensitive,
                WholeWord: wholeWord,
                WrapAround: wrapAround)
        };
    }
}
