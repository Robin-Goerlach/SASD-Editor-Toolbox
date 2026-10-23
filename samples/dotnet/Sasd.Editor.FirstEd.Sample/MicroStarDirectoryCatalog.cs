namespace Sasd.Editor.FirstEd.Sample;

/// <summary>
/// One immutable filesystem entry for the MicroStar browser. The full path
/// is an identity returned by enumeration; the UI caption is only a label.
/// </summary>
internal sealed record MicroStarDirectoryEntry(
    string FullPath, string Name, bool IsDirectory, bool IsLink = false);

/// <summary>
/// Read-only filesystem discovery for the MicroStar sample application.
/// Keeping this separate from list navigation makes it straightforward to
/// add mockable providers and safe file operations in a later milestone.
/// </summary>
/// <remarks>
/// Enumeration deliberately performs no rename, copy, delete, write or
/// directory creation. Errors are returned to the caller instead of being
/// silently interpreted as an empty directory, which could mislead users
/// into thinking their files had disappeared.
/// </remarks>
internal static class MicroStarDirectoryCatalog
{
    /// <summary>
    /// Enumerate immediate children, never recurse or follow a directory
    /// tree. Snapshot the result so subsequent UI navigation is deterministic.
    /// Directories precede files, then sort using OrdinalIgnoreCase with
    /// Ordinal as a tie breaker, independently of the current locale.
    /// </summary>
    internal static IReadOnlyList<MicroStarDirectoryEntry> Read(string directory)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(directory);

        // Path.GetFullPath normalizes relative input once at the boundary.
        // If a directory disappears or becomes inaccessible, let the
        // IOException/UnauthorizedAccessException propagate to the host.
        var root = Path.GetFullPath(directory);
        var results = new List<MicroStarDirectoryEntry>();

        foreach (var path in Directory.EnumerateFileSystemEntries(root))
        {
            var attributes = File.GetAttributes(path);
            var isDirectory = (attributes & FileAttributes.Directory) != 0;
            var isLink = (attributes & FileAttributes.ReparsePoint) != 0;

            // Keep linked entries visible for diagnosis, but mark them
            // explicitly. Treating an OS-reported linked directory as an
            // ordinary directory would let Enter escape the intended
            // non-recursive, no-link-following browser contract.
            results.Add(new MicroStarDirectoryEntry(
                path, Path.GetFileName(path), isDirectory, isLink));
        }

        return results
            .OrderByDescending(entry => entry.IsDirectory)
            .ThenBy(entry => entry.Name, StringComparer.OrdinalIgnoreCase)
            .ThenBy(entry => entry.Name, StringComparer.Ordinal)
            .ToArray();
    }

    /// <summary>
    /// The single presentation caption policy shared by the selectable
    /// list model and the popup renderer. A link may also carry the OS
    /// Directory attribute: link identity MUST take precedence over [DIR].
    /// This returns a label only; filesystem operations use FullPath and
    /// IsLink independently, never parse captions into authorization.
    /// </summary>
    internal static string Caption(MicroStarDirectoryEntry entry)
    {
        ArgumentNullException.ThrowIfNull(entry);
        var prefix = entry.IsLink ? "[LNK] "
            : entry.IsDirectory ? "[DIR] " : "      ";
        return prefix + entry.Name;
    }

    /// <summary>
    /// Adapt discovery results to a UI list. IDs are full paths rather than
    /// display names: a file and directory in different folders can share a
    /// basename, and user-visible labels must not be trusted as identities.
    /// </summary>
    internal static MicroStarSelectionList CreateList(
        IReadOnlyList<MicroStarDirectoryEntry> entries, int visibleRows)
    {
        ArgumentNullException.ThrowIfNull(entries);
        return new MicroStarSelectionList(
            entries.Select(entry => new MicroStarListItem(
                entry.FullPath, Caption(entry))),
            visibleRows);
    }
}
