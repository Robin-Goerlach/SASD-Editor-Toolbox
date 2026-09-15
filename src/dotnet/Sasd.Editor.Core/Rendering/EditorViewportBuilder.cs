using Sasd.Editor.Editing;

namespace Sasd.Editor.Rendering;

/// <summary>
/// Builds a platform-neutral projection of the visible editor region. Hosts
/// decide how to render colors, cursor shapes and status chrome.
/// </summary>
public sealed class EditorViewportBuilder(EditorSession session)
{
    public EditorViewport Build(int height, int width)
    {
        if (height <= 0) throw new ArgumentOutOfRangeException(nameof(height));
        if (width <= 0) throw new ArgumentOutOfRangeException(nameof(width));

        var window = session.CurrentWindow;
        var document = window.Document;
        var firstLine = Math.Clamp(window.TopLine, 0, document.Buffer.LineCount - 1);
        var lastExclusive = Math.Min(document.Buffer.LineCount, firstLine + height);
        var lines = new List<ViewportLine>(lastExclusive - firstLine);

        for (var lineIndex = firstLine; lineIndex < lastExclusive; lineIndex++)
        {
            var snapshot = document.Buffer.GetLine(lineIndex);
            var start = Math.Min(window.LeftColumn, snapshot.Text.Length);
            var length = Math.Min(width, snapshot.Text.Length - start);
            lines.Add(new ViewportLine(lineIndex, snapshot.Text.Substring(start, length), snapshot.Flags));
        }

        var status = new EditorStatus(
            document.DisplayName,
            window.Cursor.Line + 1,
            window.Cursor.Column + 1,
            window.Options.InsertMode,
            window.Options.WordWrap,
            window.Options.AutoIndent,
            document.IsDirty);

        return new EditorViewport(session.Hooks.TransformStatus(session, status), lines);
    }
}
