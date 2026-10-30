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

    /// <summary>
    /// Delete one explicit regular-file path, and only after the caller has
    /// collected an affirmative confirmation. This service never prompts:
    /// keeping user approval in the host makes denial and cancellation easy
    /// to audit and prevents a service call from implying consent.
    /// </summary>
    /// <remarks>
    /// This operation is permanent (not a recycle-bin move) and is not part
    /// of document Undo. Reject symbolic links/reparse points and directories
    /// in this conservative first slice; following links could affect an
    /// unintended file. The filesystem remains authoritative if the entry
    /// changes between validation and deletion. A stronger race-free
    /// capability/handle-based deletion API is future hardening work.
    /// </remarks>
    internal static void Delete(string path)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        var fullPath = Path.GetFullPath(path);
        // Use the same regular-file boundary as Copy and Rename. Only the
        // caller's user-confirmation requirements differ across operations.
        ValidateRegularFile(fullPath);

        // The host must never invoke this after a denied confirmation.
        // We intentionally do not recursively delete directories.
        File.Delete(fullPath);
    }

    /// <summary>
    /// One conservative file-kind boundary for all mutating MicroStar file
    /// operations. A reparse point or directory must never acquire different
    /// permissions merely because the caller chose Copy, Rename or Delete.
    /// </summary>
    private static void ValidateRegularFile(string fullPath)
    {
        // File.GetAttributes checks the entry's metadata (including the
        // reparse-point flag) rather than treating File.Exists as evidence
        // that an entry is an ordinary file. File.Exists may follow links.
        //
        // This preflight check is not an atomic guarantee: another process
        // could replace an entry before File.Copy, File.Move or File.Delete.
        // The host does not claim race-free filesystem authority.
        var attributes = File.GetAttributes(fullPath);
        if ((attributes & (FileAttributes.Directory | FileAttributes.ReparsePoint)) != 0)
            throw new IOException("Only regular, non-linked files are supported.");
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

        // The file manager deliberately works on regular files, not links.
        // File.Exists follows a symbolic link and would otherwise let Copy
        // read an unrelated linked target or Rename move the link itself.
        // Use the same reparse-point boundary as Delete so all three public
        // file actions share one conservative source-ownership contract.
        // This is a preflight safeguard; filesystem races still require
        // stronger handle-based operations in a future hardening phase.
        ValidateRegularFile(source);

        if (Directory.Exists(destination))
            throw new IOException("Destination is an existing directory.");

        // A dangling symbolic link can look like a free destination to
        // File.Exists. On Windows, Copy with overwrite:false may still
        // follow that link and create its previously missing target.
        // Inspect the destination directory entry itself, including links
        // whose targets do not exist, before invoking the OS file operation.
        // This is a defensive preflight, not an atomic race-free guarantee:
        // another process can replace the entry after this check.
        if (new FileInfo(destination).LinkTarget is not null)
            throw new IOException("Destination must not be a symbolic link.");

        return (source, destination);
    }
}
