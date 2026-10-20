using System.Security.Cryptography;
using System.Text;
using Sasd.Editor.Document;
using Sasd.Editor.Model;

namespace Sasd.Editor.IO;

/// <summary>
/// UTF-8 file storage for the first .NET implementation. The default encoding
/// rejects malformed UTF-8 and invalid UTF-16 input instead of silently replacing
/// bytes/characters with U+FFFD. A caller can explicitly supply another
/// <see cref="Encoding"/> with its own fallback and byte-order-mark policy.
/// </summary>
public sealed class FileTextStorage(Encoding? encoding = null) : ITextStorage
{
    // A replacement-fallback decoder can silently corrupt an existing file:
    // its invalid bytes turn into U+FFFD and the next Save would overwrite the
    // original data. The default therefore uses *strict* UTF-8 for both reads
    // and writes. Explicitly supplied encodings retain their own configured
    // fallback semantics, preserving this provider's extension boundary.
    private readonly Encoding _encoding = encoding ?? new UTF8Encoding(
        encoderShouldEmitUTF8Identifier: false,
        throwOnInvalidBytes: true);

    private readonly bool _useDefaultStrictUtf8 = encoding is null;

    public async ValueTask<EditorDocument> LoadAsync(string path, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        var fullPath = Path.GetFullPath(path);
        string text;
        DocumentFileRevision? loadedRevision = null;
        if (_useDefaultStrictUtf8)
        {
            // File.ReadAllTextAsync delegates byte-order-mark detection to a
            // StreamReader, which may switch encodings when it sees a BOM.
            // That is convenient for an explicitly configured storage adapter,
            // but it can silently reinterpret non-UTF-8 input in the default
            // *UTF-8* provider. Decode its bytes explicitly so every invalid
            // sequence reliably raises DecoderFallbackException.
            var bytes = await File.ReadAllBytesAsync(fullPath, cancellationToken)
                .ConfigureAwait(false);

            // Accept the optional UTF-8 signature used by existing files,
            // without making it part of the document's logical text. Saves
            // continue to use BOM-less UTF-8, matching the previous writer
            // policy. UTF-16/UTF-32 BOMs are not auto-detected in this default
            // path; a host must explicitly choose another Encoding for them.
            var start = bytes.Length >= 3
                && bytes[0] == 0xEF && bytes[1] == 0xBB && bytes[2] == 0xBF
                ? 3 : 0;
            text = _encoding.GetString(bytes, start, bytes.Length - start);

            // A UTF-8 BOM and newline representation are part of on-disk
            // bytes even though neither may appear in the normalized buffer.
            // Record the bytes observed during THIS load, not a second read.
            loadedRevision = RevisionOf(fullPath, bytes);
        }
        else
        {
            // Preserve legacy opt-in behavior for callers that explicitly
            // supply an Encoding: the framework reader handles its BOM and
            // configured decoder fallback independently of our strict default.
            text = await File.ReadAllTextAsync(fullPath, _encoding, cancellationToken)
                .ConfigureAwait(false);
        }

        // Build an EditorDocument only *after* all input bytes are decoded.
        // A failed decode never creates a partially initialized document that
        // an application might accidentally present or later save.
        var newLine = DetectNewLine(text);
        var normalized = text.Replace("\r\n", "\n", StringComparison.Ordinal).Replace('\r', '\n');
        var lines = normalized.Split('\n').Select(static line => new EditorLineSnapshot(line));
        var document = new EditorDocument(new LinkedLineTextBuffer(lines));
        document.SetPersistenceMetadata(fullPath, newLine);
        document.MarkSaved(fullPath);
        if (loadedRevision is not null)
            document.RecordLoadedFileRevision(loadedRevision);
        return document;
    }

    public async ValueTask SaveAsync(EditorDocument document, string? path = null, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(document);
        var requestedPath = string.IsNullOrWhiteSpace(path) ? document.FilePath : path;
        if (string.IsNullOrWhiteSpace(requestedPath))
        {
            throw new InvalidOperationException("A target path is required for a document that has not been named.");
        }

        var target = Path.GetFullPath(requestedPath);
        // Only guard a target whose original raw bytes are known. Explicit
        // Save As to another path and never-loaded documents retain their
        // existing caller-controlled overwrite policy.
        var observed = document.ObservedFileRevision;
        var expected = SameFilePath(observed?.FullPath, target) ? observed : null;

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

        // Capture precisely the bytes being published, including the
        // explicitly configured encoding's preamble. We must not re-read a
        // target after Save to establish a new baseline: an external writer
        // might already have raced with us and supplied different bytes.
        var encoded = _encoding.GetBytes(content);
        var preamble = _encoding.GetPreamble();
        var payload = new byte[preamble.Length + encoded.Length];
        preamble.CopyTo(payload, 0);
        encoded.CopyTo(payload, preamble.Length);
        var publishedRevision = RevisionOf(target, payload);

        var saveRequestId = document.BeginSaveRequest();
        await StagedFileWriter.WriteAsync(
            target,
            (temporaryPath, token) => File.WriteAllBytesAsync(temporaryPath, payload, token),
            cancellationToken,
            expected is null ? null : (destination, token) =>
                VerifyRevisionAsync(destination, expected, token)).ConfigureAwait(false);
        document.AcknowledgeSaveRequest(
            saveRequestId, versionAtWrite, snapshot, target, newLineAtWrite,
            publishedRevision);
    }

    /// <summary>
    /// Checks the target's current BYTES after staging, as close to the
    /// overwrite as our own process can arrange. Timestamp/length alone
    /// cannot detect an equal-size external edit or a changed byte-order mark.
    /// </summary>
    private static async ValueTask VerifyRevisionAsync(
        string destination, DocumentFileRevision expected,
        CancellationToken cancellationToken)
    {
        // A deleted target also fails (FileNotFoundException). Neither
        // outcome may silently turn an ordinary Save into Save As.
        var bytes = await File.ReadAllBytesAsync(destination, cancellationToken)
            .ConfigureAwait(false);
        if (!string.Equals(RevisionOf(destination, bytes).Sha256,
                           expected.Sha256, StringComparison.Ordinal))
        {
            // Avoid exposing user paths in the host-facing fallback message.
            throw new IOException(
                "The target file changed outside the editor since it was loaded or saved.");
        }
    }

    private static DocumentFileRevision RevisionOf(string path, byte[] bytes) =>
        new(path, Convert.ToHexString(SHA256.HashData(bytes)));

    private static bool SameFilePath(string? left, string? right) =>
        left is not null && string.Equals(left, right,
            OperatingSystem.IsWindows()
                ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal);

    private static string DetectNewLine(string text)
    {
        if (text.Contains("\r\n", StringComparison.Ordinal)) return "\r\n";
        if (text.Contains('\n')) return "\n";
        if (text.Contains('\r')) return "\r";
        return Environment.NewLine;
    }
}
