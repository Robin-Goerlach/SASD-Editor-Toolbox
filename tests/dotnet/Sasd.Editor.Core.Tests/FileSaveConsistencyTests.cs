using System.Text;
using Sasd.Editor.Editing;
using Sasd.Editor.IO;
using Sasd.Editor.Model;
using Xunit;

namespace Sasd.Editor.Core.Tests;

/// <summary>
/// Prevents loss of the dirty indicator when a host edits an open document
/// while its previous snapshot is still waiting for file-system I/O.
/// </summary>
public sealed class FileSaveConsistencyTests
{
    [Fact]
    public async Task Save_EditingDuringWrite_KeepsNewerTextDirty()
    {
        var codec = new PausingWriteCodec();
        var session = new EditorSession(fileCodec: codec);
        var first = session.CreateDocument("before");
        var linked = session.LinkWindow(first);
        session.SetCurrentWindow(first.WindowId);
        var document = first.Document;
        var path = Path.Combine(Path.GetTempPath(), $"sasd-pending-{Guid.NewGuid():N}.txt");

        // Writing is deliberately suspended after the snapshot was captured.
        var save = session.Files.SaveCurrentWindowAsync(path).AsTask();
        await codec.Started;

        session.SetCurrentWindow(linked.WindowId);
        session.Engine.InsertText("new ");
        Assert.Equal("new before", document.Buffer.GetLine(0).Text);
        Assert.True(document.IsDirty);

        codec.Complete();
        Assert.True(await save);

        // Disk/provider received the old snapshot; the newer linked-window
        // edit still needs saving even though I/O completed successfully.
        Assert.Equal("before", Assert.Single(codec.WrittenLines!).Text);
        Assert.Equal("new before", document.Buffer.GetLine(0).Text);
        Assert.Equal(Path.GetFullPath(path), document.FilePath);
        Assert.True(document.IsDirty);
        Assert.Equal(1, document.Version);
        Assert.Equal(1, session.Undo.Count);
    }

    [Fact]
    public async Task Save_WithoutInterveningEdit_MarksExactlyWrittenSnapshotClean()
    {
        var codec = new PausingWriteCodec();
        var session = new EditorSession(fileCodec: codec);
        var window = session.CreateDocument("old");
        session.Engine.InsertText("new ");
        Assert.True(window.Document.IsDirty);
        var path = Path.Combine(Path.GetTempPath(), $"sasd-stable-{Guid.NewGuid():N}.txt");

        var save = session.Files.SaveCurrentWindowAsync(path).AsTask();
        await codec.Started;
        codec.Complete();

        Assert.True(await save);
        Assert.Equal("new old", Assert.Single(codec.WrittenLines!).Text);
        Assert.False(window.Document.IsDirty);
        Assert.Equal(Path.GetFullPath(path), window.Document.FilePath);
        Assert.Equal(1, window.Document.Version);
    }

    [Fact]
    public void MarkSnapshotSaved_RejectsDifferentContentEvenWithMatchingVersion()
    {
        var session = new EditorSession();
        var window = session.CreateDocument("original");
        var document = window.Document;
        var savedLines = document.Buffer.Snapshot();
        var oldVersion = document.Version;

        // A custom ITextBuffer or a direct model writer may change content
        // without calling MarkChanged. Comparing version alone is insufficient.
        document.Buffer.ReplaceLine(0, "different");
        Assert.False(document.MarkSnapshotSaved(oldVersion, savedLines));
        Assert.Equal("different", document.Buffer.GetLine(0).Text);
    }

    [Fact]
    public async Task DirectWriteFile_DoesNotResetDirtyOrAssociateAFileName()
    {
        var codec = new PausingWriteCodec();
        var session = new EditorSession(fileCodec: codec);
        var window = session.CreateDocument("before");
        session.Engine.InsertText("new ");
        Assert.True(window.Document.IsDirty);
        Assert.Null(window.Document.FilePath);

        var write = session.Files.WriteCurrentWindowAsync("portable-target.txt").AsTask();
        await codec.Started;
        codec.Complete();
        await write;

        // Historical direct Write File is not modern Save As. It must not
        // acknowledge the persisted snapshot or silently rename the stream.
        Assert.Equal("new before", Assert.Single(codec.WrittenLines!).Text);
        Assert.True(window.Document.IsDirty);
        Assert.Null(window.Document.FilePath);
        Assert.Equal(1, session.Undo.Count);
    }

    [Fact]
    public async Task ModernUtf8Storage_PreservesCrlfAndAcknowledgesWrittenSnapshot()
    {
        var path = Path.Combine(Path.GetTempPath(), $"sasd-utf8-{Guid.NewGuid():N}.txt");
        try
        {
            await File.WriteAllTextAsync(path, "grün\r\nalt\r\n", new UTF8Encoding(false));
            var storage = new FileTextStorage();
            var document = await storage.LoadAsync(path);

            Assert.Equal("\r\n", document.NewLine);
            Assert.Equal(3, document.Buffer.LineCount);
            Assert.False(document.IsDirty);

            document.Buffer.ReplaceLine(1, "neu");
            document.MarkChanged();
            await storage.SaveAsync(document);

            Assert.Equal("grün\r\nneu\r\n",
                await File.ReadAllTextAsync(path, Encoding.UTF8));
            Assert.False(document.IsDirty);
            Assert.Equal(1, document.Version);
            Assert.Equal(Path.GetFullPath(path), document.FilePath);
        }
        finally
        {
            File.Delete(path);
        }
    }

    private sealed class PausingWriteCodec : IEditorFileCodec
    {
        private readonly TaskCompletionSource _started =
            new(TaskCreationOptions.RunContinuationsAsynchronously);
        private readonly TaskCompletionSource _complete =
            new(TaskCreationOptions.RunContinuationsAsynchronously);

        public Task Started => _started.Task;
        public IReadOnlyList<EditorLineSnapshot>? WrittenLines { get; private set; }

        public void Complete() => _complete.TrySetResult();

        public ValueTask<IReadOnlyList<EditorLineSnapshot>> ReadAsync(
            string path,
            CancellationToken cancellationToken = default) =>
            ValueTask.FromResult<IReadOnlyList<EditorLineSnapshot>>(
                Array.Empty<EditorLineSnapshot>());

        public async ValueTask WriteAsync(
            string path,
            IReadOnlyList<EditorLineSnapshot> lines,
            CancellationToken cancellationToken = default)
        {
            WrittenLines = lines.ToArray();
            _started.TrySetResult();
            await _complete.Task.WaitAsync(cancellationToken);
        }
    }
}
