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

    [Theory]
    [InlineData('\u0015', ConsoleKey.NoName)]
    [InlineData('\u0015', ConsoleKey.U)]
    [InlineData('\u001b', ConsoleKey.NoName)]
    public void RawTerminalControlBytesCancelWithoutNamedConsoleKey(
        char code, ConsoleKey key)
    {
        // Unix pseudo-terminals may deliver a raw control byte with no
        // reliable ConsoleKey or modifier bit. A silently ignored Ctrl-U
        // would leave a dangerous Save/Replace prompt active.
        var editor = new ConsolePromptEditor();
        editor.Process(Key('x'));
        var actual = editor.Process(
            new ConsoleKeyInfo(code, key, false, false, false));

        Assert.Equal(ConsolePromptInputResult.Cancelled, actual);
        Assert.Equal("x", editor.Answer);
    }

    [Fact]
    public void AltGrPrintableInputDoesNotAccidentallyCancelPrompt()
    {
        var editor = new ConsolePromptEditor();
        var key = new ConsoleKeyInfo('@', ConsoleKey.Q, false, true, true);
        Assert.Equal(ConsolePromptInputResult.Continue, editor.Process(key));
        Assert.Equal("@", editor.Answer);
    }

    [Fact]
    public void BackspaceDeletesEntireSupplementaryUnicodeCharacter()
    {
        var editor = new ConsolePromptEditor();
        editor.Process(Key('a'));
        // ConsoleKeyInfo gives UTF-16 chars; a supplementary-plane symbol
        // arrives as two code units, but Backspace must remove it as ONE.
        editor.Process(Key('\ud83d'));
        editor.Process(Key('\ude00'));
        Assert.Equal("a\ud83d\ude00", editor.Answer);

        editor.Process(new ConsoleKeyInfo('\b', ConsoleKey.Backspace, false, false, false));
        Assert.Equal("a", editor.Answer);
    }

    [Fact]
    public void BackspaceDeletesBaseLetterAndCombiningAccentTogether()
    {
        var editor = new ConsolePromptEditor();
        editor.Process(Key('a'));
        editor.Process(Key('e'));
        editor.Process(Key('\u0301'));

        editor.Process(new ConsoleKeyInfo('\b', ConsoleKey.Backspace, false, false, false));
        Assert.Equal("a", editor.Answer);
    }

    [Fact]
    public void NarrowProjectionNeverCutsIntoSurrogatePairOrCombiningSequence()
    {
        var emoji = new ConsolePromptEditor();
        emoji.Process(Key('a'));
        emoji.Process(Key('\ud83d'));
        emoji.Process(Key('\ude00'));
        Assert.Equal("\ud83d\ude00", emoji.Project("P:", 2).Text);
        Assert.Equal("", emoji.Project("P:", 1).Text);
        Assert.Equal("a\ud83d\ude00", emoji.Answer);

        var accented = new ConsolePromptEditor();
        accented.Process(Key('a'));
        accented.Process(Key('e'));
        accented.Process(Key('\u0301'));
        Assert.Equal("e\u0301", accented.Project("P:", 2).Text);
        Assert.Equal("", accented.Project("P:", 1).Text);
        Assert.Equal("ae\u0301", accented.Answer);
    }

    [Fact]
    public void ResetClearsFullAnswerWithoutMutatingFollowingPrompt()
    {
        var editor = new ConsolePromptEditor();
        foreach (var character in "old filename.txt")
            editor.Process(Key(character));

        // A visual clearing operation is not enough: a subsequent Save
        // prompt must not inherit an undisplayed suffix from an old path.
        Assert.NotEmpty(editor.Answer);
        editor.Reset();

        Assert.Equal(string.Empty, editor.Answer);
        Assert.Equal("Save: ", editor.Project("Save: ", 20).Text);
        Assert.Equal(ConsolePromptInputResult.Continue, editor.Process(Key('n')));
        Assert.Equal("n", editor.Answer);
        Assert.Equal(ConsolePromptInputResult.Submitted,
            editor.Process(new ConsoleKeyInfo('\r', ConsoleKey.Enter, false, false, false)));
    }

    [Fact]
    public void ResetAfterCancelAllowsIndependentNewPrompt()
    {
        var editor = new ConsolePromptEditor();
        editor.Process(Key('x'));
        Assert.Equal(ConsolePromptInputResult.Cancelled,
            editor.Process(new ConsoleKeyInfo('\u001b', ConsoleKey.Escape,
                false, false, false)));

        // Cancellation is a result of one keystroke, not sticky state.
        // Resetting removes old content without leaving the next dialog
        // permanently cancelled.
        editor.Reset();
        Assert.Equal(ConsolePromptInputResult.Continue, editor.Process(Key('y')));
        Assert.Equal("y", editor.Answer);
    }

    [Fact]
    public void ResetAfterUnicodeEntryLeavesNoPartialGraphemeOrSurrogate()
    {
        var editor = new ConsolePromptEditor();

        // A filename prompt can receive a combining sequence followed by
        // a supplementary-plane symbol as separate ConsoleKeyInfo UTF-16
        // values. Reset must discard the complete model buffer, not only
        // clear the visible suffix of a narrow terminal projection.
        foreach (var codeUnit in "e\u0301\ud83d\ude00")
            editor.Process(Key(codeUnit));
        Assert.NotEmpty(editor.Answer);

        editor.Reset();

        Assert.Empty(editor.Answer);
        Assert.Equal("Name: ", editor.Project("Name: ", 30).Text);
        editor.Process(Key('x'));
        Assert.Equal("x", editor.Answer);
    }

    [Fact]
    public void DeleteAtEndDoesNotBehaveLikeBackspace()
    {
        var editor = new ConsolePromptEditor();
        editor.Process(Key('a'));
        editor.Process(Key('b'));

        // Prompt input is append-only and always keeps the caret after
        // the last entered grapheme. Delete has nothing to remove to the
        // right of the caret; Backspace must still remove the last item.
        Assert.Equal(ConsolePromptInputResult.Continue,
            editor.Process(new ConsoleKeyInfo('\0', ConsoleKey.Delete, false, false, false)));
        Assert.Equal("ab", editor.Answer);

        editor.Process(new ConsoleKeyInfo('\b', ConsoleKey.Backspace, false, false, false));
        Assert.Equal("a", editor.Answer);
    }

    [Theory]
    [InlineData('\r')]
    [InlineData('\n')]
    public void UnnamedTerminalEnterSubmitsWithoutAppendingControlBytes(char terminator)
    {
        var editor = new ConsolePromptEditor();
        editor.Process(Key('a'));

        // Linux pseudo-terminals can report Return as a raw byte with
        // ConsoleKey.NoName rather than ConsoleKey.Enter. Both CR and LF
        // must submit exactly the answer the user typed. This is a host
        // portability rule, not a claim of historical DOS input parity.
        var result = editor.Process(new ConsoleKeyInfo(
            terminator, ConsoleKey.NoName, false, false, false));

        Assert.Equal(ConsolePromptInputResult.Submitted, result);
        Assert.Equal("a", editor.Answer);
    }

    private static ConsoleKeyInfo Key(char character) =>
        new(character, ConsoleKey.A, false, false, false);
}
