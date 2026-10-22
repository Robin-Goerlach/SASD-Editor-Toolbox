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
    /// line. With no intervening navigation, the cursor is preserved as in
    /// EditReatxtfil; later explicit navigation is never overwritten.
    /// </summary>
    /// <remarks>
    /// Modern async hosts may select another view while decoding is pending.
    /// The target is the initiating view, not the view selected at completion.
    /// If that destination is destroyed, reattached or edited meanwhile, the
    /// import is rejected before mutation because its saved line anchor may no
    /// longer identify the intended location.
    /// </remarks>
    public ValueTask<int> ReadIntoCurrentWindowAsync(
        string path,
        CancellationToken cancellationToken = default) =>
        ReadLinesAsync(path, selectImportedBlock: false, cancellationToken);

    /// <summary>
    /// Read Block reuses the existing fully validated import path. The only
    /// semantic difference from Read File is marking the inserted whole lines
    /// in the SAME transaction as the text insertion and topology repair.
    /// </summary>
    public ValueTask<int> ReadBlockIntoCurrentWindowAsync(
        string path,
        CancellationToken cancellationToken = default) =>
        ReadLinesAsync(path, selectImportedBlock: true, cancellationToken);

    private async ValueTask<int> ReadLinesAsync(
        string path, bool selectImportedBlock, CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);

        // A file decoder may suspend for I/O while a host continues processing
        // input in other windows. Bind the command to the initiating view and
        // insertion anchor BEFORE the first await; using CurrentWindow after
        // decoding would silently import into whichever document is active then.
        var window = _session.CurrentWindow;
        var document = window.Document;
        var insertionLine = window.Cursor.Line + 1;
        var originalVersion = document.Version;
        var originalLines = document.Buffer.Snapshot();

        var lines = await _session.Interruption
            .RunInterruptibleAsync(
                token => _codec.ReadAsync(path, token),
                cancellationToken)
            .ConfigureAwait(false);
        if (lines is null)
        {
            throw new IOException("The file codec returned a null decoded-line collection.");
        }

        // Copy the provider's list before any further awaited checkpoints.
        // A custom codec may expose a mutable collection; insertion must use
        // precisely the lines that were validated, not a subsequently edited
        // provider-side list.
        var decodedLines = lines.ToArray();
        if (decodedLines.Length == 0)
        {
            return 0;
        }

        // FIRST-ED line boundaries are metadata, not literal text. A custom
        // IEditorFileCodec must not smuggle delimiters into a logical line or
        // unrecognized flags into the buffer. Validate the *whole* payload
        // before committing anything so one bad trailing line cannot cause
        // a partial import or an orphaned Undo entry.
        const EditorLineFlags allowedFlags =
            EditorLineFlags.InBlock | EditorLineFlags.Wrapped | EditorLineFlags.UserColored;
        for (var index = 0; index < decodedLines.Length; index++)
        {
            var line = decodedLines[index];
            if (line is null
                || line.Text is null
                || line.Text.Contains('\r')
                || line.Text.Contains('\n')
                || line.Text.Contains('\u008D')
                || (line.Flags & ~allowedFlags) != EditorLineFlags.None)
            {
                throw new IOException(
                    "The file codec returned a malformed logical line or unsupported flags.");
            }

            // A large provider result may take time to validate. Give the host
            // a bounded chance to process Ctrl-U before structural mutation;
            // the previously captured destination is rechecked afterward.
            if ((index & 127) == 127)
            {
                await _session.Interruption.CheckpointAsync(cancellationToken).ConfigureAwait(false);
            }
        }

        // The entire compatibility file is decoded before editor mutation.
        // A Ctrl-U detected during I/O has already aborted the operation.
        // If the initiating view disappeared, was reattached to another
        // stream, or its text changed, the saved numeric line index can no
        // longer be trusted as the original insertion anchor. Reject rather
        // than risking a write into the wrong document or logical line.
        if (!_session.Windows.Any(candidate => ReferenceEquals(candidate, window))
            || !ReferenceEquals(window.Document, document)
            || document.Version != originalVersion
            || !document.Buffer.Snapshot().SequenceEqual(originalLines))
        {
            throw new InvalidOperationException(
                "The file-read destination changed while input was being decoded.");
        }

        var undo = _session.Undo.BeginTransaction(_session, window, "Read file");

        for (var index = 0; index < decodedLines.Length; index++)
        {
            var line = decodedLines[index];
            var flags = line.Flags & ~EditorLineFlags.InBlock;
            document.Buffer.InsertLine(insertionLine + index, line.Text, flags);
        }

        // The historical editor's line descriptors made existing references stay
        // attached to their logical lines automatically. Our integer line model
        // needs an explicit EditRealign-like pass after structural insertion.
        _session.Topology.LinesInserted(document, insertionLine, decodedLines.Length);

        document.MarkChanged();

        if (selectImportedBlock)
        {
            // The original initiating document is captured before awaited IO.
            // Replacing the complete block avoids cursor movement, transient
            // half-defined endpoints and a second, non-undoable state change.
            _session.ReplaceBlockForTopology(new EditorBlock(
                document.DocumentId, insertionLine,
                insertionLine + decodedLines.Length - 1));
        }

        // Do not restore the initiating cursor blindly: the host may have
        // explicitly navigated while I/O was pending. Topology realignment
        // preserves that surviving-line anchor, and an unchanged cursor stays
        // on the original source line without any special assignment.
        _session.RefreshBlockFlags();
        undo.Commit();
        return decodedLines.Length;
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

        // Capture the version together with the exact lines being written.
        // A host can keep editing this document (including in another linked
        // window) while the codec awaits filesystem I/O.
        var versionAtWrite = document.Version;
        var snapshot = document.Buffer.Snapshot();
        var saveRequestId = document.BeginSaveRequest();
        await _session.Interruption
            .RunInterruptibleAsync(
                token => _codec.WriteAsync(target, snapshot, token),
                cancellationToken)
            .ConfigureAwait(false);

        // The write itself succeeded. Mark the document clean only if no
        // newer text/metadata has replaced the persisted snapshot.
        document.AcknowledgeSaveRequest(saveRequestId, versionAtWrite, snapshot, target);
        return true;
    }
}
