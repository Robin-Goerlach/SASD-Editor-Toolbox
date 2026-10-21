using System.Text;
using Sasd.Editor.Commands;
using Sasd.Editor.Editing;
using Sasd.Editor.IO;
using Sasd.Editor.Model;
using Xunit;

namespace Sasd.Editor.Core.Tests;

/// <summary>
/// Executable V1 consumer acceptance from ANOTHER .NET assembly.
///
/// Intentionally limited to public EditorSession, EditorCommandDispatcher,
/// EditorEngine, FileTextStorage, IEditorFileCodec and model APIs: no direct
/// edits to internal undo/topology state, no reflection and no demo-only
/// shortcuts. A WinForms/WPF/console integrator can follow the same paths.
/// </summary>
/// <remarks>
/// These tests complement isolated commands and custom test codecs. They
/// check *composition*: the editor file formats remain independent,
/// public dispatch is safe on invalid input, a document can be shown by
/// multiple linked views, and Save/Undo never misrepresent on-disk bytes.
/// All filesystem data is private to each test and cleaned up afterwards.
/// </remarks>
public sealed class PublicEditorApiAcceptanceTests
{
    [Fact]
    public async Task ExternalHostCanLoadUtf8EditAndSaveThroughDocumentApi()
    {
        await WithDirectory(async directory =>
        {
            var file = Path.Combine(directory, "unicode.txt");
            await File.WriteAllTextAsync(file, "Grüße\r\nzweite",
                new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));

            // The modern whole-document provider owns UTF-8 and newline
            // semantics. The editor session only owns the *view*.
            var storage = new FileTextStorage();
            var loaded = await storage.LoadAsync(file);
            var session = new EditorSession();
            var window = session.CreateWindow(loaded);

            Assert.Equal("\r\n", loaded.NewLine);
            Assert.False(loaded.IsDirty);
            Assert.Equal(Path.GetFullPath(file), loaded.FilePath);

            var dispatcher = new EditorCommandDispatcher(session);
            Assert.True(await dispatcher.ExecuteAsync(
                new(EditorCommandId.InsertText, Text: "NEU ")));
            Assert.Equal("NEU Grüße", window.Document.Buffer.GetLine(0).Text);
            Assert.True(loaded.IsDirty);
            Assert.Equal(1, session.Undo.Count);

            await storage.SaveAsync(loaded);

            // Save persists the edited snapshot without erasing Undo.
            Assert.Equal("NEU Grüße\r\nzweite",
                await File.ReadAllTextAsync(file, Encoding.UTF8));
            Assert.False(loaded.IsDirty);
            Assert.Equal(1, session.Undo.Count);

            // The public storage API can construct another independent
            // model from exactly those bytes. No sample-host API is needed.
            var reopened = await storage.LoadAsync(file);
            Assert.Equal(new[] { "NEU Grüße", "zweite" },
                reopened.Buffer.Snapshot().Select(line => line.Text));
            Assert.Equal("\r\n", reopened.NewLine);
            Assert.False(reopened.IsDirty);
        });
    }

    [Fact]
    public async Task HistoricalWriteAndSaveRemainDifferentPublicCommands()
    {
        await WithDirectory(async directory =>
        {
            var writePath = Path.Combine(directory, "direct.txt");
            var savePath = Path.Combine(directory, "saved.txt");
            var session = new EditorSession();
            var window = session.CreateDocument("alpha\nbeta");
            var dispatcher = new EditorCommandDispatcher(session);
            Assert.True(await dispatcher.ExecuteAsync(
                new(EditorCommandId.InsertText, Text: "X")));
            var originalUndoCount = session.Undo.Count;

            // EditFileWrite writes the snapshot but deliberately does NOT
            // adopt a filename or clear Dirty. That is a different public
            // command from modern SaveFile, even when both use the same
            // historical 8-bit codec under the hood.
            Assert.True(await dispatcher.ExecuteAsync(
                new(EditorCommandId.WriteFile, Text: writePath)));
            Assert.Equal(Encoding.Latin1.GetBytes("Xalpha\r\nbeta"),
                await File.ReadAllBytesAsync(writePath));
            Assert.Null(window.Document.FilePath);
            Assert.True(window.Document.IsDirty);
            Assert.Equal(originalUndoCount, session.Undo.Count);

            Assert.True(await dispatcher.ExecuteAsync(
                new(EditorCommandId.SaveFile, Text: savePath)));
            Assert.Equal(Encoding.Latin1.GetBytes("Xalpha\r\nbeta"),
                await File.ReadAllBytesAsync(savePath));
            Assert.Equal(Path.GetFullPath(savePath), window.Document.FilePath);
            Assert.False(window.Document.IsDirty);
            Assert.Equal(originalUndoCount, session.Undo.Count);
        });
    }

    [Fact]
    public async Task PublicSaveRejectionPreservesDiskContentAndEditorHistory()
    {
        await WithDirectory(async directory =>
        {
            var path = Path.Combine(directory, "existing.txt");
            var originalBytes = Encoding.Latin1.GetBytes("valuable original");
            await File.WriteAllBytesAsync(path, originalBytes);

            var session = new EditorSession();
            var window = session.CreateDocument("ASCII and \u0100");
            var dispatcher = new EditorCommandDispatcher(session);
            Assert.True(await dispatcher.ExecuteAsync(
                new(EditorCommandId.InsertText, Text: "X")));
            var before = window.Document.Buffer.Snapshot();
            var version = window.Document.Version;

            // U+0100 cannot be stored in the historical Latin-1 file
            // format. Dispatch must report failure without truncating the
            // existing target, associating its path, clearing Dirty, or
            // inserting a phantom Undo record.
            Assert.False(await dispatcher.ExecuteAsync(
                new(EditorCommandId.SaveFile, Text: path)));
            Assert.Equal(originalBytes, await File.ReadAllBytesAsync(path));
            Assert.Equal(before, window.Document.Buffer.Snapshot());
            Assert.Equal(version, window.Document.Version);
            Assert.Null(window.Document.FilePath);
            Assert.True(window.Document.IsDirty);
            Assert.Equal(1, session.Undo.Count);
            Assert.Equal(new[] { "existing.txt" },
                Directory.GetFiles(directory).Select(Path.GetFileName));
        });
    }

    [Fact]
    public async Task PublicReadFailureLeavesLinkedViewsAndUndoUntouched()
    {
        await WithDirectory(async directory =>
        {
            var session = new EditorSession();
            var original = session.CreateDocument("before\nafter");
            var linked = session.LinkWindow(original);
            original.Cursor = new TextPosition(0, 3);
            linked.Cursor = new TextPosition(1, 9);
            session.SetCurrentWindow(original.WindowId);
            var before = original.Document.Buffer.Snapshot();
            var dispatcher = new EditorCommandDispatcher(session);

            Assert.False(await dispatcher.ExecuteAsync(
                new(EditorCommandId.ReadFile,
                    Text: Path.Combine(directory, "missing-file.txt"))));

            Assert.Equal(before, original.Document.Buffer.Snapshot());
            Assert.Same(original.Document, linked.Document);
            Assert.Equal(new TextPosition(0, 3), original.Cursor);
            Assert.Equal(new TextPosition(1, 9), linked.Cursor);
            Assert.False(original.Document.IsDirty);
            Assert.Equal(0, original.Document.Version);
            Assert.Equal(0, session.Undo.Count);
        });
    }

    [Fact]
    public async Task PublicSaveWithCancellationLeavesPreviousDiskBytesUntouched()
    {
        await WithDirectory(async directory =>
        {
            var path = Path.Combine(directory, "target.txt");
            var original = Encoding.Latin1.GetBytes("previous");
            await File.WriteAllBytesAsync(path, original);
            var session = new EditorSession();
            var window = session.CreateDocument("new");
            session.Engine.InsertText("X");
            var snapshot = window.Document.Buffer.Snapshot();
            using var source = new CancellationTokenSource();
            source.Cancel();

            // A cancellation *before* any staging is an exceptional
            // operation at the direct async service API, not a successful
            // Save. A UI host may convert it to a user-facing cancellation
            // message; it must not claim that the document reached disk.
            await Assert.ThrowsAnyAsync<OperationCanceledException>(
                async () => await session.Files.SaveCurrentWindowAsync(
                    path, source.Token));

            Assert.Equal(original, await File.ReadAllBytesAsync(path));
            Assert.Equal(snapshot, window.Document.Buffer.Snapshot());
            Assert.Null(window.Document.FilePath);
            Assert.True(window.Document.IsDirty);
            Assert.Equal(1, session.Undo.Count);
            Assert.Single(Directory.GetFiles(directory));
        });
    }

    private static async Task WithDirectory(Func<string, Task> test)
    {
        var directory = Path.Combine(
            Path.GetTempPath(), $"sasd-api-consumer-{Guid.NewGuid():N}");
        Directory.CreateDirectory(directory);
        try
        {
            await test(directory);
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }
}
