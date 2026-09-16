using Sasd.Editor.Editing;
using Sasd.Editor.Model;
using Sasd.Editor.Windows;

namespace Sasd.Editor.Commands;

/// <summary>
/// Small FIRST-ED compatibility primitives whose historical behavior differs
/// intentionally from the reusable modern <see cref="EditorEngine"/> defaults.
/// </summary>
/// <remarks>
/// Keeping these rules in a narrow adapter prevents DOS-era cursor conventions
/// from leaking into the general editing engine. Future C++, Java and JavaScript
/// implementations can reproduce the observable behavior without copying Pascal
/// data structures or pointer mechanics.
/// </remarks>
internal static class FirstEdPrimitiveCompatibilityProcessor
{
    /// <summary>
    /// Clean-room transfer of the historical EditLeftChar behavior. Moving left
    /// from column zero enters the previous logical line immediately after its
    /// last non-blank character rather than after trailing whitespace.
    /// </summary>
    public static bool MoveLeftChar(EditorSession session)
    {
        var window = RequireWindow(session);
        if (window.Cursor.Column > 0)
        {
            window.Cursor = window.Cursor with { Column = window.Cursor.Column - 1 };
            return true;
        }

        if (window.Cursor.Line <= 0)
        {
            return false;
        }

        var previousLine = window.Cursor.Line - 1;
        var previousText = window.Document.Buffer.GetLine(previousLine).Text;
        window.Cursor = new TextPosition(previousLine, LastNonBlankEnd(previousText));
        return true;
    }

    /// <summary>
    /// Clean-room transfer of EditRightChar. The historical command advances the
    /// column only; it does not cross a logical line boundary. This also means a
    /// cursor may occupy a virtual column beyond the current line length.
    /// </summary>
    public static bool MoveRightChar(EditorSession session)
    {
        var window = RequireWindow(session);
        if (window.Cursor.Column == int.MaxValue)
        {
            return false;
        }

        window.Cursor = window.Cursor with { Column = window.Cursor.Column + 1 };
        return true;
    }

    /// <summary>
    /// Clean-room transfer of EditTab. In insert mode the padding to the next tab
    /// stop is inserted into the document. In overtype mode Tab is cursor movement
    /// only and therefore must not dirty the document or create an undo entry.
    /// </summary>
    public static bool Tab(EditorSession session)
    {
        var window = RequireWindow(session);
        var tabSize = window.Options.TabSize;
        var column = Math.Max(0, window.Cursor.Column);
        var spaces = tabSize - (column % tabSize);

        if (column > int.MaxValue - spaces)
        {
            return false;
        }

        if (window.Options.InsertMode)
        {
            session.Engine.InsertText(new string(' ', spaces));
        }
        else
        {
            window.Cursor = window.Cursor with { Column = column + spaces };
        }

        return true;
    }

    private static int LastNonBlankEnd(string text)
    {
        var index = text.Length - 1;
        while (index >= 0 && char.IsWhiteSpace(text[index]))
        {
            index--;
        }

        return index + 1;
    }

    private static EditorWindow RequireWindow(EditorSession session)
    {
        ArgumentNullException.ThrowIfNull(session);
        return session.Windows.Count > 0
            ? session.CurrentWindow
            : throw new InvalidOperationException("The editor session has no windows.");
    }
}
