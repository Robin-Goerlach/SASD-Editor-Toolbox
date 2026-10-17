using Sasd.Editor.IO;
using Sasd.Editor.Model;
using Xunit;

namespace Sasd.Editor.Core.Tests;

/// <summary>
/// Exercises the byte-level FIRST-ED high-bit carriage-return convention.
/// These are compatibility boundary tests, independent of terminal rendering
/// and modern UTF-8 whole-document storage.
/// </summary>
public sealed class LegacyCodecBoundaryTests
{
    [Fact]
    public async Task TrailingWrappedMarker_PreservesFinalOutgoingSoftBoundary()
    {
        await WithTemporaryFile(async path =>
        {
            var codec = new FirstEdLegacyFileCodec();
            await codec.WriteAsync(path,
                [new EditorLineSnapshot("last", EditorLineFlags.Wrapped)]);

            Assert.Equal(new byte[] { (byte)'l', (byte)'a', (byte)'s', (byte)'t', 0x8D },
                await File.ReadAllBytesAsync(path));
            var decoded = await codec.ReadAsync(path);
            Assert.Single(decoded);
            Assert.Equal("last", decoded[0].Text);
            Assert.Equal(EditorLineFlags.Wrapped, decoded[0].Flags);
        });
    }

    [Fact]
    public async Task WrappedBoundaryWithFollowingLf_IsDecodedAsOneLogicalBoundary()
    {
        await WithTemporaryFile(async path =>
        {
            await File.WriteAllBytesAsync(path, [(byte)'a', 0x8D, 0x0A, (byte)'b']);
            var lines = await new FirstEdLegacyFileCodec().ReadAsync(path);

            Assert.Equal(2, lines.Count);
            Assert.Equal("a", lines[0].Text);
            Assert.Equal(EditorLineFlags.Wrapped, lines[0].Flags);
            Assert.Equal("b", lines[1].Text);
            Assert.Equal(EditorLineFlags.None, lines[1].Flags);
        });
    }

    [Fact]
    public async Task ConsecutiveSoftAndHardDelimiters_PreserveIntentionalBlankLines()
    {
        await WithTemporaryFile(async path =>
        {
            await File.WriteAllBytesAsync(path,
                [(byte)'a', 0x8D, 0x8D, (byte)'\r', (byte)'\n', (byte)'z']);
            var lines = await new FirstEdLegacyFileCodec().ReadAsync(path);

            Assert.Equal(new[] { "a", "", "", "z" }, lines.Select(x => x.Text));
            Assert.Equal(new[]
            {
                EditorLineFlags.Wrapped,
                EditorLineFlags.Wrapped,
                EditorLineFlags.None,
                EditorLineFlags.None
            }, lines.Select(x => x.Flags));
        });
    }

    [Fact]
    public async Task MixedCrLfCrAndLf_AreOrdinaryNonWrappedBoundaries()
    {
        await WithTemporaryFile(async path =>
        {
            await File.WriteAllBytesAsync(path,
                [(byte)'a', (byte)'\r', (byte)'\n', (byte)'b', (byte)'\r',
                 (byte)'c', (byte)'\n', (byte)'d']);
            var lines = await new FirstEdLegacyFileCodec().ReadAsync(path);

            Assert.Equal(new[] { "a", "b", "c", "d" }, lines.Select(x => x.Text));
            Assert.All(lines, line => Assert.Equal(EditorLineFlags.None, line.Flags));
        });
    }

    [Fact]
    public async Task Latin1ControlAndHighBytes_ArePreservedExceptReservedWrappedByte()
    {
        await WithTemporaryFile(async path =>
        {
            var raw = new byte[] { 0x00, 0x7F, 0x8C, 0x8E, 0xFF };
            var content = new string(raw.Select(value => (char)value).ToArray());
            var codec = new FirstEdLegacyFileCodec();

            await codec.WriteAsync(path, [new EditorLineSnapshot(content)]);
            Assert.Equal(raw, await File.ReadAllBytesAsync(path));
            var decoded = await codec.ReadAsync(path);
            Assert.Equal(content, Assert.Single(decoded).Text);
        });
    }

    [Fact]
    public async Task EmptyPhysicalFile_IsDecodedAsNoImportedLines()
    {
        await WithTemporaryFile(async path =>
        {
            await File.WriteAllBytesAsync(path, []);
            var codec = new FirstEdLegacyFileCodec();
            Assert.Empty(await codec.ReadAsync(path));

            await codec.WriteAsync(path, Array.Empty<EditorLineSnapshot>());
            Assert.Empty(await File.ReadAllBytesAsync(path));
            Assert.Empty(await codec.ReadAsync(path));
        });
    }

    [Fact]
    public async Task FinalOrdinaryCrLf_DoesNotInventExtraBlankLine()
    {
        await WithTemporaryFile(async path =>
        {
            await File.WriteAllBytesAsync(path,
                [(byte)'a', (byte)'\r', (byte)'\n']);
            var decoded = await new FirstEdLegacyFileCodec().ReadAsync(path);

            Assert.Equal("a", Assert.Single(decoded).Text);
            Assert.Equal(EditorLineFlags.None, decoded[0].Flags);
        });
    }

    [Fact]
    public async Task TwoTrailingOrdinarySeparators_PreserveOneExplicitBlankLine()
    {
        await WithTemporaryFile(async path =>
        {
            await File.WriteAllBytesAsync(path,
                [(byte)'a', (byte)'\r', (byte)'\n', (byte)'\r', (byte)'\n']);
            var decoded = await new FirstEdLegacyFileCodec().ReadAsync(path);

            Assert.Equal(new[] { "a", "" }, decoded.Select(x => x.Text));
            Assert.All(decoded, line => Assert.Equal(EditorLineFlags.None, line.Flags));
        });
    }

    private static async Task WithTemporaryFile(Func<string, Task> action)
    {
        var path = Path.Combine(Path.GetTempPath(), $"sasd-firsted-bytes-{Guid.NewGuid():N}.tmp");
        try
        {
            await action(path);
        }
        finally
        {
            if (File.Exists(path))
            {
                File.Delete(path);
            }
        }
    }
}
