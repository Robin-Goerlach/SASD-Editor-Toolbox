using Sasd.Editor.Document;
using Sasd.Editor.Editing;
using Sasd.Editor.Model;
using Sasd.Editor.Windows;

namespace Sasd.Editor.Undo;

/// <summary>
/// Represents one pending correctness-first undo transaction.
/// </summary>
/// <remarks>
/// A transaction is added to the undo history only when <see cref="Commit"/> is
/// called. This prevents a failed mutation from leaving behind a snapshot that
/// claims an edit succeeded. The object is deliberately lightweight; abandoning
/// it simply abandons the pending snapshot.
/// </remarks>
public sealed class EditorUndoTransaction
{
    private Action? _commit;

    internal EditorUndoTransaction(Action? commit)
    {
        _commit = commit;
    }

    public bool IsCommitted { get; private set; }

    /// <summary>
    /// Records the transaction after all document, topology and navigation state
    /// belonging to the logical command has reached its final post-command state.
    /// </summary>
    public void Commit()
    {
        if (IsCommitted)
        {
            throw new InvalidOperationException("The undo transaction has already been committed.");
        }

        _commit?.Invoke();
        _commit = null;
        IsCommitted = true;
    }
}

/// <summary>
/// Correctness-first snapshot undo with support for one logical transaction
/// spanning multiple documents.
/// </summary>
/// <remarks>
/// The historical toolbox Undo command is line-stack oriented. SASD deliberately
/// uses a modern snapshot backend while preserving the public editor behavior
/// behind a replaceable boundary. A compound operation such as moving a block
/// between documents is therefore one undo item, not two partially reversible
/// snapshots.
///
/// In addition to document text, committed transactions remember the session
/// navigation/anchor state that the command itself may have changed. When Undo is
/// executed, that state is restored only when it still matches the transaction's
/// recorded post-state. Explicit navigation/marker changes made after the edit
/// are therefore not blindly overwritten.
/// </remarks>
public sealed class EditorUndoManager
{
    private sealed record DocumentUndoState(Guid DocumentId, EditorDocumentSnapshot Snapshot);

    private sealed record WindowUndoState(
        Guid WindowId,
        Guid DocumentId,
        TextPosition Cursor,
        int TopLine,
        int LeftColumn);

    private sealed record SessionUndoState(
        Guid? CurrentWindowId,
        IReadOnlyDictionary<Guid, WindowUndoState> Windows,
        IReadOnlyDictionary<int, EditorMarker> Markers,
        EditorBlockState BlockState);

    private sealed record UndoEntry(
        IReadOnlyList<DocumentUndoState> Documents,
        SessionUndoState? BeforeSession,
        SessionUndoState? AfterSession,
        Guid? LegacyWindowId,
        TextPosition LegacyCursor,
        string Description);

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

    /// <summary>
    /// Number of logical undo transactions, not the number of document snapshots.
    /// A cross-document move therefore contributes exactly one item.
    /// </summary>
    public int Count => _entries.Count;

    /// <summary>
    /// Starts a transaction for one document/view.
    /// </summary>
    public EditorUndoTransaction BeginTransaction(
        EditorSession session,
        EditorWindow window,
        string description)
    {
        ArgumentNullException.ThrowIfNull(window);
        return BeginTransaction(session, [window], description);
    }

    /// <summary>
    /// Starts one logical transaction over every unique document displayed by
    /// <paramref name="windows"/>.
    /// </summary>
    /// <remarks>
    /// The pre-state is captured immediately. The post-state is captured only on
    /// <see cref="EditorUndoTransaction.Commit"/>, after topology repair and final
    /// cursor placement. This allows Undo to distinguish command-induced state
    /// from later explicit user navigation.
    /// </remarks>
    public EditorUndoTransaction BeginTransaction(
        EditorSession session,
        IEnumerable<EditorWindow> windows,
        string description)
    {
        ArgumentNullException.ThrowIfNull(session);
        ArgumentNullException.ThrowIfNull(windows);
        ArgumentNullException.ThrowIfNull(description);

        if (Limit == 0)
        {
            return new EditorUndoTransaction(null);
        }

        var participatingWindows = windows
            .Where(static window => window is not null)
            .DistinctBy(static window => window.WindowId)
            .ToArray();

        if (participatingWindows.Length == 0)
        {
            throw new ArgumentException("At least one editor window is required.", nameof(windows));
        }

        foreach (var window in participatingWindows)
        {
            if (!session.Windows.Any(candidate => candidate.WindowId == window.WindowId))
            {
                throw new ArgumentException("Every undo window must belong to the supplied session.", nameof(windows));
            }
        }

        var documents = participatingWindows
            .Select(static window => window.Document)
            .DistinctBy(static document => document.DocumentId)
            .ToArray();

        var beforeDocuments = documents
            .Select(static document => new DocumentUndoState(document.DocumentId, document.CaptureSnapshot()))
            .ToArray();
        var beforeSession = CaptureSessionState(session);

        return new EditorUndoTransaction(() =>
        {
            // A host may disable undo while a command is running. In that case
            // committing the pending transaction deliberately records nothing.
            if (Limit == 0)
            {
                return;
            }

            var afterSession = CaptureSessionState(session);
            _entries.AddFirst(new UndoEntry(
                beforeDocuments,
                beforeSession,
                afterSession,
                LegacyWindowId: null,
                LegacyCursor: default,
                description));
            Trim();
        });
    }

    /// <summary>
    /// Legacy low-level capture API retained for callers that already depend on
    /// it. New editor code should use <see cref="BeginTransaction(EditorSession, EditorWindow, string)"/>
    /// so final navigation/anchor state can be audited as part of the command.
    /// </summary>
    public void Capture(EditorWindow window, string description)
    {
        ArgumentNullException.ThrowIfNull(window);
        ArgumentNullException.ThrowIfNull(description);
        if (Limit == 0)
        {
            return;
        }

        _entries.AddFirst(new UndoEntry(
            [new DocumentUndoState(window.Document.DocumentId, window.Document.CaptureSnapshot())],
            BeforeSession: null,
            AfterSession: null,
            LegacyWindowId: window.WindowId,
            LegacyCursor: window.Cursor,
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

        var resolvedDocuments = new Dictionary<Guid, EditorDocument>();
        foreach (var documentState in entry.Documents)
        {
            var document = session.FindDocument(documentState.DocumentId);
            if (document is null)
            {
                // This matches the previous snapshot backend's destructive-close
                // policy: an undo item whose text stream is no longer open cannot
                // safely resurrect that stream.
                return false;
            }

            resolvedDocuments.Add(documentState.DocumentId, document);
        }

        foreach (var documentState in entry.Documents)
        {
            resolvedDocuments[documentState.DocumentId].Restore(documentState.Snapshot);
        }

        if (entry.BeforeSession is not null && entry.AfterSession is not null)
        {
            RestoreSessionState(session, entry.BeforeSession, entry.AfterSession);
        }
        else
        {
            RestoreLegacyCursor(session, entry);
        }

        // Any view not restored because the user explicitly navigated after the
        // command must still reference a valid line in the restored document.
        var restoredDocumentIds = entry.Documents
            .Select(static state => state.DocumentId)
            .ToHashSet();
        foreach (var window in session.Windows)
        {
            if (restoredDocumentIds.Contains(window.Document.DocumentId))
            {
                window.ClampCursor();
            }
        }

        NormalizeMarkers(session);
        NormalizeBlockState(session);
        session.RefreshBlockFlags();
        return true;
    }

    /// <summary>
    /// Removes every transaction that could resurrect a deliberately destroyed
    /// document. Compound transactions are discarded as a whole; keeping only one
    /// side of a cross-document operation would violate atomicity.
    /// </summary>
    public void DiscardDocument(Guid documentId)
    {
        var node = _entries.First;
        while (node is not null)
        {
            var next = node.Next;
            if (node.Value.Documents.Any(document => document.DocumentId == documentId))
            {
                _entries.Remove(node);
            }

            node = next;
        }
    }

    public void Clear() => _entries.Clear();

    private static SessionUndoState CaptureSessionState(EditorSession session)
    {
        var windows = session.Windows.ToDictionary(
            static window => window.WindowId,
            static window => new WindowUndoState(
                window.WindowId,
                window.Document.DocumentId,
                window.Cursor,
                window.TopLine,
                window.LeftColumn));

        var markers = session.MarkerTable.ToDictionary(
            static pair => pair.Key,
            static pair => pair.Value);

        Guid? currentWindowId = session.Windows.Count == 0
            ? null
            : session.CurrentWindow.WindowId;

        return new SessionUndoState(
            currentWindowId,
            windows,
            markers,
            session.BlockState);
    }

    private static void RestoreSessionState(
        EditorSession session,
        SessionUndoState before,
        SessionUndoState after)
    {
        foreach (var beforeWindow in before.Windows.Values)
        {
            if (!after.Windows.TryGetValue(beforeWindow.WindowId, out var afterWindow))
            {
                continue;
            }

            var current = session.Windows.FirstOrDefault(window => window.WindowId == beforeWindow.WindowId);
            if (current is null || !Matches(current, afterWindow))
            {
                // The view was closed, re-linked or explicitly navigated after the
                // transaction. Undoing text must not overwrite that later action.
                continue;
            }

            if (current.Document.DocumentId != beforeWindow.DocumentId)
            {
                continue;
            }

            current.Cursor = beforeWindow.Cursor;
            current.TopLine = beforeWindow.TopLine;
            current.LeftColumn = beforeWindow.LeftColumn;
        }

        RestoreMarkersSelectively(session, before.Markers, after.Markers);

        if (Equals(session.BlockState, after.BlockState))
        {
            session.ReplaceBlockStateForTopology(before.BlockState);
        }

        if (after.CurrentWindowId.HasValue
            && session.Windows.Count > 0
            && session.CurrentWindow.WindowId == after.CurrentWindowId.Value
            && before.CurrentWindowId.HasValue)
        {
            var originalCurrent = session.Windows.FirstOrDefault(
                window => window.WindowId == before.CurrentWindowId.Value);
            if (originalCurrent is not null)
            {
                session.SetCurrentWindow(originalCurrent.WindowId);
            }
        }
    }

    private static void RestoreMarkersSelectively(
        EditorSession session,
        IReadOnlyDictionary<int, EditorMarker> before,
        IReadOnlyDictionary<int, EditorMarker> after)
    {
        foreach (var markerNumber in before.Keys.Concat(after.Keys).Distinct())
        {
            var beforeExists = before.TryGetValue(markerNumber, out var beforeMarker);
            var afterExists = after.TryGetValue(markerNumber, out var afterMarker);
            var currentExists = session.MarkerTable.TryGetValue(markerNumber, out var currentMarker);

            var stillAtRecordedPostState =
                currentExists == afterExists
                && (!currentExists || Equals(currentMarker, afterMarker));

            if (!stillAtRecordedPostState)
            {
                // Marker was explicitly changed after the transaction.
                continue;
            }

            if (beforeExists)
            {
                session.MarkerTable[markerNumber] = beforeMarker!;
            }
            else
            {
                session.MarkerTable.Remove(markerNumber);
            }
        }
    }

    private static void RestoreLegacyCursor(EditorSession session, UndoEntry entry)
    {
        if (!entry.LegacyWindowId.HasValue)
        {
            return;
        }

        var target = session.Windows.FirstOrDefault(window => window.WindowId == entry.LegacyWindowId.Value)
            ?? session.Windows.FirstOrDefault(window =>
                entry.Documents.Any(document => document.DocumentId == window.Document.DocumentId));

        if (target is null)
        {
            return;
        }

        session.SetCurrentWindow(target.WindowId);
        target.Cursor = entry.LegacyCursor;
    }

    private static bool Matches(EditorWindow window, WindowUndoState state) =>
        window.Document.DocumentId == state.DocumentId
        && window.Cursor == state.Cursor
        && window.TopLine == state.TopLine
        && window.LeftColumn == state.LeftColumn;

    private static void NormalizeMarkers(EditorSession session)
    {
        foreach (var markerNumber in session.MarkerTable.Keys.ToArray())
        {
            var marker = session.MarkerTable[markerNumber];
            var document = session.FindDocument(marker.DocumentId);
            if (document is null || marker.Line < 0 || marker.Line >= document.Buffer.LineCount)
            {
                session.MarkerTable.Remove(markerNumber);
            }
        }
    }

    private static void NormalizeBlockState(EditorSession session)
    {
        var state = session.BlockState;
        var start = NormalizeEndpoint(session, state.Start);
        var end = NormalizeEndpoint(session, state.End);

        if (!Equals(start, state.Start) || !Equals(end, state.End))
        {
            session.ReplaceBlockStateForTopology(state with { Start = start, End = end });
        }
    }

    private static EditorBlockEndpoint? NormalizeEndpoint(
        EditorSession session,
        EditorBlockEndpoint? endpoint)
    {
        if (endpoint is null)
        {
            return null;
        }

        var document = session.FindDocument(endpoint.DocumentId);
        return document is not null
               && endpoint.Line >= 0
               && endpoint.Line < document.Buffer.LineCount
            ? endpoint
            : null;
    }

    private void Trim()
    {
        while (_entries.Count > Limit)
        {
            _entries.RemoveLast();
        }
    }
}
