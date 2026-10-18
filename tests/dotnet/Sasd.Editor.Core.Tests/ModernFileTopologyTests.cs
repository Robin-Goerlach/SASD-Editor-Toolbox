using Sasd.Editor.Editing;
using Sasd.Editor.IO;
using Sasd.Editor.Model;
using Xunit;

namespace Sasd.Editor.Core.Tests;

/// <summary>
/// Prevent a modern UTF-8 document from changing its logical line count merely
/// because a custom buffer writer supplied physical line separators as text.
/// The legacy byte codec has a separate, stricter 0x8D delimiter policy.
/// </summary>
public sealed class ModernFileTopologyTests
{
    [Theory]
    [InlineData("a\rb")]
    [InlineData("a\nb")]
    [InlineData("a\r\nb")]
    public async Task Save_EmbeddedLineBreakRejectsWithoutReplacingExistingFile(string lineText)
    {
        var directory = CreateDirectory();
        try
        {
            var path = Path.Combine(directory, "existing.txt");
            await File.WriteAllTextAsync(path, "preserve");
            var session = new EditorSession();
            var window = session.CreateDocument("original");
            session.Engine.InsertText("dirty ");
            window.Document.Buffer.ReplaceLine(0, lineText);
            var snapshot = window.Document.Buffer.Snapshot();

            await Assert.ThrowsAsync<InvalidDataException>(
                async () => await new FileTextStorage().SaveAsync(window.Document, path));

            Assert.Equal("preserve", await File.ReadAllTextAsync(path));
            Assert.Equal(snapshot, window.Document.Buffer.Snapshot());
            Assert.True(window.Document.IsDirty);
            Assert.Null(window.Document.FilePath);
            Assert.Equal(1, session.Undo.Count);
            Assert.Equal(new[] { "existing.txt" },
                Directory.GetFiles(directory).Select(Path.GetFileName));
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }

    [Theory]
    [InlineData("invalid")]
    [InlineData(" \r\n")]
    public async Task Save_NonNewlineSeparatorRejectedBeforeTouchingTarget(string configuredNewline)
    {
        var directory = CreateDirectory();
        try
        {
            var path = Path.Combine(directory, "target.txt");
            await File.WriteAllTextAsync(path, "previous");
            var session = new EditorSession();
            var view = session.CreateDocument("first\nsecond");
            session.Engine.InsertText("dirty ");
            view.Document.SetPersistenceMetadata(null, configuredNewline);

            await Assert.ThrowsAsync<InvalidDataException>(
                async () => await new FileTextStorage().SaveAsync(view.Document, path));

            Assert.Equal("previous", await File.ReadAllTextAsync(path));
            Assert.True(view.Document.IsDirty);
            Assert.Equal(1, session.Undo.Count);
            Assert.Equal(new[] { "target.txt" },
                Directory.GetFiles(directory).Select(Path.GetFileName));
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }

    [Fact]
    public void SaveAcknowledgement_ChangedNewlineDoesNotClearDirtyState()
    {
        var session = new EditorSession();
        var view = session.CreateDocument("first\nsecond");
        session.Engine.InsertText("modified ");
        var document = view.Document;
        var originalLines = document.Buffer.Snapshot();
        var version = document.Version;
        var originalNewLine = document.NewLine;
        var differentNewLine = originalNewLine == "\r\n" ? "\n" : "\r\n";
        document.SetPersistenceMetadata(null, differentNewLine);

        // Even if the document version and text match, the serialized file
        // uses a stale newline style after this concurrent metadata change.
        Assert.False(document.MarkSnapshotSaved(
            version, originalLines, writtenNewLine: originalNewLine));
        Assert.True(document.IsDirty);
        Assert.Equal(version, document.Version);
        Assert.Equal(1, session.Undo.Count);
    }

    [Fact]
    public async Task ModernUtf8_AllowsU008dInsideTextWithoutTreatingItAsWrap()
    {
        var directory = CreateDirectory();
        try
        {
            var path = Path.Combine(directory, "modern.txt");
            var session = new EditorSession();
            var view = session.CreateDocument("a\u008Db\nsecond");

            await new FileTextStorage().SaveAsync(view.Document, path);
            var loaded = await new FileTextStorage().LoadAsync(path);

            Assert.Equal(2, loaded.Buffer.LineCount);
            Assert.Equal("a\u008Db", loaded.Buffer.GetLine(0).Text);
            Assert.Equal(EditorLineFlags.None, loaded.Buffer.GetLine(0).Flags);
            Assert.Equal("second", loaded.Buffer.GetLine(1).Text);
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }

    private static string CreateDirectory()
    {
        var directory = Path.Combine(Path.GetTempPath(), $"sasd-modern-lines-{Guid.NewGuid():N}");
        Directory.CreateDirectory(directory);
        return directory;
    }
}
