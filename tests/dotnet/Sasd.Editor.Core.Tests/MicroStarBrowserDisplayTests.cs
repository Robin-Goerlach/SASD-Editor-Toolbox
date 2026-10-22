using Sasd.Editor.FirstEd.Sample;
using Xunit;

namespace Sasd.Editor.Core.Tests;

/// <summary>Ensure arbitrary filenames cannot send terminal control codes.</summary>
public sealed class MicroStarBrowserDisplayTests
{
    [Fact]
    public void EscapesTerminalControlsBeforePopupOutput()
    {
        var result = MicroStarBrowserDisplay.SafeLabel("hello\u001b[2J\r\n世界", 40);
        Assert.DoesNotContain('\u001b', result);
        Assert.DoesNotContain('\r', result);
        Assert.DoesNotContain('\n', result);
        Assert.DoesNotContain('世', result);
        Assert.Equal("hello?[2J????", result);
    }

    [Fact]
    public void TruncationDoesNotAlterFileIdentity()
    {
        var original = new string('x', 60);
        Assert.Equal(20, MicroStarBrowserDisplay.SafeLabel(original, 20).Length);
        Assert.EndsWith("...", MicroStarBrowserDisplay.SafeLabel(original, 20));
        Assert.Equal(60, original.Length);
    }

    [Fact]
    public void ImpossiblePopupWidthIsRejected()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            MicroStarBrowserDisplay.SafeLabel("name", 3));
    }
}
