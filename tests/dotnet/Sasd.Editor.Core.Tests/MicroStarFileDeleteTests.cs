using Sasd.Editor.FirstEd.Sample;
using Xunit;

namespace Sasd.Editor.Core.Tests;

/// <summary>
/// Real temporary-filesystem tests of the deliberately narrow delete contract.
/// Denied prompts are handled in the UI; the service has no hidden approval
/// or fallback that can accidentally remove entire directory trees.
/// </summary>
public sealed class MicroStarFileDeleteTests
{
    [Fact]
    public void DeletesOnlySelectedRegularFile()
    {
        var root = NewFolder();
        try
        {
            var target = Path.Combine(root, "target.txt");
            var other = Path.Combine(root, "other.txt");
            File.WriteAllText(target, "remove");
            File.WriteAllText(other, "keep");

            MicroStarFileOperations.Delete(target);

            Assert.False(File.Exists(target));
            Assert.Equal("keep", File.ReadAllText(other));
            Assert.True(Directory.Exists(root));
        }
        finally { Directory.Delete(root, recursive: true); }
    }

    [Fact]
    public void DirectoryCannotBeDeletedByFileCommand()
    {
        var root = NewFolder();
        try
        {
            var child = Directory.CreateDirectory(Path.Combine(root, "child"));
            File.WriteAllText(Path.Combine(child.FullName, "important.txt"), "keep");
            Assert.Throws<IOException>(() => MicroStarFileOperations.Delete(child.FullName));
            Assert.True(File.Exists(Path.Combine(child.FullName, "important.txt")));
        }
        finally { Directory.Delete(root, recursive: true); }
    }

    [Fact]
    public void MissingPathDoesNotCreateOrDeleteNeighboringFiles()
    {
        var root = NewFolder();
        try
        {
            var keep = Path.Combine(root, "keep.txt");
            File.WriteAllText(keep, "keep");
            Assert.Throws<FileNotFoundException>(() =>
                MicroStarFileOperations.Delete(Path.Combine(root, "missing.txt")));
            Assert.Equal("keep", File.ReadAllText(keep));
        }
        finally { Directory.Delete(root, recursive: true); }
    }

    private static string NewFolder()
    {
        var root = Path.Combine(Path.GetTempPath(), "sasd-delete-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        return root;
    }
}
