using Sasd.Editor.FirstEd.Sample;
using Xunit;

namespace Sasd.Editor.Core.Tests;

/// <summary>Deterministic, headless coverage for nested popup lifetimes.</summary>
public sealed class ConsoleMicroStarOverlayStackTests
{
    private static ConsoleMicroStarPopupLayout Popup(string text) =>
        ConsoleMicroStarPopupLayout.Create(100, 35, [text])!;

    [Fact]
    public void NestedOverlayCloseRestoresParent()
    {
        var stack = new ConsoleMicroStarOverlayStack();
        var outer = Popup("outer");
        var inner = Popup("inner");
        using (stack.Push(outer))
        {
            Assert.Equal(new[] { outer }, stack.Active);
            using (stack.Push(inner))
                Assert.Equal(new[] { outer, inner }, stack.Active);
            Assert.Equal(new[] { outer }, stack.Active);
        }
        Assert.Empty(stack.Active);
    }

    [Fact]
    public void OutOfOrderCloseFailsWithoutCorruptingOwnership()
    {
        var stack = new ConsoleMicroStarOverlayStack();
        var outer = stack.Push(Popup("outer"));
        var inner = stack.Push(Popup("inner"));
        Assert.Throws<InvalidOperationException>(() => outer.Dispose());
        Assert.Equal(2, stack.Count);
        inner.Dispose();
        outer.Dispose();
        Assert.Empty(stack.Active);
    }

    [Fact]
    public void DisposeTwiceDoesNotCloseNewerPopup()
    {
        var stack = new ConsoleMicroStarOverlayStack();
        var first = stack.Push(Popup("first"));
        first.Dispose();
        using var second = stack.Push(Popup("second"));
        first.Dispose();
        Assert.Single(stack.Active);
    }

    [Fact]
    public void ActiveReturnsStableSnapshot()
    {
        var stack = new ConsoleMicroStarOverlayStack();
        using var first = stack.Push(Popup("first"));
        var snapshot = stack.Active;
        using (stack.Push(Popup("second")))
            Assert.Single(snapshot);
        Assert.Single(stack.Active);
    }

    [Fact]
    public void NestedSelectionRestoresParentAndDoesNotLeakToNextPopup()
    {
        var stack = new ConsoleMicroStarOverlayStack();
        var parent = Popup("F Find  R Replace");
        var child = Popup("N No  Y Yes");

        using (stack.Push(parent))
        {
            stack.SetSelection(parent, 'R');
            using (stack.Push(child))
            {
                stack.SetSelection(child, 'Y');
                Assert.Equal('R', stack.SelectionOf(parent));
                Assert.Equal('Y', stack.SelectionOf(child));
            }

            // Closing a child retains its parent's choice for a full model
            // redraw; the completed child's focus must be discarded.
            Assert.Equal('R', stack.SelectionOf(parent));
            Assert.Null(stack.SelectionOf(child));
        }

        using (stack.Push(child))
            Assert.Null(stack.SelectionOf(child));
    }

    [Fact]
    public void SelectionCanOnlyChangeOnAnActivePopup()
    {
        var stack = new ConsoleMicroStarOverlayStack();
        var popup = Popup("F Find");
        Assert.Throws<InvalidOperationException>(() => stack.SetSelection(popup, 'F'));
        using (stack.Push(popup))
            stack.SetSelection(popup, 'F');
        Assert.Throws<InvalidOperationException>(() => stack.SetSelection(popup, 'F'));
    }

    [Fact]
    public void NullPopupIsRejected()
    {
        var stack = new ConsoleMicroStarOverlayStack();
        Assert.Throws<ArgumentNullException>(() => stack.Push(null!));
        Assert.Equal(0, stack.Count);
    }
}
