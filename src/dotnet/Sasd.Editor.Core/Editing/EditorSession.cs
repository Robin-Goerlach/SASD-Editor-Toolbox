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
    private EditorBlockState _blockState = EditorBlockState.Empty;
    private EditorBlock? _block;

    public EditorSession(IEditorHooks? hooks = null, IEditorFileCodec? fileCodec = null)
    {
        Hooks = hooks ?? new DefaultEditorHooks();
        Undo = new EditorUndoManager();
        Typeahead = new EditorTypeaheadBuffer();
        WindowLayout = new EditorWindowLayout(this);
        Topology = new EditorLineTopology(this);
        Engine = new EditorEngine(this);
        Search = new EditorSearchService(this);
        Interruption = new EditorInterruptionService(this);
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
    public EditorInterruptionService Interruption { get; }
    public EditorFileService Files { get; }
    public EditorScheduler Scheduler { get; }
    public EditorSystemLoop SystemLoop { get; }
    public IReadOnlyList<EditorWindow> Windows => _windows;
    public EditorWindow CurrentWindow { get; private set; } = null!;

    /// <summary>
    /// Raw whole-line block endpoint state. Unlike <see cref="Block"/>, this
    /// property can represent a begin marker without an end marker, or an end
    /// marker without a begin marker, matching the historical Blockfrom/Blockto
    /// lifecycle without exposing pointer mechanics.
    /// </summary>
    public EditorBlockState BlockState => _blockState;

    /// <summary>
    /// Returns the currently usable contiguous block, or <see langword="null"/>
    /// while one of the two historical block endpoints is still undefined.
    /// </summary>
    public EditorBlock? Block => _block;

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

        if (_blockState.ReferencesDocument(documentId))
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

    /// <summary>
    /// Defines or repoints the historical block-begin endpoint at the current
    /// line. The end endpoint remains defined only when it still follows the new
    /// begin endpoint in the same document.
    /// </summary>
    public void BeginBlock()
    {
        EnsureWindow();
        ClearBlockHighlights();

        var start = new EditorBlockEndpoint(CurrentWindow.Document.DocumentId, CurrentWindow.Cursor.Line);
        var end = _blockState.End;

        // EditBlockBegin scans forward from the new begin marker to the old end
        // marker. If the old end cannot be reached, it becomes undefined rather
        // than manufacturing a reversed/cross-stream block.
        if (end is not null
            && (end.DocumentId != start.DocumentId || start.Line > end.Line))
        {
            end = null;
        }

        SetBlockState(new EditorBlockState(start, end, Hidden: end is null));
        MarkBlockHighlights();
    }

    /// <summary>
    /// Defines or repoints the historical block-end endpoint at the current
    /// line. A usable block exists only when the begin endpoint precedes it in
    /// the same document.
    /// </summary>
    public void EndBlock()
    {
        EnsureWindow();
        ClearBlockHighlights();

        var end = new EditorBlockEndpoint(CurrentWindow.Document.DocumentId, CurrentWindow.Cursor.Line);
        var start = _blockState.Start;

        // The handbook defines Blockfrom as the top limit and Blockto as the
        // bottom limit. This is the symmetric clean-room endpoint rule: an end
        // marker before the existing begin marker, or in another stream, leaves
        // only the newly defined end marker.
        if (start is not null
            && (start.DocumentId != end.DocumentId || start.Line > end.Line))
        {
            start = null;
        }

        SetBlockState(new EditorBlockState(start, end, Hidden: start is null));
        MarkBlockHighlights();
    }

    public void ToggleBlockHidden()
    {
        if (_blockState.IsEmpty)
        {
            return;
        }

        if (!_blockState.Hidden)
        {
            SetBlockState(_blockState with { Hidden = true });
        }
        else if (_blockState.IsComplete)
        {
            SetBlockState(_blockState with { Hidden = false });
        }

        // An incomplete endpoint pair can never be displayed. Refreshing also
        // removes stale InBlock flags left by a preceding complete definition.
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
        var block = Block;
        if (block is null || block.Hidden)
        {
            return;
        }

        var document = FindDocument(block.DocumentId);
        if (document is null)
        {
            return;
        }

        var first = Math.Clamp(block.FirstLine, 0, document.Buffer.LineCount - 1);
        var last = Math.Clamp(block.LastLine, 0, document.Buffer.LineCount - 1);
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
        SetBlockState(EditorBlockState.Empty);
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
    /// Allows structural compatibility code to replace a complete block while
    /// preserving the richer endpoint-state model used by Begin/End commands.
    /// </summary>
    internal void ReplaceBlockForTopology(EditorBlock? block) =>
        SetBlockState(block is null ? EditorBlockState.Empty : EditorBlockState.FromCompleteBlock(block));

    /// <summary>
    /// Allows the topology coordinator to realign independently defined block
    /// endpoints after logical line insertions or deletions.
    /// </summary>
    internal void ReplaceBlockStateForTopology(EditorBlockState state)
    {
        ArgumentNullException.ThrowIfNull(state);
        SetBlockState(state);
    }

    internal bool BlockReferencesDocument(Guid documentId) => _blockState.ReferencesDocument(documentId);

    /// <summary>
    /// Updates raw endpoint state and the cached complete-block projection in one
    /// place. Keeping the projection stable until the endpoint state changes
    /// preserves the public Block object's identity for callers that only toggle
    /// visual highlighting.
    /// </summary>
    private void SetBlockState(EditorBlockState state)
    {
        ArgumentNullException.ThrowIfNull(state);
        _blockState = state.EnsureVisibilityInvariant();
        _block = _blockState.CompleteBlock;
    }

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
