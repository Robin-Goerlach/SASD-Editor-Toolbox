namespace Sasd.Editor.FirstEd.Sample;

/// <summary>
/// Explicit type of an output record. It is impossible to confuse a source
/// text line containing only a form feed with an intentional page break.
/// This is the sample's portable output contract, not a DOS printer API.
/// </summary>
internal enum MicroStarPrintRecordKind
{
    Text,
    PageBreak
}

/// <summary>
/// Immutable printing instruction independent of printer encoding. Text is
/// never interpreted as a control instruction, even when it equals "\\f".
/// </summary>
internal readonly record struct MicroStarPrintRecord(
    MicroStarPrintRecordKind Kind, string Text)
{
    internal static MicroStarPrintRecord Line(string text) =>
        new(MicroStarPrintRecordKind.Text,
            text ?? throw new ArgumentNullException(nameof(text)));

    internal static MicroStarPrintRecord Break() =>
        new(MicroStarPrintRecordKind.PageBreak, string.Empty);
}

/// <summary>
/// Creates a snapshot of typed print records from logical editor lines.
/// Pagination belongs to the application layer: no editor document or
/// scheduler implementation needs to know how many lines fit on a page.
/// </summary>
internal static class MicroStarTypedPrintPagination
{
    internal static IReadOnlyList<MicroStarPrintRecord> Prepare(
        IEnumerable<string> sourceLines, int linesPerPage = 60)
    {
        ArgumentNullException.ThrowIfNull(sourceLines);
        if (linesPerPage <= 0)
            throw new ArgumentOutOfRangeException(nameof(linesPerPage));

        var original = sourceLines.Select(line =>
            line ?? throw new ArgumentException("Null logical line.", nameof(sourceLines)))
            .ToArray();
        var result = new List<MicroStarPrintRecord>(original.Length);
        for (var index = 0; index < original.Length; index++)
        {
            // Insert a page instruction ONLY between pages. In particular
            // there is no trailing empty sheet after an exact final page.
            if (index != 0 && index % linesPerPage == 0)
                result.Add(MicroStarPrintRecord.Break());
            result.Add(MicroStarPrintRecord.Line(original[index]));
        }
        return result;
    }
}
