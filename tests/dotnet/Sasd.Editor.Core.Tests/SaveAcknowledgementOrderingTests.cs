using Sasd.Editor.Editing;
using Sasd.Editor.IO;
using Sasd.Editor.Model;
using Xunit;

namespace Sasd.Editor.Core.Tests;

/// <summary>
/// Different Save As target paths are independent of the per-file staging gate.
/// Complete injected codec writes out of order to verify that only the latest
/// successfully published save owns the document's file association.
/// </summary>
public sealed class SaveAcknowledgementOrderingTests
{
    [Fact]
    public async Task DifferentPaths_NewerSaveFinishingFirstKeepsItsPathAfterOlderCompletes()
    {
        var codec = new DeferredWriteCodec();
        var session = new EditorSession(fileCodec: codec);
        var view = session.CreateDocument("text");
        session.Engine.InsertText("new ");
        var firstPath = Path.Combine(Path.GetTempPath(), $"sasd-save-A-{Guid.NewGuid():N}.txt");
        var secondPath = Path.Combine(Path.GetTempPath(), $"sasd-save-B-{Guid.NewGuid():N}.txt");

        var earlier = session.Files.SaveCurrentWindowAsync(firstPath).AsTask();
        await codec.WaitForWritesAsync(1);
        var later = session.Files.SaveCurrentWindowAsync(secondPath).AsTask();
        await codec.WaitForWritesAsync(2);

        // The newer request has already written its separate target before
        // the earlier request finishes I/O. The earlier acknowledgement may
        // not silently put the document back on the first target.
        codec.Complete(1);
        Assert.True(await later);
        Assert.Equal(Path.GetFullPath(secondPath), view.Document.FilePath);
        Assert.False(view.Document.IsDirty);

        codec.Complete(0);
        Assert.True(await earlier);
        Assert.Equal(Path.GetFullPath(secondPath), view.Document.FilePath);
        Assert.False(view.Document.IsDirty);
        Assert.Equal(1, session.Undo.Count);
    }

    [Fact]
    public async Task DifferentPaths_EarlierSaveFinishingFirstThenNewerTakesOver()
    {
        var codec = new DeferredWriteCodec();
        var session = new EditorSession(fileCodec: codec);
        var view = session.CreateDocument("text");
        session.Engine.InsertText("new ");
        var firstPath = Path.Combine(Path.GetTempPath(), $"sasd-first-{Guid.NewGuid():N}.txt");
        var secondPath = Path.Combine(Path.GetTempPath(), $"sasd-second-{Guid.NewGuid():N}.txt");

        var earlier = session.Files.SaveCurrentWindowAsync(firstPath).AsTask();
        await codec.WaitForWritesAsync(1);
        var later = session.Files.SaveCurrentWindowAsync(secondPath).AsTask();
        await codec.WaitForWritesAsync(2);

        codec.Complete(0);
        Assert.True(await earlier);
        Assert.Equal(Path.GetFullPath(firstPath), view.Document.FilePath);

        codec.Complete(1);
        Assert.True(await later);
        Assert.Equal(Path.GetFullPath(secondPath), view.Document.FilePath);
        Assert.False(view.Document.IsDirty);
    }

    [Fact]
    public async Task NewerFailedSave_DoesNotSuppressEarlierSuccessfulWrite()
    {
        var codec = new DeferredWriteCodec();
        var session = new EditorSession(fileCodec: codec);
        var view = session.CreateDocument("text");
        session.Engine.InsertText("new ");
        var firstPath = Path.Combine(Path.GetTempPath(), $"sasd-valid-{Guid.NewGuid():N}.txt");
        var secondPath = Path.Combine(Path.GetTempPath(), $"sasd-failed-{Guid.NewGuid():N}.txt");

        var earlier = session.Files.SaveCurrentWindowAsync(firstPath).AsTask();
        await codec.WaitForWritesAsync(1);
        var later = session.Files.SaveCurrentWindowAsync(secondPath).AsTask();
        await codec.WaitForWritesAsync(2);

        codec.Fail(1);
        await Assert.ThrowsAsync<IOException>(() => later);
        Assert.Null(view.Document.FilePath);
        Assert.True(view.Document.IsDirty);

        codec.Complete(0);
        Assert.True(await earlier);
        Assert.Equal(Path.GetFullPath(firstPath), view.Document.FilePath);
        Assert.False(view.Document.IsDirty);
    }

    [Fact]
    public async Task NewerSuccessCannotClearEditsMadeDuringBothWrites()
    {
        var codec = new DeferredWriteCodec();
        var session = new EditorSession(fileCodec: codec);
        var view = session.CreateDocument("before");
        var firstPath = Path.Combine(Path.GetTempPath(), $"sasd-older-{Guid.NewGuid():N}.txt");
        var secondPath = Path.Combine(Path.GetTempPath(), $"sasd-newer-{Guid.NewGuid():N}.txt");
        var earlier = session.Files.SaveCurrentWindowAsync(firstPath).AsTask();
        await codec.WaitForWritesAsync(1);
        var later = session.Files.SaveCurrentWindowAsync(secondPath).AsTask();
        await codec.WaitForWritesAsync(2);

        session.Engine.InsertText("latest ");
        Assert.Equal("latest before", view.Document.Buffer.GetLine(0).Text);

        codec.Complete(1);
        Assert.True(await later);
        codec.Complete(0);
        Assert.True(await earlier);
        Assert.Equal(Path.GetFullPath(secondPath), view.Document.FilePath);
        Assert.True(view.Document.IsDirty);
        Assert.Equal(1, session.Undo.Count);
    }

    [Fact]
    public async Task DirectWriteFile_DoesNotSupersedeOrAcknowledgeSaveAsRequest()
    {
        var codec = new DeferredWriteCodec();
        var session = new EditorSession(fileCodec: codec);
        var view = session.CreateDocument("text");
        session.Engine.InsertText("new ");
        var path = Path.Combine(Path.GetTempPath(), $"sasd-associated-{Guid.NewGuid():N}.txt");
        var save = session.Files.SaveCurrentWindowAsync(path).AsTask();
        await codec.WaitForWritesAsync(1);

        var directWrite = session.Files.WriteCurrentWindowAsync("export-copy.txt").AsTask();
        await codec.WaitForWritesAsync(2);
        codec.Complete(1);
        await directWrite;
        Assert.Null(view.Document.FilePath);
        Assert.True(view.Document.IsDirty);

        codec.Complete(0);
        Assert.True(await save);
        Assert.Equal(Path.GetFullPath(path), view.Document.FilePath);
        Assert.False(view.Document.IsDirty);
    }

    private sealed class DeferredWriteCodec : IEditorFileCodec
    {
        private sealed class PendingWrite
        {
            internal TaskCompletionSource Completion { get; } =
                new(TaskCreationOptions.RunContinuationsAsynchronously);
        }

        private readonly List<PendingWrite> _pending = [];
        private readonly object _sync = new();
        private TaskCompletionSource _writeAdded =
            new(TaskCreationOptions.RunContinuationsAsynchronously);

        internal async Task WaitForWritesAsync(int count)
        {
            while (true)
            {
                Task next;
                lock (_sync)
                {
                    if (_pending.Count >= count)
                    {
                        return;
                    }

                    next = _writeAdded.Task;
                }

                await next;
            }
        }

        internal void Complete(int index)
        {
            lock (_sync)
            {
                _pending[index].Completion.TrySetResult();
            }
        }

        internal void Fail(int index)
        {
            lock (_sync)
            {
                _pending[index].Completion.TrySetException(
                    new IOException("simulated failed provider write"));
            }
        }

        public ValueTask<IReadOnlyList<EditorLineSnapshot>> ReadAsync(
            string path, CancellationToken token = default) =>
            ValueTask.FromResult<IReadOnlyList<EditorLineSnapshot>>(
                Array.Empty<EditorLineSnapshot>());

        public async ValueTask WriteAsync(
            string path, IReadOnlyList<EditorLineSnapshot> lines,
            CancellationToken cancellationToken = default)
        {
            var pending = new PendingWrite();
            lock (_sync)
            {
                _pending.Add(pending);
                var notification = _writeAdded;
                _writeAdded = new(TaskCreationOptions.RunContinuationsAsynchronously);
                notification.TrySetResult();
            }

            await pending.Completion.Task.WaitAsync(cancellationToken);
        }
    }
}
