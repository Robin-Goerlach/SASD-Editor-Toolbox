using Sasd.Editor.FirstEd.Sample;
using Xunit;

namespace Sasd.Editor.Core.Tests;

/// <summary>
/// Host-independent focus tests for future MicroStar file dialogs. They guard
/// keyboard behavior and nesting without opening any physical console.
/// </summary>
public sealed class MicroStarDialogFocusTests
{
    private static MicroStarDialogFocus Create() => new(
    [
        new("path", MicroStarDialogControlKind.Input),
        new("read-only", MicroStarDialogControlKind.Choice, Enabled: false),
        new("ok", MicroStarDialogControlKind.Action),
        new("cancel", MicroStarDialogControlKind.Action)
    ]);

    [Fact]
    public void TabSkipsDisabledItemsAndWraps()
    {
        var focus = Create();
        Assert.Equal("path", focus.FocusedId);
        Assert.Equal(MicroStarDialogNavigationResult.FocusChanged, focus.Handle(ConsoleKey.Tab));
        Assert.Equal("ok", focus.FocusedId);
        focus.Handle(ConsoleKey.Tab);
        Assert.Equal("cancel", focus.FocusedId);
        focus.Handle(ConsoleKey.Tab);
        Assert.Equal("path", focus.FocusedId);
    }

    [Fact]
    public void ShiftTabAndUpNavigateBackwards()
    {
        var focus = Create();
        focus.Handle(ConsoleKey.Tab, ConsoleModifiers.Shift);
        Assert.Equal("cancel", focus.FocusedId);
        focus.Handle(ConsoleKey.UpArrow);
        Assert.Equal("ok", focus.FocusedId);
    }

    [Fact]
    public void EnterCommitsOnlyFocusedControl()
    {
        var focus = Create();
        Assert.Equal(MicroStarDialogNavigationResult.Activate, focus.Handle(ConsoleKey.Enter));
        Assert.Equal("path", focus.FocusedId);
        Assert.Equal(MicroStarDialogControlKind.Input, focus.FocusedKind);
    }

    [Fact]
    public void EscapeAndControlUCancelWithoutMutatingFocus()
    {
        var focus = Create();
        Assert.Equal(MicroStarDialogNavigationResult.Cancel, focus.Handle(ConsoleKey.Escape));
        Assert.Equal(MicroStarDialogNavigationResult.Cancel,
            focus.Handle(ConsoleKey.U, ConsoleModifiers.Control));
        Assert.Equal("path", focus.FocusedId);
    }

    [Fact]
    public void NestedDialogsMaintainIndependentFocusState()
    {
        var parent = Create();
        parent.Handle(ConsoleKey.Tab);
        var child = new MicroStarDialogFocus([
            new("yes", MicroStarDialogControlKind.Action),
            new("no", MicroStarDialogControlKind.Action)
        ]);
        child.Handle(ConsoleKey.Tab);
        Assert.Equal("ok", parent.FocusedId);
        Assert.Equal("no", child.FocusedId);
    }

    [Fact]
    public void InvalidDefinitionsAreRejectedBeforeDialogIsShown()
    {
        Assert.Throws<ArgumentException>(() => new MicroStarDialogFocus([]));
        Assert.Throws<ArgumentException>(() => new MicroStarDialogFocus([
            new("x", MicroStarDialogControlKind.Input, false)
        ]));
        Assert.Throws<ArgumentException>(() => new MicroStarDialogFocus([
            new("same", MicroStarDialogControlKind.Input),
            new("same", MicroStarDialogControlKind.Action)
        ]));
    }

    [Fact]
    public void UnrelatedKeysNeverActivateControl()
    {
        var focus = Create();
        Assert.Equal(MicroStarDialogNavigationResult.Ignored, focus.Handle(ConsoleKey.A));
        Assert.Equal(MicroStarDialogNavigationResult.Ignored,
            focus.Handle(ConsoleKey.Tab, ConsoleModifiers.Control));
        Assert.Equal("path", focus.FocusedId);
    }
}
