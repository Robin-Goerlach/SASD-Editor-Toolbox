using Sasd.Editor.Commands;
using Sasd.Editor.Editing;
using Sasd.Editor.FirstEd.Sample;
using Sasd.Editor.Model;
using Sasd.Editor.Search;
using Xunit;

namespace Sasd.Editor.Core.Tests;

/// <summary>
/// Modern console option-prompt tests, deliberately distinguished from a
/// historical Optstr key grammar that the handbook does not document.
/// </summary>
public sealed class ConsoleFirstEdSearchOptionsTests
{
    [Fact]
    public void EmptySubmittedOptionsKeepOriginalFirstEdDefaults()
    {
        var request = new EditorCommandRequest(EditorCommandId.FindNext, Text: "abc");
        var resolved = ConsoleFirstEdSearchOptionsPolicy.Attach(request, "   ");
        Assert.NotNull(resolved);
        Assert.Equal(new SearchOptions(), resolved.Options);
        Assert.Null(request.Options); // input record remains unchanged
    }

    [Fact]
    public void ExplicitSearchOptionsCanBeCombinedInAnyOrder()
    {
        var request = new EditorCommandRequest(EditorCommandId.ReplaceNext,
            Text: "abc", Text2: "replacement");
        var resolved = ConsoleFirstEdSearchOptionsPolicy.Attach(request, "w, r;C");
        Assert.NotNull(resolved);
        Assert.Equal(new SearchOptions(true, true, true), resolved.Options);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("x")]
    [InlineData("cc")]
    [InlineData("1")]
    public void CancelOrUnknownOptionsNeverDispatch(string? answer)
    {
        var request = new EditorCommandRequest(EditorCommandId.FindNext, Text: "abc");
        Assert.Null(ConsoleFirstEdSearchOptionsPolicy.Attach(request, answer));
    }

    [Theory]
    [InlineData("C\nW")]
    [InlineData("C\tW")]
    [InlineData("C\rW")]
    [InlineData("C\u00a0W")]
    public void HiddenWhitespaceCannotSilentlyChangeSearchOptions(string answer)
    {
        // The modern option prompt intentionally accepts only ordinary
        // spaces, commas and semicolons as separators. Pasted control or
        // invisible Unicode whitespace must fail closed rather than
        // quietly running a search with possibly unintended flags.
        var request = new EditorCommandRequest(EditorCommandId.FindNext, Text: "needle");
        Assert.Null(ConsoleFirstEdSearchOptionsPolicy.Attach(request, answer));
        Assert.Null(request.Options); // never mutate an incoming request
    }

    [Theory]
    [InlineData("Ｃ")] // Fullwidth Latin C is not the ASCII shortcut.
    [InlineData("K")] // Compatibility characters do not belong to this grammar.
    [InlineData("c\u200Bw")] // Invisible formatting character between options.
    [InlineData("C,C")] // Repeating one option is a malformed request.
    public void NonAsciiOrDuplicateOptionsDoNotDispatchOrMutateRequest(string answer)
    {
        var request = new EditorCommandRequest(
            EditorCommandId.FindNext, Text: "needle");
        var rejected = ConsoleFirstEdSearchOptionsPolicy.Attach(request, answer);

        // Only a fully parsed option string may produce a new semantic
        // request. Neither invalid input nor partial recognition of an
        // initial valid letter may mutate the caller's record.
        Assert.Null(rejected);
        Assert.Null(request.Options);
        Assert.Equal("needle", request.Text);
    }

    [Fact]
    public async Task SemanticFindHonorsOptionsAndRemembersThemForFindAgain()
    {
        var session = new EditorSession();
        var view = session.CreateDocument("Alpha alpha ALPHA");
        var dispatcher = new EditorCommandDispatcher(session);
        var request = ConsoleFirstEdSearchOptionsPolicy.Attach(
            new EditorCommandRequest(EditorCommandId.FindNext, Text: "alpha"), "C");
        Assert.NotNull(request);
        Assert.True(await dispatcher.ExecuteAsync(request));
        Assert.Equal(new TextPosition(0, 6), view.Cursor);

        // Last match is advanced without accidentally dropping the
        // case-sensitive rule (the uppercase ALPHA must not match).
        Assert.False(await dispatcher.ExecuteAsync(
            new EditorCommandRequest(EditorCommandId.FindAgain)));
        Assert.Equal(new TextPosition(0, 6), view.Cursor);
        Assert.False(view.Document.IsDirty);
        Assert.Equal(0, session.Undo.Count);
    }

    [Fact]
    public async Task EmptyFindPromptRetainsPreviouslySelectedCaseSensitiveOption()
    {
        var session = new EditorSession();
        var view = session.CreateDocument("Alpha alpha ALPHA alpha");
        var dispatcher = new EditorCommandDispatcher(session);
        var initial = ConsoleFirstEdSearchOptionsPolicy.Attach(
            new EditorCommandRequest(EditorCommandId.FindNext, Text: "alpha"), "C");
        Assert.NotNull(initial);
        Assert.True(await dispatcher.ExecuteAsync(initial));
        Assert.Equal(new TextPosition(0, 6), view.Cursor);

        // The handbook's empty Find input repeats the previous search. The
        // host's modern option prompt must not reinterpret that repeat as a
        // fresh search with its default (case-insensitive) options.
        // The next hit is the lower-case word, not the intervening "ALPHA".
        Assert.True(await dispatcher.ExecuteAsync(
            new EditorCommandRequest(EditorCommandId.FindNext, Text: "")));
        Assert.Equal(new TextPosition(0, 18), view.Cursor);
        Assert.Equal("alpha", session.Search.LastPattern);
        Assert.False(view.Document.IsDirty);
        Assert.Equal(0, session.Undo.Count);
    }

    [Fact]
    public async Task EmptyFindPromptRetainsWrapAroundOptionAcrossDocumentEnd()
    {
        var session = new EditorSession();
        var view = session.CreateDocument("alpha beta alpha");
        var dispatcher = new EditorCommandDispatcher(session);
        view.Cursor = new TextPosition(0, 8);

        var initial = ConsoleFirstEdSearchOptionsPolicy.Attach(
            new EditorCommandRequest(EditorCommandId.FindNext, Text: "alpha"), "R");
        Assert.NotNull(initial);
        Assert.True(await dispatcher.ExecuteAsync(initial));
        Assert.Equal(new TextPosition(0, 11), view.Cursor);

        // A repeated search starts after the previous hit. Because the
        // original request explicitly enabled modern wrap-around, reaching
        // the end must continue at the start of the same document. Replacing
        // the remembered options with the host defaults would lose this hit.
        // This verifies the public command boundary, not the undocumented
        // historical Optstr grammar.
        Assert.True(await dispatcher.ExecuteAsync(
            new EditorCommandRequest(EditorCommandId.FindNext, Text: string.Empty)));
        Assert.Equal(new TextPosition(0, 0), view.Cursor);
        Assert.Equal("alpha", session.Search.LastPattern);
        Assert.False(view.Document.IsDirty);
        Assert.Equal(0, session.Undo.Count);
    }

    [Fact]
    public async Task EmptyFindPromptRetainsWholeWordOptionAcrossEmbeddedMatches()
    {
        var session = new EditorSession();
        var view = session.CreateDocument("cat scatter cat");
        var dispatcher = new EditorCommandDispatcher(session);
        var initial = ConsoleFirstEdSearchOptionsPolicy.Attach(
            new EditorCommandRequest(EditorCommandId.FindNext, Text: "cat"), "W");
        Assert.NotNull(initial);
        Assert.True(await dispatcher.ExecuteAsync(initial));
        Assert.Equal(new TextPosition(0, 0), view.Cursor);

        // The empty FIRST-ED Find prompt is a repeat of the previous search,
        // not a new request with default options. If WholeWord were lost, the
        // suffix of "scatter" at column 5 would be accepted as the next hit.
        // The public dispatcher must instead skip it and select the final
        // standalone "cat" at column 12. This asserts modern W-option policy
        // without claiming the undocumented historical Optstr key grammar.
        Assert.True(await dispatcher.ExecuteAsync(
            new EditorCommandRequest(EditorCommandId.FindNext, Text: string.Empty)));
        Assert.Equal(new TextPosition(0, 12), view.Cursor);
        Assert.Equal("cat", session.Search.LastPattern);
        Assert.False(view.Document.IsDirty);
        Assert.Equal(0, session.Undo.Count);
    }

    [Fact]
    public async Task SemanticReplaceHonorsWholeWordOptionsAndCanUndo()
    {
        var session = new EditorSession();
        var view = session.CreateDocument("cat scatter cat");
        var dispatcher = new EditorCommandDispatcher(session);
        view.Cursor = new TextPosition(0, 5);
        var request = ConsoleFirstEdSearchOptionsPolicy.Attach(
            new EditorCommandRequest(EditorCommandId.ReplaceNext,
                Text: "cat", Text2: "dog"), "W");
        Assert.NotNull(request);
        Assert.True(await dispatcher.ExecuteAsync(request));
        Assert.Equal("cat scatter dog", view.Document.Buffer.GetLine(0).Text);
        Assert.True(await dispatcher.ExecuteAsync(new(EditorCommandId.Undo)));
        Assert.Equal("cat scatter cat", view.Document.Buffer.GetLine(0).Text);
    }
}
