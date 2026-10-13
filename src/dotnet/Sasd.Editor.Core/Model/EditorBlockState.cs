namespace Sasd.Editor.Model;

/// <summary>
/// Identifies one independently defined whole-line block endpoint.
/// </summary>
/// <remarks>
/// The historical editor stored block limits as pointers to line descriptors.
/// SASD keeps the same endpoint lifecycle through stable document identity plus a
/// zero-based logical line number; pointer identity is not part of the public
/// compatibility contract.
/// </remarks>
public sealed record EditorBlockEndpoint
{
    public EditorBlockEndpoint(Guid documentId, int line)
    {
        if (documentId == Guid.Empty)
        {
            throw new ArgumentException("A block endpoint requires a document identity.", nameof(documentId));
        }

        if (line < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(line));
        }

        DocumentId = documentId;
        Line = line;
    }

    public Guid DocumentId { get; init; }

    public int Line { get; init; }
}

/// <summary>
/// Raw begin/end state for the historical whole-line block model.
/// </summary>
/// <remarks>
/// Begin and end are intentionally independent. A command may define only one
/// endpoint; in that state no operational <see cref="EditorBlock"/> exists and
/// highlighting remains hidden. This models the observable Blockfrom/Blockto
/// lifecycle without reproducing Pascal pointer mechanics.
/// </remarks>
public sealed record EditorBlockState(
    EditorBlockEndpoint? Start,
    EditorBlockEndpoint? End,
    bool Hidden = true)
{
    public static EditorBlockState Empty { get; } = new(null, null, true);

    public bool IsEmpty => Start is null && End is null;

    public bool IsComplete =>
        Start is not null
        && End is not null
        && Start.DocumentId == End.DocumentId
        && Start.Line <= End.Line;

    /// <summary>
    /// Materializes the usable contiguous block when both endpoints are valid.
    /// Incomplete or cross-document endpoint state deliberately returns null.
    /// </summary>
    public EditorBlock? CompleteBlock => IsComplete
        ? new EditorBlock(Start!.DocumentId, Start.Line, End!.Line, Hidden)
        : null;

    public bool ReferencesDocument(Guid documentId) =>
        Start?.DocumentId == documentId || End?.DocumentId == documentId;

    internal EditorBlockState EnsureVisibilityInvariant() =>
        IsComplete ? this : this with { Hidden = true };

    internal static EditorBlockState FromCompleteBlock(EditorBlock block)
    {
        ArgumentNullException.ThrowIfNull(block);
        return new EditorBlockState(
            new EditorBlockEndpoint(block.DocumentId, block.FirstLine),
            new EditorBlockEndpoint(block.DocumentId, block.LastLine),
            block.Hidden);
    }
}
