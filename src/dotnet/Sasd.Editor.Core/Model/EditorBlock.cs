namespace Sasd.Editor.Model;

/// <summary>
/// Whole-line block selection. V1 intentionally preserves the historical
/// whole-line block model; partial-line selections can be added independently.
/// </summary>
public sealed record EditorBlock(Guid DocumentId, int StartLine, int EndLine, bool Hidden = false)
{
    public int FirstLine => Math.Min(StartLine, EndLine);
    public int LastLine => Math.Max(StartLine, EndLine);
    public int LineCount => LastLine - FirstLine + 1;
}
