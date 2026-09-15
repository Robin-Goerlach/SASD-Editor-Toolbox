namespace Sasd.Editor.Model;

/// <summary>
/// Identifies a zero-based line and column in a document.
/// </summary>
public readonly record struct TextPosition(int Line, int Column) : IComparable<TextPosition>
{
    public int CompareTo(TextPosition other)
    {
        var lineComparison = Line.CompareTo(other.Line);
        return lineComparison != 0 ? lineComparison : Column.CompareTo(other.Column);
    }

    public static TextPosition Min(TextPosition left, TextPosition right) =>
        left.CompareTo(right) <= 0 ? left : right;

    public static TextPosition Max(TextPosition left, TextPosition right) =>
        left.CompareTo(right) >= 0 ? left : right;
}
