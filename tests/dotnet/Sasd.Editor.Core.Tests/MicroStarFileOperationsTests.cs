using Sasd.Editor.FirstEd.Sample;
using Xunit;

namespace Sasd.Editor.Core.Tests;

/// <summary>
/// Real filesystem regression tests, isolated in temporary directories.
/// Particularly important: failed operations must leave the source intact.
/// </summary>
public sealed class MicroStarFileOperationsTests
{
    [Fact]
    public void CopyPreservesSourceAndRefusesExistingDestination()
    {
        var root = NewDirectory();
        try
        {
            var source = Path.Combine(root, "source.txt");
            var target = Path.Combine(root, "target.txt");
            File.WriteAllText(source, "original");
            MicroStarFileOperations.Copy(source, target);
            Assert.Equal("original", File.ReadAllText(source));
            Assert.Equal("original", File.ReadAllText(target));

            File.WriteAllText(source, "changed");
            Assert.Throws<IOException>(() => MicroStarFileOperations.Copy(source, target));
            Assert.Equal("original", File.ReadAllText(target));
            Assert.Equal("changed", File.ReadAllText(source));
        }
        finally { Directory.Delete(root, recursive: true); }
    }

    [Fact]
    public void RenameMovesFileWithoutOverwritingDestination()
    {
        var root = NewDirectory();
        try
        {
            var source = Path.Combine(root, "old.txt");
            var target = Path.Combine(root, "new.txt");
            File.WriteAllText(source, "first");
            File.WriteAllText(target, "protected");
            Assert.Throws<IOException>(() => MicroStarFileOperations.Rename(source, target));
            Assert.Equal("first", File.ReadAllText(source));
            Assert.Equal("protected", File.ReadAllText(target));

            File.Delete(target);
            MicroStarFileOperations.Rename(source, target);
            Assert.False(File.Exists(source));
            Assert.Equal("first", File.ReadAllText(target));
        }
        finally { Directory.Delete(root, recursive: true); }
    }

    [Fact]
    public void MissingSourceAndSelfTargetAreRejected()
    {
        var root = NewDirectory();
        try
        {
            var path = Path.Combine(root, "item.txt");
            Assert.Throws<FileNotFoundException>(() => MicroStarFileOperations.Copy(
                path, Path.Combine(root, "other.txt")));
            File.WriteAllText(path, "keep");
            Assert.Throws<IOException>(() => MicroStarFileOperations.Copy(path, path));
            Assert.Throws<IOException>(() => MicroStarFileOperations.Rename(path, path));
            Assert.Equal("keep", File.ReadAllText(path));
        }
        finally { Directory.Delete(root, recursive: true); }
    }

    [Fact]
    public void DirectorySourceIsNotTreatedAsAFile()
    {
        var root = NewDirectory();
        try
        {
            var child = Directory.CreateDirectory(Path.Combine(root, "folder"));
            Assert.Throws<FileNotFoundException>(() => MicroStarFileOperations.Copy(
                child.FullName, Path.Combine(root, "target")));
            Assert.True(Directory.Exists(child.FullName));
        }
        finally { Directory.Delete(root, recursive: true); }
    }

    private static string NewDirectory()
    {
        var path = Path.Combine(Path.GetTempPath(), "sasd-fileops-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(path);
        return path;
    }
}
