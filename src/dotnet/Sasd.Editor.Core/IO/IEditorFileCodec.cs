using Sasd.Editor.Model;

namespace Sasd.Editor.IO;

/// <summary>
/// Converts an editor-compatible file format to and from logical editor lines.
/// This is intentionally separate from <see cref="ITextStorage"/>: storage
/// providers load/save complete documents, while compatibility commands may
/// insert file contents into an already open text stream.
///
/// Implementations are expected to observe the supplied cancellation token
/// promptly during awaited I/O. The editor interruption bridge uses that token
/// to translate a host-originated Ctrl-U into cancellation of in-flight file I/O.
/// </summary>
public interface IEditorFileCodec
{
    ValueTask<IReadOnlyList<EditorLineSnapshot>> ReadAsync(
        string path,
        CancellationToken cancellationToken = default);

    ValueTask WriteAsync(
        string path,
        IReadOnlyList<EditorLineSnapshot> lines,
        CancellationToken cancellationToken = default);
}
