using Sasd.Editor.Editing;
using Sasd.Editor.FirstEd.Sample;
using Xunit;

namespace Sasd.Editor.Core.Tests;

/// <summary>
/// Verify real print-file output using a temporary directory. A destination
/// is created exclusively and never overwrites existing bytes.
/// </summary>
public sealed class MicroStarPrintFileSinkTests
{
    [Fact]
    public async Task SchedulerWritesUtf8LinesThenReleasesFile()
    {
        var root = Path.Combine(Path.GetTempPath(), "sasd-print-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        try
        {
            var file = Path.Combine(root, "out.txt");
            var session = new EditorSession();
            var queue = new MicroStarPrintQueue(session);
            Assert.True(queue.TryStart(["one", "two"], new MicroStarPrintFileSink(file)));
            await session.Scheduler.RunIdleCycleAsync();
            Assert.Equal(1, queue.Active!.CompletedLines);
            await session.Scheduler.RunIdleCycleAsync();
            Assert.True(queue.Active.IsFinished);
            Assert.Equal(["one", "two"], await File.ReadAllLinesAsync(file));
        }
        finally { Directory.Delete(root, true); }
    }

    [Fact]
    public async Task FormFeedIsAControlRecordWithoutAnExtraLineEnding()
    {
        var root = Path.Combine(Path.GetTempPath(),
            "sasd-print-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        try
        {
            var file = Path.Combine(root, "pages.txt");
            var session = new EditorSession();
            var queue = new MicroStarPrintQueue(session);

            // Pagination inserts a separate FF record between pages.
            // A normal text record ends with Environment.NewLine; the FF
            // must NOT acquire its own trailing newline on either Windows
            // or Linux. Assert bytes to detect invisible extra CR/LF.
            var output = MicroStarPrintPagination.Prepare(["one", "two"], 1);
            Assert.True(queue.TryStart(output, new MicroStarPrintFileSink(file)));
            while (!queue.Active!.IsFinished)
                await session.Scheduler.RunIdleCycleAsync();

            var expected = System.Text.Encoding.UTF8.GetBytes(
                "one" + Environment.NewLine + "\f" +
                "two" + Environment.NewLine);
            Assert.Equal(expected, await File.ReadAllBytesAsync(file));
        }
        finally { Directory.Delete(root, true); }
    }

    [Fact]
    public async Task TypedPrintKeepsLiteralFormFeedTextSeparateFromPageBreak()
    {
        var root = Path.Combine(Path.GetTempPath(),
            "sasd-print-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        try
        {
            var file = Path.Combine(root, "typed.txt");
            var session = new EditorSession();
            var queue = new MicroStarPrintQueue(session);

            // Page size 1 forces a page break between two actual document
            // lines. The first line is itself a literal FF: it must still
            // receive its normal newline rather than be treated as an eject.
            var records = MicroStarTypedPrintPagination.Prepare(["\f", "next"], 1);
            Assert.Equal(3, records.Count);
            Assert.Equal(MicroStarPrintRecordKind.Text, records[0].Kind);
            Assert.Equal(MicroStarPrintRecordKind.PageBreak, records[1].Kind);
            Assert.True(queue.TryStartTyped(records, new MicroStarPrintFileSink(file)));
            while (!queue.Active!.IsFinished)
                await session.Scheduler.RunIdleCycleAsync();

            var expected = System.Text.Encoding.UTF8.GetBytes(
                "\f" + Environment.NewLine + "\f" + "next" + Environment.NewLine);
            Assert.Equal(expected, await File.ReadAllBytesAsync(file));
            Assert.Empty(session.Scheduler.Tasks);
        }
        finally { Directory.Delete(root, true); }
    }

    [Fact]
    public async Task MalformedTypedRecordsAreRejectedBeforeAnyOutputSlice()
    {
        var root = Path.Combine(Path.GetTempPath(),
            "sasd-print-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        try
        {
            var file = Path.Combine(root, "invalid.txt");
            var session = new EditorSession();
            var queue = new MicroStarPrintQueue(session);

            // Record structs are directly constructible. Neither a malformed
            // page break containing text nor an unknown tag may enter a
            // scheduled print job and fail after earlier records were sent.
            using (var sink = new MicroStarPrintFileSink(file))
            {
                var invalid = new[]
                {
                    MicroStarPrintRecord.Line("valid"),
                    new MicroStarPrintRecord(MicroStarPrintRecordKind.PageBreak, "unexpected")
                };
                Assert.Throws<ArgumentException>(() => queue.TryStartTyped(invalid, sink));
                Assert.Empty(session.Scheduler.Tasks);
                Assert.Null(queue.Active);
            }
            Assert.Empty(await File.ReadAllBytesAsync(file));
        }
        finally { Directory.Delete(root, true); }
    }

    [Fact]
    public void ExistingDestinationIsNeverTruncated()
    {
        var root = Path.Combine(Path.GetTempPath(), "sasd-print-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        try
        {
            var file = Path.Combine(root, "keep.txt");
            File.WriteAllText(file, "original");
            Assert.Throws<IOException>(() => new MicroStarPrintFileSink(file));
            Assert.Equal("original", File.ReadAllText(file));
        }
        finally { Directory.Delete(root, true); }
    }

    [Fact]
    public void ExistingSymbolicLinkDestinationCannotRedirectPrintOutput()
    {
        var root = Path.Combine(Path.GetTempPath(),
            "sasd-print-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        try
        {
            var protectedTarget = Path.Combine(root, "protected.txt");
            var printPath = Path.Combine(root, "print-link.txt");
            var original = new byte[] { 0x41, 0x8D, 0x42 };
            File.WriteAllBytes(protectedTarget, original);

            try
            {
                File.CreateSymbolicLink(printPath, protectedTarget);
            }
            catch (Exception error) when (error is UnauthorizedAccessException
                or PlatformNotSupportedException or IOException)
            {
                // Some Windows runners cannot create symlinks without the
                // relevant privilege. Linux CI still exercises this path.
                return;
            }

            // FileMode.CreateNew must reject an existing directory entry
            // even when that entry is a link to a writable real file.
            // This protects the output contract against accidental redirection
            // to data that the user never selected for printing.
            Assert.Throws<IOException>(() => new MicroStarPrintFileSink(printPath));
            Assert.Equal(original, File.ReadAllBytes(protectedTarget));
            Assert.True(File.Exists(printPath));
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    [Fact]
    public void ExistingDirectoryDestinationIsRejectedWithoutChangingItsContents()
    {
        var root = Path.Combine(Path.GetTempPath(),
            "sasd-print-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        try
        {
            var outputDirectory = Path.Combine(root, "output.txt");
            Directory.CreateDirectory(outputDirectory);
            var protectedFile = Path.Combine(outputDirectory, "protected.txt");
            var original = new byte[] { 0x41, 0x8D, 0x42 };
            File.WriteAllBytes(protectedFile, original);

            // Directory targets are a deterministic cross-platform failure
            // case, unlike symbolic links whose creation can require special
            // Windows privileges. A print path must designate a new FILE;
            // it must never be reinterpreted as a directory or overwrite
            // unrelated content already stored beneath that directory.
            // On Unix a directory-as-file attempt fails with IOException;
            // Windows can instead report UnauthorizedAccessException from
            // CreateFile. Both represent a rejected operation, and neither
            // may damage the existing directory or its contents.
            var error = Record.Exception(() => new MicroStarPrintFileSink(outputDirectory));
            Assert.True(error is IOException or UnauthorizedAccessException,
                $"Expected a rejected directory print target; actual: {error}");
            Assert.True(Directory.Exists(outputDirectory));
            Assert.Equal(original, File.ReadAllBytes(protectedFile));
            Assert.Equal(new[] { "protected.txt" },
                Directory.GetFiles(outputDirectory).Select(Path.GetFileName));
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    [Fact]
    public async Task PreCancelledPrintRecordsDoNotWriteAnyBytes()
    {
        var root = Path.Combine(Path.GetTempPath(),
            "sasd-print-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        try
        {
            var output = Path.Combine(root, "cancelled.txt");
            using var cancellation = new CancellationTokenSource();
            cancellation.Cancel();

            // Both the original string-oriented adapter and the newer typed
            // print API must reject an already-cancelled operation BEFORE
            // writing text or a form feed. Print-to-file deliberately keeps
            // a partial file after mid-job cancellation; this different
            // pre-cancel case must leave the newly created file empty.
            using (var sink = new MicroStarPrintFileSink(output))
            {
                await Assert.ThrowsAnyAsync<OperationCanceledException>(
                    async () => await sink.WriteLineAsync("unwanted",
                        cancellation.Token));
                await Assert.ThrowsAnyAsync<OperationCanceledException>(
                    async () => await sink.WriteRecordAsync(
                        MicroStarPrintRecord.Break(), cancellation.Token));
            }

            Assert.Empty(await File.ReadAllBytesAsync(output));
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    [Fact]
    public async Task CancellationClosesPartialOutputAndPreventsFurtherLines()
    {
        var root = Path.Combine(Path.GetTempPath(), "sasd-print-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        try
        {
            var file = Path.Combine(root, "partial.txt");
            var session = new EditorSession();
            var queue = new MicroStarPrintQueue(session);
            Assert.True(queue.TryStart(["first", "second"], new MicroStarPrintFileSink(file)));
            await session.Scheduler.RunIdleCycleAsync();
            queue.Cancel();
            await session.Scheduler.RunIdleCycleAsync();
            Assert.Equal(["first"], await File.ReadAllLinesAsync(file));
        }
        finally { Directory.Delete(root, true); }
    }
}
