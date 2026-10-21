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
