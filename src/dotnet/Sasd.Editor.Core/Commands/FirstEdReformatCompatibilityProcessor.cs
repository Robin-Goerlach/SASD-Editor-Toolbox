using Sasd.Editor.Document;
using Sasd.Editor.Editing;
using Sasd.Editor.Model;
using Sasd.Editor.Windows;

namespace Sasd.Editor.Commands;

/// <summary>
/// Clean-room implementation of the FIRST-ED paragraph reformat behavior.
/// </summary>
/// <remarks>
/// The historical implementation expresses reformatting through pointer-oriented
/// helpers such as EditCompressLine, EditShiftLine, EditLongLine and EditShortLine.
/// This implementation preserves their observable responsibilities but first
/// builds a complete reformat plan and only then mutates the document. Keeping the
/// planning phase side-effect free makes word-too-long failures atomic and keeps
/// line-topology repair centralized in <see cref="EditorLineTopology"/>.
/// </remarks>
internal static class FirstEdReformatCompatibilityProcessor
{
    /// <summary>
    /// Reformats the paragraph beginning on the current logical line so its words
    /// fit between the current window's left and right margins.
    /// </summary>
    /// <remarks>
    /// A line whose <see cref="EditorLineFlags.Wrapped"/> bit is set has a soft
    /// outgoing boundary to the next logical line. The paragraph therefore ends
    /// at the first line whose outgoing boundary is hard, or at end of stream.
    /// Reformatting is intentionally independent of the window's WordWrap mode.
    /// </remarks>
    public static async ValueTask<bool> ReformatAsync(
        EditorSession session,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(session);
        var window = RequireWindow(session);
        var document = window.Document;
        // Capture the entire source buffer and the initiating view *before*
        // yielding to any host input callback. During a long reformat plan,
        // a linked window can edit text or the initiating host can navigate,
        // change margins or reattach this view. Reading the live buffer at
        // each checkpoint would mix different document revisions.
        var initialCursor = window.Cursor;
        var startLine = initialCursor.Line;
        var initialVersion = document.Version;
        var initialLines = document.Buffer.Snapshot();
        var leftMargin = window.Options.LeftMargin;
        var rightMargin = window.Options.RightMargin;
        var initialBlockState = session.BlockState;

        var sourceLines = await ReadParagraphAsync(
            session,
            initialLines,
            startLine,
            cancellationToken).ConfigureAwait(false);
        var words = await CompressLinesAsync(
            session,
            sourceLines,
            cancellationToken).ConfigureAwait(false);

        if (words.Count == 0)
        {
            return false;
        }

        var contentWidth = checked(rightMargin - leftMargin + 1);

        var tooLong = words.FirstOrDefault(word => word.Length > contentWidth);
        if (tooLong is not null)
        {
            // The handbook describes this as the EditLongLine "word too long"
            // failure. Planning before mutation guarantees that the document and
            // undo stack remain untouched when this condition is detected.
            throw new InvalidOperationException(
                $"Cannot reformat paragraph because the word '{tooLong}' is wider than the current margins.");
        }

        var formattedText = await WrapWordsAsync(
            session,
            words,
            leftMargin,
            contentWidth,
            cancellationToken).ConfigureAwait(false);
        var plannedLines = BuildPlannedLines(sourceLines, formattedText);

        // Even a no-op plan must not silently succeed after an intervening
        // edit or a view change. Always complete the final cancellation poll
        // and the same consistency check before returning or committing.
        await session.Interruption.CheckpointAsync(cancellationToken).ConfigureAwait(false);
        EnsureUnchangedSource(
            session, window, document, initialCursor, initialVersion,
            initialLines, leftMargin, rightMargin, initialBlockState);

        if (Equivalent(sourceLines, plannedLines))
        {
            return false;
        }

        // From this point onward the structural in-memory update is applied
        // without further async yields. The original cursor is restored only
        // because we have just proved that this view has not navigated.
        var undo = session.Undo.BeginTransaction(session, window, "Reformat paragraph");
        ApplyPlan(session, document, startLine, sourceLines.Count, plannedLines);
        document.MarkChanged();

        // The technical reference describes where reformatting begins and how
        // words flow, but does not prescribe a new cursor location. Preserving
        // the initiating logical line/column is therefore the documented modern
        // host-neutral policy. Virtual columns remain valid by design.
        window.Cursor = originalCursor;
        session.RefreshBlockFlags();
        undo.Commit();
        return true;
    }

    /// <summary>
    /// Reads the current paragraph according to the Wrapped-bit chain. The final
    /// hard-boundary line belongs to the paragraph and terminates the scan.
    /// </summary>
    private static async ValueTask<IReadOnlyList<EditorLineSnapshot>> ReadParagraphAsync(
        EditorSession session,
        IReadOnlyList<EditorLineSnapshot> sourceSnapshot,
        int startLine,
        CancellationToken cancellationToken)
    {
        var lines = new List<EditorLineSnapshot>();
        var lineIndex = startLine;

        while (lineIndex < sourceSnapshot.Count)
        {
            await session.Interruption.CheckpointAsync(cancellationToken).ConfigureAwait(false);
            var line = sourceSnapshot[lineIndex];
            lines.Add(line);

            if (!line.Flags.HasFlag(EditorLineFlags.Wrapped))
            {
                break;
            }

            lineIndex++;
        }

        return lines;
    }

    /// <summary>
    /// Modern equivalent of EditCompressLine: repeated whitespace is reduced to
    /// word separators before splicing. The historical routine names spaces;
    /// Unicode whitespace is accepted as a deliberate modern extension.
    /// </summary>
    private static async ValueTask<IReadOnlyList<string>> CompressLinesAsync(
        EditorSession session,
        IReadOnlyList<EditorLineSnapshot> lines,
        CancellationToken cancellationToken)
    {
        var words = new List<string>();
        foreach (var line in lines)
        {
            await session.Interruption.CheckpointAsync(cancellationToken).ConfigureAwait(false);
            words.AddRange(line.Text.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries));
        }

        return words;
    }

    /// <summary>
    /// Modern plan-building counterpart of EditShiftLine/EditLongLine/EditShortLine.
    /// Words are shifted to the left margin, pushed down when a line would exceed
    /// the right margin, and pulled up whenever the next word still fits.
    /// </summary>
    private static async ValueTask<IReadOnlyList<string>> WrapWordsAsync(
        EditorSession session,
        IReadOnlyList<string> words,
        int leftMargin,
        int contentWidth,
        CancellationToken cancellationToken)
    {
        var result = new List<string>();
        var currentWords = new List<string>();
        var currentLength = 0;

        foreach (var word in words)
        {
            await session.Interruption.CheckpointAsync(cancellationToken).ConfigureAwait(false);
            var required = currentWords.Count == 0 ? word.Length : checked(word.Length + 1);

            if (currentWords.Count > 0 && currentLength + required > contentWidth)
            {
                result.Add(ShiftLine(currentWords, leftMargin));
                currentWords.Clear();
                currentLength = 0;
                required = word.Length;
            }

            currentWords.Add(word);
            currentLength = checked(currentLength + required);
        }

        if (currentWords.Count > 0)
        {
            result.Add(ShiftLine(currentWords, leftMargin));
        }

        return result;
    }

    /// <summary>
    /// Modern equivalent of EditShiftLine. Prefixing the normalized content with
    /// the current left margin ensures the first non-blank character is never left
    /// of that margin.
    /// </summary>
    private static string ShiftLine(IReadOnlyList<string> words, int leftMargin) =>
        new string(' ', leftMargin) + string.Join(' ', words);

    private static IReadOnlyList<EditorLineSnapshot> BuildPlannedLines(
        IReadOnlyList<EditorLineSnapshot> sourceLines,
        IReadOnlyList<string> formattedText)
    {
        var result = new EditorLineSnapshot[formattedText.Count];
        for (var index = 0; index < formattedText.Count; index++)
        {
            // User color is line-local presentation metadata and can be retained
            // on surviving descriptor positions. InBlock is re-projected from the
            // logical block after topology repair instead of copied blindly.
            var flags = index < sourceLines.Count
                ? sourceLines[index].Flags & EditorLineFlags.UserColored
                : EditorLineFlags.None;

            // Wrapped represents the soft outgoing boundary. Every line created
            // inside the paragraph is soft except the final paragraph line.
            if (index < formattedText.Count - 1)
            {
                flags |= EditorLineFlags.Wrapped;
            }

            result[index] = new EditorLineSnapshot(formattedText[index], flags);
        }

        return result;
    }

    private static bool Equivalent(
        IReadOnlyList<EditorLineSnapshot> sourceLines,
        IReadOnlyList<EditorLineSnapshot> plannedLines)
    {
        if (sourceLines.Count != plannedLines.Count)
        {
            return false;
        }

        for (var index = 0; index < sourceLines.Count; index++)
        {
            if (!string.Equals(sourceLines[index].Text, plannedLines[index].Text, StringComparison.Ordinal))
            {
                return false;
            }

            var sourceComparable = sourceLines[index].Flags & (EditorLineFlags.Wrapped | EditorLineFlags.UserColored);
            var plannedComparable = plannedLines[index].Flags & (EditorLineFlags.Wrapped | EditorLineFlags.UserColored);
            if (sourceComparable != plannedComparable)
            {
                return false;
            }
        }

        return true;
    }

    private static void ApplyPlan(
        EditorSession session,
        EditorDocument document,
        int startLine,
        int sourceCount,
        IReadOnlyList<EditorLineSnapshot> plannedLines)
    {
        var commonCount = Math.Min(sourceCount, plannedLines.Count);
        for (var index = 0; index < commonCount; index++)
        {
            var planned = plannedLines[index];
            document.Buffer.ReplaceLine(startLine + index, planned.Text, planned.Flags);
        }

        if (plannedLines.Count > sourceCount)
        {
            var firstInsertedLine = startLine + sourceCount;
            var insertedCount = plannedLines.Count - sourceCount;
            for (var index = sourceCount; index < plannedLines.Count; index++)
            {
                var planned = plannedLines[index];
                document.Buffer.InsertLine(startLine + index, planned.Text, planned.Flags);
            }

            session.Topology.LinesInserted(document, firstInsertedLine, insertedCount);
            return;
        }

        if (plannedLines.Count < sourceCount)
        {
            var firstDeletedLine = startLine + plannedLines.Count;
            var deletedCount = sourceCount - plannedLines.Count;
            for (var index = 0; index < deletedCount; index++)
            {
                document.Buffer.RemoveLine(firstDeletedLine);
            }

            session.Topology.LinesDeleted(document, firstDeletedLine, deletedCount);
        }
    }

    /// <summary>
    /// Treat the computed paragraph as an optimistic edit transaction. A host
    /// can switch focus during the awaited checkpoints (which is harmless),
    /// but may not reattach/move the initiating view, change its margins or
    /// block definition, or edit any line of the source document meanwhile.
    /// Version alone is insufficient because public buffer adapters can edit
    /// directly without calling MarkChanged; compare exact line snapshots too.
    /// </summary>
    private static void EnsureUnchangedSource(
        EditorSession session,
        EditorWindow window,
        EditorDocument document,
        TextPosition initialCursor,
        long initialVersion,
        IReadOnlyList<EditorLineSnapshot> initialLines,
        int leftMargin,
        int rightMargin,
        EditorBlockState initialBlockState)
    {
        if (!session.Windows.Any(candidate => candidate.WindowId == window.WindowId)
            || !ReferenceEquals(window.Document, document)
            || window.Cursor != initialCursor
            || window.Options.LeftMargin != leftMargin
            || window.Options.RightMargin != rightMargin
            || session.BlockState != initialBlockState
            || document.Version != initialVersion
            || !document.Buffer.Snapshot().SequenceEqual(initialLines))
        {
            throw new InvalidOperationException(
                "The paragraph or initiating editor view changed during reformat planning.");
        }
    }

    private static EditorWindow RequireWindow(EditorSession session) =>
        session.Windows.Count > 0
            ? session.CurrentWindow
            : throw new InvalidOperationException("The editor session has no windows.");
}
