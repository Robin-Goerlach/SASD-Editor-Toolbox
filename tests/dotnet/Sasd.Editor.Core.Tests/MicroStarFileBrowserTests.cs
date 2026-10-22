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
