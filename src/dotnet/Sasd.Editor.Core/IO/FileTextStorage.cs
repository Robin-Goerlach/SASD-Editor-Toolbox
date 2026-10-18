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

        // Keep modern UTF-8 storage subject to the same in-flight edit rule
        // as the historical FIRST-ED codec: saving yesterday's snapshot must
        // never mark today's document clean.
        var versionAtWrite = document.Version;
        var snapshot = document.Buffer.Snapshot();
        var newLineAtWrite = document.NewLine;

        // The modern storage format may encode Unicode freely, but it still
        // uses one logical line per buffer snapshot. CR/LF inside a line would
        // become structural delimiters on the next LoadAsync and corrupt the
        // line/marker topology. Verify all records *before* staging begins.
        for (var index = 0; index < snapshot.Count; index++)
        {
            var line = snapshot[index];
            if (line is null || line.Text is null
                || line.Text.Contains('\r') || line.Text.Contains('\n'))
            {
                throw new InvalidDataException(
                    "A logical line cannot contain CR or LF in modern UTF-8 storage.");
            }
        }

        // SetPersistenceMetadata is public, so reject invalid configured
        // separators rather than save a file that loads as one merged line.
        if (newLineAtWrite is not ("\r\n" or "\n" or "\r"))
        {
            throw new InvalidDataException(
                "The document newline style must be CRLF, LF or CR.");
        }

        var content = string.Join(newLineAtWrite, snapshot.Select(static line => line.Text));
        // Publish only a fully encoded and written snapshot. An interrupted
        // staging write leaves the old destination and document identity alone.
        await StagedFileWriter.WriteAsync(
            target,
            (temporaryPath, token) => File.WriteAllTextAsync(temporaryPath, content, _encoding, token),
            cancellationToken).ConfigureAwait(false);
        document.MarkSnapshotSaved(versionAtWrite, snapshot, target, newLineAtWrite);
    }

    private static string DetectNewLine(string text)
    {
        if (text.Contains("\r\n", StringComparison.Ordinal)) return "\r\n";
        if (text.Contains('\n')) return "\n";
        if (text.Contains('\r')) return "\r";
        return Environment.NewLine;
    }
}
