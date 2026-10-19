using Sasd.Editor.Editing;
using Sasd.Editor.Hooks;
using Sasd.Editor.Scheduling;
using Xunit;

namespace Sasd.Editor.Core.Tests;

/// <summary>
/// The scheduler is input-first and cooperative, not a thread pool. Each
/// deterministic test controls the pump and background callbacks directly.
/// </summary>
public sealed class SchedulerLifecycleTests
{
    [Fact]
    public async Task InputIsProcessedBeforeIdleAndBackground()
    {
        var calls = new List<string>();
        var hooks = new IdleHooks { OnIdle = _ => calls.Add("idle") };
        var session = new EditorSession(hooks);
        session.Scheduler.Add(new StepTask(_ => calls.Add("background")));

        var result = await session.Scheduler.RunCycleAsync(
            new Pump(_ => { calls.Add("input"); return true; }));

        Assert.Equal(EditorScheduleResult.InputProcessed, result);
        Assert.Equal(new[] { "input" }, calls);
    }

    [Fact]
    public async Task InputPumpRequestingRundownDoesNotRunIdleWork()
    {
        var calls = new List<string>();
        var session = new EditorSession(new IdleHooks { OnIdle = _ => calls.Add("idle") });
        session.Scheduler.Add(new StepTask(_ => calls.Add("background")));

        await session.Scheduler.RunCycleAsync(new Pump(editor =>
        {
            editor.RequestRundown();
            return false;
        }));

        Assert.True(session.RundownRequested);
        Assert.Empty(calls);
    }

    [Fact]
    public async Task IdleHookRequestingRundownSkipsAllBackgroundTasks()
    {
        var calls = new List<string>();
        var session = new EditorSession(new IdleHooks
        {
            OnIdle = editor => { calls.Add("idle"); editor.RequestRundown(); }
        });
        session.Scheduler.Add(new StepTask(_ => calls.Add("background")));

        await session.Scheduler.RunIdleCycleAsync();

        Assert.Equal(new[] { "idle" }, calls);
        Assert.True(session.RundownRequested);
    }

    [Fact]
    public async Task BackgroundRundownStopsAllSubsequentSlices()
    {
        var calls = new List<string>();
        var session = new EditorSession();
        session.Scheduler.Add(new StepTask(editor =>
        {
            calls.Add("first");
            editor.RequestRundown();
        }));
        session.Scheduler.Add(new StepTask(_ => calls.Add("second")));

        await session.Scheduler.RunIdleCycleAsync();

        Assert.Equal(new[] { "first" }, calls);
    }

    [Fact]
    public async Task RemovedTaskIsNotInvokedFromOldSnapshot()
    {
        var calls = new List<string>();
        var session = new EditorSession();
        var removed = new StepTask(_ => calls.Add("removed"));
        session.Scheduler.Add(new StepTask(editor =>
        {
            calls.Add("first");
            Assert.True(editor.Scheduler.Remove(removed));
        }));
        session.Scheduler.Add(removed);

        await session.Scheduler.RunIdleCycleAsync();

        Assert.Equal(new[] { "first" }, calls);
        Assert.Single(session.Scheduler.Tasks);
    }

    [Fact]
    public async Task AddedTaskDoesNotRunUntilNextIdleCycle()
    {
        var calls = new List<string>();
        var session = new EditorSession();
        var added = new StepTask(_ => calls.Add("new"));
        var registered = false;
        session.Scheduler.Add(new StepTask(editor =>
        {
            calls.Add("first");
            if (!registered)
            {
                registered = true;
                editor.Scheduler.Add(added);
            }
        }));

        await session.Scheduler.RunIdleCycleAsync();
        Assert.Equal(new[] { "first" }, calls);

        await session.Scheduler.RunIdleCycleAsync();
        Assert.Equal(new[] { "first", "first", "new" }, calls);
    }

    [Fact]
    public async Task ExternalCancellationBeforeIdleDoesNotInvokeHostOrTask()
    {
        var calls = new List<string>();
        var session = new EditorSession(new IdleHooks { OnIdle = _ => calls.Add("idle") });
        session.Scheduler.Add(new StepTask(_ => calls.Add("background")));
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            async () => await session.Scheduler.RunIdleCycleAsync(cancellation.Token));
        Assert.Empty(calls);
    }

    [Fact]
    public async Task CancellationInsideIdleCallbackStopsBackgroundWork()
    {
        var calls = new List<string>();
        using var cancellation = new CancellationTokenSource();
        var session = new EditorSession(new IdleHooks
        {
            OnIdle = _ => { calls.Add("idle"); cancellation.Cancel(); }
        });
        session.Scheduler.Add(new StepTask(_ => calls.Add("background")));

        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            async () => await session.Scheduler.RunIdleCycleAsync(cancellation.Token));
        Assert.Equal(new[] { "idle" }, calls);
    }

    private sealed class Pump(Func<EditorSession, bool> process) : IEditorInputPump
    {
        public ValueTask<bool> TryProcessInputAsync(
            EditorSession session, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return ValueTask.FromResult(process(session));
        }
    }

    private sealed class StepTask(Action<EditorSession> step) : IEditorBackgroundTask
    {
        public ValueTask ExecuteSliceAsync(EditorSession session, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            step(session);
            return ValueTask.CompletedTask;
        }
    }

    private sealed class IdleHooks : IEditorHooks
    {
        public Action<EditorSession>? OnIdle { get; init; }

        public ValueTask OnIdleAsync(EditorSession session, CancellationToken cancellationToken)
        {
            OnIdle?.Invoke(session);
            return ValueTask.CompletedTask;
        }
    }
}
