namespace Sasd.Editor.IO;

/// <summary>
/// Stages complete files beside their destinations and serializes publication
/// for the same normalized target path within this editor process.
/// </summary>
/// <remarks>
/// Without a per-target gate, an older slow write could publish *after* a
/// newer write, leaving stale bytes on disk even though the newer Save was
/// already acknowledged. Different target files remain independent.
///
/// The gate protects cooperating built-in .NET writers only: independent
/// processes, custom codecs, symlinks/hardlinks and filesystem case aliases
/// require coordination by the embedding host. Same-directory rename is
/// best-effort atomic publication, not a crash-durability/fsync promise.
/// File permissions and provider-specific metadata are not preserved here.
/// </remarks>
internal static class StagedFileWriter
{
    private sealed class TargetWriteGate
    {
        internal SemaphoreSlim Semaphore { get; } = new(1, 1);
        internal int Reservations { get; set; }
    }

    // Retain an entry while any writer holds or awaits that target gate.
    // Reference counting prevents an unbounded cache of paths for documents
    // that the editor has long since closed.
    private static readonly object GateSync = new();
    private static readonly Dictionary<string, TargetWriteGate> ActiveTargets = new(
        OperatingSystem.IsWindows() ? StringComparer.OrdinalIgnoreCase : StringComparer.Ordinal);

    /// <summary>
    /// The injectable staging delegate receives only a private temporary path.
    /// It enables deterministic failure/cancellation tests without filesystem
    /// timing assumptions. No user-visible document state is updated here.
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
        var gate = ReserveTarget(destination);
        var entered = false;

        try
        {
            // Reserving synchronously before the first await ensures a later
            // write to this path cannot begin staging before the active write
            // finishes. A cancelled waiter never creates a staging file.
            await gate.Semaphore.WaitAsync(cancellationToken).ConfigureAwait(false);
            entered = true;
            cancellationToken.ThrowIfCancellationRequested();

            var directory = Path.GetDirectoryName(destination)!;
            var temporaryPath = Path.Combine(
                directory,
                $".{Path.GetFileName(destination)}.sasd-{Guid.NewGuid():N}.tmp");

            try
            {
                // Until this stage is completely written, the old destination
                // remains untouched. A cancellation just before publication
                // likewise leaves the previous bytes in place.
                await writeStage(temporaryPath, cancellationToken).ConfigureAwait(false);
                cancellationToken.ThrowIfCancellationRequested();

                // This final same-directory rename is intentionally not
                // cancellable; after it succeeds, the new bytes are published.
                File.Move(temporaryPath, destination, overwrite: true);
            }
            finally
            {
                // A partially written stage never becomes a visible target.
                // Cleanup is best effort, without masking the real I/O error.
                try
                {
                    if (File.Exists(temporaryPath))
                    {
                        File.Delete(temporaryPath);
                    }
                }
                catch (IOException)
                {
                    // Preserve the original staging/publishing failure.
                }
                catch (UnauthorizedAccessException)
                {
                    // An inaccessible orphan must not hide the original error.
                }
            }
        }
        finally
        {
            if (entered)
            {
                gate.Semaphore.Release();
            }

            ReleaseTarget(destination, gate);
        }
    }

    private static TargetWriteGate ReserveTarget(string destination)
    {
        lock (GateSync)
        {
            if (!ActiveTargets.TryGetValue(destination, out var gate))
            {
                gate = new TargetWriteGate();
                ActiveTargets.Add(destination, gate);
            }

            gate.Reservations++;
            return gate;
        }
    }

    private static void ReleaseTarget(string destination, TargetWriteGate gate)
    {
        lock (GateSync)
        {
            gate.Reservations--;
            if (gate.Reservations == 0)
            {
                // All waiting/active users have exited; the next save can
                // allocate a fresh gate without leaking one per edited path.
                ActiveTargets.Remove(destination);
                gate.Semaphore.Dispose();
            }
        }
    }
}
