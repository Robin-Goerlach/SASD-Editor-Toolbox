using Sasd.Editor.Document;
using Sasd.Editor.Model;
using Sasd.Editor.Windows;

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

        var state = _session.BlockState;
        _session.ReplaceBlockStateForTopology(state with
        {
            Start = ShiftEndpointForInsertion(state.Start, document, firstInsertedLine, count),
            End = ShiftEndpointForInsertion(state.End, document, firstInsertedLine, count)
        });
    }

    /// <summary>
    /// Realigns references after a contiguous range has already been removed from
    /// <paramref name="document"/>.
    /// </summary>
    /// <remarks>
    /// Windows that pointed at a removed line are attached to the line that now
    /// occupies the first removed index, or to the new final line when the removed
    /// range reached the end of the document. Markers on removed lines become
    /// undefined. Block endpoints are invalidated independently, preserving the
    /// remaining historical endpoint when only one boundary line is deleted.
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

        var state = _session.BlockState;
        _session.ReplaceBlockStateForTopology(state with
        {
            Start = InvalidateEndpoint(state.Start, document, lineIndex),
            End = InvalidateEndpoint(state.End, document, lineIndex)
        });
    }

    private void RealignBlockAfterDeletion(
        EditorDocument document,
        int firstDeletedLine,
        int lastDeletedLine,
        int count)
    {
        var state = _session.BlockState;
        _session.ReplaceBlockStateForTopology(state with
        {
            Start = ShiftEndpointForDeletion(state.Start, document, firstDeletedLine, lastDeletedLine, count),
            End = ShiftEndpointForDeletion(state.End, document, firstDeletedLine, lastDeletedLine, count)
        });
    }

    private IEnumerable<EditorWindow> WindowsFor(EditorDocument document) =>
        _session.Windows.Where(window => window.Document.DocumentId == document.DocumentId);

    private static EditorBlockEndpoint? ShiftEndpointForInsertion(
        EditorBlockEndpoint? endpoint,
        EditorDocument document,
        int firstInsertedLine,
        int count)
    {
        if (endpoint is null || endpoint.DocumentId != document.DocumentId || endpoint.Line < firstInsertedLine)
        {
            return endpoint;
        }

        return endpoint with { Line = checked(endpoint.Line + count) };
    }

    private static EditorBlockEndpoint? ShiftEndpointForDeletion(
        EditorBlockEndpoint? endpoint,
        EditorDocument document,
        int firstDeletedLine,
        int lastDeletedLine,
        int count)
    {
        if (endpoint is null || endpoint.DocumentId != document.DocumentId)
        {
            return endpoint;
        }

        if (endpoint.Line >= firstDeletedLine && endpoint.Line <= lastDeletedLine)
        {
            return null;
        }

        return endpoint.Line > lastDeletedLine
            ? endpoint with { Line = endpoint.Line - count }
            : endpoint;
    }

    private static EditorBlockEndpoint? InvalidateEndpoint(
        EditorBlockEndpoint? endpoint,
        EditorDocument document,
        int lineIndex) =>
        endpoint is not null
        && endpoint.DocumentId == document.DocumentId
        && endpoint.Line == lineIndex
            ? null
            : endpoint;

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
