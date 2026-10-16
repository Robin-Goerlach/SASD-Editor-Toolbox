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

        // Subtract before taking the minimum. Using firstLine + height here
        // would overflow with an int.MaxValue-sized host viewport even when
        // the document itself only contains a handful of logical lines.
        var visibleCount = Math.Min(height, document.Buffer.LineCount - firstLine);
        var lines = new List<ViewportLine>(visibleCount);
        var horizontalOffset = Math.Max(0, window.LeftColumn);

        for (var row = 0; row < visibleCount; row++)
        {
            var lineIndex = firstLine + row;
            var snapshot = document.Buffer.GetLine(lineIndex);

            // Hosts are expected to normalize viewport coordinates. A stale
            // negative horizontal offset should nevertheless be harmless when
            // constructing a read-only projection: do not mutate view state
            // or pass a negative index to Substring.
            var start = Math.Min(horizontalOffset, snapshot.Text.Length);
            var length = Math.Min(width, snapshot.Text.Length - start);
            lines.Add(new ViewportLine(lineIndex, snapshot.Text.Substring(start, length), snapshot.Flags));
        }

        // EditGenlineno historically traversed linked descriptors to count
        // absolute lines for every displayed window. Each modern view already
        // stores an absolute zero-based line index, so the equivalent status
        // value is available in O(1) without polling or mutating the document.
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
