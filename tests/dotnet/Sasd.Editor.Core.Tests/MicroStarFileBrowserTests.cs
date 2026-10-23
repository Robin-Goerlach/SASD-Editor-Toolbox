using Sasd.Editor.FirstEd.Sample;
using Xunit;

namespace Sasd.Editor.Core.Tests;

/// <summary>
/// Real temporary-directory tests for browser navigation, while text editing
/// and the physical terminal remain out of scope for this UI controller.
/// </summary>
public sealed class MicroStarFileBrowserTests
{
    [Fact]
    public void EnterDirectoryThenFileReturnsStableFullPath()
    {
        var root = Path.Combine(Path.GetTempPath(), "sasd-browser-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        try
        {
            var sub = Directory.CreateDirectory(Path.Combine(root, "Docs"));
            var file = Path.Combine(sub.FullName, "readme.txt");
            File.WriteAllText(file, "Test");

            var browser = new MicroStarFileBrowser(root, 3);
            Assert.Equal("Docs", browser.VisibleEntries[0].Name);
            Assert.Equal(MicroStarBrowserResultKind.Redraw,
                browser.Handle(ConsoleKey.Enter).Kind);
            Assert.Equal(sub.FullName, browser.CurrentDirectory);
            var chosen = browser.Handle(ConsoleKey.Enter);
            Assert.Equal(MicroStarBrowserResultKind.FileChosen, chosen.Kind);
            Assert.Equal(file, chosen.Path);
            Assert.Equal("Test", File.ReadAllText(file));
        }
        finally { Directory.Delete(root, true); }
    }

    [Fact]
    public void BackspaceReturnsToParentAndDoesNotModifyFilesystem()
    {
        var root = Path.Combine(Path.GetTempPath(), "sasd-browser-" + Guid.NewGuid().ToString("N"));
        var child = Directory.CreateDirectory(Path.Combine(root, "Nested"));
        try
        {
            var browser = new MicroStarFileBrowser(child.FullName, 3);
            Assert.Equal(MicroStarBrowserResultKind.Redraw,
                browser.Handle(ConsoleKey.Backspace).Kind);
            Assert.Equal(root, browser.CurrentDirectory);
            Assert.True(Directory.Exists(child.FullName));
        }
        finally { Directory.Delete(root, true); }
    }

    [Fact]
    public void EmptyDirectoryCanCancelButCannotChooseFile()
    {
        var root = Path.Combine(Path.GetTempPath(), "sasd-browser-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        try
        {
            var browser = new MicroStarFileBrowser(root, 4);
            Assert.Null(browser.Selection.Selected);
            Assert.Equal(MicroStarBrowserResultKind.Ignored,
                browser.Handle(ConsoleKey.Enter).Kind);
            Assert.Equal(MicroStarBrowserResultKind.Cancelled,
                browser.Handle(ConsoleKey.Escape).Kind);
            Assert.Equal(MicroStarBrowserResultKind.Cancelled,
                browser.Handle(ConsoleKey.U, ConsoleModifiers.Control).Kind);
        }
        finally { Directory.Delete(root); }
    }

    [Fact]
    public void MissingStartDirectoryDoesNotCreateAnything()
    {
        var missing = Path.Combine(Path.GetTempPath(), "sasd-missing-" + Guid.NewGuid().ToString("N"));
        Assert.Throws<DirectoryNotFoundException>(() => new MicroStarFileBrowser(missing, 3));
        Assert.False(Directory.Exists(missing));
    }

    [Fact]
    public void LinkedDirectoryAndFileRemainVisibleButCannotBeOpened()
    {
        var root = Path.Combine(Path.GetTempPath(),
            "sasd-browser-links-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        try
        {
            var directory = Directory.CreateDirectory(Path.Combine(root, "real-dir"));
            var file = Path.Combine(root, "real-file.txt");
            File.WriteAllText(file, "protected");
            try
            {
                Directory.CreateSymbolicLink(Path.Combine(root, "linked-dir"), directory.FullName);
                File.CreateSymbolicLink(Path.Combine(root, "linked-file.txt"), file);
            }
            catch (Exception error) when (error is IOException
                or UnauthorizedAccessException or PlatformNotSupportedException)
            {
                // Symlink creation requires special privileges on some
                // Windows runners. Linux runners exercise this fixture.
                return;
            }

            var browser = new MicroStarFileBrowser(root, 10);
            var linked = browser.VisibleEntries.Where(entry => entry.IsLink).ToArray();
            Assert.Equal(2, linked.Length);

            // Selection navigates one entry at a time. Linked entries are
            // visible, but Enter may neither traverse them nor choose them
            // as file-open destinations, even when the targets are valid.
            for (var i = 0; i < browser.TotalEntries; i++)
            {
                var selected = browser.SelectedEntry!;
                if (selected.IsLink)
                {
                    Assert.Equal(MicroStarBrowserResultKind.Ignored,
                        browser.Handle(ConsoleKey.Enter).Kind);
                    Assert.Null(MicroStarFileActionPolicy.Resolve(
                        ConsoleKey.F5, ConsoleModifiers.None, selected));
                    Assert.Equal(root, browser.CurrentDirectory);
                }
                if (i < browser.TotalEntries - 1)
                    browser.Handle(ConsoleKey.DownArrow);
            }
            Assert.Equal("protected", File.ReadAllText(file));
            Assert.True(Directory.Exists(directory.FullName));
        }
        finally { Directory.Delete(root, recursive: true); }
    }

    [Fact]
    public void ArrowNavigationNeverChoosesFileByItself()
    {
        var root = Path.Combine(Path.GetTempPath(), "sasd-browser-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        try
        {
            File.WriteAllText(Path.Combine(root, "one.txt"), "1");
            File.WriteAllText(Path.Combine(root, "two.txt"), "2");
            var browser = new MicroStarFileBrowser(root, 1);
            Assert.Equal(MicroStarBrowserResultKind.Redraw,
                browser.Handle(ConsoleKey.DownArrow).Kind);
            Assert.Equal(1, browser.FirstVisible);
            Assert.Equal(MicroStarBrowserResultKind.Ignored,
                browser.Handle(ConsoleKey.Enter, ConsoleModifiers.Alt).Kind);
            Assert.Equal(MicroStarBrowserResultKind.FileChosen,
                browser.Handle(ConsoleKey.Enter).Kind);
        }
        finally { Directory.Delete(root, true); }
    }
}
