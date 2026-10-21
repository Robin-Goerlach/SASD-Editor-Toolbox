using Sasd.Editor.Editing;
using Sasd.Editor.FirstEd.Sample;
using Sasd.Editor.Model;
using Sasd.Editor.Rendering;
using Xunit;

namespace Sasd.Editor.Core.Tests;

/// <summary>
/// Headless regression tests for FIRST-ED's historic screen attributes and
/// optional final-column indicators. The old video-memory implementation
/// must NOT leak into the host-neutral editor model.
/// </summary>
public sealed class FirstEdPresentationPolicyTests
{
    [Theory]
    [InlineData(EditorLineFlags.None, FirstEdColorRole.Text)]
    [InlineData(EditorLineFlags.Wrapped, FirstEdColorRole.Text)]
    [InlineData(EditorLineFlags.InBlock, FirstEdColorRole.Block)]
    [InlineData(EditorLineFlags.UserColored, FirstEdColorRole.User)]
    [InlineData(EditorLineFlags.InBlock | EditorLineFlags.UserColored,
        FirstEdColorRole.User)]
    public void ColorFlagHasPriorityOverVisibleBlock(
        EditorLineFlags flags, FirstEdColorRole expected)
    {
        Assert.Equal(expected, ConsoleFirstEdPresentationPolicy.TextRole(flags));
    }

    [Fact]
    public void EveryHistoricalAttributeRoleHasAHostColor()
    {
        foreach (var role in Enum.GetValues<FirstEdColorRole>())
        {
            var colors = ConsoleFirstEdPresentationPolicy.Colors(role);
            Assert.True(Enum.IsDefined(colors.Foreground));
            Assert.True(Enum.IsDefined(colors.Background));
        }
    }

    [Theory]
    [InlineData(false, true, EditorLineFlags.None, null)]
    [InlineData(true, true, EditorLineFlags.None, '<')]
    [InlineData(true, true, EditorLineFlags.UserColored, '<')]
    [InlineData(true, true, EditorLineFlags.Wrapped, ' ')]
    [InlineData(true, false, EditorLineFlags.None, '.')]
    [InlineData(true, false, EditorLineFlags.Wrapped, '.')]
    public void PhysicalRightEdgeMarkerIsOnlyPresentation(
        bool enabled, bool hasLine, EditorLineFlags flags, char? expected)
    {
        Assert.Equal(expected, ConsoleFirstEdPresentationPolicy.RightEdgeMarker(
            enabled, hasLine, flags));
    }

    [Fact]
    public void LinkedViewsShareLineColorsButRenderingChangesNothing()
    {
        var session = new EditorSession();
        var first = session.CreateDocument("hard\nsoft\nlast");
        var second = session.LinkWindow(first);
        session.ColorCurrentDocument();
        first.Document.Buffer.SetFlags(1,
            EditorLineFlags.Wrapped | EditorLineFlags.UserColored);
        var before = first.Document.Buffer.Snapshot();
        var viewport = new EditorViewportBuilder(session).Build(second, 4, 18);

        Assert.Equal(3, viewport.Lines.Count);
        Assert.All(viewport.Lines, line =>
            Assert.Equal(FirstEdColorRole.User,
                ConsoleFirstEdPresentationPolicy.TextRole(line.Flags)));
        Assert.Equal(' ', ConsoleFirstEdPresentationPolicy.RightEdgeMarker(
            true, true, viewport.Lines[1].Flags));
        Assert.Equal('.', ConsoleFirstEdPresentationPolicy.RightEdgeMarker(
            true, false, EditorLineFlags.None));

        // Rendering never converts the soft line break to a hard CR/LF or
        // creates an Undo entry. A linked sibling sees the same line flags.
        Assert.Equal(before, first.Document.Buffer.Snapshot());
        Assert.Same(first.Document, second.Document);
        Assert.False(first.Document.IsDirty);
        Assert.Equal(0, session.Undo.Count);
    }

    [Fact]
    public void RightEdgeIndicatorIsOutsideEscapedDocumentData()
    {
        var line = "one\u001b[2J";
        var marker = ConsoleFirstEdPresentationPolicy.RightEdgeMarker(
            true, true, EditorLineFlags.None);
        // Reserve the marker cell separately, so no unsafe ESC byte is
        // emitted and the historic marker is never stored in the document.
        var body = ConsoleFirstEdRowProjection.Project(line, 0, 12);
        var visible = body.Text + marker;
        Assert.Equal(13, visible.Length);
        Assert.DoesNotContain('\u001b', visible);
        Assert.EndsWith("<", visible);
    }
}
