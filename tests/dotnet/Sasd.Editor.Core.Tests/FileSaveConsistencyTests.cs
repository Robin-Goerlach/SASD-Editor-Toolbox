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
    public async Task Save_SwitchingToDifferentDocumentDuringWrite_DoesNotAlterActiveDocument()
    {
        var codec = new PausingWriteCodec();
        var session = new EditorSession(fileCodec: codec);
        var source = session.CreateDocument("source");
        session.Engine.InsertText("edited ");
        var other = session.CreateDocument("unrelated");
        session.SetCurrentWindow(source.WindowId);
        var path = Path.Combine(Path.GetTempPath(), $"sasd-origin-{Guid.NewGuid():N}.txt");

        // The pending save belongs to the initiating document, not to whatever
        // view becomes active while the codec is awaiting external I/O.
        var pending = session.Files.SaveCurrentWindowAsync(path).AsTask();
        await codec.Started;
        session.SetCurrentWindow(other.WindowId);
        codec.Complete();

        Assert.True(await pending);
        Assert.Same(other, session.CurrentWindow);
        Assert.Equal("edited source", Assert.Single(codec.WrittenLines!).Text);
        Assert.Equal(Path.GetFullPath(path), source.Document.FilePath);
        Assert.False(source.Document.IsDirty);
        Assert.Equal("unrelated", other.Document.Buffer.GetLine(0).Text);
        Assert.Null(other.Document.FilePath);
        Assert.False(other.Document.IsDirty);
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
    public async Task Save_ExternalCancellationDuringWrite_DoesNotAcknowledgeUnwrittenSnapshot()
    {
        using var cancellation = new CancellationTokenSource();
        var codec = new PausingWriteCodec();
        var session = new EditorSession(fileCodec: codec);
        var window = session.CreateDocument("original");
        session.Engine.InsertText("edited ");
        var document = window.Document;
        var previousLines = document.Buffer.Snapshot();
        var previousVersion = document.Version;
        var previousCursor = window.Cursor;
        var previousUndoCount = session.Undo.Count;
        var path = Path.Combine(Path.GetTempPath(), $"sasd-cancelled-{Guid.NewGuid():N}.txt");

        // Hold the codec before it can complete its write. An external
        // cancellation is control flow, not proof that the snapshot reached
        // durable storage; acknowledging it would silently lose the dirty
        // indicator and falsely associate the requested destination path.
        var pending = session.Files.SaveCurrentWindowAsync(
            path, cancellation.Token).AsTask();
        await codec.Started;
        cancellation.Cancel();

        // The test codec honors the token through WaitAsync; no delay, polling
        // or platform-specific filesystem permissions are needed.
        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            async () => await pending);

        Assert.Equal(previousLines, document.Buffer.Snapshot());
        Assert.Equal(previousVersion, document.Version);
        Assert.Equal(previousCursor, window.Cursor);
        Assert.Equal(previousUndoCount, session.Undo.Count);
        Assert.True(document.IsDirty);
        Assert.Null(document.FilePath);
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
    public async Task DirectWriteFile_ExternalCancellationPreservesDocumentState()
    {
        using var cancellation = new CancellationTokenSource();
        var codec = new PausingWriteCodec();
        var session = new EditorSession(fileCodec: codec);
        var window = session.CreateDocument("before");
        session.Engine.InsertText("new ");
        var document = window.Document;
        var originalLines = document.Buffer.Snapshot();
        var originalVersion = document.Version;
        var originalCursor = window.Cursor;
        var originalUndoCount = session.Undo.Count;

        // Unlike modern Save, historical direct Write File never acknowledges
        // a saved snapshot or changes the document identity. Cancellation must
        // preserve that invariant as well, even after the provider has received
        // an immutable copy of the outgoing text.
        var pending = session.Files.WriteCurrentWindowAsync(
            "cancelled-direct-write.txt", cancellation.Token).AsTask();
        await codec.Started;
        Assert.Equal("new before", Assert.Single(codec.WrittenLines!).Text);
        cancellation.Cancel();

        // The injected codec waits on the passed cancellation token. This
        // reproduces a slow provider deterministically without touching disk.
        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            async () => await pending);
        Assert.Equal(originalLines, document.Buffer.Snapshot());
        Assert.Equal(originalVersion, document.Version);
        Assert.Equal(originalCursor, window.Cursor);
        Assert.Equal(originalUndoCount, session.Undo.Count);
        Assert.True(document.IsDirty);
        Assert.Null(document.FilePath);
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

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Save_ProviderIoFailurePreservesDocumentAndFileAssociation(
        bool existingAssociation)
    {
        // Unlike a cancelled filename prompt, this failure happens AFTER
        // the user has authorized a Save and the file provider has been
        // called. Only a completed write may acknowledge a saved snapshot.
        // The injected failing codec reproduces I/O errors deterministically
        // without relying on OS-specific permissions or real user files.
        var codec = new FailingWriteCodec();
        var session = new EditorSession(fileCodec: codec);
        var window = session.CreateDocument("original");
        var document = window.Document;
        var attemptedPath = Path.Combine(Path.GetTempPath(),
            "sasd-io-failure-" + Guid.NewGuid().ToString("N") + ".txt");

        // An existing association is installed using the public model
        // acknowledgement method, not by mutating a read-only property.
        // This deliberately tests both Save As and Save to an existing name.
        string? previousPath = null;
        if (existingAssociation)
        {
            document.MarkSaved(attemptedPath);
            previousPath = document.FilePath;
        }
        session.Engine.InsertText("edited ");
        var previousLines = document.Buffer.Snapshot();
        var previousVersion = document.Version;
        var previousCursor = window.Cursor;
        var previousUndoCount = session.Undo.Count;

        await Assert.ThrowsAsync<IOException>(async () =>
            await session.Files.SaveCurrentWindowAsync(
                existingAssociation ? null : attemptedPath));

        Assert.Equal(1, codec.Attempts);
        Assert.Equal(previousLines, document.Buffer.Snapshot());
        Assert.Equal(previousVersion, document.Version);
        Assert.Equal(previousCursor, window.Cursor);
        Assert.Equal(previousUndoCount, session.Undo.Count);
        Assert.Equal(previousPath, document.FilePath);
        Assert.True(document.IsDirty);
    }

    [Fact]
    public async Task LegacySave_ToMissingParentDirectory_LeavesDocumentDirty()
    {
        // Exercise the REAL staged legacy file writer rather than an injected
        // codec. The parent folder is intentionally absent: the writer must
        // propagate its filesystem error and must not interpret an attempted
        // Save As as a successful document association.
        var root = Path.Combine(Path.GetTempPath(),
            "sasd-save-missing-" + Guid.NewGuid().ToString("N"));
        var target = Path.Combine(root, "absent", "editor.txt");
        try
        {
            Directory.CreateDirectory(root);
            var session = new EditorSession();
            var window = session.CreateDocument("original");
            session.Engine.InsertText("changed ");
            var document = window.Document;
            var before = document.Buffer.Snapshot();
            var version = document.Version;
            var cursor = window.Cursor;
            var undo = session.Undo.Count;

            await Assert.ThrowsAnyAsync<IOException>(async () =>
                await session.Files.SaveCurrentWindowAsync(target));

            // Never create parent directories as an implicit side effect of
            // Save. Failed persistence must not clear Dirty or change the
            // association, even though encoding the text itself succeeded.
            Assert.False(Directory.Exists(Path.GetDirectoryName(target)));
            Assert.False(File.Exists(target));
            Assert.Equal(before, document.Buffer.Snapshot());
            Assert.Equal(version, document.Version);
            Assert.Equal(cursor, window.Cursor);
            Assert.Equal(undo, session.Undo.Count);
            Assert.True(document.IsDirty);
            Assert.Null(document.FilePath);
        }
        finally
        {
            if (Directory.Exists(root))
                Directory.Delete(root, recursive: true);
        }
    }

    [Fact]
    public async Task LegacySave_ToExistingDirectory_DoesNotReplaceItsContents()
    {
        var root = Path.Combine(Path.GetTempPath(),
            "sasd-save-directory-" + Guid.NewGuid().ToString("N"));
        try
        {
            Directory.CreateDirectory(root);
            var protectedFile = Path.Combine(root, "keep.txt");
            File.WriteAllText(protectedFile, "must survive");

            var session = new EditorSession();
            var window = session.CreateDocument("original");
            session.Engine.InsertText("changed ");
            var originalVersion = window.Document.Version;
            var originalUndo = session.Undo.Count;

            // A selected directory is not an output file. This also guards
            // the staged writer's cleanup path: failed writes must not remove
            // any pre-existing directory entries or acknowledge Save.
            // Windows commonly reports UnauthorizedAccessException for a
            // directory target, while Unix reports IOException. Both mean
            // the filesystem refused the output path. Assert the shared
            // safety contract rather than a platform-specific subclass.
            var error = await Record.ExceptionAsync(async () =>
                await session.Files.SaveCurrentWindowAsync(root));
            Assert.True(error is IOException or UnauthorizedAccessException,
                $"Expected a filesystem write error, got: {error?.GetType().FullName ?? "no exception"}");

            Assert.True(Directory.Exists(root));
            Assert.Equal("must survive", File.ReadAllText(protectedFile));
            Assert.True(window.Document.IsDirty);
            Assert.Null(window.Document.FilePath);
            Assert.Equal(originalVersion, window.Document.Version);
            Assert.Equal(originalUndo, session.Undo.Count);
        }
        finally
        {
            if (Directory.Exists(root))
                Directory.Delete(root, recursive: true);
        }
    }

    /// <summary>
    /// A storage boundary that fails only when asked to write. The failure
    /// is intentionally independent of access permissions, filesystems and
    /// CI platform, so the test proves editor state semantics rather than
    /// an incidental operating-system error message.
    /// </summary>
    private sealed class FailingWriteCodec : IEditorFileCodec
    {
        internal int Attempts { get; private set; }

        public ValueTask<IReadOnlyList<EditorLineSnapshot>> ReadAsync(
            string path, CancellationToken cancellationToken = default) =>
            ValueTask.FromResult<IReadOnlyList<EditorLineSnapshot>>(
                Array.Empty<EditorLineSnapshot>());

        public ValueTask WriteAsync(
            string path, IReadOnlyList<EditorLineSnapshot> lines,
            CancellationToken cancellationToken = default)
        {
            Attempts++;
            throw new IOException("Injected write failure.");
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
