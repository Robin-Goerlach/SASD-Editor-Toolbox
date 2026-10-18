using Sasd.Editor.Abstractions;
using Sasd.Editor.Model;

namespace Sasd.Editor.Document;

/// <summary>
/// Owns a text buffer and persistence-related document state. Views/windows
/// reference a document instead of owning text themselves.
/// </summary>
public sealed class EditorDocument
{
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
            FilePath = Path.GetFullPath(filePath);
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

    public void SetPersistenceMetadata(string? filePath, string? newLine = null)
    {
        FilePath = string.IsNullOrWhiteSpace(filePath) ? null : Path.GetFullPath(filePath);
        if (!string.IsNullOrEmpty(newLine))
        {
            NewLine = newLine;
        }
    }
}
