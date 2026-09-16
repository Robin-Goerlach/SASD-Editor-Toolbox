using Sasd.Editor.Document;
using Sasd.Editor.Model;

namespace Sasd.Editor.Editing;

/// <summary>
/// Keeps line-number based editor references aligned when logical lines are
/// inserted or removed from a document.
/// </summary>
/// <remarks>
/// The historical Turbo Editor Toolbox stored windows, markers and block limits
/// as pointers to linked line descriptors and used routines such as
/// <c>EditDelline</c> and <c>EditRealign</c> to repair those references after a
/// structural edit. The modern implementation deliberately uses stable document
/// identities plus zero-based line numbers instead of exposing pointer mechanics.
/// This service is the single coordination point that translates structural line
/// changes into updated window, marker and block positions.
/// </remarks>
internal sealed class EditorLineTopology
{
    private readonly EditorSession _session;

    public EditorLineTopology(EditorSession session)
    {
        _session = session ?? throw new ArgumentNullException(nameof(session));
    }

    /// <summary>
    /// Realigns all references that identify existing lines after one or more
    /// lines have been inserted before <paramref name="firstInsertedLine"/>.
    /// </summary>
    public void LinesInserted(EditorDocument document, int firstInsertedLine, int count)
    {
        ArgumentNullException.ThrowIfNull(document);
        ValidateInsertion(document, firstInsertedLine, count);

        foreach (var window in WindowsFor(document))
        {
            if (window.Cursor.Line >= firstInsertedLine)
            {
                window.Cursor = window.Cursor with { Line = checked(window.Cursor.Line + count) };
            }

            if (window.TopLine >= firstInsertedLine)
            {
                window.TopLine = checked(window.TopLine + count);
            }
        }

        foreach (var markerNumber in _session.MarkerTable.Keys.ToArray())
        {
            var marker = _session.MarkerTable[markerNumber];
            if (marker.DocumentId == document.DocumentId && marker.Line >= firstInsertedLine)
            {
                _session.MarkerTable[markerNumber] = marker with { Line = checked(marker.Line + count) };
            }
        }

        var block = _session.Block;
        if (block is not null && block.DocumentId == document.DocumentId)
        {
            _session.ReplaceBlockForTopology(block with
            {
                StartLine = ShiftForInsertion(block.StartLine, firstInsertedLine, count),
                EndLine = ShiftForInsertion(block.EndLine, firstInsertedLine, count)
            });
        }
    }

    /// <summary>
    /// Realigns references after a contiguous range has already been removed from
    /// <paramref name="document"/>.
    /// </summary>
    /// <remarks>
    /// Windows that pointed at a removed line are attached to the line that now
    /// occupies the first removed index, or to the new final line when the removed
    /// range reached the end of the document. Markers on removed lines become
    /// undefined. The current V1 block model stores a complete pair of limits, so
    /// deleting either boundary clears the block rather than keeping a half-defined
    /// block; that conservative policy is documented as a modernization boundary.
    /// </remarks>
    public void LinesDeleted(EditorDocument document, int firstDeletedLine, int count)
    {
        ArgumentNullException.ThrowIfNull(document);
        ValidateDeletion(document, firstDeletedLine, count);

        var lastDeletedLine = checked(firstDeletedLine + count - 1);
        var lastRemainingLine = document.Buffer.LineCount - 1;

        foreach (var window in WindowsFor(document))
        {
            window.Cursor = window.Cursor with
            {
                Line = ShiftForDeletion(window.Cursor.Line, firstDeletedLine, lastDeletedLine, count, lastRemainingLine)
            };
            window.TopLine = ShiftForDeletion(
                window.TopLine,
                firstDeletedLine,
                lastDeletedLine,
                count,
                lastRemainingLine);
        }

        foreach (var markerNumber in _session.MarkerTable.Keys.ToArray())
        {
            var marker = _session.MarkerTable[markerNumber];
            if (marker.DocumentId != document.DocumentId)
            {
                continue;
            }

            if (marker.Line >= firstDeletedLine && marker.Line <= lastDeletedLine)
            {
                _session.MarkerTable.Remove(markerNumber);
                continue;
            }

            if (marker.Line > lastDeletedLine)
            {
                _session.MarkerTable[markerNumber] = marker with { Line = marker.Line - count };
            }
        }

        RealignBlockAfterDeletion(document, firstDeletedLine, lastDeletedLine, count);
    }

    /// <summary>
    /// Invalidates pointer-like metadata when a compatibility delete operation
    /// blanks the only remaining logical line instead of physically removing its
    /// descriptor.
    /// </summary>
    public void InvalidateLineReferences(EditorDocument document, int lineIndex)
    {
        ArgumentNullException.ThrowIfNull(document);
        if (lineIndex < 0 || lineIndex >= document.Buffer.LineCount)
        {
            throw new ArgumentOutOfRangeException(nameof(lineIndex));
        }

        foreach (var markerNumber in _session.MarkerTable.Keys.ToArray())
        {
            var marker = _session.MarkerTable[markerNumber];
            if (marker.DocumentId == document.DocumentId && marker.Line == lineIndex)
            {
                _session.MarkerTable.Remove(markerNumber);
            }
        }

        var block = _session.Block;
        if (block is not null
            && block.DocumentId == document.DocumentId
            && (block.StartLine == lineIndex || block.EndLine == lineIndex))
        {
            _session.ClearBlock();
        }
    }

    private void RealignBlockAfterDeletion(
        EditorDocument document,
        int firstDeletedLine,
        int lastDeletedLine,
        int count)
    {
        var block = _session.Block;
        if (block is null || block.DocumentId != document.DocumentId)
        {
            return;
        }

        if (IsDeleted(block.StartLine, firstDeletedLine, lastDeletedLine)
            || IsDeleted(block.EndLine, firstDeletedLine, lastDeletedLine))
        {
            _session.ClearBlock();
            return;
        }

        _session.ReplaceBlockForTopology(block with
        {
            StartLine = ShiftAfterDeletedRange(block.StartLine, lastDeletedLine, count),
            EndLine = ShiftAfterDeletedRange(block.EndLine, lastDeletedLine, count)
        });
    }

    private IEnumerable<Windows.EditorWindow> WindowsFor(EditorDocument document) =>
        _session.Windows.Where(window => window.Document.DocumentId == document.DocumentId);

    private static int ShiftForInsertion(int line, int firstInsertedLine, int count) =>
        line >= firstInsertedLine ? checked(line + count) : line;

    private static int ShiftAfterDeletedRange(int line, int lastDeletedLine, int count) =>
        line > lastDeletedLine ? line - count : line;

    private static bool IsDeleted(int line, int firstDeletedLine, int lastDeletedLine) =>
        line >= firstDeletedLine && line <= lastDeletedLine;

    private static int ShiftForDeletion(
        int line,
        int firstDeletedLine,
        int lastDeletedLine,
        int count,
        int lastRemainingLine)
    {
        if (line < firstDeletedLine)
        {
            return line;
        }

        if (line > lastDeletedLine)
        {
            return line - count;
        }

        return Math.Min(firstDeletedLine, lastRemainingLine);
    }

    private static void ValidateInsertion(EditorDocument document, int firstInsertedLine, int count)
    {
        if (count < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(count));
        }

        // This method is called after insertion. Therefore the first inserted line
        // must refer to an existing line in the resulting document.
        if (firstInsertedLine < 0 || firstInsertedLine >= document.Buffer.LineCount)
        {
            throw new ArgumentOutOfRangeException(nameof(firstInsertedLine));
        }
    }

    private static void ValidateDeletion(EditorDocument document, int firstDeletedLine, int count)
    {
        if (count < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(count));
        }

        // LinkedLineTextBuffer always retains at least one line. Deletion is
        // reported after the physical removal, so firstDeletedLine may equal the
        // new line count when the old tail was removed; the realignment helper
        // then attaches references to the new final line.
        if (firstDeletedLine < 0 || firstDeletedLine > document.Buffer.LineCount)
        {
            throw new ArgumentOutOfRangeException(nameof(firstDeletedLine));
        }
    }
}
