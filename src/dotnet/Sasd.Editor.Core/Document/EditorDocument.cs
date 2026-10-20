using Sasd.Editor.Abstractions;
using Sasd.Editor.Model;

namespace Sasd.Editor.Document;

/// <summary>
/// Owns a text buffer and persistence-related document state. Views/windows
/// reference a document instead of owning text themselves.
/// </summary>
public sealed class EditorDocument
{
    // Save As may target multiple independent paths while I/O is pending.
    // Distinct targets cannot share the staged writer's *path* gate; a
    // monotonically increasing request ID prevents an older completion from
    // stealing the newer successful save's document association.
    private long _nextSaveRequestId;
    private long _latestAcknowledgedSaveId;
    private readonly object _saveAcknowledgementSync = new();

    // Observed *disk bytes* for modern file saves, independent of editor Undo.
    // This revision is deliberately not part of EditorDocumentSnapshot: Undo
    // changes in-memory text but can never roll back the external filesystem.
    private DocumentFileRevision? _observedFileRevision;

    /// <summary>Last known revision of the associated on-disk file.</summary>
    internal DocumentFileRevision? ObservedFileRevision
    {
        get
        {
            lock (_saveAcknowledgementSync) return _observedFileRevision;
        }
    }

    /// <summary>
    /// Records an original byte snapshot only after a complete successful
    /// storage load. No Save operation may guess an unobserved target state.
    /// </summary>
    internal void RecordLoadedFileRevision(DocumentFileRevision revision)
    {
        ArgumentNullException.ThrowIfNull(revision);
        lock (_saveAcknowledgementSync) _observedFileRevision = revision;
    }

    public EditorDocument(ITextBuffer? buffer = null)
    {
        Buffer = buffer ?? new LinkedLineTextBuffer();
    }

    public Guid DocumentId { get; } = Guid.NewGuid();

    public ITextBuffer Buffer { get; }

    public string? FilePath { get; private set; }

    public string DisplayName => string.IsNullOrWhiteSpace(FilePath)
        ? "NONAME"
        : Path.GetFileName(FilePath);

    public string NewLine { get; private set; } = Environment.NewLine;

    public bool IsDirty { get; private set; }

    public long Version { get; private set; }

    public EditorDocumentSnapshot CaptureSnapshot() =>
        new(Buffer.Snapshot(), FilePath, NewLine, IsDirty, Version);

    public void Restore(EditorDocumentSnapshot snapshot)
    {
        ArgumentNullException.ThrowIfNull(snapshot);
        Buffer.ReplaceAll(snapshot.Lines);
        // A restored file association may refer to a completely different
        // disk file. Never transfer the current baseline to that old path.
        if (!SameFilePath(FilePath, snapshot.FilePath))
            _observedFileRevision = null;
        FilePath = snapshot.FilePath;
        NewLine = snapshot.NewLine;
        IsDirty = snapshot.IsDirty;
        Version = snapshot.Version;
    }

    public void MarkChanged()
    {
        IsDirty = true;
        Version++;
    }

    public void MarkSaved(string? filePath = null)
    {
        if (!string.IsNullOrWhiteSpace(filePath))
        {
            FilePath = Path.GetFullPath(filePath);
            // This public method does not prove which bytes were written.
            // Keep disk-revision ownership in the storage adapter.
            _observedFileRevision = null;
        }

        IsDirty = false;
    }

    /// <summary>
    /// Acknowledges a completed write only if the document still represents
    /// the snapshot which was handed to the storage provider.
    /// </summary>
    /// <remarks>
    /// File writes can be asynchronous while a linked editor view remains
    /// editable. Setting IsDirty to false unconditionally after an older
    /// snapshot reaches disk would incorrectly hide newer unsaved edits.
    /// Version is checked for the fast common case; line snapshots are also
    /// compared because Undo or a custom buffer can restore a prior version
    /// without guaranteeing that its current content equals the saved bytes.
    /// Modern UTF-8 storage also supplies the newline style used for its
    /// serialization; changing that policy while writing means the saved
    /// bytes are not the current document's preferred representation.
    /// A successful write may still establish the document's file association
    /// even if its contents have changed during I/O.
    /// </remarks>
    /// <returns>True only if the current content was the snapshot written.</returns>
    public bool MarkSnapshotSaved(
        long snapshotVersion,
        IReadOnlyList<EditorLineSnapshot> snapshotLines,
        string? filePath = null,
        string? writtenNewLine = null)
    {
        ArgumentNullException.ThrowIfNull(snapshotLines);

        if (!string.IsNullOrWhiteSpace(filePath))
        {
            var fullPath = Path.GetFullPath(filePath);
            if (!SameFilePath(FilePath, fullPath))
                _observedFileRevision = null;
            FilePath = fullPath;
        }

        if (Version != snapshotVersion
            || (writtenNewLine is not null
                && !string.Equals(NewLine, writtenNewLine, StringComparison.Ordinal))
            || !Buffer.Snapshot().SequenceEqual(snapshotLines))
        {
            // The persisted snapshot is not the current editor document.
            // In particular, never clear a dirty flag set by a later edit.
            return false;
        }

        IsDirty = false;
        return true;
    }

    /// <summary>
    /// Reserves an ordering token before the first awaited write. Only saves
    /// which actually succeed can advance the acknowledgement watermark.
    /// A newer failed/cancelled request must not suppress an older successful
    /// save, while a completed newer Save As must not be undone by a slow one.
    /// </summary>
    internal long BeginSaveRequest() => Interlocked.Increment(ref _nextSaveRequestId);

    /// <summary>
    /// Acknowledges an I/O-complete save if no *newer successful* save has
    /// already published its document association. The same version, exact
    /// line snapshot and optional newline checks still protect dirty state.
    /// A superseded save still wrote its own target, but cannot rename or
    /// mark this document clean after a later save to another path succeeded.
    /// </summary>
    internal bool AcknowledgeSaveRequest(
        long requestId,
        long snapshotVersion,
        IReadOnlyList<EditorLineSnapshot> snapshotLines,
        string? targetPath,
        string? writtenNewLine = null,
        DocumentFileRevision? writtenFileRevision = null)
    {
        lock (_saveAcknowledgementSync)
        {
            if (requestId <= _latestAcknowledgedSaveId)
            {
                return false;
            }

            _latestAcknowledgedSaveId = requestId;
            var currentTextSaved = MarkSnapshotSaved(
                snapshotVersion, snapshotLines, targetPath, writtenNewLine);

            // A successful write changes the disk baseline even if a newer
            // in-memory edit keeps the document Dirty. The update must share
            // the save-order lock so an earlier, out-of-order Save As cannot
            // overwrite the later acknowledged file association or revision.
            _observedFileRevision = writtenFileRevision;
            return currentTextSaved;
        }
    }

    public void SetPersistenceMetadata(string? filePath, string? newLine = null)
    {
        FilePath = string.IsNullOrWhiteSpace(filePath) ? null : Path.GetFullPath(filePath);
        // External reassociation is not a file read. Requiring a new
        // observed baseline is safer than trusting an unrelated old hash.
        _observedFileRevision = null;
        if (!string.IsNullOrEmpty(newLine))
        {
            NewLine = newLine;
        }
    }

    private static bool SameFilePath(string? a, string? b) =>
        string.Equals(a, b, OperatingSystem.IsWindows()
            ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal);
}
