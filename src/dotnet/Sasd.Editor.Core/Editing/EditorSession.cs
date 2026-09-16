using Sasd.Editor.Document;
using Sasd.Editor.Hooks;
using Sasd.Editor.IO;
using Sasd.Editor.Input;
using Sasd.Editor.Model;
using Sasd.Editor.Scheduling;
using Sasd.Editor.Search;
using Sasd.Editor.Undo;
using Sasd.Editor.Windows;

namespace Sasd.Editor.Editing;

/// <summary>
/// Coordinates documents, views/windows, block state, markers and editor-wide
/// services. UI hosts normally keep one session per editor workspace.
/// </summary>
public sealed class EditorSession
{
    private readonly List<EditorWindow> _windows = [];
    private readonly Dictionary<int, EditorMarker> _markers = [];

    public EditorSession(IEditorHooks? hooks = null, IEditorFileCodec? fileCodec = null)
    {
        Hooks = hooks ?? new DefaultEditorHooks();
        Undo = new EditorUndoManager();
        Typeahead = new EditorTypeaheadBuffer();
        WindowLayout = new EditorWindowLayout(this);
        Topology = new EditorLineTopology(this);
        Engine = new EditorEngine(this);
        Search = new EditorSearchService(this);
        Files = new EditorFileService(this, fileCodec);
        Scheduler = new EditorScheduler(this);
        SystemLoop = new EditorSystemLoop(this, Scheduler);
    }

    public IEditorHooks Hooks { get; }
    public EditorUndoManager Undo { get; }
    public EditorTypeaheadBuffer Typeahead { get; }
    public EditorWindowLayout WindowLayout { get; }
    public EditorEngine Engine { get; }
    public EditorSearchService Search { get; }
    public EditorFileService Files { get; }
    public EditorScheduler Scheduler { get; }
    public EditorSystemLoop SystemLoop { get; }
    public IReadOnlyList<EditorWindow> Windows => _windows;
    public EditorWindow CurrentWindow { get; private set; } = null!;
    public EditorBlock? Block { get; private set; }

    /// <summary>
    /// Internal line-topology coordinator. Structural editing code uses this
    /// service instead of independently repairing window, marker and block line
    /// numbers after insertions or deletions.
    /// </summary>
    internal EditorLineTopology Topology { get; }

    /// <summary>
    /// Internal marker table exposed only to the topology coordinator. Hosts use
    /// SetMarker/JumpToMarker and never mutate this dictionary directly.
    /// </summary>
    internal IDictionary<int, EditorMarker> MarkerTable => _markers;

    /// <summary>
    /// Modern equivalent of the historical global Rundown flag. The editor loop
    /// exits after the flag becomes true. Requesting rundown never saves files.
    /// </summary>
    public bool RundownRequested { get; private set; }

    public void RequestRundown() => RundownRequested = true;

    /// <summary>
    /// Allows a host or test harness to reuse a session after a completed run.
    /// Normal interactive hosts usually never need to call this method.
    /// </summary>
    public void ResetRundown() => RundownRequested = false;

    public EditorWindow CreateDocument(string? text = null)
    {
        var document = CreateDocumentModel(text);
        return CreateWindow(document);
    }

    public EditorWindow CreateWindow(EditorDocument document, EditorWindowOptions? options = null)
    {
        ArgumentNullException.ThrowIfNull(document);
        var window = new EditorWindow(document, options);
        _windows.Add(window);
        CurrentWindow = window;
        return window;
    }

    /// <summary>
    /// Creates a new blank document/view immediately below an existing displayed
    /// window. The compatibility layer uses this to reproduce EditWindowCreate's
    /// linked-list insertion order without exposing list mutation to hosts.
    /// </summary>
    internal EditorWindow CreateDocumentAfter(EditorWindow anchor)
    {
        ArgumentNullException.ThrowIfNull(anchor);
        var anchorIndex = _windows.FindIndex(window => window.WindowId == anchor.WindowId);
        if (anchorIndex < 0)
        {
            throw new ArgumentException("The anchor window is not part of this session.", nameof(anchor));
        }

        var window = new EditorWindow(CreateDocumentModel(string.Empty));
        _windows.Insert(anchorIndex + 1, window);
        CurrentWindow = window;
        return window;
    }

    public EditorWindow LinkWindow(EditorWindow source)
    {
        ArgumentNullException.ThrowIfNull(source);
        var linked = CreateWindow(source.Document, source.Options.Clone());
        linked.Cursor = source.Cursor;
        linked.TopLine = source.TopLine;
        linked.LeftColumn = source.LeftColumn;
        return linked;
    }

    public bool CloseWindow(Guid windowId)
    {
        var index = _windows.FindIndex(window => window.WindowId == windowId);
        if (index < 0)
        {
            return false;
        }

        var target = _windows[index];
        WindowLayout.OnWindowRemoving(target, index);

        var wasCurrent = CurrentWindow?.WindowId == windowId;
        _windows.RemoveAt(index);
        if (_windows.Count == 0)
        {
            CurrentWindow = null!;
        }
        else if (wasCurrent)
        {
            CurrentWindow = _windows[Math.Min(index, _windows.Count - 1)];
        }

        return true;
    }

    public void SetCurrentWindow(Guid windowId)
    {
        CurrentWindow = _windows.FirstOrDefault(window => window.WindowId == windowId)
            ?? throw new ArgumentException("Unknown window id.", nameof(windowId));
    }

    public EditorWindow? NextWindow()
    {
        if (_windows.Count == 0)
        {
            return null;
        }

        var currentIndex = _windows.FindIndex(window => window.WindowId == CurrentWindow.WindowId);
        CurrentWindow = _windows[(currentIndex + 1) % _windows.Count];
        return CurrentWindow;
    }

    public EditorDocument? FindDocument(Guid documentId) =>
        _windows.Select(static window => window.Document)
            .FirstOrDefault(document => document.DocumentId == documentId);

    /// <summary>
    /// Destructively clears the text stream displayed by the current window,
    /// reproducing the observable behavior of EditWindowDeleteText.
    /// </summary>
    /// <remarks>
    /// This operation intentionally does not create an undo entry. Every window
    /// currently linked to the same document is detached and receives its own new
    /// blank NONAME document, which destroys the historical link relationship as
    /// well as the text. Block and marker state referring to the destroyed stream
    /// is discarded so no dangling document references remain.
    /// </remarks>
    public bool DeleteCurrentWindowText()
    {
        EnsureWindow();
        var documentId = CurrentWindow.Document.DocumentId;

        if (Block?.DocumentId == documentId)
        {
            ClearBlock();
        }

        Undo.DiscardDocument(documentId);
        RemoveMarkersForDocument(documentId);

        var affectedWindows = _windows
            .Where(window => window.Document.DocumentId == documentId)
            .ToArray();

        foreach (var window in affectedWindows)
        {
            window.AttachDocument(CreateDocumentModel(string.Empty));
            window.Cursor = default;
            window.TopLine = 0;
            window.LeftColumn = 0;
        }

        return true;
    }

    public void BeginBlock()
    {
        EnsureWindow();
        Block = new EditorBlock(CurrentWindow.Document.DocumentId, CurrentWindow.Cursor.Line, CurrentWindow.Cursor.Line);
        RefreshBlockFlags();
    }

    public void EndBlock()
    {
        EnsureWindow();
        if (Block is null || Block.DocumentId != CurrentWindow.Document.DocumentId)
        {
            BeginBlock();
            return;
        }

        Block = Block with { EndLine = CurrentWindow.Cursor.Line };
        RefreshBlockFlags();
    }

    public void ToggleBlockHidden()
    {
        if (Block is null)
        {
            return;
        }

        Block = Block with { Hidden = !Block.Hidden };
        RefreshBlockFlags();
    }

    /// <summary>
    /// Modern clean-room counterpart of EditOffblock. It clears the visual
    /// <see cref="EditorLineFlags.InBlock"/> bit from every line in every open
    /// text stream but deliberately leaves the logical block limits unchanged.
    /// </summary>
    public void ClearBlockHighlights()
    {
        foreach (var document in _windows
                     .Select(static window => window.Document)
                     .GroupBy(static document => document.DocumentId)
                     .Select(static group => group.First()))
        {
            ClearBlockFlags(document);
        }
    }

    /// <summary>
    /// Modern clean-room counterpart of EditMarkblock. The currently defined
    /// whole-line block is projected back into line flags without changing its
    /// limits. Hidden or undefined blocks deliberately produce no markings.
    /// </summary>
    public void MarkBlockHighlights()
    {
        if (Block is null || Block.Hidden)
        {
            return;
        }

        var document = FindDocument(Block.DocumentId);
        if (document is null)
        {
            return;
        }

        var first = Math.Clamp(Block.FirstLine, 0, document.Buffer.LineCount - 1);
        var last = Math.Clamp(Block.LastLine, 0, document.Buffer.LineCount - 1);
        for (var line = first; line <= last; line++)
        {
            var snapshot = document.Buffer.GetLine(line);
            document.Buffer.SetFlags(line, snapshot.Flags | EditorLineFlags.InBlock);
        }
    }

    public void ClearBlock()
    {
        // Clearing stale block bits globally is intentionally stronger than only
        // touching the active document and mirrors EditOffblock's recovery role.
        ClearBlockHighlights();
        Block = null;
    }

    public void SetMarker(int markerNumber)
    {
        EnsureWindow();
        ValidateMarker(markerNumber);

        // Turbo Editor Toolbox markers identify a line, not a saved cursor
        // column. Keeping that distinction is important when jumping later.
        _markers[markerNumber] = new EditorMarker(
            CurrentWindow.Document.DocumentId,
            CurrentWindow.Cursor.Line);
    }

    public bool JumpToMarker(int markerNumber)
    {
        ValidateMarker(markerNumber);
        if (!_markers.TryGetValue(markerNumber, out var marker))
        {
            return false;
        }

        // Prefer the current view when it already displays the marker's stream;
        // otherwise choose an open view of that document. In either case the
        // view's column remains untouched, matching the historical command.
        var window = CurrentWindow is not null && CurrentWindow.Document.DocumentId == marker.DocumentId
            ? CurrentWindow
            : _windows.FirstOrDefault(candidate => candidate.Document.DocumentId == marker.DocumentId);

        if (window is null)
        {
            return false;
        }

        CurrentWindow = window;
        var targetLine = Math.Clamp(marker.Line, 0, window.Document.Buffer.LineCount - 1);
        window.Cursor = window.Cursor with { Line = targetLine };
        return true;
    }

    internal void RefreshBlockFlags()
    {
        ClearBlockHighlights();
        MarkBlockHighlights();
    }

    /// <summary>
    /// Allows the topology service to replace a complete block after line-number
    /// realignment without making the public block setter mutable.
    /// </summary>
    internal void ReplaceBlockForTopology(EditorBlock? block) => Block = block;

    private static EditorDocument CreateDocumentModel(string? text)
    {
        var lines = SplitLogicalLines(text ?? string.Empty).Select(static line => new EditorLineSnapshot(line));
        return new EditorDocument(new LinkedLineTextBuffer(lines));
    }

    private static IEnumerable<string> SplitLogicalLines(string text)
    {
        var normalized = text.Replace("\r\n", "\n", StringComparison.Ordinal).Replace('\r', '\n');
        return normalized.Split('\n');
    }

    private static void ClearBlockFlags(EditorDocument document)
    {
        for (var line = 0; line < document.Buffer.LineCount; line++)
        {
            var snapshot = document.Buffer.GetLine(line);
            document.Buffer.SetFlags(line, snapshot.Flags & ~EditorLineFlags.InBlock);
        }
    }

    private void RemoveMarkersForDocument(Guid documentId)
    {
        var markerNumbers = _markers
            .Where(pair => pair.Value.DocumentId == documentId)
            .Select(static pair => pair.Key)
            .ToArray();

        foreach (var markerNumber in markerNumbers)
        {
            _markers.Remove(markerNumber);
        }
    }

    private static void ValidateMarker(int markerNumber)
    {
        if (markerNumber is < 1 or > 20)
        {
            throw new ArgumentOutOfRangeException(nameof(markerNumber), "Markers are numbered 1 through 20.");
        }
    }

    private void EnsureWindow()
    {
        if (_windows.Count == 0)
        {
            throw new InvalidOperationException("The editor session has no windows.");
        }
    }

    private sealed class DefaultEditorHooks : IEditorHooks
    {
    }
}
