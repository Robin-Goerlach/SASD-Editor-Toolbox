using Sasd.Editor.Editing;
using Sasd.Editor.Model;

namespace Sasd.Editor.IO;

/// <summary>
/// Executes file-oriented editor commands independently from host prompting.
/// Hosts obtain a path from the user and pass it through a semantic command;
/// this service owns the actual compatibility operation.
/// </summary>
public sealed class EditorFileService
{
    private readonly EditorSession _session;
    private readonly IEditorFileCodec _codec;

    internal EditorFileService(EditorSession session, IEditorFileCodec? codec = null)
    {
        _session = session ?? throw new ArgumentNullException(nameof(session));
        _codec = codec ?? new FirstEdLegacyFileCodec();
    }

    /// <summary>
    /// Reads a file and inserts its logical lines immediately after the current
    /// line. The cursor is intentionally preserved, matching EditReatxtfil.
    /// </summary>
    public async ValueTask<int> ReadIntoCurrentWindowAsync(
        string path,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        var lines = await _codec.ReadAsync(path, cancellationToken).ConfigureAwait(false);
        if (lines.Count == 0)
        {
            return 0;
        }

        var window = _session.CurrentWindow;
        var document = window.Document;
        var originalCursor = window.Cursor;
        var insertionLine = window.Cursor.Line + 1;

        _session.Undo.Capture(window, "Read file");

        for (var index = 0; index < lines.Count; index++)
        {
            var line = lines[index];
            var flags = line.Flags & ~EditorLineFlags.InBlock;
            document.Buffer.InsertLine(insertionLine + index, line.Text, flags);
        }

        document.MarkChanged();
        window.Cursor = originalCursor;
        _session.RefreshBlockFlags();
        return lines.Count;
    }

    /// <summary>
    /// Writes the current text stream to a named file. This operation does not
    /// rename the document and does not clear its dirty flag; it corresponds to
    /// the historical direct EditFileWrite operation rather than modern Save As.
    /// </summary>
    public ValueTask WriteCurrentWindowAsync(
        string path,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        return _codec.WriteAsync(path, _session.CurrentWindow.Document.Buffer.Snapshot(), cancellationToken);
    }

    /// <summary>
    /// Saves to an explicit path, or to the document's associated path when one
    /// exists. The optional path is a modern integration convenience because
    /// hosts may already manage document identity separately from FIRST-ED.
    /// </summary>
    public async ValueTask<bool> SaveCurrentWindowAsync(
        string? path = null,
        CancellationToken cancellationToken = default)
    {
        var document = _session.CurrentWindow.Document;
        var target = string.IsNullOrWhiteSpace(path) ? document.FilePath : path;
        if (string.IsNullOrWhiteSpace(target))
        {
            return false;
        }

        await _codec.WriteAsync(target, document.Buffer.Snapshot(), cancellationToken).ConfigureAwait(false);
        document.MarkSaved(target);
        return true;
    }
}
