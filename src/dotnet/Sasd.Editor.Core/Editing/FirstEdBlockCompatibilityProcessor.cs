using Sasd.Editor.Document;
using Sasd.Editor.Model;
using Sasd.Editor.Windows;

namespace Sasd.Editor.Editing;

/// <summary>
/// Clean-room implementation of the historical whole-line block manipulation
/// commands: copy, move and delete.
/// </summary>
/// <remarks>
/// The Turbo Editor Toolbox represented blocks, windows and markers through
/// pointers to linked line descriptors. This implementation preserves the
/// observable whole-line behavior while delegating structural reference repair
/// to <see cref="EditorLineTopology"/>. It intentionally does not reproduce
/// Pascal pointer splicing or heap-management mechanics.
/// </remarks>
internal static class FirstEdBlockCompatibilityProcessor
{
    /// <summary>
    /// Copies the active block immediately before the current cursor line.
    /// The source block remains defined. The current line is treated as an anchor,
    /// so topology realignment keeps the cursor attached to that original line
    /// after the copied lines are inserted above it.
    /// </summary>
    public static bool CopyToCursor(EditorSession session)
    {
        ArgumentNullException.ThrowIfNull(session);
        var (block, sourceDocument, _) = RequireBlock(session);
        var targetWindow = RequireCurrentWindow(session);
        var targetDocument = targetWindow.Document;
        var insertionLine = targetWindow.Cursor.Line;
        var lines = SnapshotBlock(sourceDocument, block);

        session.Undo.Capture(targetWindow, "Copy block");
        InsertLines(targetDocument, insertionLine, lines);
        session.Topology.LinesInserted(targetDocument, insertionLine, lines.Length);
        targetDocument.MarkChanged();
        session.RefreshBlockFlags();
        return true;
    }

    /// <summary>
    /// Deletes every logical line in the active block. The session keeps the
    /// buffer invariant that a text stream contains at least one logical line;
    /// deleting a block that spans the complete stream therefore leaves one blank
    /// logical line behind.
    /// </summary>
    public static bool Delete(EditorSession session)
    {
        ArgumentNullException.ThrowIfNull(session);
        var (block, sourceDocument, sourceWindow) = RequireBlock(session);

        session.Undo.Capture(sourceWindow, "Delete block");
        RemoveLines(sourceDocument, block.FirstLine, block.LineCount);

        // Report the logical range, not merely the number of physical linked-list
        // nodes removed. If the final line container was blanked rather than
        // physically removed, its old logical line was still deleted and anchors
        // on that line must become invalid.
        session.Topology.LinesDeleted(sourceDocument, block.FirstLine, block.LineCount);
        sourceDocument.MarkChanged();
        session.RefreshBlockFlags();
        return true;
    }

    /// <summary>
    /// Moves the active block immediately before the current cursor line.
    /// The current cursor may not lie inside the block when source and target are
    /// the same document, matching the explicit historical safety check.
    /// </summary>
    public static bool MoveToCursor(EditorSession session)
    {
        ArgumentNullException.ThrowIfNull(session);
        var (block, sourceDocument, sourceWindow) = RequireBlock(session);
        var targetWindow = RequireCurrentWindow(session);
        var targetDocument = targetWindow.Document;
        var sameDocument = sourceDocument.DocumentId == targetDocument.DocumentId;

        if (sameDocument
            && targetWindow.Cursor.Line >= block.FirstLine
            && targetWindow.Cursor.Line <= block.LastLine)
        {
            throw new InvalidOperationException("The cursor cannot be inside the block being moved.");
        }

        var movedLines = SnapshotBlock(sourceDocument, block);
        var hidden = block.Hidden;

        // The current snapshot backend records both affected streams separately
        // for a cross-document move. This keeps both sides recoverable without
        // introducing a premature transaction journal; compound undo remains a
        // replaceable-backend concern documented for the final undo audit.
        session.Undo.Capture(targetWindow, sameDocument ? "Move block" : "Move block target");
        if (!sameDocument)
        {
            session.Undo.Capture(sourceWindow, "Move block source");
        }

        RemoveLines(sourceDocument, block.FirstLine, block.LineCount);
        session.Topology.LinesDeleted(sourceDocument, block.FirstLine, block.LineCount);

        // Topology realignment above keeps the target cursor attached to the same
        // surviving target line when source and destination share a document. Its
        // post-delete line number is therefore the correct insertion anchor.
        var insertionLine = targetWindow.Cursor.Line;
        InsertLines(targetDocument, insertionLine, movedLines);
        session.Topology.LinesInserted(targetDocument, insertionLine, movedLines.Length);

        session.ReplaceBlockForTopology(new EditorBlock(
            targetDocument.DocumentId,
            insertionLine,
            insertionLine + movedLines.Length - 1,
            hidden));

        sourceDocument.MarkChanged();
        if (!sameDocument)
        {
            targetDocument.MarkChanged();
        }

        session.RefreshBlockFlags();
        return true;
    }

    private static (EditorBlock Block, EditorDocument Document, EditorWindow Window) RequireBlock(EditorSession session)
    {
        var block = session.Block ?? throw new InvalidOperationException("No block is defined.");
        var document = session.FindDocument(block.DocumentId)
            ?? throw new InvalidOperationException("The block document is no longer open.");

        if (block.FirstLine < 0 || block.LastLine >= document.Buffer.LineCount)
        {
            throw new InvalidOperationException("The active block no longer identifies a valid contiguous line range.");
        }

        var window = session.Windows.First(candidate => candidate.Document.DocumentId == document.DocumentId);
        return (block, document, window);
    }

    private static EditorWindow RequireCurrentWindow(EditorSession session) =>
        session.CurrentWindow ?? throw new InvalidOperationException("No current editor window.");

    private static EditorLineSnapshot[] SnapshotBlock(EditorDocument document, EditorBlock block) =>
        Enumerable.Range(block.FirstLine, block.LineCount)
            .Select(index =>
            {
                var line = document.Buffer.GetLine(index);
                return line with { Flags = line.Flags & ~EditorLineFlags.InBlock };
            })
            .ToArray();

    private static void InsertLines(EditorDocument document, int insertionLine, IReadOnlyList<EditorLineSnapshot> lines)
    {
        if (insertionLine < 0 || insertionLine > document.Buffer.LineCount)
        {
            throw new ArgumentOutOfRangeException(nameof(insertionLine));
        }

        for (var index = 0; index < lines.Count; index++)
        {
            var line = lines[index];
            document.Buffer.InsertLine(insertionLine + index, line.Text, line.Flags);
        }
    }

    private static void RemoveLines(EditorDocument document, int firstLine, int count)
    {
        for (var index = firstLine + count - 1; index >= firstLine; index--)
        {
            document.Buffer.RemoveLine(index);
        }
    }
}
