using Sasd.Editor.Editing;
using Sasd.Editor.Windows;

namespace Sasd.Editor.Rendering;

/// <summary>
/// Builds platform-neutral projections of visible editor regions. Hosts decide
/// how to render colors, cursor shapes, status chrome and physical placement.
/// </summary>
public sealed class EditorViewportBuilder(EditorSession session)
{
    public EditorViewport Build(int height, int width) => Build(session.CurrentWindow, height, width);

    /// <summary>
    /// Builds a viewport for a specific displayed window. This overload allows a
    /// stacked-window host to render all windows while preserving one shared core
    /// rendering model.
    /// </summary>
    public EditorViewport Build(EditorWindow window, int height, int width)
    {
        ArgumentNullException.ThrowIfNull(window);
        if (height <= 0) throw new ArgumentOutOfRangeException(nameof(height));
        if (width <= 0) throw new ArgumentOutOfRangeException(nameof(width));

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
