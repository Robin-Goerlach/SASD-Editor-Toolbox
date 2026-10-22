using Sasd.Editor.Editing;
using Sasd.Editor.IO;
using Sasd.Editor.Model;

namespace Sasd.Editor.FirstEd.Sample;

/// <summary>
/// MicroStar sample application helper for writing ONLY the currently marked
/// whole-line block to a file. This is distinct from FIRST-ED's EditFileWrite,
/// which exports the complete current document.
/// </summary>
/// <remarks>
/// The 1985 handbook describes MicroStar block-specific file commands. It
/// does not establish a cross-platform overwrite or atomicity contract.
/// The sample therefore uses the existing verified legacy codec and host
/// confirmation, without inventing a new file encoding or editing Core.
/// </remarks>
internal static class ConsoleMicroStarBlockFileService
{
    internal static bool HasExportableBlock(EditorSession session)
    {
        ArgumentNullException.ThrowIfNull(session);
        var block = session.Block;
        if (block is null)
            return false;
        var document = session.FindDocument(block.DocumentId);
        return document is not null
            && block.FirstLine >= 0
            && block.LastLine < document.Buffer.LineCount;
    }

    internal static async ValueTask<bool> ExportAsync(
        EditorSession session, string path,
        CancellationToken cancellationToken = default,
        IEditorFileCodec? codec = null)
    {
        ArgumentNullException.ThrowIfNull(session);
        ArgumentException.ThrowIfNullOrWhiteSpace(path);

        var block = session.Block;
        if (block is null || !HasExportableBlock(session))
            return false;

        var document = session.FindDocument(block.DocumentId)!;
        var selected = new EditorLineSnapshot[block.LineCount];
        for (var i = 0; i < selected.Length; i++)
        {
            var line = document.Buffer.GetLine(block.FirstLine + i);

            // InBlock is session-derived VISUAL state, not a persisted text
            // flag. Wrapped is actual logical file topology and must survive.
            // UserColored is also an annotation; the historical byte-level
            // codec controls which metadata can be expressed on disk.
            selected[i] = line with { Flags = line.Flags & ~EditorLineFlags.InBlock };
        }

        // Snapshot the entire selected content before awaited I/O. A user
        // may move the marker or edit another window while the codec writes;
        // the export must always contain the originally selected block.
        await session.Interruption.RunInterruptibleAsync(
            token => (codec ?? new FirstEdLegacyFileCodec()).WriteAsync(
                path, selected, token), cancellationToken).ConfigureAwait(false);

        // No document renaming, Dirty reset, cursor movement or Undo entry.
        return true;
    }
}
