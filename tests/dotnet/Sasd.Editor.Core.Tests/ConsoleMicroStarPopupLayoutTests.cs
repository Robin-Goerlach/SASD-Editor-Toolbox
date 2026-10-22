using Sasd.Editor.FirstEd.Sample;
using Xunit;

namespace Sasd.Editor.Core.Tests;

/// <summary>
/// Pure popup tests are portable on Windows and Linux CI because geometry
/// does not require Console.WindowWidth, a TTY or a physical cursor.
/// </summary>
public sealed class ConsoleMicroStarPopupLayoutTests
{
    [Fact]
    public void FitsCenteredAndNeverWritesLastPhysicalColumn()
    {
        var popup = ConsoleMicroStarPopupLayout.Create(
            100, 30, ConsoleMicroStarPopupLayout.MenuLines);
        Assert.NotNull(popup);
        Assert.Equal((100 - popup.Width) / 2, popup.Left);
        Assert.Equal((30 - popup.Height) / 2, popup.Top);
        Assert.True(popup.Left + popup.Width < 100);
        Assert.True(popup.Top + popup.Height < 29);

        for (var row = 0; row < popup.Height; row++)
            Assert.Equal(popup.Width, popup.GetRow(row).Length);
    }

    [Theory]
    [InlineData(20, 30)]
    [InlineData(100, 8)]
    [InlineData(0, 0)]
    public void SmallTerminalUsesSafeFallback(int width, int height)
    {
        Assert.Null(ConsoleMicroStarPopupLayout.Create(
            width, height, ConsoleMicroStarPopupLayout.MenuLines));
    }

    [Fact]
    public void BorderAndChoiceMappingRemainStable()
    {
        var popup = ConsoleMicroStarPopupLayout.Create(
            100, 30, ConsoleMicroStarPopupLayout.MenuLines)!;
        Assert.StartsWith("+", popup.GetRow(0));
        Assert.StartsWith("| MICROSTAR", popup.GetRow(1));
        Assert.EndsWith("+", popup.GetRow(popup.Height - 1));
        foreach (var key in "FRGUSWINBECMDTLQ")
            Assert.True(ConsoleMicroStarPopupLayout.IsMenuChoice(key));
        Assert.False(ConsoleMicroStarPopupLayout.IsMenuChoice('Z'));
        Assert.False(ConsoleMicroStarPopupLayout.IsMenuChoice('1'));
        Assert.Throws<ArgumentOutOfRangeException>(() => popup.GetRow(-1));
    }
}
