namespace Sasd.Editor.FirstEd.Sample;

/// <summary>
/// Browser-local interpretation of file-management shortcuts. This is
/// separate from FIRST-ED's semantic editing command key map.
/// </summary>
internal enum MicroStarFileAction { Copy, Rename, Delete }

internal static class MicroStarFileActionPolicy
{
    /// <summary>
    /// Only regular files can be copied or renamed by this first version.
    /// Shift is benign, while Alt and Ctrl are not accepted because many
    /// terminals reserve modified function keys for unrelated behavior.
    /// </summary>
    internal static MicroStarFileAction? Resolve(
        ConsoleKey key, ConsoleModifiers modifiers,
        MicroStarDirectoryEntry? entry)
    {
        if (entry is null || entry.IsDirectory || entry.IsLink ||
            (modifiers & (ConsoleModifiers.Alt | ConsoleModifiers.Control)) != 0)
            return null;

        return key switch
        {
            ConsoleKey.F5 => MicroStarFileAction.Copy,
            ConsoleKey.F6 => MicroStarFileAction.Rename,
            ConsoleKey.F8 => MicroStarFileAction.Delete,
            _ => null
        };
    }

    /// <summary>
    /// Relative destination names are anchored to the currently browsed
    /// directory, NEVER to the process working directory (which may differ
    /// from the browser after several navigation steps).
    /// </summary>
    internal static string ResolveDestination(string currentDirectory, string enteredPath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(currentDirectory);
        ArgumentException.ThrowIfNullOrWhiteSpace(enteredPath);
        return Path.GetFullPath(enteredPath, currentDirectory);
    }
}
