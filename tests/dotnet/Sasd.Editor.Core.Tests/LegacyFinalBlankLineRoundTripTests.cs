using System.Text;
using Sasd.Editor.Commands;
using Sasd.Editor.Editing;
using Sasd.Editor.IO;
using Sasd.Editor.Model;
using Xunit;

namespace Sasd.Editor.Core.Tests;

/// <summary>
/// Byte-exact round-trip evidence for the distinction between an explicitly
/// stored final blank line and an external file's ordinary terminal newline.
/// </summary>
/// <remarks>
/// FIRST-ED's 0x8D outgoing soft line boundary is independent of a hard
/// CR/LF boundary. The decoder already treats one final CR/LF as the end
/// of its preceding line, not as another logical blank descriptor.
///
/// Before the writer fix, ["alpha", ""] therefore serialized identically
/// to ["alpha"] plus a terminal CR/LF and silently lost the last descriptor.
/// Encoding an actual final empty descriptor's OWN outgoing hard boundary
/// restores the round-trip invariant while leaving foreign input unchanged.
/// This is a documented SASD codec policy, not an assertion of undocumented
/// historical Pascal-internal memory layout.
/// </remarks>
public sealed class LegacyFinalBlankLineRoundTripTests
{
    [Fact]
    public async Task ExplicitFinalBlankLineGetsItsOwnOrdinaryTerminator()
    {
        await WithTemporaryFile(async path =>
        {
            var codec = new FirstEdLegacyFileCodec();
            await codec.WriteAsync(path,
            [
                new EditorLineSnapshot("alpha"),
                new EditorLineSnapshot("")
            ]);

            Assert.Equal(Encoding.Latin1.GetBytes("alpha\r\n\r\n"),
                await File.ReadAllBytesAsync(path));

            var lines = await codec.ReadAsync(path);
            Assert.Equal(new[] { "alpha", "" }, lines.Select(x => x.Text));
            Assert.All(lines, line => Assert.Equal(EditorLineFlags.None, line.Flags));
        });
    }

    [Fact]
    public async Task SeveralExplicitTrailingBlankLinesRemainSeparateDescriptors()
    {
        await WithTemporaryFile(async path =>
        {
            var codec = new FirstEdLegacyFileCodec();
            await codec.WriteAsync(path,
            [
                new EditorLineSnapshot("alpha"),
                new EditorLineSnapshot(""),
                new EditorLineSnapshot("")
            ]);

            Assert.Equal(Encoding.Latin1.GetBytes("alpha\r\n\r\n\r\n"),
                await File.ReadAllBytesAsync(path));

            var decoded = await codec.ReadAsync(path);
            Assert.Equal(new[] { "alpha", "", "" }, decoded.Select(x => x.Text));
        });
    }

    [Fact]
    public async Task SoleExplicitBlankLineRoundTripsButZeroImportedLinesStayEmpty()
    {
        await WithTemporaryFile(async path =>
        {
            var codec = new FirstEdLegacyFileCodec();

            // One blank line is a valid logical editor stream. Encoding it
            // as zero physical bytes would be ambiguous with a physically
            // empty external input, which the Read File command must import
            // as ZERO lines without Dirty/Undo side effects.
            await codec.WriteAsync(path, [new EditorLineSnapshot("")]);
            Assert.Equal(Encoding.Latin1.GetBytes("\r\n"),
                await File.ReadAllBytesAsync(path));
            Assert.Equal("", Assert.Single(await codec.ReadAsync(path)).Text);

            await codec.WriteAsync(path, Array.Empty<EditorLineSnapshot>());
            Assert.Empty(await File.ReadAllBytesAsync(path));
            Assert.Empty(await codec.ReadAsync(path));
        });
    }

    [Fact]
    public async Task FinalEmptySoftWrappedLineRetainsOnlyItsWrappedMarker()
    {
        await WithTemporaryFile(async path =>
        {
            var codec = new FirstEdLegacyFileCodec();
            await codec.WriteAsync(path,
            [
                new EditorLineSnapshot("alpha"),
                new EditorLineSnapshot("", EditorLineFlags.Wrapped)
            ]);

            Assert.Equal(Encoding.Latin1.GetBytes("alpha\r\n")
                .Concat([(byte)0x8D]), await File.ReadAllBytesAsync(path));

            var decoded = await codec.ReadAsync(path);
            Assert.Equal(new[] { "alpha", "" }, decoded.Select(x => x.Text));
            Assert.Equal(EditorLineFlags.Wrapped, decoded[1].Flags);
        });
    }

    [Fact]
    public async Task ExternalFileEndingInSingleOrdinaryCrlfStillDoesNotInventBlankLine()
    {
        await WithTemporaryFile(async path =>
        {
            await File.WriteAllBytesAsync(path, Encoding.Latin1.GetBytes("alpha\r\n"));
            var decoded = await new FirstEdLegacyFileCodec().ReadAsync(path);
            Assert.Equal("alpha", Assert.Single(decoded).Text);
        });
    }

    [Fact]
    public async Task ReadFileImportsAllExplicitBlankDescriptorsAsOneUndoTransaction()
    {
        await WithTemporaryFile(async path =>
        {
            var codec = new FirstEdLegacyFileCodec();
            await codec.WriteAsync(path,
            [
                new EditorLineSnapshot("external"),
                new EditorLineSnapshot("")
            ]);

            var session = new EditorSession();
            var window = session.CreateDocument("before\nafter");
            window.Cursor = new TextPosition(0, 3);
            var baseline = window.Document.Buffer.Snapshot();
            var dispatcher = new EditorCommandDispatcher(session);

            Assert.True(await dispatcher.ExecuteAsync(
                new(EditorCommandId.ReadFile, Text: path)));
            Assert.Equal(new[] { "before", "external", "", "after" },
                window.Document.Buffer.Snapshot().Select(x => x.Text));
            Assert.Equal(new TextPosition(0, 3), window.Cursor);
            Assert.True(window.Document.IsDirty);
            Assert.Equal(1, session.Undo.Count);

            Assert.True(await dispatcher.ExecuteAsync(
                new(EditorCommandId.Undo)));
            Assert.Equal(baseline, window.Document.Buffer.Snapshot());
            Assert.False(window.Document.IsDirty);
            Assert.Equal(0, session.Undo.Count);
        });
    }

    [Fact]
    public async Task InvalidFinalLineCannotReplaceExistingDestination()
    {
        await WithTemporaryFile(async path =>
        {
            var previousBytes = Encoding.Latin1.GetBytes("do not replace");
            await File.WriteAllBytesAsync(path, previousBytes);
            var codec = new FirstEdLegacyFileCodec();

            // The first line is legitimate, but the *last* explicit blank
            // descriptor carries forbidden separator text. Validation and
            // encoding must complete before StagedFileWriter publishes
            // anything; neither the earlier valid line nor a new terminator
            // may be partially written to the pre-existing destination.
            await Assert.ThrowsAsync<EncoderFallbackException>(async () =>
                await codec.WriteAsync(path,
                [
                    new EditorLineSnapshot("alpha"),
                    new EditorLineSnapshot("\u008D")
                ]));

            Assert.Equal(previousBytes, await File.ReadAllBytesAsync(path));
        });
    }

    private static async Task WithTemporaryFile(Func<string, Task> action)
    {
        var directory = Path.Combine(
            Path.GetTempPath(), $"sasd-final-blank-{Guid.NewGuid():N}");
        Directory.CreateDirectory(directory);
        try
        {
            await action(Path.Combine(directory, "legacy.txt"));
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }
}
