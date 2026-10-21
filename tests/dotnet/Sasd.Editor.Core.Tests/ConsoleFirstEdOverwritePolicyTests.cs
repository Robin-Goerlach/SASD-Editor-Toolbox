using Sasd.Editor.Commands;
using Sasd.Editor.Editing;
using Sasd.Editor.FirstEd.Sample;
using Sasd.Editor.Input;
using Xunit;

namespace Sasd.Editor.Core.Tests;

/// <summary>
/// Regression tests for FIRST-ED's host-level overwrite prompt. These are
/// headless: only the *explicitly confirmed* request reaches the dispatcher.
/// Disk fixtures prove cancellation does not overwrite a real existing file.
/// </summary>
public sealed class ConsoleFirstEdOverwritePolicyTests
{
    [Fact]
    public async Task UnnamedFirstSave_ExistingTargetNeedsExplicitYes()
    {
        var directory = CreateDirectory();
        try
        {
            var path = Path.Combine(directory, "existing.txt");
            await File.WriteAllTextAsync(path, "important original");
            var session = new EditorSession();
            var window = session.CreateDocument("new content");
            session.Engine.InsertText("modified ");
            var undoCount = session.Undo.Count;
            var lines = window.Document.Buffer.Snapshot();
            var binding = SaveBinding();
            var request = ConsoleFirstEdSavePromptPolicy.Resolve(
                binding, 10, window.Document.FilePath, path);
            Assert.NotNull(request);
            Assert.True(ConsoleFirstEdOverwritePolicy.RequiresConfirmation(
                request, File.Exists(request.Text)));

            // A user who presses Escape, Ctrl-U, Enter or NO never causes a
            // write: in the real host, null is not sent to the dispatcher.
            Assert.Null(ConsoleFirstEdOverwritePolicy.Confirm(request, null));
            Assert.Equal("important original", await File.ReadAllTextAsync(path));
            Assert.Equal(lines, window.Document.Buffer.Snapshot());
            Assert.True(window.Document.IsDirty);
            Assert.Null(window.Document.FilePath);
            Assert.Equal(undoCount, session.Undo.Count);

            var approved = ConsoleFirstEdOverwritePolicy.Confirm(request, " yes ");
            Assert.Same(request, approved);
            Assert.True(await new EditorCommandDispatcher(session).ExecuteAsync(approved));
            Assert.Equal("modified new content", await File.ReadAllTextAsync(path));
            Assert.Equal(Path.GetFullPath(path), window.Document.FilePath);
            Assert.False(window.Document.IsDirty);
            Assert.Equal(undoCount, session.Undo.Count);
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }

    [Fact]
    public async Task DirectWriteFile_RequiresConsentButPreservesDirtyAndName()
    {
        var directory = CreateDirectory();
        try
        {
            var path = Path.Combine(directory, "export.txt");
            await File.WriteAllTextAsync(path, "original export");
            var session = new EditorSession();
            var document = session.CreateDocument("export body").Document;
            session.Engine.InsertText("local ");
            var undoCount = session.Undo.Count;
            var request = EditorCommandPromptResolver.Resolve(
                new EditorCommandBinding(
                    EditorCommandId.WriteFile, EditorCommandArgumentKind.FilePath),
                pageSize: 12, firstResponse: path);
            Assert.NotNull(request);
            Assert.True(ConsoleFirstEdOverwritePolicy.RequiresConfirmation(
                request, File.Exists(request.Text)));

            Assert.Null(ConsoleFirstEdOverwritePolicy.Confirm(request, "NO"));
            Assert.Equal("original export", await File.ReadAllTextAsync(path));
            Assert.True(document.IsDirty);
            Assert.Null(document.FilePath);

            var approved = ConsoleFirstEdOverwritePolicy.Confirm(request, "YES");
            Assert.True(await new EditorCommandDispatcher(session).ExecuteAsync(approved));
            Assert.Equal("local export body", await File.ReadAllTextAsync(path));

            // Direct Write File remains distinct from modern Save As: it does
            // not acknowledge a disk snapshot or change the document's name.
            Assert.Null(document.FilePath);
            Assert.True(document.IsDirty);
            Assert.Equal(undoCount, session.Undo.Count);
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData("NO")]
    [InlineData("Y")]
    [InlineData("YES, PLEASE")]
    public void NonAffirmativeAnswerDoesNotReturnWriteCommand(string? answer)
    {
        var request = new EditorCommandRequest(
            EditorCommandId.SaveFile, Text: "existing.txt", PageSize: 17);
        Assert.Null(ConsoleFirstEdOverwritePolicy.Confirm(request, answer));
    }

    [Fact]
    public void ConfirmationPreservesOriginalFilenameAndOtherArguments()
    {
        var request = new EditorCommandRequest(
            EditorCommandId.WriteFile, Text: "my export.txt", PageSize: 23);
        var result = ConsoleFirstEdOverwritePolicy.Confirm(request, "  yEs  ");
        Assert.Same(request, result);
        Assert.Equal("my export.txt", result!.Text);
        Assert.Equal(23, result.PageSize);
    }

    [Fact]
    public void OnlyNamedOutputCommandsTriggerOverwriteConfirmation()
    {
        var read = new EditorCommandRequest(EditorCommandId.ReadFile, Text: "input.txt");
        var unnamedSave = new EditorCommandRequest(EditorCommandId.SaveFile);
        var directWrite = new EditorCommandRequest(EditorCommandId.WriteFile, Text: "export.txt");
        var namedSave = new EditorCommandRequest(EditorCommandId.SaveFile, Text: "file.txt");

        Assert.False(ConsoleFirstEdOverwritePolicy.RequiresConfirmation(read, true));
        Assert.False(ConsoleFirstEdOverwritePolicy.RequiresConfirmation(unnamedSave, true));
        Assert.False(ConsoleFirstEdOverwritePolicy.RequiresConfirmation(namedSave, false));
        Assert.True(ConsoleFirstEdOverwritePolicy.RequiresConfirmation(namedSave, true));
        Assert.True(ConsoleFirstEdOverwritePolicy.RequiresConfirmation(directWrite, true));
        Assert.Throws<ArgumentException>(
            () => ConsoleFirstEdOverwritePolicy.Confirm(unnamedSave, "YES"));
        Assert.Throws<ArgumentException>(
            () => ConsoleFirstEdOverwritePolicy.Confirm(read, "YES"));
    }

    [Fact]
    public async Task AlreadyAssociatedSaveNeedsNoSecondConfirmation()
    {
        var directory = CreateDirectory();
        try
        {
            var path = Path.Combine(directory, "document.txt");
            await File.WriteAllTextAsync(path, "earlier");

            var session = new EditorSession();
            var window = session.CreateDocument("current");
            var first = ConsoleFirstEdSavePromptPolicy.Resolve(
                SaveBinding(), 9, window.Document.FilePath, path);
            Assert.NotNull(first);
            Assert.True(await new EditorCommandDispatcher(session).ExecuteAsync(first));
            session.Engine.InsertText("updated ");

            var ordinarySave = ConsoleFirstEdSavePromptPolicy.Resolve(
                SaveBinding(), 9, window.Document.FilePath, null);
            Assert.NotNull(ordinarySave);
            Assert.Null(ordinarySave.Text);
            Assert.False(ConsoleFirstEdOverwritePolicy.RequiresConfirmation(
                ordinarySave, true));
            Assert.True(await new EditorCommandDispatcher(session)
                .ExecuteAsync(ordinarySave));
            Assert.Equal("updated current", await File.ReadAllTextAsync(path));
            Assert.False(window.Document.IsDirty);
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }

    private static EditorCommandBinding SaveBinding()
    {
        var map = new FirstEdKeyMap();
        Assert.Equal(EditorInputActionKind.PrefixPending,
            map.Translate(EditorKeyStroke.Control('K')).Kind);
        var action = map.Translate(EditorKeyStroke.ForCharacter('S'));
        Assert.NotNull(action.Binding);
        Assert.Equal(EditorCommandId.SaveFile, action.Binding.CommandId);
        return action.Binding;
    }

    private static string CreateDirectory()
    {
        var directory = Path.Combine(
            Path.GetTempPath(), $"sasd-overwrite-host-{Guid.NewGuid():N}");
        Directory.CreateDirectory(directory);
        return directory;
    }
}
