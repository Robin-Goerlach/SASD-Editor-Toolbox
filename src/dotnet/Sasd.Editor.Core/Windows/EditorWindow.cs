using Sasd.Editor.Document;
using Sasd.Editor.Model;

namespace Sasd.Editor.Windows;

/// <summary>
/// A view onto a document. Multiple windows may reference the same document,
/// which is the modern equivalent of linked windows sharing a text stream.
/// </summary>
public sealed class EditorWindow
{
    public EditorWindow(EditorDocument document, EditorWindowOptions? options = null)
    {
        Document = document ?? throw new ArgumentNullException(nameof(document));
        Options = options ?? new EditorWindowOptions();
    }

    public Guid WindowId { get; } = Guid.NewGuid();

    public EditorDocument Document { get; }

    public EditorWindowOptions Options { get; }

    public TextPosition Cursor { get; set; }

    public int TopLine { get; set; }

    public int LeftColumn { get; set; }

    public void ClampCursor()
    {
        var line = Math.Clamp(Cursor.Line, 0, Document.Buffer.LineCount - 1);
        var text = Document.Buffer.GetLine(line).Text;
        var column = Math.Clamp(Cursor.Column, 0, text.Length);
        Cursor = new TextPosition(line, column);
        TopLine = Math.Clamp(TopLine, 0, Math.Max(0, Document.Buffer.LineCount - 1));
        LeftColumn = Math.Max(0, LeftColumn);
    }
}
