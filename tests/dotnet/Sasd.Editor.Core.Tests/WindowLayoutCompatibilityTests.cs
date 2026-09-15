using Sasd.Editor.Commands;
using Sasd.Editor.Editing;
using Sasd.Editor.Model;
using Xunit;

namespace Sasd.Editor.Core.Tests;

public sealed class WindowLayoutCompatibilityTests
{
    [Fact]
    public async Task CreateWindow_TakesRequestedRowsFromDonorAndInsertsBelowIt()
    {
        var session = new EditorSession();
        var donor = session.CreateDocument("one\ntwo\nthree\nfour\nfive\nsix\nseven\neight\nnine\nten\neleven\ntwelve");
        Assert.True(session.WindowLayout.Configure(20));
        donor.Cursor = new TextPosition(10, 2);
        var dispatcher = new EditorCommandDispatcher(session);

        Assert.True(await dispatcher.ExecuteAsync(new(EditorCommandId.CreateWindow, Number: 5, Number2: 1)));

        Assert.Equal(2, session.Windows.Count);
        Assert.Equal(donor.WindowId, session.Windows[0].WindowId);
        Assert.Equal(session.CurrentWindow.WindowId, session.Windows[1].WindowId);

        var donorFrame = session.WindowLayout.GetFrame(donor.WindowId)!;
        var newFrame = session.WindowLayout.GetFrame(session.Windows[1].WindowId)!;
        Assert.Equal(15, donorFrame.Height);
        Assert.Equal(5, newFrame.Height);
        Assert.Equal(0, donorFrame.TopRow);
        Assert.Equal(15, newFrame.TopRow);
        Assert.Equal(20, session.WindowLayout.TotalRows);
        Assert.Equal(string.Empty, session.Windows[1].Document.Buffer.GetLine(0).Text);
    }

    [Fact]
    public async Task CreateWindow_MovesDonorCursorInsideCompressedVisibleSpan()
    {
        var session = new EditorSession();
        var donor = session.CreateDocument(string.Join('\n', Enumerable.Range(1, 30).Select(static value => $"line {value}")));
        Assert.True(session.WindowLayout.Configure(12));
        donor.TopLine = 5;
        donor.Cursor = new TextPosition(14, 7);
        var dispatcher = new EditorCommandDispatcher(session);

        Assert.True(await dispatcher.ExecuteAsync(new(EditorCommandId.CreateWindow, Number: 4, Number2: 1)));

        // Donor height becomes 8: one status row plus seven text rows. With
        // TopLine 5, line 11 is therefore its last displayed document line.
        Assert.Equal(new TextPosition(11, 7), donor.Cursor);
    }

    [Theory]
    [InlineData(2, 1)]
    [InlineData(8, 1)]
    public async Task CreateWindow_RejectsInvalidMinimumGeometry(int size, int donor)
    {
        var session = new EditorSession();
        session.CreateDocument("text");
        Assert.True(session.WindowLayout.Configure(10));
        var dispatcher = new EditorCommandDispatcher(session);

        Assert.False(await dispatcher.ExecuteAsync(new(EditorCommandId.CreateWindow, Number: size, Number2: donor)));
        Assert.Single(session.Windows);
        Assert.Equal(10, session.WindowLayout.GetFrame(session.CurrentWindow.WindowId)!.Height);
    }

    [Fact]
    public async Task DeleteFirstWindow_GivesFreedRowsToSecondWindow()
    {
        var session = new EditorSession();
        session.CreateDocument("first");
        Assert.True(session.WindowLayout.Configure(18));
        var dispatcher = new EditorCommandDispatcher(session);
        Assert.True(await dispatcher.ExecuteAsync(new(EditorCommandId.CreateWindow, Number: 6, Number2: 1)));
        var second = session.Windows[1];

        Assert.True(await dispatcher.ExecuteAsync(new(EditorCommandId.DeleteWindow, Number: 1)));

        Assert.Single(session.Windows);
        Assert.Equal(second.WindowId, session.Windows[0].WindowId);
        Assert.Equal(18, session.WindowLayout.GetFrame(second.WindowId)!.Height);
        Assert.Equal(18, session.WindowLayout.TotalRows);
    }

    [Fact]
    public async Task DeleteNonFirstWindow_GivesFreedRowsToWindowAbove()
    {
        var session = new EditorSession();
        session.CreateDocument("first");
        Assert.True(session.WindowLayout.Configure(24));
        var dispatcher = new EditorCommandDispatcher(session);

        // 24 -> [18, 6], then split window 1 again -> [15, 3, 6].
        Assert.True(await dispatcher.ExecuteAsync(new(EditorCommandId.CreateWindow, Number: 6, Number2: 1)));
        Assert.True(await dispatcher.ExecuteAsync(new(EditorCommandId.CreateWindow, Number: 3, Number2: 1)));
        Assert.Equal(new[] { 15, 3, 6 }, session.WindowLayout.Frames.Select(static frame => frame.Height).ToArray());

        var target = session.Windows[2];
        var above = session.Windows[1];
        Assert.True(await dispatcher.ExecuteAsync(new(EditorCommandId.DeleteWindow, Number: 3)));

        Assert.Equal(new[] { 15, 9 }, session.WindowLayout.Frames.Select(static frame => frame.Height).ToArray());
        Assert.Equal(above.WindowId, session.Windows[1].WindowId);
        Assert.DoesNotContain(session.Windows, window => window.WindowId == target.WindowId);
    }

    [Fact]
    public async Task DeleteWindow_ClearsActiveBlockFromDeletedWindow()
    {
        var session = new EditorSession();
        var first = session.CreateDocument("one\ntwo");
        Assert.True(session.WindowLayout.Configure(12));
        first.Cursor = new TextPosition(0, 0);
        session.BeginBlock();
        var dispatcher = new EditorCommandDispatcher(session);
        Assert.True(await dispatcher.ExecuteAsync(new(EditorCommandId.CreateWindow, Number: 4, Number2: 1)));

        Assert.True(await dispatcher.ExecuteAsync(new(EditorCommandId.DeleteWindow, Number: 1)));

        Assert.Null(session.Block);
    }

    [Fact]
    public void ResizeWorkspace_PreservesMinimumWindowHeight()
    {
        var session = new EditorSession();
        session.CreateDocument("one");
        session.CreateDocument("two");
        Assert.True(session.WindowLayout.Configure(12));

        Assert.True(session.WindowLayout.ResizeWorkspace(8));

        Assert.Equal(8, session.WindowLayout.TotalRows);
        Assert.All(session.WindowLayout.Frames, frame => Assert.True(frame.Height >= 3));
        Assert.False(session.WindowLayout.ResizeWorkspace(5));
        Assert.Equal(8, session.WindowLayout.TotalRows);
    }
}
