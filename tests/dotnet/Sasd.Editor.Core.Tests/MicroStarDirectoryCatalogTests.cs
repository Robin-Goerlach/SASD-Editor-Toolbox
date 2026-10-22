using Sasd.Editor.FirstEd.Sample;
using Xunit;

namespace Sasd.Editor.Core.Tests;

/// <summary>
/// Temporary-directory integration tests exercise the real platform's
/// read-only filesystem path, not a substitute in-memory sorting routine.
/// </summary>
public sealed class MicroStarDirectoryCatalogTests
{
    [Fact]
    public void CatalogListsOnlyDirectChildrenAndDirectoriesFirst()
    {
        var root = Path.Combine(Path.GetTempPath(), "sasd-microstar-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        try
        {
            Directory.CreateDirectory(Path.Combine(root, "Sub"));
            File.WriteAllText(Path.Combine(root, "zeta.txt"), "z");
            File.WriteAllText(Path.Combine(root, "Alpha.txt"), "a");
            File.WriteAllText(Path.Combine(root, "Sub", "nested.txt"), "hidden");

            var result = MicroStarDirectoryCatalog.Read(root);
            Assert.Equal(new[] { "Sub", "Alpha.txt", "zeta.txt" },
                result.Select(e => e.Name).ToArray());
            Assert.True(result[0].IsDirectory);
            Assert.DoesNotContain(result, e => e.Name == "nested.txt");

            var list = MicroStarDirectoryCatalog.CreateList(result, 2);
            Assert.Equal(result[0].FullPath, list.Selected?.Id);
            list.Handle(ConsoleKey.DownArrow);
            Assert.Equal(result[1].FullPath, list.Selected?.Id);
            Assert.StartsWith("[DIR] ", MicroStarDirectoryCatalog.CreateList(result, 2).Visible[0].Caption);
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    [Fact]
    public void MissingDirectoryIsReportedInsteadOfPretendingToBeEmpty()
    {
        var missing = Path.Combine(Path.GetTempPath(), "sasd-missing-" + Guid.NewGuid().ToString("N"));
        Assert.Throws<DirectoryNotFoundException>(() => MicroStarDirectoryCatalog.Read(missing));
    }

    [Fact]
    public void EmptyDirectoryCanBeDisplayedWithoutFabricatingSelection()
    {
        var root = Path.Combine(Path.GetTempPath(), "sasd-empty-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        try
        {
            var entries = MicroStarDirectoryCatalog.Read(root);
            Assert.Empty(entries);
            Assert.Null(MicroStarDirectoryCatalog.CreateList(entries, 5).Selected);
        }
        finally
        {
            Directory.Delete(root);
        }
    }
}
