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

    public void SetPersistenceMetadata(string? filePath, string? newLine = null)
    {
        FilePath = string.IsNullOrWhiteSpace(filePath) ? null : Path.GetFullPath(filePath);
        if (!string.IsNullOrEmpty(newLine))
        {
            NewLine = newLine;
        }
    }
}
