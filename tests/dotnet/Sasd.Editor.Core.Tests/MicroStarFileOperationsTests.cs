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

    [Fact]
    public void CopyAndRenameRefuseLinkedDestinationWithoutChangingItsTarget()
    {
        var root = NewDirectory();
        try
        {
            var source = Path.Combine(root, "source.txt");
            var protectedTarget = Path.Combine(root, "protected.txt");
            var destinationLink = Path.Combine(root, "destination-link.txt");
            File.WriteAllText(source, "incoming content");
            File.WriteAllText(protectedTarget, "must survive");

            try
            {
                File.CreateSymbolicLink(destinationLink, protectedTarget);
            }
            catch (Exception error) when (error is UnauthorizedAccessException
                or PlatformNotSupportedException or IOException)
            {
                // Creating links is not available to every Windows runner.
                // Linux generally exercises this exact filesystem case.
                return;
            }

            // A destination that is a link is already occupied even when
            // the editor user sees it as just another filename. The
            // no-overwrite contract must apply to the directory entry AND
            // to the unrelated file that the link resolves to.
            Assert.ThrowsAny<IOException>(() =>
                MicroStarFileOperations.Copy(source, destinationLink));
            Assert.ThrowsAny<IOException>(() =>
                MicroStarFileOperations.Rename(source, destinationLink));

            // Rejecting the destination must leave all three entries
            // intact, including the source used by a subsequent retry.
            Assert.Equal("incoming content", File.ReadAllText(source));
            Assert.Equal("must survive", File.ReadAllText(protectedTarget));
            Assert.NotNull(new FileInfo(destinationLink).LinkTarget);
        }
        finally { Directory.Delete(root, recursive: true); }
    }

    [Fact]
    public void CopyAndRenameRefuseDanglingSymbolicLinkDestination()
    {
        var root = NewDirectory();
        try
        {
            var source = Path.Combine(root, "source.txt");
            var missingTarget = Path.Combine(root, "missing-target.txt");
            var destinationLink = Path.Combine(root, "dangling-link.txt");
            File.WriteAllText(source, "keep original bytes");

            try
            {
                File.CreateSymbolicLink(destinationLink, missingTarget);
            }
            catch (Exception error) when (error is UnauthorizedAccessException
                or PlatformNotSupportedException or IOException)
            {
                // Windows link creation can require additional privileges;
                // the Linux runner normally covers this filesystem case.
                return;
            }

            // A dangling link is still an occupied filesystem entry.
            // File.Exists is NOT a portable test of link existence: some
            // .NET/OS combinations report true for the entry even when its
            // target is absent. Inspect the link itself and keep the safety
            // assertions focused on the real operation contract instead.
            // Copy/Move with overwrite:false must reject the existing link.
            Assert.False(File.Exists(missingTarget));
            Assert.NotNull(new FileInfo(destinationLink).LinkTarget);
            Assert.ThrowsAny<IOException>(() =>
                MicroStarFileOperations.Copy(source, destinationLink));
            Assert.ThrowsAny<IOException>(() =>
                MicroStarFileOperations.Rename(source, destinationLink));

            // Failed operations must preserve both the original file and
            // the dangling link; they may not create its missing target.
            Assert.Equal("keep original bytes", File.ReadAllText(source));
            Assert.False(File.Exists(missingTarget));
            Assert.NotNull(new FileInfo(destinationLink).LinkTarget);
        }
        finally { Directory.Delete(root, recursive: true); }
    }

    [Fact]
    public void DeleteRemovesOnlyTheRequestedRegularFile()
    {
        var root = NewDirectory();
        try
        {
            var kept = Path.Combine(root, "keep.txt");
            var removed = Path.Combine(root, "remove.txt");
            File.WriteAllText(kept, "must survive");
            File.WriteAllText(removed, "disposable");

            // Confirmation belongs to the UI, not this service. This test
            // calls the operation directly after intentionally selecting
            // an expendable path; unrelated siblings must remain unchanged.
            MicroStarFileOperations.Delete(removed);
            Assert.False(File.Exists(removed));
            Assert.Equal("must survive", File.ReadAllText(kept));
        }
        finally { Directory.Delete(root, recursive: true); }
    }

    [Fact]
    public void DeleteNeverRecursivelyDeletesADirectory()
    {
        var root = NewDirectory();
        try
        {
            var directory = Directory.CreateDirectory(Path.Combine(root, "folder"));
            var child = Path.Combine(directory.FullName, "protected.txt");
            File.WriteAllText(child, "must survive");

            Assert.Throws<IOException>(() =>
                MicroStarFileOperations.Delete(directory.FullName));
            Assert.Equal("must survive", File.ReadAllText(child));
        }
        finally { Directory.Delete(root, recursive: true); }
    }

    [Fact]
    public void DeleteRejectsSymbolicLinkWithoutTouchingItsTarget()
    {
        var root = NewDirectory();
        try
        {
            var target = Path.Combine(root, "target.txt");
            var link = Path.Combine(root, "link.txt");
            File.WriteAllText(target, "protected");
            try
            {
                File.CreateSymbolicLink(link, target);
            }
            catch (Exception error) when (error is UnauthorizedAccessException
                or PlatformNotSupportedException or IOException)
            {
                // A Windows runner without symlink privileges cannot create
                // this fixture. Native Linux typically supports it.
                return;
            }

            // A symbolic link must not count as an ordinary source file,
            // even if its target exists and can be read successfully.
            Assert.Throws<IOException>(() => MicroStarFileOperations.Delete(link));
            Assert.Equal("protected", File.ReadAllText(target));
            Assert.True(File.Exists(link));
        }
        finally { Directory.Delete(root, recursive: true); }
    }

    [Fact]
    public void CopyAndRenamePreserveExactLegacyBytesWithoutTextConversion()
    {
        var root = NewDirectory();
        try
        {
            var source = Path.Combine(root, "legacy-source.txt");
            var copied = Path.Combine(root, "legacy-copy.txt");
            var renamed = Path.Combine(root, "legacy-moved.txt");

            // MicroStar's file manager operates on files, not decoded editor
            // buffers. In particular, 0x8D is a historical soft-line marker
            // and must not be normalized to a host newline or UTF-8 sequence.
            // Include a high-byte character and both kinds of line endings
            // to make accidental text conversion immediately visible.
            var original = new byte[]
            {
                0x41, 0x8D, 0xE9, 0x0D, 0x0A, 0x42, 0x0A, 0x43
            };
            File.WriteAllBytes(source, original);

            MicroStarFileOperations.Copy(source, copied);
            Assert.Equal(original, File.ReadAllBytes(source));
            Assert.Equal(original, File.ReadAllBytes(copied));

            // Renaming the copied file must preserve the physical byte stream
            // and leave the unrelated original source intact. This test does
            // not depend on a particular filesystem's inode or rename model.
            MicroStarFileOperations.Rename(copied, renamed);
            Assert.False(File.Exists(copied));
            Assert.Equal(original, File.ReadAllBytes(renamed));
            Assert.Equal(original, File.ReadAllBytes(source));
        }
        finally { Directory.Delete(root, recursive: true); }
    }

    [Fact]
    public void RenameMissingSourceDoesNotCreateDestination()
    {
        var root = NewDirectory();
        try
        {
            var missing = Path.Combine(root, "missing.txt");
            var destination = Path.Combine(root, "destination.txt");

            // A file may disappear between its selection in the browser
            // and confirmation in the terminal. Reject this condition
            // without creating an empty replacement or changing siblings.
            var preserved = Path.Combine(root, "preserved.txt");
            File.WriteAllText(preserved, "original");
            Assert.Throws<FileNotFoundException>(() =>
                MicroStarFileOperations.Rename(missing, destination));

            Assert.False(File.Exists(destination));
            Assert.Equal("original", File.ReadAllText(preserved));
        }
        finally { Directory.Delete(root, recursive: true); }
    }

    [Fact]
    public void CopyAndRenameNeverUseAnExistingDirectoryAsDestination()
    {
        var root = NewDirectory();
        try
        {
            var source = Path.Combine(root, "source.txt");
            var directory = Path.Combine(root, "destination");
            var child = Path.Combine(directory, "protected.txt");
            Directory.CreateDirectory(directory);
            File.WriteAllText(source, "source content");
            File.WriteAllText(child, "protected content");

            // The target is intentionally a directory, not a filename.
            // Neither operation may reinterpret it as a request to choose
            // a filename inside the folder or to replace its contents.
            Assert.Throws<IOException>(() =>
                MicroStarFileOperations.Copy(source, directory));
            Assert.Throws<IOException>(() =>
                MicroStarFileOperations.Rename(source, directory));

            Assert.Equal("source content", File.ReadAllText(source));
            Assert.Equal("protected content", File.ReadAllText(child));
            Assert.True(Directory.Exists(directory));
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
