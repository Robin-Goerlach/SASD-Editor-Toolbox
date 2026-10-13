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
        var lines = await _session.Interruption
            .RunInterruptibleAsync(
                token => _codec.ReadAsync(path, token),
                cancellationToken)
            .ConfigureAwait(false);
        if (lines.Count == 0)
        {
            return 0;
        }

        // The entire compatibility file is decoded before editor mutation starts.
        // A Ctrl-U detected during the awaited read therefore cancels I/O and
        // leaves document text, cursor, dirty state and undo history untouched.
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

        // The historical editor's line descriptors made existing references stay
        // attached to their logical lines automatically. Our integer line model
        // needs an explicit EditRealign-like pass after structural insertion.
        _session.Topology.LinesInserted(document, insertionLine, lines.Count);

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
        var snapshot = _session.CurrentWindow.Document.Buffer.Snapshot();
        return _session.Interruption.RunInterruptibleAsync(
            token => _codec.WriteAsync(path, snapshot, token),
            cancellationToken);
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

        var snapshot = document.Buffer.Snapshot();
        await _session.Interruption
            .RunInterruptibleAsync(
                token => _codec.WriteAsync(target, snapshot, token),
                cancellationToken)
            .ConfigureAwait(false);
        document.MarkSaved(target);
        return true;
    }
}
