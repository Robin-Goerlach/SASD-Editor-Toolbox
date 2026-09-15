using System.Text;
using Sasd.Editor.Document;
using Sasd.Editor.Model;

namespace Sasd.Editor.IO;

/// <summary>
/// UTF-8 file storage for the first .NET implementation. Encoding policy is
/// explicit so additional storage/encoding providers can be added later.
/// </summary>
public sealed class FileTextStorage(Encoding? encoding = null) : ITextStorage
{
    private readonly Encoding _encoding = encoding ?? new UTF8Encoding(encoderShouldEmitUTF8Identifier: false);

    public async ValueTask<EditorDocument> LoadAsync(string path, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        var fullPath = Path.GetFullPath(path);
        var text = await File.ReadAllTextAsync(fullPath, _encoding, cancellationToken).ConfigureAwait(false);
        var newLine = DetectNewLine(text);
        var normalized = text.Replace("\r\n", "\n", StringComparison.Ordinal).Replace('\r', '\n');
        var lines = normalized.Split('\n').Select(static line => new EditorLineSnapshot(line));
        var document = new EditorDocument(new LinkedLineTextBuffer(lines));
        document.SetPersistenceMetadata(fullPath, newLine);
        document.MarkSaved(fullPath);
        return document;
    }

    public async ValueTask SaveAsync(EditorDocument document, string? path = null, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(document);
        var target = string.IsNullOrWhiteSpace(path) ? document.FilePath : Path.GetFullPath(path);
        if (string.IsNullOrWhiteSpace(target))
        {
            throw new InvalidOperationException("A target path is required for a document that has not been named.");
        }

        var content = string.Join(document.NewLine, document.Buffer.Snapshot().Select(static line => line.Text));
        await File.WriteAllTextAsync(target, content, _encoding, cancellationToken).ConfigureAwait(false);
        document.MarkSaved(target);
    }

    private static string DetectNewLine(string text)
    {
        if (text.Contains("\r\n", StringComparison.Ordinal)) return "\r\n";
        if (text.Contains('\n')) return "\n";
        if (text.Contains('\r')) return "\r";
        return Environment.NewLine;
    }
}
