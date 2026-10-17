using Sasd.Editor.Editing;
using Sasd.Editor.Hooks;
using Sasd.Editor.Model;
using Sasd.Editor.Search;
using Xunit;

namespace Sasd.Editor.Core.Tests;

/// <summary>
/// The host's BeforeReplace candidate describes a real document substring.
/// It must not silently use the user's query spelling for case-insensitive
/// matches, as that could mislead a confirmation dialog or review integration.
/// </summary>
public sealed class ReplaceCandidateAccuracyTests
{
    [Fact]
    public async Task ReplaceNext_ConfirmationReceivesExactCapitalizedMatch()
    {
        var hooks = new RecordingReplacementHooks();
        var session = new EditorSession(hooks);
        var view = session.CreateDocument("Alpha beta");

        Assert.True(await session.Search.ReplaceNextAsync("alpha", "item"));
        var candidate = Assert.Single(hooks.Candidates);
        Assert.Equal("Alpha", candidate.ExistingText);
        Assert.Equal("item", candidate.Replacement);
        Assert.Equal(new TextPosition(0, 0), candidate.Position);
        Assert.Equal("item beta", view.Document.Buffer.GetLine(0).Text);
        Assert.Equal(1, session.Undo.Count);

        Assert.True(session.Engine.Undo());
        Assert.Equal("Alpha beta", view.Document.Buffer.GetLine(0).Text);
        Assert.False(view.Document.IsDirty);
    }

    [Fact]
    public async Task ReplaceNext_SkipStillShowsOriginalTextAndLeavesStreamClean()
    {
        var hooks = new RecordingReplacementHooks { Decision = ReplaceDecision.Skip };
        var session = new EditorSession(hooks);
        var view = session.CreateDocument("aLPHA beta");

        Assert.False(await session.Search.ReplaceNextAsync("alpha", "value"));
        Assert.Equal("aLPHA", Assert.Single(hooks.Candidates).ExistingText);
        Assert.Equal(new TextPosition(0, 5), view.Cursor);
        Assert.Equal("aLPHA beta", view.Document.Buffer.GetLine(0).Text);
        Assert.False(view.Document.IsDirty);
        Assert.Equal(0, session.Undo.Count);
    }

    [Fact]
    public async Task ReplaceNext_CancelShowsOriginalTextAndDoesNotChangeDocument()
    {
        var hooks = new RecordingReplacementHooks { Decision = ReplaceDecision.Cancel };
        var session = new EditorSession(hooks);
        var view = session.CreateDocument("ALPHA");

        Assert.False(await session.Search.ReplaceNextAsync("alpha", "value"));
        Assert.Equal("ALPHA", Assert.Single(hooks.Candidates).ExistingText);
        Assert.Equal("ALPHA", view.Document.Buffer.GetLine(0).Text);
        Assert.False(view.Document.IsDirty);
        Assert.Equal(0, session.Undo.Count);
    }

    [Fact]
    public async Task ReplaceNext_WholeWordCandidateDescribesOnlyTheRealMatch()
    {
        var hooks = new RecordingReplacementHooks();
        var session = new EditorSession(hooks);
        var view = session.CreateDocument("scatter Cat");
        var options = new SearchOptions(WholeWord: true);

        Assert.True(await session.Search.ReplaceNextAsync("cat", "Dog", options));
        var candidate = Assert.Single(hooks.Candidates);
        Assert.Equal(new TextPosition(0, 8), candidate.Position);
        Assert.Equal("Cat", candidate.ExistingText);
        Assert.Equal("scatter Dog", view.Document.Buffer.GetLine(0).Text);
        Assert.Equal(1, session.Undo.Count);
    }

    [Fact]
    public async Task ReplaceNext_MissingMatchDoesNotCallHostConfirmation()
    {
        var hooks = new RecordingReplacementHooks();
        var session = new EditorSession(hooks);
        var view = session.CreateDocument("Beta");

        Assert.False(await session.Search.ReplaceNextAsync("alpha", "value"));
        Assert.Empty(hooks.Candidates);
        Assert.Equal("Beta", view.Document.Buffer.GetLine(0).Text);
        Assert.False(view.Document.IsDirty);
    }

    private sealed class RecordingReplacementHooks : IEditorHooks
    {
        public ReplaceDecision Decision { get; init; } = ReplaceDecision.Replace;
        public List<ReplaceCandidate> Candidates { get; } = [];

        public ValueTask<ReplaceDecision> BeforeReplaceAsync(
            EditorSession session, ReplaceCandidate candidate, CancellationToken cancellationToken)
        {
            Candidates.Add(candidate);
            return ValueTask.FromResult(Decision);
        }
    }
}
