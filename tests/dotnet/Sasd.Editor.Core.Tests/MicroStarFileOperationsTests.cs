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

    [Fact]
    public void LinkedSourceCannotBeCopiedOrRenamed()
    {
        var root = NewDirectory();
        try
        {
            var target = Path.Combine(root, "real.txt");
            var link = Path.Combine(root, "link.txt");
            var copy = Path.Combine(root, "copy.txt");
            var renamed = Path.Combine(root, "renamed.txt");
            File.WriteAllText(target, "protected target");

            try
            {
                File.CreateSymbolicLink(link, target);
            }
            catch (Exception error) when (error is UnauthorizedAccessException
                or PlatformNotSupportedException or IOException)
            {
                // Windows may require Developer Mode or a specific permission
                // to create a symlink. Do not fail unrelated CI environments;
                // native Linux runners normally exercise the actual link.
                return;
            }

            // File.Exists follows the link, so only an explicit attributes
            // check protects the regular-file-only contract. Reject both
            // copy and rename *before* creating or moving a destination.
            Assert.True(File.Exists(link));
            Assert.Throws<IOException>(() =>
                MicroStarFileOperations.Copy(link, copy));
            Assert.Throws<IOException>(() =>
                MicroStarFileOperations.Rename(link, renamed));
            Assert.False(File.Exists(copy));
            Assert.False(File.Exists(renamed));
            Assert.Equal("protected target", File.ReadAllText(target));
            Assert.True(File.Exists(link));
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
