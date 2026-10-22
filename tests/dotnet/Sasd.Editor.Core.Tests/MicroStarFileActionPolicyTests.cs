using Sasd.Editor.FirstEd.Sample;
using Xunit;

namespace Sasd.Editor.Core.Tests;

public sealed class MicroStarFileActionPolicyTests
{
    [Theory]
    [InlineData(ConsoleKey.F5, 0)]
    [InlineData(ConsoleKey.F6, 1)]
    [InlineData(ConsoleKey.F8, 2)]
    public void OnlyExplicitFunctionKeysAllowRegularFileOperation(
        ConsoleKey key, int expected)
    {
        var entry = new MicroStarDirectoryEntry("/tmp/file", "file", false);
        Assert.Equal((MicroStarFileAction)expected, MicroStarFileActionPolicy.Resolve(key,
            ConsoleModifiers.None, entry));
        Assert.Null(MicroStarFileActionPolicy.Resolve(key,
            ConsoleModifiers.Control, entry));
        Assert.Null(MicroStarFileActionPolicy.Resolve(key,
            ConsoleModifiers.None, entry with { IsDirectory = true }));
        Assert.Null(MicroStarFileActionPolicy.Resolve(key,
            ConsoleModifiers.None, null));
        Assert.Null(MicroStarFileActionPolicy.Resolve(ConsoleKey.Enter,
            ConsoleModifiers.None, entry));
    }

    [Fact]
    public void RelativeTargetIsResolvedAgainstBrowserDirectory()
    {
        var root = Path.GetFullPath(Path.GetTempPath());
        Assert.Equal(Path.GetFullPath(Path.Combine(root, "new.txt")),
            MicroStarFileActionPolicy.ResolveDestination(root, "new.txt"));
    }

    [Fact]
    public void RefreshAfterCopyMakesFileVisible()
    {
        var folder = Path.Combine(Path.GetTempPath(), "sasd-copybrowser-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(folder);
        try
        {
            var source = Path.Combine(folder, "one.txt");
            var target = Path.Combine(folder, "two.txt");
            File.WriteAllText(source, "hello");
            var browser = new MicroStarFileBrowser(folder, 8);
            Assert.Single(browser.VisibleEntries);

            MicroStarFileOperations.Copy(source, target);
            browser.Refresh();

            Assert.Equal(2, browser.TotalEntries);
            Assert.Contains(browser.VisibleEntries, x => x.FullPath == target);
            Assert.Equal("hello", File.ReadAllText(source));
        }
        finally { Directory.Delete(folder, true); }
    }
}
