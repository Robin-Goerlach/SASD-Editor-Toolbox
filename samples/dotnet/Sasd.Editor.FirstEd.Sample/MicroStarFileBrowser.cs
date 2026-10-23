namespace Sasd.Editor.FirstEd.Sample;

/// <summary>
/// The possible results of one file-browser key stroke. Only FileChosen
/// authorizes the host to hand a path to an editor command. Browsing itself
/// is read-only and never imports or modifies document text.
/// </summary>
internal enum MicroStarBrowserResultKind
{
    Ignored,
    Redraw,
    FileChosen,
    Cancelled
}

internal readonly record struct MicroStarBrowserResult(
    MicroStarBrowserResultKind Kind, string? Path = null);

/// <summary>
/// Host-neutral directory-navigation controller. It composes the existing
/// read-only filesystem catalog with the independent scrolling list model.
/// No Console calls, screen geometry or EditorSession state belong here.
/// </summary>
/// <remarks>
/// Enter on a directory changes the browsed folder; Enter on a regular file
/// returns its full path. Backspace visits the parent directory; Escape and
/// Ctrl-U cancel without producing a path. The caller owns the decision to
/// read/write the chosen file. The browser never guesses permissions, follows
/// links recursively or performs destructive filesystem operations.
/// </remarks>
internal sealed class MicroStarFileBrowser
{
    private readonly int _visibleRows;
    private MicroStarDirectoryEntry[] _entries = [];
    private MicroStarSelectionList _selection;

    internal MicroStarFileBrowser(string initialDirectory, int visibleRows)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(initialDirectory);
        if (visibleRows < 1) throw new ArgumentOutOfRangeException(nameof(visibleRows));

        _visibleRows = visibleRows;
        _selection = new MicroStarSelectionList([], visibleRows);
        CurrentDirectory = Path.GetFullPath(initialDirectory);

        // A failed enumeration is an actual error, not a silent empty list.
        // It occurs before the browser becomes visible or chooses any file.
        Reload();
    }

    internal string CurrentDirectory { get; private set; }
    internal MicroStarSelectionList Selection => _selection;
    internal IReadOnlyList<MicroStarDirectoryEntry> VisibleEntries =>
        _entries.Skip(_selection.FirstVisible).Take(_visibleRows).ToArray();
    internal int FirstVisible => _selection.FirstVisible;
    internal int TotalEntries => _entries.Length;

    /// <summary>
    /// Current filesystem identity, not the abbreviated screen caption.
    /// File operations must use this value and refuse directories.
    /// </summary>
    internal MicroStarDirectoryEntry? SelectedEntry =>
        _selection.Selected is { } item
            ? _entries.First(entry => entry.FullPath == item.Id)
            : null;

    /// <summary>
    /// After a successful copy/rename, obtain a fresh listing. The same
    /// prepare-then-commit logic is used as directory navigation, so an
    /// I/O error leaves the old list intact.
    /// </summary>
    internal void Refresh() => Navigate(CurrentDirectory);

    internal MicroStarBrowserResult Handle(ConsoleKey key, ConsoleModifiers modifiers = 0)
    {
        if (key == ConsoleKey.Escape ||
            (key == ConsoleKey.U && modifiers.HasFlag(ConsoleModifiers.Control)))
            return new(MicroStarBrowserResultKind.Cancelled);

        // Do not reinterpret terminal Alt/Ctrl shortcuts as a directory
        // transition. Ctrl-U is deliberately handled above.
        if ((modifiers & (ConsoleModifiers.Control | ConsoleModifiers.Alt)) != 0)
            return new(MicroStarBrowserResultKind.Ignored);

        if (key == ConsoleKey.Backspace)
        {
            var parent = Directory.GetParent(CurrentDirectory);
            if (parent is null) return new(MicroStarBrowserResultKind.Ignored);
            Navigate(parent.FullName);
            return new(MicroStarBrowserResultKind.Redraw);
        }

        if (key == ConsoleKey.Enter)
        {
            var selected = _selection.Selected;
            if (selected is null) return new(MicroStarBrowserResultKind.Ignored);

            // Selection IDs are full paths supplied by the read-only catalog;
            // never parse a screen caption into a filesystem path.
            var entry = _entries.First(item => item.FullPath == selected.Id);
            // A link must never become a navigable directory nor an
            // implicit file-open target. The browser only returns ordinary
            // entries; linked paths require a separately reviewed policy.
            if (entry.IsLink)
                return new(MicroStarBrowserResultKind.Ignored);

            if (entry.IsDirectory)
            {
                Navigate(entry.FullPath);
                return new(MicroStarBrowserResultKind.Redraw);
            }

            return new(MicroStarBrowserResultKind.FileChosen, entry.FullPath);
        }

        return _selection.Handle(key, modifiers) switch
        {
            MicroStarListResult.SelectionChanged => new(MicroStarBrowserResultKind.Redraw),
            MicroStarListResult.Cancelled => new(MicroStarBrowserResultKind.Cancelled),
            _ => new(MicroStarBrowserResultKind.Ignored)
        };
    }

    private void Navigate(string nextDirectory)
    {
        // Prepare the complete new listing BEFORE committing the directory
        // transition. A permissions failure leaves the original directory
        // and its selection usable after the host reports the error.
        var normalized = Path.GetFullPath(nextDirectory);
        var entries = MicroStarDirectoryCatalog.Read(normalized).ToArray();
        var selection = MicroStarDirectoryCatalog.CreateList(entries, _visibleRows);
        CurrentDirectory = normalized;
        _entries = entries;
        _selection = selection;
    }

    private void Reload() => Navigate(CurrentDirectory);
}
