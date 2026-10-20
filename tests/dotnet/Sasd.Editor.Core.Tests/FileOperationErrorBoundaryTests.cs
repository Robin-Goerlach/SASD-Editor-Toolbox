using System.Security;
using System.Text;
using Sasd.Editor.Commands;
using Sasd.Editor.Editing;
using Sasd.Editor.Hooks;
using Sasd.Editor.IO;
using Sasd.Editor.Model;
using Xunit;

namespace Sasd.Editor.Core.Tests;

/// <summary>
/// Verifies errors at the *semantic file command* boundary rather than merely
/// testing isolated exception constructors. Real legacy output is used for
/// unsafe encodings; a deterministic codec simulates restricted providers.
/// </summary>
/// <remarks>
/// These cases deliberately do not depend on file ACLs, which differ between
/// Windows and Unix (and can be bypassed under privileged CI processes).
/// Cancellation has its own control-flow contract and is not translated here.
/// </remarks>
public sealed class FileOperationErrorBoundaryTests
{
    [Theory]
    [InlineData(EditorCommandId.SaveFile, "\u0100")]
    [InlineData(EditorCommandId.WriteFile, "\u0100")]
    [InlineData(EditorCommandId.SaveFile, "\u008D")]
    [InlineData(EditorCommandId.WriteFile, "\u008D")]
    public async Task UnrepresentableLegacyText_ReportsFileFailureWithoutReplacingDestination(
        EditorCommandId commandId, string invalidCharacter)
    {
        var directory = Path.Combine(Path.GetTempPath(), $"sasd-encoding-{Guid.NewGuid():N}");
        Directory.CreateDirectory(directory);
        try
        {
            var target = Path.Combine(directory, "existing.txt");
            var oldBytes = new byte[] { 0x6F, 0x6C, 0x64 };
            await File.WriteAllBytesAsync(target, oldBytes);

            var hooks = new RecordingHooks();
            var session = new EditorSession(hooks);
            var window = session.CreateDocument($"prefix{invalidCharacter}suffix");
            session.Engine.InsertText("edited ");
            var unchangedLines = window.Document.Buffer.Snapshot();
            var originalVersion = window.Document.Version;
            var originalCursor = window.Cursor;
            var originalUndo = session.Undo.Count;

            // The real FIRST-ED codec must reject loss of information BEFORE
            // StagedFileWriter creates a temporary file or publishes output.
            // The dispatcher must report a typed file failure rather than
            // bubbling EncoderFallbackException into a terminal application.
            var completed = await new EditorCommandDispatcher(session)
                .ExecuteAsync(new EditorCommandRequest(commandId, Text: target));

            Assert.False(completed);
            var error = Assert.Single(hooks.Errors);
            Assert.Equal(EditorErrorId.FileIoFailed, error.Id);
            Assert.Equal("EDITOR_FILE_IO_FAILED", error.Code);
            Assert.Equal("Editor.Errors.File.IoFailed", error.ResourceKey);
            Assert.IsType<EncoderFallbackException>(error.Exception);
            Assert.NotEqual(error.Exception!.Message, error.Message);

            Assert.Equal(oldBytes, await File.ReadAllBytesAsync(target));
            Assert.Equal(new[] { "existing.txt" },
                Directory.GetFiles(directory).Select(Path.GetFileName));
            Assert.Equal(unchangedLines, window.Document.Buffer.Snapshot());
            Assert.Equal(originalVersion, window.Document.Version);
            Assert.Equal(originalCursor, window.Cursor);
            Assert.Equal(originalUndo, session.Undo.Count);
            Assert.True(window.Document.IsDirty);
            Assert.Null(window.Document.FilePath);
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }

    [Theory]
    [InlineData(EditorCommandId.ReadFile)]
    [InlineData(EditorCommandId.WriteFile)]
    [InlineData(EditorCommandId.SaveFile)]
    public async Task SecurityExceptionFromProvider_ReportsAccessDeniedWithoutEditing(
        EditorCommandId commandId)
    {
        var denial = new SecurityException("sensitive provider diagnostic");
        var hooks = new RecordingHooks();
        var session = new EditorSession(hooks, new DeniedCodec(denial));
        var window = session.CreateDocument("unchanged");
        session.Engine.InsertText("new ");

        // An injected provider failure is deterministic on every platform.
        // In particular, Save must not claim that the unwritten snapshot has
        // been persisted or change the document's associated filename.
        var before = window.Document.Buffer.Snapshot();
        var beforeCursor = window.Cursor;
        var beforeVersion = window.Document.Version;
        var beforeUndo = session.Undo.Count;
        var succeeded = await new EditorCommandDispatcher(session)
            .ExecuteAsync(new EditorCommandRequest(commandId, Text: "denied.txt"));

        Assert.False(succeeded);
        var error = Assert.Single(hooks.Errors);
        Assert.Equal(EditorErrorId.AccessDenied, error.Id);
        Assert.Equal("EDITOR_ACCESS_DENIED", error.Code);
        Assert.Equal("Editor.Errors.Access.Denied", error.ResourceKey);
        Assert.Same(denial, error.Exception);
        Assert.NotEqual(denial.Message, error.Message);
        Assert.Equal(before, window.Document.Buffer.Snapshot());
        Assert.Equal(beforeVersion, window.Document.Version);
        Assert.Equal(beforeCursor, window.Cursor);
        Assert.Equal(beforeUndo, session.Undo.Count);
        Assert.True(window.Document.IsDirty);
        Assert.Null(window.Document.FilePath);
    }

    [Fact]
    public void InvalidCommandArgumentsStillUseTheirOwnErrorCategory()
    {
        // Do not solve file-encoding errors by treating *every* ArgumentException
        // as I/O failure: invalid semantic command requests remain a different
        // category in the shared portable error vocabulary.
        Assert.Equal(EditorErrorId.InvalidCommandArgument,
            EditorErrorCatalog.FromException(
                new ArgumentException("bad command payload")).Id);
        Assert.Equal(EditorErrorId.FileIoFailed,
            EditorErrorCatalog.FromException(
                new EncoderFallbackException("invalid byte encoding")).Id);
    }

    private sealed class RecordingHooks : IEditorHooks
    {
        public List<EditorError> Errors { get; } = [];

        public ValueTask OnErrorAsync(
            EditorSession session, EditorError error, CancellationToken cancellationToken)
        {
            Errors.Add(error);
            return ValueTask.CompletedTask;
        }
    }

    private sealed class DeniedCodec(SecurityException denial) : IEditorFileCodec
    {
        public ValueTask<IReadOnlyList<EditorLineSnapshot>> ReadAsync(
            string path, CancellationToken cancellationToken = default) =>
            ValueTask.FromException<IReadOnlyList<EditorLineSnapshot>>(denial);

        public ValueTask WriteAsync(
            string path, IReadOnlyList<EditorLineSnapshot> lines,
            CancellationToken cancellationToken = default) =>
            ValueTask.FromException(denial);
    }
}
