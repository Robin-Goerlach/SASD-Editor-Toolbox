using Sasd.Editor.Model;

namespace Sasd.Editor.Rendering;

public sealed record EditorStatus(
    string FileName,
    int Line,
    int Column,
    bool InsertMode,
    bool WordWrap,
    bool AutoIndent,
    bool IsDirty);

public sealed record ViewportLine(int DocumentLine, string Text, EditorLineFlags Flags);

public sealed record EditorViewport(EditorStatus Status, IReadOnlyList<ViewportLine> Lines);
