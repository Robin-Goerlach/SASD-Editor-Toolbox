using Sasd.Editor.Commands;
using Sasd.Editor.Editing;
using Sasd.Editor.Model;
using Xunit;

namespace Sasd.Editor.Core.Tests;

/// <summary>
/// Regression coverage for the whole-line FIRST-ED block manipulation family.
/// The tests focus on externally visible text and anchor behavior rather than
/// reproducing historical linked-descriptor pointer mechanics.
/// </summary>
public sealed class BlockCompatibilityTests
{
    [Fact]
    public async Task CopyBlock_InsertsBeforeCursorAndKeepsTargetLineAnchored()
    {
        var session = new EditorSession();
        var source = session.CreateDocument("a\nb\nc\nd");
        source.Cursor = new TextPosition(1, 0);
        session.BeginBlock();
        source.Cursor = new TextPosition(2, 0);
        session.EndBlock();

        var target = session.CreateDocument("x\ny");
        target.Cursor = new TextPosition(1, 5);
        session.SetMarker(1);
        var dispatcher = new EditorCommandDispatcher(session);

        Assert.True(await dispatcher.ExecuteAsync(new(EditorCommandId.CopyBlock)));

        Assert.Equal(new[] { "x", "b", "c", "y" }, target.Document.Buffer.Snapshot().Select(line => line.Text));
        Assert.Equal(new TextPosition(3, 5), target.Cursor);
        Assert.True(session.JumpToMarker(1));
        Assert.Equal(3, target.Cursor.Line);
        Assert.Equal(source.Document.DocumentId, session.Block!.DocumentId);
        Assert.Equal(1, session.Block.FirstLine);
        Assert.Equal(2, session.Block.LastLine);
        Assert.True(target.Document.IsDirty);
        Assert.False(source.Document.IsDirty);
    }

    [Fact]
    public async Task CopyBlock_PreservesWrappedAndUserColorButDoesNotCloneBlockFlag()
    {
        var session = new EditorSession();
        var source = session.CreateDocument("a\nb\nc");
        source.Cursor = new TextPosition(1, 0);
        session.BeginBlock();
        session.EndBlock();
        source.Document.Buffer.SetFlags(
            1,
            EditorLineFlags.InBlock | EditorLineFlags.Wrapped | EditorLineFlags.UserColored);

        var target = session.CreateDocument("target");
        var dispatcher = new EditorCommandDispatcher(session);

        Assert.True(await dispatcher.ExecuteAsync(new(EditorCommandId.CopyBlock)));

        var copied = target.Document.Buffer.GetLine(0);
        Assert.True(copied.Flags.HasFlag(EditorLineFlags.Wrapped));
        Assert.True(copied.Flags.HasFlag(EditorLineFlags.UserColored));
        Assert.False(copied.Flags.HasFlag(EditorLineFlags.InBlock));
    }

    [Fact]
    public async Task DeleteBlock_RealignsLinkedWindowAndLaterMarker()
    {
        var session = new EditorSession();
        var window = session.CreateDocument("a\nb\nc\nd");
        window.Cursor = new TextPosition(3, 0);
        session.SetMarker(1);
        var linked = session.LinkWindow(window);
        linked.Cursor = new TextPosition(3, 7);
        linked.TopLine = 3;

        session.SetCurrentWindow(window.WindowId);
        window.Cursor = new TextPosition(1, 0);
        session.BeginBlock();
        window.Cursor = new TextPosition(2, 0);
        session.EndBlock();
        var dispatcher = new EditorCommandDispatcher(session);

        Assert.True(await dispatcher.ExecuteAsync(new(EditorCommandId.DeleteBlock)));

        Assert.Equal(new[] { "a", "d" }, window.Document.Buffer.Snapshot().Select(line => line.Text));
        Assert.Null(session.Block);
        Assert.Equal(new TextPosition(1, 7), linked.Cursor);
        Assert.Equal(1, linked.TopLine);
        Assert.True(session.JumpToMarker(1));
        Assert.Equal(1, session.CurrentWindow.Cursor.Line);
    }

    [Fact]
    public async Task DeleteBlock_ThatSpansWholeStreamLeavesSingleBlankLine()
    {
        var session = new EditorSession();
        var window = session.CreateDocument("a\nb");
        window.Cursor = new TextPosition(0, 0);
        session.BeginBlock();
        window.Cursor = new TextPosition(1, 0);
        session.EndBlock();
        var dispatcher = new EditorCommandDispatcher(session);

        Assert.True(await dispatcher.ExecuteAsync(new(EditorCommandId.DeleteBlock)));

        Assert.Equal(1, window.Document.Buffer.LineCount);
        Assert.Equal(string.Empty, window.Document.Buffer.GetLine(0).Text);
        Assert.Null(session.Block);
    }

    [Fact]
    public async Task MoveBlock_WithinDocumentUsesPostDeleteTargetAnchor()
    {
        var session = new EditorSession();
        var window = session.CreateDocument("a\nb\nc\nd\ne");
        window.Cursor = new TextPosition(1, 0);
        session.BeginBlock();
        window.Cursor = new TextPosition(2, 0);
        session.EndBlock();
        window.Cursor = new TextPosition(4, 9);
        var dispatcher = new EditorCommandDispatcher(session);

        Assert.True(await dispatcher.ExecuteAsync(new(EditorCommandId.MoveBlock)));

        Assert.Equal(new[] { "a", "d", "b", "c", "e" }, window.Document.Buffer.Snapshot().Select(line => line.Text));
        Assert.Equal(new TextPosition(4, 9), window.Cursor);
        Assert.Equal(window.Document.DocumentId, session.Block!.DocumentId);
        Assert.Equal(2, session.Block.FirstLine);
        Assert.Equal(3, session.Block.LastLine);
        Assert.True(window.Document.Buffer.GetLine(2).Flags.HasFlag(EditorLineFlags.InBlock));
        Assert.True(window.Document.Buffer.GetLine(3).Flags.HasFlag(EditorLineFlags.InBlock));
    }

    [Fact]
    public async Task MoveBlock_RejectsCursorInsideSourceRange()
    {
        var session = new EditorSession();
        var window = session.CreateDocument("a\nb\nc\nd");
        window.Cursor = new TextPosition(1, 0);
        session.BeginBlock();
        window.Cursor = new TextPosition(2, 0);
        session.EndBlock();
        window.Cursor = new TextPosition(1, 3);
        var before = window.Document.Buffer.Snapshot();
        var dispatcher = new EditorCommandDispatcher(session);

        Assert.False(await dispatcher.ExecuteAsync(new(EditorCommandId.MoveBlock)));

        Assert.Equal(before, window.Document.Buffer.Snapshot());
        Assert.False(window.Document.IsDirty);
        Assert.Equal(0, session.Undo.Count);
    }

    [Fact]
    public async Task MoveBlock_AcrossDocumentsMovesDefinitionAndRepairsBothStreams()
    {
        var session = new EditorSession();
        var source = session.CreateDocument("a\nb\nc\nd");
        source.Cursor = new TextPosition(1, 0);
        session.BeginBlock();
        source.Cursor = new TextPosition(2, 0);
        session.EndBlock();

        var sourceLinked = session.LinkWindow(source);
        sourceLinked.Cursor = new TextPosition(3, 6);
        sourceLinked.TopLine = 3;

        var target = session.CreateDocument("x\ny");
        target.Cursor = new TextPosition(1, 4);
        var dispatcher = new EditorCommandDispatcher(session);

        Assert.True(await dispatcher.ExecuteAsync(new(EditorCommandId.MoveBlock)));

        Assert.Equal(new[] { "a", "d" }, source.Document.Buffer.Snapshot().Select(line => line.Text));
        Assert.Equal(new[] { "x", "b", "c", "y" }, target.Document.Buffer.Snapshot().Select(line => line.Text));
        Assert.Equal(new TextPosition(1, 6), sourceLinked.Cursor);
        Assert.Equal(1, sourceLinked.TopLine);
        Assert.Equal(new TextPosition(3, 4), target.Cursor);
        Assert.Equal(target.Document.DocumentId, session.Block!.DocumentId);
        Assert.Equal(1, session.Block.FirstLine);
        Assert.Equal(2, session.Block.LastLine);
        Assert.True(source.Document.IsDirty);
        Assert.True(target.Document.IsDirty);
    }

    [Fact]
    public void DirectEngineBlockMethodsUseCompatibilityImplementation()
    {
        var session = new EditorSession();
        var window = session.CreateDocument("a\nb\nc");
        window.Cursor = new TextPosition(1, 0);
        session.BeginBlock();
        session.EndBlock();
        window.Cursor = new TextPosition(2, 2);

        session.Engine.CopyBlockToCursor();

        Assert.Equal(new[] { "a", "b", "b", "c" }, window.Document.Buffer.Snapshot().Select(line => line.Text));
        Assert.Equal(new TextPosition(3, 2), window.Cursor);
    }
}
