using System.Text;
using Sasd.Editor.Editing;
using Sasd.Editor.IO;
using Xunit;

namespace Sasd.Editor.Core.Tests;

/// <summary>
/// Prevents a modern Save from silently destroying on-disk changes made by
/// another application after our UTF-8 load. These tests use real disk bytes,
/// because a mocked codec cannot prove byte-level conflict behavior.
/// </summary>
/// <remarks>
/// File content deliberately changes without relying on timestamp precision,
/// thread sleeps, platform-specific file watchers, or real user permissions.
/// These are best-effort checks at the staged-publish boundary, NOT proof of
/// cross-process atomic transactions.
/// </remarks>
public sealed class ExternalFileRevisionTests
{
    [Fact]
    public async Task Save_ExternalSameLengthEditPreservesDiskAndLocalDirtyText()
    {
        var directory = CreateDirectory();
        try
        {
            var path = Path.Combine(directory, "shared.txt");
            await File.WriteAllTextAsync(path, "alpha", new UTF8Encoding(false));
            var storage = new FileTextStorage();
            var document = await storage.LoadAsync(path);
            document.Buffer.ReplaceLine(0, "local");
            document.MarkChanged();
            var version = document.Version;
            var lines = document.Buffer.Snapshot();

            // Equal byte count: checking only Length is insufficient.
            await File.WriteAllTextAsync(path, "other", new UTF8Encoding(false));
            await Assert.ThrowsAsync<IOException>(
                async () => await storage.SaveAsync(document));

            Assert.Equal("other", await File.ReadAllTextAsync(path));
            Assert.Equal(lines, document.Buffer.Snapshot());
            Assert.Equal(version, document.Version);
            Assert.True(document.IsDirty);
            Assert.Equal(Path.GetFullPath(path), document.FilePath);
            Assert.Equal(new[] { "shared.txt" },
                Directory.GetFiles(directory).Select(Path.GetFileName));
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }

    [Fact]
    public async Task Save_AfterExternalDeletionDoesNotRecreateTrackedFile()
    {
        var directory = CreateDirectory();
        try
        {
            var path = Path.Combine(directory, "deleted.txt");
            await File.WriteAllTextAsync(path, "original");
            var document = await new FileTextStorage().LoadAsync(path);
            document.Buffer.ReplaceLine(0, "local edit");
            document.MarkChanged();
            File.Delete(path);

            // A normal Save of a loaded document is not an implicit
            // "recreate missing file" action. Only explicit Save As may
            // intentionally create a new target.
            await Assert.ThrowsAsync<FileNotFoundException>(
                async () => await new FileTextStorage().SaveAsync(document));
            Assert.False(File.Exists(path));
            Assert.Empty(Directory.GetFiles(directory));
            Assert.True(document.IsDirty);
            Assert.Equal(Path.GetFullPath(path), document.FilePath);
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }

    [Fact]
    public async Task Save_ChangedBomConflictsEvenWithIdenticalDecodedText()
    {
        var directory = CreateDirectory();
        try
        {
            var path = Path.Combine(directory, "bom.txt");
            await File.WriteAllBytesAsync(path, [0xEF, 0xBB, 0xBF, (byte)'a']);
            var document = await new FileTextStorage().LoadAsync(path);
            Assert.Equal("a", document.Buffer.GetLine(0).Text);

            // Decoding strips the BOM, but revision comparison must not.
            // The external file now has exactly the same logical text.
            await File.WriteAllBytesAsync(path, [(byte)'a']);
            await Assert.ThrowsAsync<IOException>(
                async () => await new FileTextStorage().SaveAsync(document));
            Assert.Equal([(byte)'a'], await File.ReadAllBytesAsync(path));
            Assert.False(document.IsDirty);
            Assert.Equal(Path.GetFullPath(path), document.FilePath);
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }

    [Fact]
    public async Task Save_IdenticalExternalRewriteIsAcceptedAndAdvancesRevision()
    {
        var directory = CreateDirectory();
        try
        {
            var path = Path.Combine(directory, "same.txt");
            await File.WriteAllTextAsync(path, "old");
            var storage = new FileTextStorage();
            var document = await storage.LoadAsync(path);
            document.Buffer.ReplaceLine(0, "first");
            document.MarkChanged();

            // Identical bytes are not a data conflict regardless of a file
            // timestamp updated by the external tool.
            await File.WriteAllTextAsync(path, "old");
            await storage.SaveAsync(document);
            Assert.Equal("first", await File.ReadAllTextAsync(path));
            Assert.False(document.IsDirty);

            // A second Save must compare with the FIRST SAVE'S bytes, not
            // with the originally loaded "old" bytes.
            document.Buffer.ReplaceLine(0, "second");
            document.MarkChanged();
            await new FileTextStorage().SaveAsync(document);
            Assert.Equal("second", await File.ReadAllTextAsync(path));
            Assert.False(document.IsDirty);
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }

    [Fact]
    public async Task SaveAs_OtherDestinationKeepsExplicitOverwriteBehavior()
    {
        var directory = CreateDirectory();
        try
        {
            var source = Path.Combine(directory, "source.txt");
            var destination = Path.Combine(directory, "copy.txt");
            await File.WriteAllTextAsync(source, "source");
            await File.WriteAllTextAsync(destination, "older copy");
            var document = await new FileTextStorage().LoadAsync(source);
            document.Buffer.ReplaceLine(0, "edited");
            document.MarkChanged();

            // The editor has never observed the destination's original
            // contents. The caller explicitly chose this Save As target;
            // prompting about overwriting it is a host responsibility.
            await new FileTextStorage().SaveAsync(document, destination);
            Assert.Equal("edited", await File.ReadAllTextAsync(destination));
            Assert.Equal("source", await File.ReadAllTextAsync(source));
            Assert.Equal(Path.GetFullPath(destination), document.FilePath);
            Assert.False(document.IsDirty);

            // The new association now has its own trusted baseline.
            await File.WriteAllTextAsync(destination, "another external version");
            document.Buffer.ReplaceLine(0, "new local changes");
            document.MarkChanged();
            await Assert.ThrowsAsync<IOException>(
                async () => await new FileTextStorage().SaveAsync(document));
            Assert.Equal("another external version",
                await File.ReadAllTextAsync(destination));
            Assert.True(document.IsDirty);
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }

    [Fact]
    public async Task UnnamedDocument_StillAllowsExplicitSaveToExistingTarget()
    {
        var directory = CreateDirectory();
        try
        {
            var destination = Path.Combine(directory, "first.txt");
            await File.WriteAllTextAsync(destination, "before");
            var session = new EditorSession();
            var document = session.CreateDocument("new").Document;

            // Not every document is loaded from a disk file. The adapter
            // cannot invent a baseline for a brand-new document.
            await new FileTextStorage().SaveAsync(document, destination);
            Assert.Equal("new", await File.ReadAllTextAsync(destination));
            Assert.Equal(Path.GetFullPath(destination), document.FilePath);
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }

    [Fact]
    public async Task StageValidationHook_RejectsChangesAfterStageBeforePublication()
    {
        var directory = CreateDirectory();
        try
        {
            var path = Path.Combine(directory, "boundary.txt");
            await File.WriteAllTextAsync(path, "first");
            var validationRan = false;
            await Assert.ThrowsAsync<IOException>(async () =>
                await StagedFileWriter.WriteAsync(
                    path,
                    async (stage, token) =>
                    {
                        await File.WriteAllTextAsync(stage, "staged", token);
                        // Simulate the external application writing after
                        // staging, before the final rename. This requires no
                        // timing guesses: the callback creates the race in
                        // precisely the intended test interval.
                        await File.WriteAllTextAsync(path, "external", token);
                    },
                    verifyBeforePublish: async (destination, token) =>
                    {
                        validationRan = true;
                        var actual = await File.ReadAllTextAsync(destination, token);
                        if (actual != "first")
                            throw new IOException("simulated external conflict");
                    }));

            Assert.True(validationRan);
            Assert.Equal("external", await File.ReadAllTextAsync(path));
            Assert.Equal(new[] { "boundary.txt" },
                Directory.GetFiles(directory).Select(Path.GetFileName));
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }

    private static string CreateDirectory()
    {
        var directory = Path.Combine(
            Path.GetTempPath(), $"sasd-file-revision-{Guid.NewGuid():N}");
        Directory.CreateDirectory(directory);
        return directory;
    }
}
