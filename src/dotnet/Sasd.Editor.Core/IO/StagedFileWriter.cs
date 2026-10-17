namespace Sasd.Editor.IO;

/// <summary>
/// Writes a complete file beside its destination and publishes it only after
/// the staging write succeeds. Used by both the modern UTF-8 storage provider
/// and the byte-compatible FIRST-ED codec.
/// </summary>
/// <remarks>
/// The staging file is placed in the same directory as the destination so
/// replacing the destination uses a same-filesystem rename. On supported
/// filesystems the rename is atomic; this is not a durability/fsync guarantee
/// and does not try to preserve every platform-specific file attribute.
/// A failed or cancelled staging write never opens or truncates the old target.
/// The final rename itself is deliberately not cancellable: once publication
/// begins, cancellation must not report failure after replacing the file.
/// </remarks>
internal static class StagedFileWriter
{
    /// <summary>
    /// The writeStage callback writes only to the private temporary path.
    /// Keeping it injectable permits deterministic cancellation/failure tests
    /// without depending on real disk-write timing.
    /// </summary>
    internal static async ValueTask WriteAsync(
        string path,
        Func<string, CancellationToken, Task> writeStage,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        ArgumentNullException.ThrowIfNull(writeStage);
        cancellationToken.ThrowIfCancellationRequested();

        var destination = Path.GetFullPath(path);
        var directory = Path.GetDirectoryName(destination)!;
        var temporaryPath = Path.Combine(
            directory,
            $".{Path.GetFileName(destination)}.sasd-{Guid.NewGuid():N}.tmp");

        try
        {
            // The destination is untouched until the entire encoded payload
            // has been staged and the last cancellation check has passed.
            await writeStage(temporaryPath, cancellationToken).ConfigureAwait(false);
            cancellationToken.ThrowIfCancellationRequested();

            // Same-directory replacement avoids an intermediate partially
            // written destination. Only this commit point changes the target.
            File.Move(temporaryPath, destination, overwrite: true);
        }
        finally
        {
            // If writing, cancellation, or rename fails, discard a staged
            // fragment. Cleanup errors must not hide the original I/O failure.
            try
            {
                if (File.Exists(temporaryPath))
                {
                    File.Delete(temporaryPath);
                }
            }
            catch (IOException)
            {
                // Best effort: callers still receive the original exception.
            }
            catch (UnauthorizedAccessException)
            {
                // Best effort: do not turn a completed rename into failure.
            }
        }
    }
}
