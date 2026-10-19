using Sasd.Editor.Commands;
using Sasd.Editor.Editing;
using Sasd.Editor.Model;
using Xunit;

namespace Sasd.Editor.Core.Tests;

/// <summary>
/// Centering should record an Undo transaction only for changed text.
/// Cursor movement from virtual space alone does not make a file dirty.
/// </summary>
public sealed class CenterLineCompatibilityTests
{
    [Fact]
    public async Task AlreadyCenteredLineDoesNotProduceUndoOrDirtyState()
    {
        var session = new EditorSession();
        var view = session.CreateDocument("   text");
        view.Options.RightMargin = 9; // Inclusive width 10 -> padding 3.
        view.Cursor = new TextPosition(0, 12);
        var linked = session.LinkWindow(view);
        linked.Cursor = new TextPosition(0, 40);
        session.SetCurrentWindow(view.WindowId);

        Assert.True(await new EditorCommandDispatcher(session)
            .ExecuteAsync(new(EditorCommandId.CenterLine)));

        Assert.Equal("   text", view.Document.Buffer.GetLine(0).Text);
        Assert.Equal(new TextPosition(0, 7), view.Cursor);
        Assert.Equal(new TextPosition(0, 40), linked.Cursor);
        Assert.False(view.Document.IsDirty);
        Assert.Equal(0, view.Document.Version);
        Assert.Equal(0, session.Undo.Count);
    }

    [Fact]
    public async Task ActualCenteringCreatesOneUndoableMutation()
    {
        var session = new EditorSession();
        var view = session.CreateDocument("text");
        view.Options.RightMargin = 9;
        view.Cursor = new TextPosition(0, 12);
        var dispatcher = new EditorCommandDispatcher(session);

        Assert.True(await dispatcher.ExecuteAsync(new(EditorCommandId.CenterLine)));
        Assert.Equal("   text", view.Document.Buffer.GetLine(0).Text);
        Assert.Equal(new TextPosition(0, 7), view.Cursor);
        Assert.True(view.Document.IsDirty);
        Assert.Equal(1, view.Document.Version);
        Assert.Equal(1, session.Undo.Count);

        Assert.True(await dispatcher.ExecuteAsync(new(EditorCommandId.Undo)));
        Assert.Equal("text", view.Document.Buffer.GetLine(0).Text);
        Assert.Equal(new TextPosition(0, 12), view.Cursor);
        Assert.Equal(0, view.Document.Version);
        Assert.False(view.Document.IsDirty);
    }

    [Fact]
    public async Task RepeatedCenteringPreservesPreviousUndo()
    {
        var session = new EditorSession();
        var view = session.CreateDocument("text");
        view.Options.RightMargin = 9;
        var dispatcher = new EditorCommandDispatcher(session);
        Assert.True(await dispatcher.ExecuteAsync(new(EditorCommandId.CenterLine)));
        var version = view.Document.Version;
        Assert.True(await dispatcher.ExecuteAsync(new(EditorCommandId.CenterLine)));
        Assert.Equal(version, view.Document.Version);
        Assert.Equal(1, session.Undo.Count);
    }

    [Fact]
    public async Task NonzeroLeftMarginContributesToPadding()
    {
        var session = new EditorSession();
        var view = session.CreateDocument("word");
        view.Options.LeftMargin = 2;
        view.Options.RightMargin = 11;
        Assert.True(await new EditorCommandDispatcher(session)
            .ExecuteAsync(new(EditorCommandId.CenterLine)));
        Assert.Equal("     word", view.Document.Buffer.GetLine(0).Text);
        Assert.Equal(1, session.Undo.Count);
    }

    [Fact]
    public async Task UnrepresentablePhysicalLengthIsRejectedBeforeUndo()
    {
        var session = new EditorSession();
        var view = session.CreateDocument("word");
        view.Options.RightMargin = int.MaxValue;
        view.Options.LeftMargin = int.MaxValue - 1;
        Assert.False(await new EditorCommandDispatcher(session)
            .ExecuteAsync(new(EditorCommandId.CenterLine)));
        Assert.Equal("word", view.Document.Buffer.GetLine(0).Text);
        Assert.False(view.Document.IsDirty);
        Assert.Equal(0, session.Undo.Count);
    }
}
