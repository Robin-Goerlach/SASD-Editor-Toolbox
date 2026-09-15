using Sasd.Editor.Abstractions;
using Sasd.Editor.Model;

namespace Sasd.Editor.Document;

/// <summary>
/// Linked-list text buffer inspired by the line-descriptor model used by the
/// historical toolbox. The public contract deliberately hides the storage
/// choice so a piece table, rope or gap buffer can be introduced later.
/// </summary>
public sealed class LinkedLineTextBuffer : ITextBuffer
{
    private sealed class LineState(string text, EditorLineFlags flags)
    {
        public string Text { get; set; } = text;
        public EditorLineFlags Flags { get; set; } = flags;
    }

    private readonly LinkedList<LineState> _lines = new();

    public LinkedLineTextBuffer(IEnumerable<EditorLineSnapshot>? lines = null)
    {
        ReplaceAll(lines ?? [new EditorLineSnapshot(string.Empty)]);
    }

    public int LineCount => _lines.Count;

    public EditorLineSnapshot GetLine(int lineIndex)
    {
        var node = GetNode(lineIndex);
        return new EditorLineSnapshot(node.Value.Text, node.Value.Flags);
    }

    public void ReplaceLine(int lineIndex, string text, EditorLineFlags? flags = null)
    {
        ArgumentNullException.ThrowIfNull(text);
        var node = GetNode(lineIndex);
        node.Value.Text = text;
        if (flags.HasValue)
        {
            node.Value.Flags = flags.Value;
        }
    }

    public void InsertLine(int lineIndex, string text, EditorLineFlags flags = EditorLineFlags.None)
    {
        ArgumentNullException.ThrowIfNull(text);
        if (lineIndex < 0 || lineIndex > _lines.Count)
        {
            throw new ArgumentOutOfRangeException(nameof(lineIndex));
        }

        if (lineIndex == _lines.Count)
        {
            _lines.AddLast(new LineState(text, flags));
            return;
        }

        _lines.AddBefore(GetNode(lineIndex), new LineState(text, flags));
    }

    public EditorLineSnapshot RemoveLine(int lineIndex)
    {
        var node = GetNode(lineIndex);
        var removed = new EditorLineSnapshot(node.Value.Text, node.Value.Flags);

        if (_lines.Count == 1)
        {
            node.Value.Text = string.Empty;
            node.Value.Flags = EditorLineFlags.None;
            return removed;
        }

        _lines.Remove(node);
        return removed;
    }

    public void SetFlags(int lineIndex, EditorLineFlags flags) => GetNode(lineIndex).Value.Flags = flags;

    public IReadOnlyList<EditorLineSnapshot> Snapshot() =>
        _lines.Select(static line => new EditorLineSnapshot(line.Text, line.Flags)).ToArray();

    public void ReplaceAll(IEnumerable<EditorLineSnapshot> lines)
    {
        ArgumentNullException.ThrowIfNull(lines);
        _lines.Clear();

        foreach (var line in lines)
        {
            ArgumentNullException.ThrowIfNull(line);
            _lines.AddLast(new LineState(line.Text, line.Flags));
        }

        if (_lines.Count == 0)
        {
            _lines.AddLast(new LineState(string.Empty, EditorLineFlags.None));
        }
    }

    private LinkedListNode<LineState> GetNode(int lineIndex)
    {
        if (lineIndex < 0 || lineIndex >= _lines.Count)
        {
            throw new ArgumentOutOfRangeException(nameof(lineIndex));
        }

        if (lineIndex <= _lines.Count / 2)
        {
            var node = _lines.First!;
            for (var index = 0; index < lineIndex; index++)
            {
                node = node.Next!;
            }

            return node;
        }

        var reverseNode = _lines.Last!;
        for (var index = _lines.Count - 1; index > lineIndex; index--)
        {
            reverseNode = reverseNode.Previous!;
        }

        return reverseNode;
    }
}
