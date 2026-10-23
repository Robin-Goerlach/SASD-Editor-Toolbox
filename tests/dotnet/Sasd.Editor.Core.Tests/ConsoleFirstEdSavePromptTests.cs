using Sasd.Editor.Commands;
using Sasd.Editor.Editing;
using Sasd.Editor.FirstEd.Sample;
using Sasd.Editor.IO;
using Sasd.Editor.Input;
using Sasd.Editor.Model;
using Xunit;

namespace Sasd.Editor.Core.Tests;

/// <summary>
/// Tests the FIRST-ED sample's first-save prompt policy without starting a
/// real terminal. All file writes use an in-memory codec so a failed test
/// cannot accidentally create or overwrite a user file.
/// </summary>
public sealed class ConsoleFirstEdSavePromptTests
{
    [Fact]
    public async Task FirstSaveOfUnnamedDocumentUsesEnteredNameAndAcknowledgesSnapshot()
    {
        var codec = new RecordingCodec();
        var session = new EditorSession(fileCodec: codec);
        var window = session.CreateDocument("before");
        session.Engine.InsertText("edited ");
        Assert.True(window.Document.IsDirty);
        Assert.Null(window.Document.FilePath);

        var binding = SaveBinding();
        Assert.True(ConsoleFirstEdSavePromptPolicy.RequiresPath(window.Document.FilePath));
        var request = ConsoleFirstEdSavePromptPolicy.Resolve(
            binding, visibleRows: 12, associatedPath: window.Document.FilePath,
            enteredPath: "  first-save.txt  ");

        Assert.NotNull(request);
        Assert.Equal(EditorCommandId.SaveFile, request.Id);
        Assert.Equal("first-save.txt", request.Text);
        Assert.Equal(12, request.PageSize);
        Assert.True(await new EditorCommandDispatcher(session).ExecuteAsync(request));

        // Save As publishes a snapshot, associates the document with the new
        // path, and clears Dirty. It does not create an additional Undo edit.
        Assert.Single(codec.Writes);
        Assert.Equal("first-save.txt", codec.Writes[0].Path);
        Assert.Equal("edited before", Assert.Single(codec.Writes[0].Lines).Text);
        Assert.Equal(Path.GetFullPath("first-save.txt"), window.Document.FilePath);
        Assert.False(window.Document.IsDirty);
        Assert.Equal(1, session.Undo.Count);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void AbandonedFirstSaveNeverConstructsAFileWrite(string? response)
    {
        var session = new EditorSession();
        var window = session.CreateDocument("not saved");

        var request = ConsoleFirstEdSavePromptPolicy.Resolve(
            SaveBinding(), visibleRows: 12,
            associatedPath: window.Document.FilePath, enteredPath: response);

        // A null result tells the actual host NOT to call the dispatcher.
        // In particular Escape, Ctrl-U and empty Enter must not cause an
        // overwrite, a document association or a phantom Undo transaction.
        Assert.Null(request);
        Assert.Null(window.Document.FilePath);
        Assert.False(window.Document.IsDirty);
        Assert.Equal(0, session.Undo.Count);
    }

    [Fact]
    public async Task SubsequentSaveUsesCurrentAssociationWithoutNewFilenamePrompt()
    {
        var codec = new RecordingCodec();
        var session = new EditorSession(fileCodec: codec);
        var window = session.CreateDocument("original");
        var binding = SaveBinding();
        var first = ConsoleFirstEdSavePromptPolicy.Resolve(
            binding, 10, window.Document.FilePath, "first-save.txt");
        Assert.NotNull(first);
        Assert.True(await new EditorCommandDispatcher(session).ExecuteAsync(first));

        // Once the first Save succeeded, Ctrl-K S is direct Save. The
        // policy deliberately disregards any stray filename response.
        session.Engine.InsertText("new ");
        Assert.True(window.Document.IsDirty);
        Assert.False(ConsoleFirstEdSavePromptPolicy.RequiresPath(window.Document.FilePath));
        var second = ConsoleFirstEdSavePromptPolicy.Resolve(
            binding, 10, window.Document.FilePath, "should-not-be-used.txt");

        Assert.NotNull(second);
        Assert.Null(second.Text);
        Assert.Equal(10, second.PageSize);
        Assert.True(await new EditorCommandDispatcher(session).ExecuteAsync(second));

        Assert.Equal(2, codec.Writes.Count);
        // First Save used the entered relative name. The acknowledged document
        // association is normalized to an absolute path for the next Save.
        Assert.Equal("first-save.txt", codec.Writes[0].Path);
        Assert.Equal(Path.GetFullPath("first-save.txt"), codec.Writes[1].Path);
        Assert.Equal("new original", Assert.Single(codec.Writes[1].Lines).Text);
        Assert.Equal(Path.GetFullPath("first-save.txt"), window.Document.FilePath);
        Assert.False(window.Document.IsDirty);
        Assert.Equal(1, session.Undo.Count);
    }

    [Fact]
    public async Task FirstSaveDecisionUsesSelectedDocumentRatherThanAnotherWindow()
    {
        var codec = new RecordingCodec();
        var session = new EditorSession(fileCodec: codec);
        var named = session.CreateDocument("named");
        var unnamed = session.CreateDocument("unnamed");
        session.SetCurrentWindow(named.WindowId);

        // Associate only the first document through the public Save path,
        // never by mutating a read-only document property in the test.
        var save = ConsoleFirstEdSavePromptPolicy.Resolve(
            SaveBinding(), 10, named.Document.FilePath, "named.txt");
        Assert.NotNull(save);
        Assert.True(await new EditorCommandDispatcher(session).ExecuteAsync(save));

        session.SetCurrentWindow(unnamed.WindowId);
        // No global filename/default destination is allowed to leak from one
        // window to another. The prompting decision belongs to each document.
        Assert.False(ConsoleFirstEdSavePromptPolicy.RequiresPath(named.Document.FilePath));
        Assert.True(ConsoleFirstEdSavePromptPolicy.RequiresPath(unnamed.Document.FilePath));
        Assert.Null(ConsoleFirstEdSavePromptPolicy.Resolve(
            SaveBinding(), 10, unnamed.Document.FilePath, null));
        Assert.Equal(Path.GetFullPath("named.txt"), named.Document.FilePath);
        Assert.Null(unnamed.Document.FilePath);
        Assert.Single(codec.Writes);
    }

    [Fact]
    public void SavePolicyRejectsOtherKeyBindingsRatherThanSilentlyRemappingCommands()
    {
        var wrong = new EditorCommandBinding(
            EditorCommandId.WriteFile, EditorCommandArgumentKind.FilePath);
        Assert.Throws<ArgumentException>(() =>
            ConsoleFirstEdSavePromptPolicy.Resolve(wrong, 10, null, "x.txt"));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("NO")]
    [InlineData("MAYBE")]
    public async Task ExistingFirstSaveTargetRequiresExactApprovalBeforeDispatch(
        string? answer)
    {
        // This is a complete, headless route through the FIRST-ED Ctrl-K/S
        // binding, Save As prompt policy, overwrite authorization and semantic
        // dispatcher. A missing YES must prevent the dispatcher from ever
        // reaching the codec; document identity, edits and Undo stay intact.
        // The recording codec deliberately avoids touching user files.
        var codec = new RecordingCodec();
        var session = new EditorSession(fileCodec: codec);
        var window = session.CreateDocument("original");
        session.Engine.InsertText("changed ");
        var initialUndo = session.Undo.Count;
        var version = window.Document.Version;
        var request = ConsoleFirstEdSavePromptPolicy.Resolve(
            SaveBinding(), 10, window.Document.FilePath, "existing.txt");
        Assert.NotNull(request);

        // The console host reports existence; no File.Exists call is needed
        // in this deterministic test. Confirmation is a prerequisite for
        // dispatch, not a substitute request that could discard the path.
        Assert.True(ConsoleFirstEdOverwritePolicy.RequiresConfirmation(
            request, targetExists: true));
        var authorized = ConsoleFirstEdOverwritePolicy.Confirm(request, answer);
        Assert.Null(authorized);
        Assert.Empty(codec.Writes);
        Assert.Null(window.Document.FilePath);
        Assert.True(window.Document.IsDirty);
        Assert.Equal(version, window.Document.Version);
        Assert.Equal(initialUndo, session.Undo.Count);
    }

    [Theory]
    [InlineData("YES")]
    [InlineData("yes")]
    [InlineData(" Yes ")]
    public async Task ApprovedFirstSaveRetainsFilenameAndNextSaveNeedsNoNewApproval(string answer)
    {
        var codec = new RecordingCodec();
        var session = new EditorSession(fileCodec: codec);
        var window = session.CreateDocument("original");
        session.Engine.InsertText("first ");
        var binding = SaveBinding();
        var first = ConsoleFirstEdSavePromptPolicy.Resolve(
            binding, 10, window.Document.FilePath, "existing.txt");
        Assert.NotNull(first);

        // An explicit YES confirmation is necessary for the *new* selected
        // destination. The shared resolver intentionally accepts casing and
        // surrounding spaces; it is not a case-sensitive secret token. The original semantic request must keep its Text
        // filename and PageSize when the user confirms overwriting.
        Assert.True(ConsoleFirstEdOverwritePolicy.RequiresConfirmation(first, true));
        var approved = ConsoleFirstEdOverwritePolicy.Confirm(first, answer);
        Assert.Same(first, approved);
        Assert.Equal("existing.txt", approved!.Text);
        Assert.Equal(10, approved.PageSize);
        Assert.True(await new EditorCommandDispatcher(session).ExecuteAsync(approved));
        Assert.False(window.Document.IsDirty);
        Assert.Single(codec.Writes);

        // A subsequent Ctrl-K/S uses this document's established file
        // association; an unrelated filename from an earlier dialog must
        // not redirect an ordinary Save to a different destination.
        session.Engine.InsertText("second ");
        var next = ConsoleFirstEdSavePromptPolicy.Resolve(
            binding, 10, window.Document.FilePath, "unwanted.txt");
        Assert.NotNull(next);
        Assert.Null(next.Text);
        Assert.False(ConsoleFirstEdOverwritePolicy.RequiresConfirmation(next, true));
        Assert.True(await new EditorCommandDispatcher(session).ExecuteAsync(next));
        Assert.Equal(2, codec.Writes.Count);
        Assert.Equal(Path.GetFullPath("existing.txt"), codec.Writes[1].Path);
        Assert.Equal(Path.GetFullPath("existing.txt"), window.Document.FilePath);
        Assert.False(window.Document.IsDirty);
    }

    private static EditorCommandBinding SaveBinding()
    {
        var keyMap = new FirstEdKeyMap();
        Assert.Equal(EditorInputActionKind.PrefixPending,
            keyMap.Translate(EditorKeyStroke.Control('K')).Kind);
        var action = keyMap.Translate(EditorKeyStroke.ForCharacter('S'));
        Assert.Equal(EditorInputActionKind.Command, action.Kind);
        Assert.NotNull(action.Binding);
        Assert.Equal(EditorCommandId.SaveFile, action.Binding.CommandId);
        Assert.Equal(EditorCommandArgumentKind.None, action.Binding.ArgumentKind);
        return action.Binding;
    }

    private sealed class RecordingCodec : IEditorFileCodec
    {
        internal List<(string Path, IReadOnlyList<EditorLineSnapshot> Lines)> Writes { get; } = [];

        public ValueTask<IReadOnlyList<EditorLineSnapshot>> ReadAsync(
            string path, CancellationToken cancellationToken = default) =>
            ValueTask.FromResult<IReadOnlyList<EditorLineSnapshot>>(
                Array.Empty<EditorLineSnapshot>());

        public ValueTask WriteAsync(
            string path, IReadOnlyList<EditorLineSnapshot> lines,
            CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            // Snapshot the provider input so subsequent document edits cannot
            // accidentally change what the test considers to have been saved.
            Writes.Add((path, lines.ToArray()));
            return ValueTask.CompletedTask;
        }
    }
}
