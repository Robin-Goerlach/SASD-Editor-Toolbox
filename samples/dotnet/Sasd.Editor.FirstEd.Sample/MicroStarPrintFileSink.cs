using System.Text;

namespace Sasd.Editor.FirstEd.Sample;

/// <summary>
/// Portable, explicit file output for a MicroStar print job. This is a
/// print-to-file adapter, NOT the historical DOS printer port or a spooler.
/// The caller must obtain an explicitly new path from the user.
/// </summary>
internal sealed class MicroStarPrintFileSink : IMicroStarPrintSink, IDisposable
{
    private readonly StreamWriter _writer;
    private bool _disposed;

    internal MicroStarPrintFileSink(string outputPath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(outputPath);

        // CreateNew atomically refuses existing files, including when
        // another process races a host-side existence check. Unlike append
        // or truncate modes, a mistaken target never destroys user data.
        var stream = new FileStream(Path.GetFullPath(outputPath),
            FileMode.CreateNew, FileAccess.Write, FileShare.None);
        _writer = new StreamWriter(stream, new UTF8Encoding(false));
    }

    public async ValueTask WriteLineAsync(string text, CancellationToken cancellationToken)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        ArgumentNullException.ThrowIfNull(text);
        cancellationToken.ThrowIfCancellationRequested();

        // The explicit line boundary is the print-file policy. We do NOT
        // silently reinterpret original Wrapped flags as paper formatting.
        // Flush each bounded scheduler slice so the file has an observable
        // prefix after any error or cancellation. A partial output file is
        // retained intentionally rather than pretending printing is atomic.
        // Pagination emits a stand-alone form-feed record. Unlike a text
        // row, FF is a device-control character, not a logical line. Writing
        // it with WriteLineAsync would append an unintended platform-specific
        // LF/CRLF byte sequence after the page eject, changing the output
        // stream depending on the host OS. Keep this explicit boundary
        // local to the file adapter; the cooperative job stays agnostic.
        //
        // This is SASD's modern print-file encoding convention and does
        // not claim to reconstruct all historical printer escape sequences.
        if (text == "\f")
            await _writer.WriteAsync(text.AsMemory(), cancellationToken)
                .ConfigureAwait(false);
        else
            await _writer.WriteLineAsync(text.AsMemory(), cancellationToken)
                .ConfigureAwait(false);
        await _writer.FlushAsync(cancellationToken).ConfigureAwait(false);
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        _writer.Dispose();
    }
}
