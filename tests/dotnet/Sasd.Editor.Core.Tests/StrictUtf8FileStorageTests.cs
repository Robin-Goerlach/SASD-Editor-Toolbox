using System.Text;
using Sasd.Editor.Editing;
using Sasd.Editor.IO;
using Xunit;

namespace Sasd.Editor.Core.Tests;

/// <summary>
/// Exercises byte-level integrity at the default modern UTF-8 storage boundary.
/// The old permissive decoder could turn malformed bytes into U+FFFD and
/// subsequently save that replacement text, permanently losing the source.
/// </summary>
/// <remarks>
/// The tests create their own isolated directories and use real file bytes:
/// mock text storage would not exercise the framework's UTF-8 decoder or
/// encoder-fallback behavior. Invalid input must never become a document.
/// </remarks>
public sealed class StrictUtf8FileStorageTests
{
    [Theory]
    [InlineData("61FF62")]           // Standalone invalid continuation byte.
    [InlineData("E282")]             // Truncated three-byte sequence.
    [InlineData("EFBBBF61C328")]     // Valid UTF-8 BOM, then invalid sequence.
    [InlineData("FFFE6100")]         // UTF-16 LE BOM: not default UTF-8 input.
    public async Task Load_InvalidDefaultUtf8DoesNotSilentlyReplaceBytes(string hex)
    {
        var directory = CreateDirectory();
        try
        {
            var path = Path.Combine(directory, "malformed.txt");
            var original = Convert.FromHexString(hex);
            await File.WriteAllBytesAsync(path, original);

            // The default must not quietly replace invalid bytes with U+FFFD.
            // No EditorDocument is returned, so an application cannot
            // accidentally save the corrupted interpretation back to disk.
            await Assert.ThrowsAsync<DecoderFallbackException>(
                async () => await new FileTextStorage().LoadAsync(path));

            Assert.Equal(original, await File.ReadAllBytesAsync(path));
            Assert.Equal(new[] { "malformed.txt" },
                Directory.GetFiles(directory).Select(Path.GetFileName));
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }

    [Fact]
    public async Task Load_ValidUtf8BomIsStrippedAndSaveRemainsBomless()
    {
        var directory = CreateDirectory();
        try
        {
            var path = Path.Combine(directory, "bom.txt");
            var original = new byte[] {
                0xEF, 0xBB, 0xBF,
                (byte)'g', (byte)'r', 0xC3, 0xBC, (byte)'n',
                (byte)'\r', (byte)'\n',
                (byte)'z'
            };
            await File.WriteAllBytesAsync(path, original);

            var storage = new FileTextStorage();
            var document = await storage.LoadAsync(path);

            Assert.Equal(2, document.Buffer.LineCount);
            Assert.Equal("grün", document.Buffer.GetLine(0).Text);
            Assert.Equal("z", document.Buffer.GetLine(1).Text);
            Assert.Equal("\r\n", document.NewLine);
            Assert.False(document.IsDirty);
            Assert.Equal(Path.GetFullPath(path), document.FilePath);

            // UTF-8 BOM recognition should not make the BOM a literal
            // character at the start of the document, or change the default
            // writer's historical BOM-less output policy.
            await storage.SaveAsync(document);
            Assert.Equal(Encoding.UTF8.GetBytes("grün\r\nz"),
                await File.ReadAllBytesAsync(path));
            Assert.False(document.IsDirty);
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }

    [Fact]
    public async Task Save_UnpairedSurrogateDoesNotOverwriteFileOrAcknowledgeDocument()
    {
        var directory = CreateDirectory();
        try
        {
            var path = Path.Combine(directory, "existing.txt");
            var original = Encoding.UTF8.GetBytes("existing bytes");
            await File.WriteAllBytesAsync(path, original);
            var session = new EditorSession();
            var window = session.CreateDocument("original");
            session.Engine.InsertText("dirty ");
            // A custom buffer or external API may supply invalid UTF-16 even
            // though normal keyboard input would not. The encoder must fail
            // rather than replacing the invalid code unit with U+FFFD.
            window.Document.Buffer.ReplaceLine(0, "left\uD800right");
            var lines = window.Document.Buffer.Snapshot();
            var version = window.Document.Version;
            var undoCount = session.Undo.Count;

            await Assert.ThrowsAsync<EncoderFallbackException>(
                async () => await new FileTextStorage().SaveAsync(
                    window.Document, path));

            Assert.Equal(original, await File.ReadAllBytesAsync(path));
            Assert.Equal(new[] { "existing.txt" },
                Directory.GetFiles(directory).Select(Path.GetFileName));
            Assert.Equal(lines, window.Document.Buffer.Snapshot());
            Assert.Equal(version, window.Document.Version);
            Assert.True(window.Document.IsDirty);
            Assert.Null(window.Document.FilePath);
            Assert.Equal(undoCount, session.Undo.Count);
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }

    [Fact]
    public async Task ValidReplacementCharacterAndSupplementaryUnicodeRoundTrip()
    {
        var directory = CreateDirectory();
        try
        {
            var path = Path.Combine(directory, "unicode.txt");
            var session = new EditorSession();
            var view = session.CreateDocument("ok \uFFFD \U0001F984\nzweite Zeile");
            var storage = new FileTextStorage();

            await storage.SaveAsync(view.Document, path);
            var loaded = await storage.LoadAsync(path);

            // A valid U+FFFD has its own legal UTF-8 byte sequence. The
            // decoder rejects only malformed sequences, not this character
            // or correctly paired supplementary-plane Unicode.
            Assert.Equal("ok \uFFFD \U0001F984", loaded.Buffer.GetLine(0).Text);
            Assert.Equal("zweite Zeile", loaded.Buffer.GetLine(1).Text);
            Assert.False(loaded.IsDirty);
            Assert.Equal(Path.GetFullPath(path), loaded.FilePath);
            Assert.Equal(
                Encoding.UTF8.GetBytes("ok \uFFFD \U0001F984" + Environment.NewLine + "zweite Zeile"),
                await File.ReadAllBytesAsync(path));
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }

    [Fact]
    public async Task ExplicitAlternateEncodingStillLoadsAndSavesLatin1()
    {
        var directory = CreateDirectory();
        try
        {
            var path = Path.Combine(directory, "latin1.txt");
            var original = new byte[] {
                (byte)'g', (byte)'r', 0xFC, (byte)'n'
            };
            await File.WriteAllBytesAsync(path, original);

            // A host that consciously selects a non-UTF-8 provider is still
            // free to do so. The strict default is not a global restriction
            // on ITextStorage or on the historic Latin-1 file codec.
            var storage = new FileTextStorage(Encoding.Latin1);
            var document = await storage.LoadAsync(path);
            Assert.Equal("grün", document.Buffer.GetLine(0).Text);

            await storage.SaveAsync(document);
            Assert.Equal(original, await File.ReadAllBytesAsync(path));
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }

    private static string CreateDirectory()
    {
        var path = Path.Combine(
            Path.GetTempPath(), $"sasd-strict-utf8-{Guid.NewGuid():N}");
        Directory.CreateDirectory(path);
        return path;
    }
}
