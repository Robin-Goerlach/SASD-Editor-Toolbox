using System.Text;
using Sasd.Editor.Model;
using Sasd.Editor.Windows;

namespace Sasd.Editor.Editing;

/// <summary>
/// Core text-editing primitives. Commands, menus and keyboard mappings call
/// this layer rather than editing buffers directly.
/// </summary>
public sealed class EditorEngine
{
    private readonly EditorSession _session;

    internal EditorEngine(EditorSession session)
    {
        _session = session;
    }

    private EditorWindow Window => _session.CurrentWindow ?? throw new InvalidOperationException("No current editor window.");

    public void InsertText(string text)
    {
        ArgumentNullException.ThrowIfNull(text);
        if (text.Length == 0)
        {
            return;
        }

        Mutate("Insert text", () =>
        {
            foreach (var ch in text)
            {
                if (ch == '\r') continue;
                if (ch == '\n') InsertNewLineCore();
                else InsertCharacterCore(ch);
            }
        });
    }

    public void InsertControlCharacter(char value) => InsertText(value.ToString());
    public void InsertNewLine() => Mutate("Insert line", InsertNewLineCore);

    public void Tab()
    {
        var spaces = Window.Options.TabSize - (Window.Cursor.Column % Window.Options.TabSize);
        InsertText(new string(' ', spaces));
    }

    public void DeleteRightCharacter() => Mutate("Delete right character", () =>
    {
        var window = Window;
        var line = window.Document.Buffer.GetLine(window.Cursor.Line).Text;
        if (window.Cursor.Column < line.Length)
        {
            window.Document.Buffer.ReplaceLine(window.Cursor.Line, line.Remove(window.Cursor.Column, 1));
            return;
        }

        if (window.Cursor.Line + 1 < window.Document.Buffer.LineCount)
        {
            var nextLineIndex = window.Cursor.Line + 1;
            var next = window.Document.Buffer.GetLine(nextLineIndex).Text;
            window.Document.Buffer.ReplaceLine(window.Cursor.Line, line + next);
            window.Document.Buffer.RemoveLine(nextLineIndex);
            _session.Topology.LinesDeleted(window.Document, nextLineIndex, 1);
        }
    });

    public void DeleteLeftCharacter()
    {
        if (Window.Cursor.Column == 0 && Window.Cursor.Line == 0) return;
        Mutate("Delete left character", () =>
        {
            var window = Window;
            if (window.Cursor.Column > 0)
            {
                var line = window.Document.Buffer.GetLine(window.Cursor.Line).Text;
                window.Document.Buffer.ReplaceLine(window.Cursor.Line, line.Remove(window.Cursor.Column - 1, 1));
                window.Cursor = window.Cursor with { Column = window.Cursor.Column - 1 };
                return;
            }

            var removedLineIndex = window.Cursor.Line;
            var previousLineIndex = removedLineIndex - 1;
            var previous = window.Document.Buffer.GetLine(previousLineIndex).Text;
            var current = window.Document.Buffer.GetLine(removedLineIndex).Text;
            var joinColumn = previous.Length;
            window.Document.Buffer.ReplaceLine(previousLineIndex, previous + current);
            window.Document.Buffer.RemoveLine(removedLineIndex);
            _session.Topology.LinesDeleted(window.Document, removedLineIndex, 1);
            window.Cursor = new TextPosition(previousLineIndex, joinColumn);
        });
    }

    public void DeleteLine() => Mutate("Delete line", () =>
    {
        var window = Window;
        var lineIndex = window.Cursor.Line;
        var oldLineCount = window.Document.Buffer.LineCount;
        window.Document.Buffer.RemoveLine(lineIndex);
        if (oldLineCount > 1)
        {
            _session.Topology.LinesDeleted(window.Document, lineIndex, 1);
        }
        window.Cursor = new TextPosition(Math.Min(lineIndex, window.Document.Buffer.LineCount - 1), 0);
    });

    public void DeleteRightWord() => Mutate("Delete right word", () =>
    {
        var window = Window;
        var line = window.Document.Buffer.GetLine(window.Cursor.Line).Text;
        if (window.Cursor.Column >= line.Length)
        {
            DeleteRightCharacterCore();
            return;
        }

        var end = window.Cursor.Column;
        while (end < line.Length && char.IsWhiteSpace(line[end])) end++;
        while (end < line.Length && !char.IsWhiteSpace(line[end])) end++;
        window.Document.Buffer.ReplaceLine(window.Cursor.Line, line.Remove(window.Cursor.Column, end - window.Cursor.Column));
    });

    public void DeleteToEndOfLine() => Mutate("Delete to end of line", () =>
    {
        var window = Window;
        var line = window.Document.Buffer.GetLine(window.Cursor.Line).Text;
        if (window.Cursor.Column < line.Length)
            window.Document.Buffer.ReplaceLine(window.Cursor.Line, line[..window.Cursor.Column]);
    });

    public void ChangeCase() => Mutate("Change case", () =>
    {
        var window = Window;
        var line = window.Document.Buffer.GetLine(window.Cursor.Line).Text;
        if (window.Cursor.Column >= line.Length) return;
        var chars = line.ToCharArray();
        var ch = chars[window.Cursor.Column];
        chars[window.Cursor.Column] = char.IsUpper(ch) ? char.ToLowerInvariant(ch) : char.ToUpperInvariant(ch);
        window.Document.Buffer.ReplaceLine(window.Cursor.Line, new string(chars));
    });

    public void CenterLine() => Mutate("Center line", () =>
    {
        var window = Window;
        var content = window.Document.Buffer.GetLine(window.Cursor.Line).Text.Trim();
        var width = Math.Max(0, window.Options.RightMargin - window.Options.LeftMargin + 1);
        var leftPadding = Math.Max(window.Options.LeftMargin, window.Options.LeftMargin + (width - content.Length) / 2);
        window.Document.Buffer.ReplaceLine(window.Cursor.Line, new string(' ', leftPadding) + content);
        window.Cursor = window.Cursor with { Column = Math.Min(window.Cursor.Column, leftPadding + content.Length) };
    });

    public void ReformatParagraph() => Mutate("Reformat paragraph", () =>
    {
        var window = Window;
        var buffer = window.Document.Buffer;
        var start = window.Cursor.Line;
        while (start > 0 && !string.IsNullOrWhiteSpace(buffer.GetLine(start - 1).Text)) start--;
        var end = window.Cursor.Line;
        while (end + 1 < buffer.LineCount && !string.IsNullOrWhiteSpace(buffer.GetLine(end + 1).Text)) end++;
        var words = Enumerable.Range(start, end - start + 1)
            .SelectMany(index => buffer.GetLine(index).Text.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries))
            .ToArray();
        if (words.Length == 0) return;
        var formatted = WrapWords(words, window.Options.LeftMargin, window.Options.RightMargin)
            .Select((text, index) => new EditorLineSnapshot(text, index == 0 ? EditorLineFlags.None : EditorLineFlags.Wrapped))
            .ToArray();
        var rebuilt = buffer.Snapshot().ToList();
        rebuilt.RemoveRange(start, end - start + 1);
        rebuilt.InsertRange(start, formatted);
        buffer.ReplaceAll(rebuilt);
        window.Cursor = new TextPosition(start, window.Options.LeftMargin);
    });

    public void ToggleInsertMode() => Window.Options.InsertMode = !Window.Options.InsertMode;
    public void ToggleWordWrap() => Window.Options.WordWrap = !Window.Options.WordWrap;
    public void ToggleAutoIndent() => Window.Options.AutoIndent = !Window.Options.AutoIndent;

    public void MoveLeft()
    {
        var window = Window;
        if (window.Cursor.Column > 0) window.Cursor = window.Cursor with { Column = window.Cursor.Column - 1 };
        else if (window.Cursor.Line > 0)
        {
            var previousLine = window.Cursor.Line - 1;
            window.Cursor = new TextPosition(previousLine, window.Document.Buffer.GetLine(previousLine).Text.Length);
        }
    }

    public void MoveRight()
    {
        var window = Window;
        var line = window.Document.Buffer.GetLine(window.Cursor.Line).Text;
        if (window.Cursor.Column < line.Length) window.Cursor = window.Cursor with { Column = window.Cursor.Column + 1 };
        else if (window.Cursor.Line + 1 < window.Document.Buffer.LineCount) window.Cursor = new TextPosition(window.Cursor.Line + 1, 0);
    }

    public void MoveUp() => MoveVertical(-1);
    public void MoveDown() => MoveVertical(1);
    public void MovePageUp(int pageSize = 20) => MoveVertical(-Math.Max(1, pageSize));
    public void MovePageDown(int pageSize = 20) => MoveVertical(Math.Max(1, pageSize));
    public void MoveBeginningOfLine() => Window.Cursor = Window.Cursor with { Column = 0 };
    public void MoveEndOfLine() { var w = Window; w.Cursor = w.Cursor with { Column = w.Document.Buffer.GetLine(w.Cursor.Line).Text.Length }; }
    public void MoveTopOfFile() => Window.Cursor = new TextPosition(0, 0);
    public void MoveBottomOfFile() { var w = Window; var line = w.Document.Buffer.LineCount - 1; w.Cursor = new TextPosition(line, w.Document.Buffer.GetLine(line).Text.Length); }

    public void MoveLeftWord()
    {
        var window = Window;
        var line = window.Document.Buffer.GetLine(window.Cursor.Line).Text;
        var column = Math.Min(window.Cursor.Column, line.Length);
        if (column == 0) { MoveLeft(); return; }
        column--;
        while (column > 0 && char.IsWhiteSpace(line[column])) column--;
        while (column > 0 && !char.IsWhiteSpace(line[column - 1])) column--;
        window.Cursor = window.Cursor with { Column = column };
    }

    public void MoveRightWord()
    {
        var window = Window;
        var line = window.Document.Buffer.GetLine(window.Cursor.Line).Text;
        var column = Math.Min(window.Cursor.Column, line.Length);
        while (column < line.Length && !char.IsWhiteSpace(line[column])) column++;
        while (column < line.Length && char.IsWhiteSpace(line[column])) column++;
        if (column == line.Length && window.Cursor.Line + 1 < window.Document.Buffer.LineCount)
            window.Cursor = new TextPosition(window.Cursor.Line + 1, 0);
        else window.Cursor = window.Cursor with { Column = column };
    }

    public void CopyBlockToCursor()
    {
        var block = RequireCurrentBlock();
        var sourceDocument = _session.FindDocument(block.DocumentId)!;
        var lines = Enumerable.Range(block.FirstLine, block.LineCount)
            .Select(index => sourceDocument.Buffer.GetLine(index) with { Flags = EditorLineFlags.None }).ToArray();
        Mutate("Copy block", () =>
        {
            var insertionLine = Window.Cursor.Line;
            for (var index = 0; index < lines.Length; index++)
                Window.Document.Buffer.InsertLine(insertionLine + index, lines[index].Text, lines[index].Flags);
            Window.Cursor = new TextPosition(insertionLine, 0);
        });
    }

    public void DeleteBlock()
    {
        var block = RequireCurrentBlock();
        var document = _session.FindDocument(block.DocumentId)!;
        var targetWindow = _session.Windows.First(window => window.Document.DocumentId == document.DocumentId);
        _session.SetCurrentWindow(targetWindow.WindowId);
        Mutate("Delete block", () =>
        {
            for (var index = block.LastLine; index >= block.FirstLine; index--) document.Buffer.RemoveLine(index);
            targetWindow.Cursor = new TextPosition(Math.Min(block.FirstLine, document.Buffer.LineCount - 1), 0);
            _session.ClearBlock();
        });
    }

    public void MoveBlockToCursor()
    {
        var block = RequireCurrentBlock();
        var sourceDocument = _session.FindDocument(block.DocumentId)!;
        var targetWindow = Window;
        var targetDocument = targetWindow.Document;
        var targetLine = targetWindow.Cursor.Line;
        var lines = Enumerable.Range(block.FirstLine, block.LineCount)
            .Select(index => sourceDocument.Buffer.GetLine(index) with { Flags = EditorLineFlags.None }).ToArray();
        _session.Undo.Capture(targetWindow, "Move block");
        if (sourceDocument.DocumentId != targetDocument.DocumentId)
        {
            var sourceWindow = _session.Windows.First(window => window.Document.DocumentId == sourceDocument.DocumentId);
            _session.Undo.Capture(sourceWindow, "Move block source");
        }
        for (var index = block.LastLine; index >= block.FirstLine; index--) sourceDocument.Buffer.RemoveLine(index);
        if (sourceDocument.DocumentId == targetDocument.DocumentId && targetLine > block.LastLine) targetLine -= block.LineCount;
        targetLine = Math.Clamp(targetLine, 0, targetDocument.Buffer.LineCount);
        for (var index = 0; index < lines.Length; index++) targetDocument.Buffer.InsertLine(targetLine + index, lines[index].Text, lines[index].Flags);
        sourceDocument.MarkChanged();
        if (sourceDocument.DocumentId != targetDocument.DocumentId) targetDocument.MarkChanged();
        targetWindow.Cursor = new TextPosition(targetLine, 0);
        _session.ClearBlock();
        ClampLinkedWindows(sourceDocument);
        ClampLinkedWindows(targetDocument);
    }

    public bool Undo() => _session.Undo.Undo(_session);

    internal void ReplaceSingleLineRange(TextPosition position, int length, string replacement)
    {
        ArgumentNullException.ThrowIfNull(replacement);
        var window = Window;
        if (position.Line < 0 || position.Line >= window.Document.Buffer.LineCount) throw new ArgumentOutOfRangeException(nameof(position));
        Mutate("Replace text", () =>
        {
            var line = window.Document.Buffer.GetLine(position.Line).Text;
            if (position.Column < 0 || position.Column + length > line.Length) throw new ArgumentOutOfRangeException(nameof(position));
            window.Document.Buffer.ReplaceLine(position.Line, line.Remove(position.Column, length).Insert(position.Column, replacement));
            window.Cursor = new TextPosition(position.Line, position.Column + replacement.Length);
        });
    }

    private void InsertCharacterCore(char ch)
    {
        var window = Window;
        var line = window.Document.Buffer.GetLine(window.Cursor.Line);
        var text = line.Text;
        var column = Math.Max(0, window.Cursor.Column);
        if (column > text.Length) text = text.PadRight(column);
        text = window.Options.InsertMode || column >= text.Length ? text.Insert(column, ch.ToString()) : text.Remove(column, 1).Insert(column, ch.ToString());
        window.Document.Buffer.ReplaceLine(window.Cursor.Line, text, line.Flags & ~EditorLineFlags.Wrapped);
        window.Cursor = window.Cursor with { Column = column + 1 };
        if (window.Options.WordWrap && window.Cursor.Column > window.Options.RightMargin + 1) WrapCurrentLine();
    }

    private void InsertNewLineCore()
    {
        var window = Window;
        var lineIndex = window.Cursor.Line;
        var line = window.Document.Buffer.GetLine(lineIndex);
        var column = Math.Min(window.Cursor.Column, line.Text.Length);
        var left = line.Text[..column];
        var right = line.Text[column..];
        var indentation = window.Options.AutoIndent ? new string(' ', left.TakeWhile(char.IsWhiteSpace).Count()) : new string(' ', window.Options.LeftMargin);
        window.Document.Buffer.ReplaceLine(lineIndex, left, line.Flags & ~EditorLineFlags.Wrapped);
        window.Document.Buffer.InsertLine(lineIndex + 1, indentation + right);
        _session.Topology.LinesInserted(window.Document, lineIndex + 1, 1);
        window.Cursor = new TextPosition(lineIndex + 1, indentation.Length);
    }

    private void WrapCurrentLine()
    {
        var window = Window;
        var lineIndex = window.Cursor.Line;
        var snapshot = window.Document.Buffer.GetLine(lineIndex);
        var text = snapshot.Text;
        var margin = Math.Min(window.Options.RightMargin + 1, text.Length);
        var split = margin;
        for (var index = margin - 1; index >= window.Options.LeftMargin; index--)
        {
            if (char.IsWhiteSpace(text[index])) { split = index; break; }
        }
        var left = text[..split].TrimEnd();
        var right = text[split..].TrimStart();
        var prefix = new string(' ', window.Options.LeftMargin);
        window.Document.Buffer.ReplaceLine(lineIndex, left, snapshot.Flags);
        window.Document.Buffer.InsertLine(lineIndex + 1, prefix + right, EditorLineFlags.Wrapped);
        _session.Topology.LinesInserted(window.Document, lineIndex + 1, 1);
        window.Cursor = new TextPosition(lineIndex + 1, prefix.Length + right.Length);
    }

    private void DeleteRightCharacterCore()
    {
        var window = Window;
        var lineIndex = window.Cursor.Line;
        var line = window.Document.Buffer.GetLine(lineIndex).Text;
        if (lineIndex + 1 >= window.Document.Buffer.LineCount) return;
        var nextLineIndex = lineIndex + 1;
        var next = window.Document.Buffer.GetLine(nextLineIndex).Text;
        window.Document.Buffer.ReplaceLine(lineIndex, line + next);
        window.Document.Buffer.RemoveLine(nextLineIndex);
        _session.Topology.LinesDeleted(window.Document, nextLineIndex, 1);
    }

    private void MoveVertical(int delta)
    {
        var window = Window;
        var targetLine = Math.Clamp(window.Cursor.Line + delta, 0, window.Document.Buffer.LineCount - 1);
        var targetColumn = Math.Min(window.Cursor.Column, window.Document.Buffer.GetLine(targetLine).Text.Length);
        window.Cursor = new TextPosition(targetLine, targetColumn);
    }

    private EditorBlock RequireCurrentBlock()
    {
        var block = _session.Block ?? throw new InvalidOperationException("No block is defined.");
        if (_session.FindDocument(block.DocumentId) is null) throw new InvalidOperationException("The block document is no longer open.");
        return block;
    }

    private void Mutate(string description, Action mutation)
    {
        var window = Window;
        _session.Undo.Capture(window, description);
        mutation();
        window.Document.MarkChanged();
        window.ClampCursor();
        ClampLinkedWindows(window.Document);
        _session.RefreshBlockFlags();
    }

    private void ClampLinkedWindows(Sasd.Editor.Document.EditorDocument document)
    {
        foreach (var linkedWindow in _session.Windows.Where(window => window.Document.DocumentId == document.DocumentId)) linkedWindow.ClampCursor();
    }

    private static IEnumerable<string> WrapWords(IEnumerable<string> words, int leftMargin, int rightMargin)
    {
        var prefix = new string(' ', leftMargin);
        var width = Math.Max(1, rightMargin - leftMargin + 1);
        var line = new StringBuilder(prefix);
        var contentLength = 0;
        foreach (var word in words)
        {
            var required = contentLength == 0 ? word.Length : word.Length + 1;
            if (contentLength > 0 && contentLength + required > width)
            {
                yield return line.ToString();
                line.Clear();
                line.Append(prefix);
                contentLength = 0;
            }
            if (contentLength > 0) { line.Append(' '); contentLength++; }
            line.Append(word);
            contentLength += word.Length;
        }
        if (line.Length > prefix.Length || contentLength == 0) yield return line.ToString();
    }
}
