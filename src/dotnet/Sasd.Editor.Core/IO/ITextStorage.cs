using Sasd.Editor.Document;

namespace Sasd.Editor.IO;

public interface ITextStorage
{
    ValueTask<EditorDocument> LoadAsync(string path, CancellationToken cancellationToken = default);

    ValueTask SaveAsync(EditorDocument document, string? path = null, CancellationToken cancellationToken = default);
}
