using Sasd.Editor.Document;
using Sasd.Editor.Editing;
using Sasd.Editor.Model;
using Sasd.Editor.Windows;

namespace Sasd.Editor.Undo;

/// <summary>
/// Correctness-first snapshot undo. The interface boundary is intentionally
/// narrow so a delta/command journal can replace it without changing callers.
/// </summary>
public sealed class EditorUndoManager
{
    private sealed record UndoEntry(Guid DocumentId, EditorDocumentSnapshot Snapshot, TextPosition Cursor, string Description);

    private readonly LinkedList<UndoEntry> _entries = new();
    private int _limit = 100;

    public int Limit
    {
        get => _limit;
        set
        {
            _limit = value >= 0 ? value : throw new ArgumentOutOfRangeException(nameof(value));
            Trim();
        }
    }

    public int Count => _entries.Count;

    public void Capture(EditorWindow window, string description)
    {
        ArgumentNullException.ThrowIfNull(window);
        if (Limit == 0)
        {
            return;
        }

        _entries.AddFirst(new UndoEntry(
            window.Document.DocumentId,
            window.Document.CaptureSnapshot(),
            window.Cursor,
            description));
        Trim();
    }

    public bool Undo(EditorSession session)
    {
        ArgumentNullException.ThrowIfNull(session);
        if (_entries.First is null)
        {
            return false;
        }

        var entry = _entries.First.Value;
        _entries.RemoveFirst();
        var document = session.FindDocument(entry.DocumentId);
        if (document is null)
        {
            return false;
        }

        document.Restore(entry.Snapshot);
        foreach (var window in session.Windows.Where(window => window.Document.DocumentId == entry.DocumentId))
        {
            window.ClampCursor();
        }

        var target = session.Windows.FirstOrDefault(window => window.Document.DocumentId == entry.DocumentId);
        if (target is not null)
        {
            session.SetCurrentWindow(target.WindowId);
            target.Cursor = entry.Cursor;
            target.ClampCursor();
        }

        return true;
    }

    /// <summary>
    /// Removes all undo snapshots belonging to a document whose text stream has
    /// been intentionally destroyed. This prevents a destructive compatibility
    /// command from leaving stale snapshots that could later be resurrected.
    /// </summary>
    public void DiscardDocument(Guid documentId)
    {
        var node = _entries.First;
        while (node is not null)
        {
            var next = node.Next;
            if (node.Value.DocumentId == documentId)
            {
                _entries.Remove(node);
            }

            node = next;
        }
    }

    public void Clear() => _entries.Clear();

    private void Trim()
    {
        while (_entries.Count > Limit)
        {
            _entries.RemoveLast();
        }
    }
}
