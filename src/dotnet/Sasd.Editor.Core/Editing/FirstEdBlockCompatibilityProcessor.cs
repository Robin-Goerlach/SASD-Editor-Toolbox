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

        var undo = session.Undo.BeginTransaction(session, targetWindow, "Copy block");
        InsertLines(targetDocument, insertionLine, lines);
        session.Topology.LinesInserted(targetDocument, insertionLine, lines.Length);
        targetDocument.MarkChanged();
        session.RefreshBlockFlags();
        undo.Commit();
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

        var undo = session.Undo.BeginTransaction(session, sourceWindow, "Delete block");
        RemoveLines(sourceDocument, block.FirstLine, block.LineCount);

        // Report the logical range, not merely the number of physical linked-list
        // nodes removed. If the final line container was blanked rather than
        // physically removed, its old logical line was still deleted and anchors
        // on that line must become invalid.
        session.Topology.LinesDeleted(sourceDocument, block.FirstLine, block.LineCount);
        sourceDocument.MarkChanged();
        session.RefreshBlockFlags();
        undo.Commit();
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
            // This is an explicit historical restriction: inserting a block
            // into its own selected range has no well-defined destination.
            // Reject before starting any Undo or changing any line.
            throw new InvalidOperationException("The cursor cannot be inside the block being moved.");
        }

        // A whole-line move inserts immediately BEFORE the target cursor's
        // logical line. If that line ALREADY follows the selected block, the
        // remove-and-reinsert algorithm would reproduce exactly the same text,
        // flags, block endpoints and navigation state. This is a successful
        // no-op, not a text mutation.
        //
        // The 1985 handbook documents the destination placement but does
        // not specify Dirty/Undo accounting for this adjacent-boundary case.
        // SASD's modern no-op policy avoids a phantom document version bump
        // and an Undo item that cannot actually undo any visible change.
        //
        // Check before taking snapshots or touching the topology coordinator,
        // so linked windows, markers and incomplete block state stay intact.
        if (sameDocument && targetWindow.Cursor.Line == block.LastLine + 1)
        {
            return true;
        }

        var movedLines = SnapshotBlock(sourceDocument, block);
        var hidden = block.Hidden;

        // A move is one logical command even when it spans two documents. Capture
        // both streams in one transaction so one Undo cannot restore the source
        // while accidentally leaving a duplicate copy in the destination.
        var undo = sameDocument
            ? session.Undo.BeginTransaction(session, targetWindow, "Move block")
            : session.Undo.BeginTransaction(
                session,
                new[] { sourceWindow, targetWindow },
                "Move block");

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
        undo.Commit();
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
