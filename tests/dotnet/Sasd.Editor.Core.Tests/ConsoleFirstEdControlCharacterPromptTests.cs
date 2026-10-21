using Sasd.Editor.Commands;
using Sasd.Editor.Editing;
using Sasd.Editor.FirstEd.Sample;
using Sasd.Editor.Input;
using Sasd.Editor.Model;
using Xunit;

namespace Sasd.Editor.Core.Tests;

/// <summary>
/// Verifies the FIRST-ED terminal's second-key handling for Ctrl-P.
/// The historical EditInsertCtrlChar routine accepts one ordinary character
/// and translates it to an ASCII control character before inserting text.
/// </summary>
/// <remarks>
/// All input normalization is testable without a real Console. The reusable
/// editor dispatcher intentionally still receives a literal control value,
/// making command semantics independent of which UI host collects the key.
/// </remarks>
public sealed class ConsoleFirstEdControlCharacterPromptTests
{
    [Theory]
    [InlineData('A', '\u0001')]
    [InlineData('a', '\u0001')]
    [InlineData('Z', '\u001a')]
    [InlineData('z', '\u001a')]
    [InlineData('U', '\u0015')]
    [InlineData('@', '\u0000')]
    [InlineData('[', '\u001b')]
    [InlineData('\\', '\u001c')]
    [InlineData(']', '\u001d')]
    [InlineData('^', '\u001e')]
    [InlineData('_', '\u001f')]
    public void PrintableAsciiKeyTranslatesToLiteralControlValue(char input, char expected)
    {
        // ConsoleKeyInfo.KeyChar, not ConsoleKey's ordinal, is the typed
        // character: keyboard layouts and Shift change the physical key.
        Assert.Equal(expected,
            ConsoleFirstEdControlCharacterPromptPolicy.Resolve(Key(input)));
    }

    [Theory]
    [InlineData('\u0001')]
    [InlineData('\u0002')]
    [InlineData('\u001a')]
    public void AlreadyTranslatedPhysicalControlKeyIsNotConvertedTwice(char input)
    {
        var info = new ConsoleKeyInfo(input, ConsoleKey.A, false, false, true);
        Assert.Equal(input, ConsoleFirstEdControlCharacterPromptPolicy.Resolve(info));
    }

    [Fact]
    public void CtrlUAndEscapeCancelWhilePlainUCanRequestControlU()
    {
        Assert.Null(ConsoleFirstEdControlCharacterPromptPolicy.Resolve(
            new ConsoleKeyInfo('\u0015', ConsoleKey.U, false, false, true)));
        Assert.Null(ConsoleFirstEdControlCharacterPromptPolicy.Resolve(
            new ConsoleKeyInfo('\u0015', ConsoleKey.NoName, false, false, false)));
        Assert.Null(ConsoleFirstEdControlCharacterPromptPolicy.Resolve(
            new ConsoleKeyInfo('\u001b', ConsoleKey.Escape, false, false, false)));

        // Typing a literal U *without* Ctrl is not the abort shortcut. It
        // explicitly requests the stored ASCII 0x15 control code.
        Assert.Equal('\u0015',
            ConsoleFirstEdControlCharacterPromptPolicy.Resolve(Key('U')));
    }

    [Theory]
    [InlineData('\r', ConsoleKey.Enter)]
    [InlineData('\b', ConsoleKey.Backspace)]
    [InlineData('\0', ConsoleKey.F1)]
    [InlineData('1', ConsoleKey.D1)]
    [InlineData(' ', ConsoleKey.Spacebar)]
    [InlineData('é', ConsoleKey.E)]
    [InlineData('ſ', ConsoleKey.S)]
    public void UnsupportedOrPromptNavigationKeyCannotCreateInsertRequest(
        char value, ConsoleKey key)
    {
        Assert.Null(ConsoleFirstEdControlCharacterPromptPolicy.Resolve(
            new ConsoleKeyInfo(value, key, false, false, false)));
    }

    [Fact]
    public async Task CtrlPThenAInsertsOneActualControlByteAndUndoRestoresLine()
    {
        var map = new FirstEdKeyMap();
        var action = map.Translate(EditorKeyStroke.Control('P'));
        Assert.Equal(EditorInputActionKind.Command, action.Kind);
        var binding = Assert.IsType<EditorCommandBinding>(action.Binding);
        Assert.Equal(EditorCommandId.InsertControlCharacter, binding.CommandId);
        Assert.Equal(EditorCommandArgumentKind.Character, binding.ArgumentKind);

        var session = new EditorSession();
        var window = session.CreateDocument("abcd");
        window.Cursor = new TextPosition(0, 1);

        // If the renderer used raw KeyChar here, it would dispatch "A"
        // instead of ASCII 0x01. Exercise the actual semantic dispatcher
        // after the *host-only* translation to catch that specific bug.
        var translated = ConsoleFirstEdControlCharacterPromptPolicy.Resolve(Key('A'));
        Assert.Equal('\u0001', translated);
        var request = Assert.IsType<EditorCommandRequest>(
            EditorCommandPromptResolver.Resolve(
                binding, pageSize: 17, characterResponse: translated));
        Assert.Equal("\u0001", request.Text);
        Assert.Equal(17, request.PageSize);

        var dispatcher = new EditorCommandDispatcher(session);
        Assert.True(await dispatcher.ExecuteAsync(request));
        Assert.Equal("a\u0001bcd", window.Document.Buffer.GetLine(0).Text);
        Assert.Equal(new TextPosition(0, 2), window.Cursor);
        Assert.True(window.Document.IsDirty);
        Assert.Equal(1, window.Document.Version);
        Assert.Equal(1, session.Undo.Count);

        Assert.True(await dispatcher.ExecuteAsync(new(EditorCommandId.Undo)));
        Assert.Equal("abcd", window.Document.Buffer.GetLine(0).Text);
        Assert.False(window.Document.IsDirty);
        Assert.Equal(0, session.Undo.Count);
    }

    [Fact]
    public void CancelledPromptNeverResolvesToSemanticControlInsertion()
    {
        var map = new FirstEdKeyMap();
        var binding = Assert.IsType<EditorCommandBinding>(
            map.Translate(EditorKeyStroke.Control('P')).Binding);
        var cancelled = ConsoleFirstEdControlCharacterPromptPolicy.Resolve(
            new ConsoleKeyInfo('\u0015', ConsoleKey.U, false, false, true));
        Assert.Null(cancelled);
        Assert.Null(EditorCommandPromptResolver.Resolve(
            binding, characterResponse: cancelled));

        // No semantic command is constructed: neither document text nor
        // its Undo history needs to be touched or rolled back.
        var session = new EditorSession();
        var view = session.CreateDocument("safe");
        Assert.Equal("safe", view.Document.Buffer.GetLine(0).Text);
        Assert.False(view.Document.IsDirty);
        Assert.Equal(0, session.Undo.Count);
    }

    private static ConsoleKeyInfo Key(char value) =>
        new(value, ConsoleKey.A, false, false, false);
}
