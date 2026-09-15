using Sasd.Editor.Document;
using Sasd.Editor.Hooks;
using Sasd.Editor.IO;
using Sasd.Editor.Model;
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
        Engine = new EditorEngine(this);
        Search = new EditorSearchService(this);
        Files = new EditorFileService(this, fileCodec);
    }

    public IEditorHooks Hooks { get; }
    public EditorUndoManager Undo { get; }
    public EditorEngine Engine { get; }
    public EditorSearchService Search { get; }
    public EditorFileService Files { get; }
    public IReadOnlyList<EditorWindow> Windows => _windows;
    public EditorWindow CurrentWindow { get; private set; } = null!;
    public EditorBlock? Block { get; private set; }

    public EditorWindow CreateDocument(string? text = null)
    {
        var lines = SplitLogicalLines(text ?? string.Empty).Select(static line => new EditorLineSnapshot(line));
        var document = new EditorDocument(new LinkedLineTextBuffer(lines));
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

    public void ClearBlock()
    {
        if (Block is not null)
        {
            var document = FindDocument(Block.DocumentId);
            if (document is not null)
            {
                ClearBlockFlags(document);
            }
        }

        Block = null;
    }

    public void SetMarker(int markerNumber)
    {
        EnsureWindow();
        ValidateMarker(markerNumber);
        _markers[markerNumber] = new EditorMarker(CurrentWindow.Document.DocumentId, CurrentWindow.Cursor);
    }

    public bool JumpToMarker(int markerNumber)
    {
        ValidateMarker(markerNumber);
        if (!_markers.TryGetValue(markerNumber, out var marker))
        {
            return false;
        }

        var window = _windows.FirstOrDefault(window => window.Document.DocumentId == marker.DocumentId);
        if (window is null)
        {
            return false;
        }

        CurrentWindow = window;
        window.Cursor = marker.Position;
        window.ClampCursor();
        return true;
    }

    internal void RefreshBlockFlags()
    {
        if (Block is null)
        {
            return;
        }

        var document = FindDocument(Block.DocumentId);
        if (document is null)
        {
            return;
        }

        ClearBlockFlags(document);
        if (Block.Hidden)
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
