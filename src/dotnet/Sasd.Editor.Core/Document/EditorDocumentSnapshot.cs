using Sasd.Editor.Model;

namespace Sasd.Editor.Document;

/// <summary>
/// Immutable document state used by the initial correctness-first undo model.
/// </summary>
public sealed record EditorDocumentSnapshot(
    IReadOnlyList<EditorLineSnapshot> Lines,
    string? FilePath,
    string NewLine,
    bool IsDirty,
    long Version);
