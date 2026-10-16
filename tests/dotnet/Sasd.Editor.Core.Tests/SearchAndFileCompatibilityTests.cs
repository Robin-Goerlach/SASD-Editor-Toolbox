using System.Text;
using Sasd.Editor.Commands;
using Sasd.Editor.Editing;
using Sasd.Editor.IO;
using Sasd.Editor.Model;
using Xunit;

namespace Sasd.Editor.Core.Tests;

public sealed class SearchAndFileCompatibilityTests
{
    [Fact]
    public async Task FindNext_EmptyPromptRepeatsPreviousSearchInsteadOfRejectingIt()
    {
        var session = new EditorSession();
        var window = session.CreateDocument("alpha beta alpha");
        var dispatcher = new EditorCommandDispatcher(session);

        Assert.True(await dispatcher.ExecuteAsync(new(EditorCommandId.FindNext, Text: "alpha")));
        Assert.Equal(new TextPosition(0, 0), window.Cursor);
        Assert.True(await dispatcher.ExecuteAsync(new(EditorCommandId.FindNext, Text: "")));

        Assert.Equal("alpha", session.Search.LastPattern);
        Assert.Equal(new TextPosition(0, 11), window.Cursor);
        Assert.False(window.Document.IsDirty);
        Assert.Equal(0, session.Undo.Count);
    }

    [Fact]
    public async Task FindNext_EmptyPromptWithNoSavedPatternIsHarmless()
    {
        var session = new EditorSession();
        var window = session.CreateDocument("alpha");
        window.Cursor = new TextPosition(0, 2);

        Assert.False(await new EditorCommandDispatcher(session).ExecuteAsync(
            new(EditorCommandId.FindNext, Text: string.Empty)));

        Assert.Null(session.Search.LastPattern);
        Assert.Equal(new TextPosition(0, 2), window.Cursor);
        Assert.False(window.Document.IsDirty);
    }

    [Fact]
    public void FindNext_EmptyPatternReusesRememberedOptionsInDirectService()
    {
        var session = new EditorSession();
        var window = session.CreateDocument("Alpha alpha ALPHA");
        var options = new Sasd.Editor.Search.SearchOptions(CaseSensitive: true);
        var first = session.Search.FindNext("alpha", options);

        Assert.Equal(new TextPosition(0, 6), first);

        // Ignore the new argument options for the empty prompt: the historical
        // repeat request must preserve case-sensitive semantics.
        window.Cursor = new TextPosition(0, 10);
        var next = session.Search.FindNext("", new Sasd.Editor.Search.SearchOptions());
        Assert.Null(next);
        Assert.Equal(new TextPosition(0, 10), window.Cursor);
        Assert.Equal("alpha", session.Search.LastPattern);
    }

    [Fact]
    public async Task FindAgain_ContinuesAfterPreviousMatch()
    {
        var session = new EditorSession();
        session.CreateDocument("alpha beta alpha");
        var dispatcher = new EditorCommandDispatcher(session);

        Assert.True(await dispatcher.ExecuteAsync(new(EditorCommandId.FindNext, Text: "alpha")));
        Assert.Equal(new TextPosition(0, 0), session.CurrentWindow.Cursor);

        Assert.True(await dispatcher.ExecuteAsync(new(EditorCommandId.FindAgain)));
        Assert.Equal(new TextPosition(0, 11), session.CurrentWindow.Cursor);
    }

    [Fact]
    public async Task FindAgain_WithoutPreviousPattern_DoesNothing()
    {
        var session = new EditorSession();
        session.CreateDocument("alpha");
        var dispatcher = new EditorCommandDispatcher(session);

        Assert.False(await dispatcher.ExecuteAsync(new(EditorCommandId.FindAgain)));
        Assert.Equal(new TextPosition(0, 0), session.CurrentWindow.Cursor);
    }

    /// <summary>
    /// A delimiter hidden inside line content must not become a second
    /// logical line after decoding. Encode runs before the destination is
    /// touched, so every rejected input must preserve its previous bytes.
    /// </summary>
    [Theory]
    [InlineData("\r")]
    [InlineData("\n")]
    [InlineData("\u008D")]
    [InlineData("\u0100")]
    public async Task LegacyCodec_RejectsUnrepresentableLineTextWithoutTruncatingFile(string invalid)
    {
        var path = Path.GetTempFileName();
        try
        {
            var originalBytes = new byte[] { (byte)'o', (byte)'l', (byte)'d' };
            await File.WriteAllBytesAsync(path, originalBytes);

            var codec = new FirstEdLegacyFileCodec();
            await Assert.ThrowsAsync<EncoderFallbackException>(
                async () => await codec.WriteAsync(path, [new EditorLineSnapshot("a" + invalid + "b")]));

            Assert.Equal(originalBytes, await File.ReadAllBytesAsync(path));
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Fact]
    public async Task LegacyCodec_RoundTripsLatin1AndMixedHardSoftBoundaries()
    {
        var path = Path.GetTempFileName();
        try
        {
            var codec = new FirstEdLegacyFileCodec();
            await codec.WriteAsync(path,
            [
                new EditorLineSnapshot("grün", EditorLineFlags.Wrapped),
                new EditorLineSnapshot("älter"),
                new EditorLineSnapshot("Ende")
            ]);

            var expected = new byte[]
            {
                (byte)'g', (byte)'r', 0xFC, (byte)'n', 0x8D,
                0xE4, (byte)'l', (byte)'t', (byte)'e', (byte)'r',
                (byte)'\r', (byte)'\n',
                (byte)'E', (byte)'n', (byte)'d', (byte)'e'
            };
            Assert.Equal(expected, await File.ReadAllBytesAsync(path));

            var decoded = await codec.ReadAsync(path);
            Assert.Equal(3, decoded.Count);
            Assert.Equal("grün", decoded[0].Text);
            Assert.Equal(EditorLineFlags.Wrapped, decoded[0].Flags);
            Assert.Equal("älter", decoded[1].Text);
            Assert.Equal(EditorLineFlags.None, decoded[1].Flags);
            Assert.Equal("Ende", decoded[2].Text);
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Fact]
    public async Task LegacyCodec_DecodesHighBitCarriageReturnAsWrappedLine()
    {
        var path = Path.GetTempFileName();
        try
        {
            await File.WriteAllBytesAsync(path,
            [
                (byte)'o', (byte)'n', (byte)'e', FirstEdLegacyFileCodec.WrappedLineTerminator,
                (byte)'t', (byte)'w', (byte)'o'
            ]);

            var codec = new FirstEdLegacyFileCodec();
            var lines = await codec.ReadAsync(path);

            Assert.Equal(2, lines.Count);
            Assert.Equal("one", lines[0].Text);
            Assert.True(lines[0].Flags.HasFlag(EditorLineFlags.Wrapped));
            Assert.Equal("two", lines[1].Text);
            Assert.Equal(EditorLineFlags.None, lines[1].Flags);
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Fact]
    public async Task LegacyCodec_WritesWrappedSeparator()
    {
        var path = Path.GetTempFileName();
        try
        {
            var codec = new FirstEdLegacyFileCodec();
            await codec.WriteAsync(path,
            [
                new EditorLineSnapshot("one", EditorLineFlags.Wrapped),
                new EditorLineSnapshot("two")
            ]);

            var bytes = await File.ReadAllBytesAsync(path);
            Assert.Contains(FirstEdLegacyFileCodec.WrappedLineTerminator, bytes);

            var decoded = await codec.ReadAsync(path);
            Assert.Equal(2, decoded.Count);
            Assert.True(decoded[0].Flags.HasFlag(EditorLineFlags.Wrapped));
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Fact]
    public async Task ReadFile_InsertsAfterCurrentLineAndPreservesCursor()
    {
        var path = Path.GetTempFileName();
        try
        {
            await File.WriteAllBytesAsync(path,
            [
                (byte)'i', (byte)'n', (byte)'s', (byte)'e', (byte)'r', (byte)'t', (byte)'e', (byte)'d',
                (byte)'\r', (byte)'\n',
                (byte)'m', (byte)'o', (byte)'r', (byte)'e'
            ]);

            var session = new EditorSession();
            session.CreateDocument("before\nafter");
            session.CurrentWindow.Cursor = new TextPosition(0, 3);
            var dispatcher = new EditorCommandDispatcher(session);

            Assert.True(await dispatcher.ExecuteAsync(new(EditorCommandId.ReadFile, Text: path)));

            Assert.Equal(4, session.CurrentWindow.Document.Buffer.LineCount);
            Assert.Equal("before", session.CurrentWindow.Document.Buffer.GetLine(0).Text);
            Assert.Equal("inserted", session.CurrentWindow.Document.Buffer.GetLine(1).Text);
            Assert.Equal("more", session.CurrentWindow.Document.Buffer.GetLine(2).Text);
            Assert.Equal("after", session.CurrentWindow.Document.Buffer.GetLine(3).Text);
            Assert.Equal(new TextPosition(0, 3), session.CurrentWindow.Cursor);
            Assert.True(session.CurrentWindow.Document.IsDirty);
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Fact]
    public async Task SaveFile_UsesAssociatedPathAndClearsDirtyState()
    {
        var path = Path.GetTempFileName();
        try
        {
            var session = new EditorSession();
            session.CreateDocument("text");
            session.CurrentWindow.Document.SetPersistenceMetadata(path);
            session.Engine.MoveEndOfLine();
            session.Engine.InsertText(" changed");
            Assert.True(session.CurrentWindow.Document.IsDirty);

            var dispatcher = new EditorCommandDispatcher(session);
            Assert.True(await dispatcher.ExecuteAsync(new(EditorCommandId.SaveFile)));

            Assert.False(session.CurrentWindow.Document.IsDirty);
            var bytes = await File.ReadAllBytesAsync(path);
            Assert.NotEmpty(bytes);
        }
        finally
        {
            File.Delete(path);
        }
    }
}
