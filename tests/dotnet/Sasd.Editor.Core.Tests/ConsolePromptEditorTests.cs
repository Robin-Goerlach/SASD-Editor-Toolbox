using Sasd.Editor.FirstEd.Sample;
using Xunit;

namespace Sasd.Editor.Core.Tests;

/// <summary>
/// Headless regression tests for the terminal sample's own line-editing state.
/// No Console streams, window sizes or terminal emulators are required.
/// </summary>
public sealed class ConsolePromptEditorTests
{
    [Fact]
    public void LongPathRetainsItsCompleteValueWhileDisplayingNewestCharacters()
    {
        var editor = new ConsolePromptEditor();
        const string path = "folder/with/a/very/long/file-name.txt";

        foreach (var character in path)
        {
            Assert.Equal(ConsolePromptInputResult.Continue,
                editor.Process(Key(character)));
        }

        var projected = editor.Project("File path: ", 18);

        Assert.Equal(path, editor.Answer);
        Assert.Equal(("File path: " + path)[^18..], projected.Text);
        Assert.Equal(17, projected.CaretColumn);
        Assert.Equal(ConsolePromptInputResult.Submitted,
            editor.Process(new ConsoleKeyInfo('\r', ConsoleKey.Enter, false, false, false)));
        Assert.Equal(path, editor.Answer);
    }

    [Fact]
    public void NarrowDisplayDoesNotAlterBackspaceOrSubmittedEmptyAnswer()
    {
        var editor = new ConsolePromptEditor();
        editor.Process(Key('A'));
        editor.Process(Key('B'));
        Assert.Equal("t:AB", editor.Project("Prompt:", 4).Text);
        editor.Process(new ConsoleKeyInfo('\b', ConsoleKey.Backspace, false, false, false));
        editor.Process(new ConsoleKeyInfo('\b', ConsoleKey.Backspace, false, false, false));
        editor.Process(new ConsoleKeyInfo('\b', ConsoleKey.Backspace, false, false, false));

        Assert.Equal("", editor.Answer);
        Assert.Equal(ConsolePromptInputResult.Submitted,
            editor.Process(new ConsoleKeyInfo('\r', ConsoleKey.Enter, false, false, false)));
        Assert.Equal(3, editor.Project("Prompt:", 4).CaretColumn);
    }

    [Fact]
    public void EscapeAndPhysicalControlUCancelRatherThanSubmitAnEmptyAnswer()
    {
        var escape = new ConsolePromptEditor();
        Assert.Equal(ConsolePromptInputResult.Cancelled,
            escape.Process(new ConsoleKeyInfo('\u001b', ConsoleKey.Escape, false, false, false)));

        var controlU = new ConsolePromptEditor();
        controlU.Process(Key('x'));
        Assert.Equal(ConsolePromptInputResult.Cancelled,
            controlU.Process(new ConsoleKeyInfo('\u0015', ConsoleKey.U, false, false, true)));
        Assert.Equal("x", controlU.Answer);
    }

    [Fact]
    public void PrintableAltGrCharactersRemainValidPromptInput()
    {
        var editor = new ConsolePromptEditor();

        // Windows terminals may identify AltGr as Ctrl+Alt. The printable
        // KeyChar is still the user's intended filename/search character.
        Assert.Equal(ConsolePromptInputResult.Continue,
            editor.Process(new ConsoleKeyInfo('@', ConsoleKey.Q, false, true, true)));
        Assert.Equal("@", editor.Answer);
    }

    [Fact]
    public void UnprintableKeysAreIgnoredAndInvalidProjectionWidthIsRejected()
    {
        var editor = new ConsolePromptEditor();
        editor.Process(new ConsoleKeyInfo('\0', ConsoleKey.F1, false, false, false));
        Assert.Equal("", editor.Answer);
        Assert.Throws<ArgumentOutOfRangeException>(() => editor.Project("Find:", 0));
    }

    private static ConsoleKeyInfo Key(char character) =>
        new(character, ConsoleKey.A, false, false, false);
}
