using Sasd.Editor.Model;

namespace Sasd.Editor.Abstractions;

/// <summary>
/// Language- and UI-neutral contract for logical line storage.
/// </summary>
public interface ITextBuffer
{
    int LineCount { get; }

    EditorLineSnapshot GetLine(int lineIndex);

    void ReplaceLine(int lineIndex, string text, EditorLineFlags? flags = null);

    void InsertLine(int lineIndex, string text, EditorLineFlags flags = EditorLineFlags.None);

    EditorLineSnapshot RemoveLine(int lineIndex);

    void SetFlags(int lineIndex, EditorLineFlags flags);

    IReadOnlyList<EditorLineSnapshot> Snapshot();

    void ReplaceAll(IEnumerable<EditorLineSnapshot> lines);
}
