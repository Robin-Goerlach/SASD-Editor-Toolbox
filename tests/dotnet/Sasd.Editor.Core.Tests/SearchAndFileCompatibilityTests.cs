using Sasd.Editor.Commands;
using Sasd.Editor.Editing;
using Sasd.Editor.IO;
using Sasd.Editor.Model;
using Xunit;

namespace Sasd.Editor.Core.Tests;

public sealed class SearchAndFileCompatibilityTests
{
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
