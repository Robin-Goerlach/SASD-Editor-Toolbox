using Sasd.Editor.Editing;
using Sasd.Editor.Rendering;
using Xunit;

namespace Sasd.Editor.Core.Tests;

/// <summary>
/// Defensive viewport tests for the V1 UI-neutral rendering boundary.
/// Large host sizes and stale offsets must not corrupt document/view state.
/// </summary>
public sealed class ViewportBoundaryTests
{
    [Fact]
    public void Build_VeryLargePositiveHeight_CapsAtRemainingDocumentLines()
    {
        var session = new EditorSession();
        var window = session.CreateDocument("one\ntwo\nthree");
        window.TopLine = 1;

        // This used to overflow firstLine + height and attempt to allocate a
        // List with negative capacity instead of projecting the two lines.
        var viewport = new EditorViewportBuilder(session).Build(
            window, int.MaxValue, 3);

        Assert.Equal(new[] { "two", "thr" }, viewport.Lines.Select(static row => row.Text));
        Assert.Equal(1, viewport.Lines[0].DocumentLine);
        Assert.Equal(2, viewport.Lines[1].DocumentLine);
        Assert.Equal(1, window.TopLine);
        Assert.False(window.Document.IsDirty);
        Assert.Equal(0, session.Undo.Count);
    }

    [Fact]
    public void Build_NegativeHorizontalOffset_ProjectsFromStartWithoutChangingView()
    {
        var session = new EditorSession();
        var window = session.CreateDocument("abcdef");
        window.LeftColumn = -7;

        var viewport = new EditorViewportBuilder(session).Build(window, 1, 4);

        Assert.Equal("abcd", Assert.Single(viewport.Lines).Text);
        Assert.Equal(-7, window.LeftColumn);
        Assert.False(window.Document.IsDirty);
    }

    [Fact]
    public void Build_VisibleAreaPastEndOfDocument_ReturnsOneEmptyLineOnly()
    {
        var session = new EditorSession();
        var window = session.CreateDocument();
        window.TopLine = int.MaxValue;
        window.LeftColumn = int.MaxValue;

        var viewport = new EditorViewportBuilder(session).Build(
            window, int.MaxValue, int.MaxValue);

        Assert.Equal(string.Empty, Assert.Single(viewport.Lines).Text);
        Assert.Equal(0, viewport.Lines[0].DocumentLine);
        Assert.Equal(int.MaxValue, window.TopLine);
        Assert.Equal(int.MaxValue, window.LeftColumn);
        Assert.False(window.Document.IsDirty);
    }

    [Theory]
    [InlineData(0, 10)]
    [InlineData(1, 0)]
    [InlineData(-1, 10)]
    [InlineData(1, -1)]
    public void Build_InvalidDimensions_RejectsBeforeMutatingWindow(int height, int width)
    {
        var session = new EditorSession();
        var window = session.CreateDocument("sample");
        window.TopLine = 0;
        window.LeftColumn = 2;

        Assert.Throws<ArgumentOutOfRangeException>(
            () => new EditorViewportBuilder(session).Build(window, height, width));

        Assert.Equal(0, window.TopLine);
        Assert.Equal(2, window.LeftColumn);
        Assert.False(window.Document.IsDirty);
    }
}
