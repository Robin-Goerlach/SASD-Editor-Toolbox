using Sasd.Editor.Commands;
using Sasd.Editor.Editing;
using Sasd.Editor.Model;
using Xunit;

namespace Sasd.Editor.Core.Tests;

/// <summary>
/// Behavioral coverage for the clean-room EditReformat transfer. The tests focus
/// on margins, Wrapped paragraph boundaries and topology rather than reproducing
/// the historical pointer-splicing implementation.
/// </summary>
public sealed class ReformatCompatibilityTests
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Reformat_WrapsToMargins_RegardlessOfWordWrapMode(bool wordWrapMode)
    {
        var session = new EditorSession();
        var window = session.CreateDocument("one    two three");
        window.Options.LeftMargin = 2;
        window.Options.RightMargin = 10;
        window.Options.WordWrap = wordWrapMode;
        window.Cursor = new TextPosition(0, 7);
        var dispatcher = new EditorCommandDispatcher(session);

        Assert.True(await dispatcher.ExecuteAsync(new(EditorCommandId.ReformatParagraph)));

        var lines = window.Document.Buffer.Snapshot();
        Assert.Equal(new[] { "  one two", "  three" }, lines.Select(line => line.Text));
        Assert.True(lines[0].Flags.HasFlag(EditorLineFlags.Wrapped));
        Assert.False(lines[1].Flags.HasFlag(EditorLineFlags.Wrapped));
        Assert.Equal(new TextPosition(0, 7), window.Cursor);
        Assert.True(window.Document.IsDirty);
        Assert.Equal(1, session.Undo.Count);
    }

    [Fact]
    public async Task Reformat_PullsWordsUpAndRealignsFollowingReferences()
    {
        var session = new EditorSession();
        var source = session.CreateDocument("one two\nthree\ntail");
        source.Document.Buffer.SetFlags(0, EditorLineFlags.Wrapped);

        source.Cursor = new TextPosition(2, 0);
        session.SetMarker(1);
        var linked = session.LinkWindow(source);
        linked.Cursor = new TextPosition(2, 9);
        linked.TopLine = 2;

        session.SetCurrentWindow(source.WindowId);
        source.Options.RightMargin = 20;
        source.Cursor = new TextPosition(0, 4);
        var dispatcher = new EditorCommandDispatcher(session);

        Assert.True(await dispatcher.ExecuteAsync(new(EditorCommandId.ReformatParagraph)));

        Assert.Equal(new[] { "one two three", "tail" }, source.Document.Buffer.Snapshot().Select(line => line.Text));
        Assert.False(source.Document.Buffer.GetLine(0).Flags.HasFlag(EditorLineFlags.Wrapped));
        Assert.Equal(new TextPosition(1, 9), linked.Cursor);
        Assert.Equal(1, linked.TopLine);
        Assert.True(session.JumpToMarker(1));
        Assert.Equal(1, source.Cursor.Line);
        Assert.Equal(4, source.Cursor.Column);
    }

    [Fact]
    public async Task Reformat_StopsAtFirstHardParagraphBoundary()
    {
        var session = new EditorSession();
        var window = session.CreateDocument("alpha    beta\ndo   not   touch");
        window.Options.RightMargin = 30;
        var dispatcher = new EditorCommandDispatcher(session);

        Assert.True(await dispatcher.ExecuteAsync(new(EditorCommandId.ReformatParagraph)));

        Assert.Equal("alpha beta", window.Document.Buffer.GetLine(0).Text);
        Assert.Equal("do   not   touch", window.Document.Buffer.GetLine(1).Text);
    }

    [Fact]
    public async Task Reformat_ExpansionRealignsMarkerAndLinkedWindowAfterParagraph()
    {
        var session = new EditorSession();
        var source = session.CreateDocument("one two three four\ntail");
        source.Cursor = new TextPosition(1, 0);
        session.SetMarker(1);
        var linked = session.LinkWindow(source);
        linked.Cursor = new TextPosition(1, 12);
        linked.TopLine = 1;

        session.SetCurrentWindow(source.WindowId);
        source.Options.RightMargin = 6;
        source.Cursor = new TextPosition(0, 15);
        var dispatcher = new EditorCommandDispatcher(session);

        Assert.True(await dispatcher.ExecuteAsync(new(EditorCommandId.ReformatParagraph)));

        var snapshot = source.Document.Buffer.Snapshot();
        Assert.Equal(new[] { "one two", "three", "four", "tail" }, snapshot.Select(line => line.Text));
        Assert.True(snapshot[0].Flags.HasFlag(EditorLineFlags.Wrapped));
        Assert.True(snapshot[1].Flags.HasFlag(EditorLineFlags.Wrapped));
        Assert.False(snapshot[2].Flags.HasFlag(EditorLineFlags.Wrapped));
        Assert.Equal(new TextPosition(3, 12), linked.Cursor);
        Assert.Equal(3, linked.TopLine);
        Assert.True(session.JumpToMarker(1));
        Assert.Equal(3, source.Cursor.Line);
        Assert.Equal(15, source.Cursor.Column);
    }

    [Fact]
    public async Task Reformat_WordTooLongFailsAtomically()
    {
        var session = new EditorSession();
        var window = session.CreateDocument("abcdef");
        window.Options.LeftMargin = 2;
        window.Options.RightMargin = 5;
        window.Cursor = new TextPosition(0, 3);
        var before = window.Document.Buffer.Snapshot();
        var dispatcher = new EditorCommandDispatcher(session);

        Assert.False(await dispatcher.ExecuteAsync(new(EditorCommandId.ReformatParagraph)));

        Assert.Equal(before, window.Document.Buffer.Snapshot());
        Assert.False(window.Document.IsDirty);
        Assert.Equal(0, session.Undo.Count);
        Assert.Equal(new TextPosition(0, 3), window.Cursor);
    }

    [Fact]
    public async Task Reformat_NoChangeDoesNotCreateUndoOrDirtyState()
    {
        var session = new EditorSession();
        var window = session.CreateDocument("one two");
        window.Options.RightMargin = 20;
        var dispatcher = new EditorCommandDispatcher(session);

        Assert.False(await dispatcher.ExecuteAsync(new(EditorCommandId.ReformatParagraph)));

        Assert.False(window.Document.IsDirty);
        Assert.Equal(0, session.Undo.Count);
    }

    [Fact]
    public async Task Reformat_PreservesUserColorOnSurvivingDescriptorPositions()
    {
        var session = new EditorSession();
        var window = session.CreateDocument("one    two");
        window.Document.Buffer.SetFlags(0, EditorLineFlags.UserColored);
        window.Options.RightMargin = 20;
        var dispatcher = new EditorCommandDispatcher(session);

        Assert.True(await dispatcher.ExecuteAsync(new(EditorCommandId.ReformatParagraph)));

        Assert.True(window.Document.Buffer.GetLine(0).Flags.HasFlag(EditorLineFlags.UserColored));
    }
}
