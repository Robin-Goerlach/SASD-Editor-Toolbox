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
