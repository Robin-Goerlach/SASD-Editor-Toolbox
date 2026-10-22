namespace Sasd.Editor.FirstEd.Sample;

/// <summary>
/// Small, explicitly write-capable application service for MicroStar's file
/// manager. Unlike directory discovery, these methods have side effects and
/// may therefore only be called after an affirmative user action in the host.
/// </summary>
/// <remarks>
/// Never implement file copying or renaming inside a renderer. The service
/// validates its filesystem contract, while the UI selects source, target
/// and presents errors. It is intentionally limited to regular files: a
/// directory operation could recursively move or copy enormous data and
/// requires a different, separately reviewed safety policy.
/// </remarks>
internal static class MicroStarFileOperations
{
    /// <summary>
    /// Copy a regular file to a NEW target. This overload intentionally
    /// offers no overwrite flag: an existing target is never destroyed by
    /// accident or by an Enter key used to dismiss a dialog.
    /// </summary>
    internal static void Copy(string sourcePath, string destinationPath)
    {
        var (source, destination) = Validate(sourcePath, destinationPath);

        // File.Copy with overwrite:false makes the final creation check at
        // the filesystem operation itself, not in a racy UI File.Exists
        // check. This is important when another process creates the target
        // while the user is entering its name.
        File.Copy(source, destination, overwrite: false);
    }

    /// <summary>
    /// Rename or move one regular file without replacing an existing target.
    /// A destination on another filesystem may not be supported by the
    /// underlying platform; let that error surface rather than emulating
    /// a cross-volume move with non-atomic copy/delete.
    /// </summary>
    internal static void Rename(string sourcePath, string destinationPath)
    {
        var (source, destination) = Validate(sourcePath, destinationPath);
        File.Move(source, destination, overwrite: false);
    }

    private static (string Source, string Destination) Validate(
        string sourcePath, string destinationPath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(sourcePath);
        ArgumentException.ThrowIfNullOrWhiteSpace(destinationPath);

        var source = Path.GetFullPath(sourcePath);
        var destination = Path.GetFullPath(destinationPath);

        // The same physical file must not be its own copy or rename target.
        // This lexical guard is useful feedback; aliases via hard links or
        // symlinks still need OS-level checks, not unsupported assumptions.
        if (string.Equals(source, destination,
            OperatingSystem.IsWindows() ? StringComparison.OrdinalIgnoreCase
                : StringComparison.Ordinal))
            throw new IOException("Source and destination must differ.");

        // File.Exists is false for a directory and for inaccessible paths,
        // but failures are never treated as permission to perform work.
        // File.Copy/Move subsequently perform authoritative OS checks.
        if (!File.Exists(source))
            throw new FileNotFoundException("Source must be an existing regular file.", source);
        if (Directory.Exists(destination))
            throw new IOException("Destination is an existing directory.");

        return (source, destination);
    }
}
