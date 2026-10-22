using Sasd.Editor.FirstEd.Sample;
using Xunit;

namespace Sasd.Editor.Core.Tests;

/// <summary>
/// Pure formatting tests; real printer spoolers and host ANSI behavior
/// remain outside the reusable pagination policy.
/// </summary>
public sealed class MicroStarPrintPaginationTests
{
    [Fact]
    public void SplitsOnlyAtPageBoundaryWithoutTrailingBreak()
    {
        var output = MicroStarPrintPagination.Prepare(
            ["one", "two", "three", "four", "five"], 2);
        Assert.Equal(new[] { "one", "two", "\f", "three", "four", "\f", "five" }, output);
    }

    [Fact]
    public void EmptyLinesCountAsPrintedRows()
    {
        var output = MicroStarPrintPagination.Prepare(["first", "", "last"], 2);
        Assert.Equal(new[] { "first", "", "\f", "last" }, output);
    }

    [Fact]
    public void ExactPageLengthAndEmptyInputNeverAppendBlankPage()
    {
        Assert.Equal(new[] { "a", "b" },
            MicroStarPrintPagination.Prepare(["a", "b"], 2));
        Assert.Empty(MicroStarPrintPagination.Prepare([], 2));
    }

    [Fact]
    public void InvalidPageLengthIsRejectedBeforeProcessing()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            MicroStarPrintPagination.Prepare(["x"], 0));
    }

    [Fact]
    public void SourceChangesCannotModifyPreparedOutput()
    {
        var lines = new List<string> { "original" };
        var prepared = MicroStarPrintPagination.Prepare(lines, 2);
        lines[0] = "edited";
        Assert.Equal("original", Assert.Single(prepared));
    }
}
