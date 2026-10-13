using Sasd.Editor.Commands;
using Sasd.Editor.Editing;
using Sasd.Editor.Model;
using Xunit;

namespace Sasd.Editor.Core.Tests;

/// <summary>
/// Regression coverage for the independent historical block-limit lifecycle.
/// These tests protect observable begin/end behavior without depending on the
/// original Pascal pointer representation.
/// </summary>
public sealed class BlockEndpointStateTests
{
    [Fact]
    public void BeginBlock_DefinesOnlyStartUntilEndExists()
    {
        var session = new EditorSession();
        var window = session.CreateDocument("a\nb\nc");
        window.Cursor = new TextPosition(1, 0);

        session.BeginBlock();

        Assert.Null(session.Block);
        Assert.Equal(window.Document.DocumentId, session.BlockState.Start!.DocumentId);
        Assert.Equal(1, session.BlockState.Start.Line);
        Assert.Null(session.BlockState.End);
        Assert.True(session.BlockState.Hidden);
        Assert.DoesNotContain(
            window.Document.Buffer.Snapshot(),
            line => line.Flags.HasFlag(EditorLineFlags.InBlock));
    }

    [Fact]
    public void EndBlock_DefinesOnlyEndUntilBeginExists()
    {
        var session = new EditorSession();
        var window = session.CreateDocument("a\nb\nc");
        window.Cursor = new TextPosition(2, 0);

        session.EndBlock();

        Assert.Null(session.Block);
        Assert.Null(session.BlockState.Start);
        Assert.Equal(window.Document.DocumentId, session.BlockState.End!.DocumentId);
        Assert.Equal(2, session.BlockState.End.Line);
        Assert.True(session.BlockState.Hidden);
    }

    [Fact]
    public void EndThenBegin_CompletesOrderedBlockAndShowsHighlight()
    {
        var session = new EditorSession();
        var window = session.CreateDocument("a\nb\nc\nd");
        window.Cursor = new TextPosition(3, 0);
        session.EndBlock();

        window.Cursor = new TextPosition(1, 0);
        session.BeginBlock();

        Assert.NotNull(session.Block);
        Assert.Equal(1, session.Block!.StartLine);
        Assert.Equal(3, session.Block.EndLine);
        Assert.False(session.Block.Hidden);
        Assert.True(window.Document.Buffer.GetLine(1).Flags.HasFlag(EditorLineFlags.InBlock));
        Assert.True(window.Document.Buffer.GetLine(2).Flags.HasFlag(EditorLineFlags.InBlock));
        Assert.True(window.Document.Buffer.GetLine(3).Flags.HasFlag(EditorLineFlags.InBlock));
    }

    [Fact]
    public void RepointBeginAfterExistingEnd_DropsEndInsteadOfCreatingReversedBlock()
    {
        var session = new EditorSession();
        var window = session.CreateDocument("a\nb\nc\nd");
        window.Cursor = new TextPosition(1, 0);
        session.BeginBlock();
        window.Cursor = new TextPosition(2, 0);
        session.EndBlock();

        window.Cursor = new TextPosition(3, 0);
        session.BeginBlock();

        Assert.Null(session.Block);
        Assert.Equal(3, session.BlockState.Start!.Line);
        Assert.Null(session.BlockState.End);
        Assert.True(session.BlockState.Hidden);
    }

    [Fact]
    public void RepointEndBeforeExistingBegin_DropsBeginInsteadOfCreatingReversedBlock()
    {
        var session = new EditorSession();
        var window = session.CreateDocument("a\nb\nc\nd");
        window.Cursor = new TextPosition(2, 0);
        session.BeginBlock();

        window.Cursor = new TextPosition(1, 0);
        session.EndBlock();

        Assert.Null(session.Block);
        Assert.Null(session.BlockState.Start);
        Assert.Equal(1, session.BlockState.End!.Line);
        Assert.True(session.BlockState.Hidden);
    }

    [Fact]
    public void EndpointInAnotherDocumentReplacesOppositeStreamLimit()
    {
        var session = new EditorSession();
        var first = session.CreateDocument("a\nb");
        first.Cursor = new TextPosition(1, 0);
        session.BeginBlock();

        var second = session.CreateDocument("x\ny");
        second.Cursor = new TextPosition(1, 0);
        session.EndBlock();

        Assert.Null(session.Block);
        Assert.Null(session.BlockState.Start);
        Assert.Equal(second.Document.DocumentId, session.BlockState.End!.DocumentId);
        Assert.Equal(1, session.BlockState.End.Line);
    }

    [Fact]
    public async Task DeletingOneBoundaryInvalidatesOnlyThatEndpoint()
    {
        var session = new EditorSession();
        var window = session.CreateDocument("a\nb\nc\nd");
        window.Cursor = new TextPosition(1, 0);
        session.BeginBlock();
        window.Cursor = new TextPosition(3, 0);
        session.EndBlock();

        window.Cursor = new TextPosition(1, 0);
        var dispatcher = new EditorCommandDispatcher(session);

        Assert.True(await dispatcher.ExecuteAsync(new(EditorCommandId.DeleteLine)));

        Assert.Null(session.Block);
        Assert.Null(session.BlockState.Start);
        Assert.Equal(2, session.BlockState.End!.Line);
        Assert.True(session.BlockState.Hidden);
        Assert.Equal(new[] { "a", "c", "d" }, window.Document.Buffer.Snapshot().Select(line => line.Text));
    }

    [Fact]
    public async Task StructuralInsertionRealignsAStandaloneEndpoint()
    {
        var session = new EditorSession();
        var window = session.CreateDocument("a\nb\nc");
        window.Cursor = new TextPosition(2, 0);
        session.BeginBlock();

        window.Cursor = new TextPosition(0, 0);
        var dispatcher = new EditorCommandDispatcher(session);

        Assert.True(await dispatcher.ExecuteAsync(new(EditorCommandId.InsertLine)));

        Assert.Null(session.Block);
        Assert.Equal(3, session.BlockState.Start!.Line);
        Assert.Null(session.BlockState.End);
        Assert.True(session.BlockState.Hidden);
    }

    [Fact]
    public void HideToggleCannotDisplayIncompleteStateButTogglesCompleteBlock()
    {
        var session = new EditorSession();
        var window = session.CreateDocument("a\nb\nc");
        window.Cursor = new TextPosition(1, 0);
        session.BeginBlock();

        session.ToggleBlockHidden();

        Assert.True(session.BlockState.Hidden);
        Assert.Null(session.Block);

        window.Cursor = new TextPosition(2, 0);
        session.EndBlock();
        Assert.False(session.Block!.Hidden);

        session.ToggleBlockHidden();
        Assert.True(session.Block!.Hidden);
        Assert.False(window.Document.Buffer.GetLine(1).Flags.HasFlag(EditorLineFlags.InBlock));

        session.ToggleBlockHidden();
        Assert.False(session.Block!.Hidden);
        Assert.True(window.Document.Buffer.GetLine(1).Flags.HasFlag(EditorLineFlags.InBlock));
    }
}
