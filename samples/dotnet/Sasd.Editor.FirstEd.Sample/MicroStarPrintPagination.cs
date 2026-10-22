namespace Sasd.Editor.FirstEd.Sample;

/// <summary>
/// Deliberately simple host-level pagination policy for printed text.
/// A page is a fixed number of output rows separated by form-feed.
/// </summary>
/// <remarks>
/// This is a documented SASD modernization policy, NOT a reconstruction of
/// undocumented Borland PrintNext control codes. Pagination happens before
/// constructing the background job so the scheduler can remain oblivious
/// to page geometry and output at most one record per idle cycle.
/// </remarks>
internal static class MicroStarPrintPagination
{
    /// <summary>
    /// Prepare output records from immutable logical-line text.
    /// Every input line becomes one output row, even when empty. A form-feed
    /// is inserted as its own record between pages, never after the last one.
    /// This first version deliberately does not wrap long lines, paginate
    /// by printer pixel width, or insert headers, footers or margins.
    /// </summary>
    internal static IReadOnlyList<string> Prepare(
        IEnumerable<string> sourceLines, int linesPerPage = 60)
    {
        ArgumentNullException.ThrowIfNull(sourceLines);
        if (linesPerPage < 1)
            throw new ArgumentOutOfRangeException(nameof(linesPerPage));

        // Taking a snapshot first means the caller can freely edit its
        // collection after scheduling without changing page boundaries.
        var lines = sourceLines.Select(line =>
            line ?? throw new ArgumentException("Null text line.", nameof(sourceLines)))
            .ToArray();
        var output = new List<string>(lines.Length);
        for (var i = 0; i < lines.Length; i++)
        {
            if (i > 0 && i % linesPerPage == 0)
                output.Add("\f");
            output.Add(lines[i]);
        }
        return output.ToArray();
    }
}
