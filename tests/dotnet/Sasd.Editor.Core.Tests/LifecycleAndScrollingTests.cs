using Sasd.Editor.Commands;
using Sasd.Editor.Editing;
using Sasd.Editor.Hooks;
using Sasd.Editor.Model;
using Sasd.Editor.Scheduling;
using Xunit;

namespace Sasd.Editor.Core.Tests;

public sealed class LifecycleAndScrollingTests
{
    [Fact]
    public async Task Exit_RequestsRundownWithoutSavingDirtyDocument()
    {
        var session = new EditorSession();
        session.CreateDocument("abc");
        session.Engine.InsertText("X");
        Assert.True(session.CurrentWindow.Document.IsDirty);

        var dispatcher = new EditorCommandDispatcher(session);
        Assert.True(await dispatcher.ExecuteAsync(new(EditorCommandId.Exit)));

        Assert.True(session.RundownRequested);
        Assert.True(session.CurrentWindow.Document.IsDirty);
    }

    [Fact]
    public async Task SystemLoop_RunsBackgroundOnlyWhenNoInputIsPending()
    {
        var hooks = new CountingHooks();
        var session = new EditorSession(hooks);
        session.CreateDocument("text");
        var input = new ExitOnSecondProbeInputPump();

        await session.SystemLoop.RunAsync(input);

        Assert.Equal(2, input.Probes);
        Assert.Equal(1, hooks.IdleCalls);
        Assert.True(session.RundownRequested);
    }

    [Fact]
    public async Task Scheduler_PrioritizesInputOverBackgroundTask()
    {
        var session = new EditorSession();
        session.CreateDocument("text");
        var task = new CountingBackgroundTask();
        session.Scheduler.Add(task);

        var result = await session.Scheduler.RunCycleAsync(new AlwaysInputPump());

        Assert.Equal(EditorScheduleResult.InputProcessed, result);
        Assert.Equal(0, task.Executions);
    }

    [Fact]
    public async Task ScrollDown_MovesTopLineAndKeepsCursorVisible()
    {
        var session = CreateFiveLineSession();
        session.CurrentWindow.TopLine = 1;
        session.CurrentWindow.Cursor = new TextPosition(1, 4);
        var dispatcher = new EditorCommandDispatcher(session);

        Assert.True(await dispatcher.ExecuteAsync(new(EditorCommandId.ScrollDown, PageSize: 3)));

        Assert.Equal(2, session.CurrentWindow.TopLine);
        Assert.Equal(new TextPosition(2, 4), session.CurrentWindow.Cursor);
    }

    [Fact]
    public async Task ScrollUp_MovesCursorWhenItWasOnLastDisplayedLine()
    {
        var session = CreateFiveLineSession();
        session.CurrentWindow.TopLine = 1;
        session.CurrentWindow.Cursor = new TextPosition(3, 2);
        var dispatcher = new EditorCommandDispatcher(session);

        Assert.True(await dispatcher.ExecuteAsync(new(EditorCommandId.ScrollUp, PageSize: 3)));

        Assert.Equal(0, session.CurrentWindow.TopLine);
        Assert.Equal(new TextPosition(2, 2), session.CurrentWindow.Cursor);
    }

    [Fact]
    public async Task PageMovement_UsesVisibleLinesMinusOneAndPreservesScreenRow()
    {
        var session = new EditorSession();
        session.CreateDocument(string.Join('\n', Enumerable.Range(0, 10).Select(index => $"line-{index}")));
        session.CurrentWindow.TopLine = 0;
        session.CurrentWindow.Cursor = new TextPosition(1, 3);
        var dispatcher = new EditorCommandDispatcher(session);

        Assert.True(await dispatcher.ExecuteAsync(new(EditorCommandId.PageDown, PageSize: 4)));
        Assert.Equal(3, session.CurrentWindow.TopLine);
        Assert.Equal(new TextPosition(4, 3), session.CurrentWindow.Cursor);

        Assert.True(await dispatcher.ExecuteAsync(new(EditorCommandId.PageUp, PageSize: 4)));
        Assert.Equal(0, session.CurrentWindow.TopLine);
        Assert.Equal(new TextPosition(1, 3), session.CurrentWindow.Cursor);
    }

    [Fact]
    public async Task UpAndDownLine_ScrollAtViewportEdges()
    {
        var session = CreateFiveLineSession();
        var dispatcher = new EditorCommandDispatcher(session);

        session.CurrentWindow.TopLine = 1;
        session.CurrentWindow.Cursor = new TextPosition(1, 2);
        Assert.True(await dispatcher.ExecuteAsync(new(EditorCommandId.CursorUp, PageSize: 3)));
        Assert.Equal(0, session.CurrentWindow.TopLine);
        Assert.Equal(new TextPosition(0, 2), session.CurrentWindow.Cursor);

        session.CurrentWindow.TopLine = 0;
        session.CurrentWindow.Cursor = new TextPosition(2, 2);
        Assert.True(await dispatcher.ExecuteAsync(new(EditorCommandId.CursorDown, PageSize: 3)));
        Assert.Equal(1, session.CurrentWindow.TopLine);
        Assert.Equal(new TextPosition(3, 2), session.CurrentWindow.Cursor);
    }

    [Fact]
    public async Task TopAndBottomFile_UseHistoricalViewportPlacement()
    {
        var session = CreateFiveLineSession();
        var dispatcher = new EditorCommandDispatcher(session);

        Assert.True(await dispatcher.ExecuteAsync(new(EditorCommandId.BottomOfFile)));
        Assert.Equal(4, session.CurrentWindow.TopLine);
        Assert.Equal(new TextPosition(4, 0), session.CurrentWindow.Cursor);

        Assert.True(await dispatcher.ExecuteAsync(new(EditorCommandId.TopOfFile)));
        Assert.Equal(0, session.CurrentWindow.TopLine);
        Assert.Equal(new TextPosition(0, 0), session.CurrentWindow.Cursor);
    }

    private static EditorSession CreateFiveLineSession()
    {
        var session = new EditorSession();
        session.CreateDocument("zero\none\ntwo\nthree\nfour");
        return session;
    }

    private sealed class CountingHooks : IEditorHooks
    {
        public int IdleCalls { get; private set; }

        public ValueTask OnIdleAsync(EditorSession session, CancellationToken cancellationToken)
        {
            IdleCalls++;
            return ValueTask.CompletedTask;
        }
    }

    private sealed class CountingBackgroundTask : IEditorBackgroundTask
    {
        public int Executions { get; private set; }

        public ValueTask ExecuteSliceAsync(EditorSession session, CancellationToken cancellationToken)
        {
            Executions++;
            return ValueTask.CompletedTask;
        }
    }

    private sealed class AlwaysInputPump : IEditorInputPump
    {
        public ValueTask<bool> TryProcessInputAsync(EditorSession session, CancellationToken cancellationToken = default) =>
            ValueTask.FromResult(true);
    }

    private sealed class ExitOnSecondProbeInputPump : IEditorInputPump
    {
        public int Probes { get; private set; }

        public async ValueTask<bool> TryProcessInputAsync(
            EditorSession session,
            CancellationToken cancellationToken = default)
        {
            Probes++;
            if (Probes < 2)
            {
                return false;
            }

            var dispatcher = new EditorCommandDispatcher(session);
            return await dispatcher.ExecuteAsync(new(EditorCommandId.Exit), cancellationToken);
        }
    }
}
