using Sasd.Editor.Commands;
using Sasd.Editor.Editing;
using Sasd.Editor.Hooks;
using Sasd.Editor.IO;
using Sasd.Editor.Model;
using Xunit;

namespace Sasd.Editor.Core.Tests;

/// <summary>
/// Verifies that editor failures expose stable semantic IDs/codes/resource keys
/// independently from exception wording and host presentation.
/// </summary>
public sealed class EditorErrorModelTests
{
    [Theory]
    [InlineData(EditorErrorId.InvalidCommandArgument, "EDITOR_COMMAND_INVALID_ARGUMENT", "Editor.Errors.Command.InvalidArgument")]
    [InlineData(EditorErrorId.CommandRejected, "EDITOR_COMMAND_REJECTED", "Editor.Errors.Command.Rejected")]
    [InlineData(EditorErrorId.OperationFailed, "EDITOR_OPERATION_FAILED", "Editor.Errors.Operation.Failed")]
    [InlineData(EditorErrorId.FileIoFailed, "EDITOR_FILE_IO_FAILED", "Editor.Errors.File.IoFailed")]
    [InlineData(EditorErrorId.AccessDenied, "EDITOR_ACCESS_DENIED", "Editor.Errors.Access.Denied")]
    [InlineData(EditorErrorId.UnsupportedOperation, "EDITOR_OPERATION_UNSUPPORTED", "Editor.Errors.Operation.Unsupported")]
    [InlineData(EditorErrorId.TypeaheadOverflow, "EDITOR_TYPEAHEAD_OVERFLOW", "Editor.Errors.Input.TypeaheadOverflow")]
    [InlineData(EditorErrorId.OperationAborted, "EDITOR_OPERATION_ABORTED", "Editor.Errors.Operation.Aborted")]
    public void Catalog_UsesStableMachineAndResourceIdentifiers(
        EditorErrorId id,
        string code,
        string resourceKey)
    {
        var descriptor = EditorErrorCatalog.Describe(id);

        Assert.Equal(id, descriptor.Id);
        Assert.Equal(code, descriptor.Code);
        Assert.Equal(resourceKey, descriptor.ResourceKey);
        Assert.False(string.IsNullOrWhiteSpace(descriptor.DefaultMessage));
    }

    [Fact]
    public async Task BoundaryValidation_PreservesTypedValidationDetail()
    {
        var hooks = new RecordingHooks();
        var session = new EditorSession(hooks);
        session.CreateDocument("alpha");
        var dispatcher = new EditorCommandDispatcher(session);

        Assert.False(await dispatcher.ExecuteAsync(
            new(EditorCommandId.GoToLine, Number: 0)));

        var error = Assert.Single(hooks.Errors);
        Assert.Equal(EditorErrorId.InvalidCommandArgument, error.Id);
        Assert.Equal("EDITOR_COMMAND_INVALID_ARGUMENT", error.Code);
        Assert.Equal("Editor.Errors.Command.InvalidArgument", error.ResourceKey);
        Assert.Equal(EditorCommandValidationCode.NumberOutOfRange.ToString(), error.DetailCode);
        Assert.Equal(nameof(EditorCommandRequest.Number), error.ParameterName);
    }

    [Fact]
    public async Task StatefulCompatibilityRejection_UsesCommandRejectedError()
    {
        var hooks = new RecordingHooks();
        var session = new EditorSession(hooks);
        session.CreateDocument("alpha");
        var dispatcher = new EditorCommandDispatcher(session);

        // A single displayed window cannot be deleted. Unlike ordinary movement
        // at a file boundary, this historical command is an actual error case.
        Assert.False(await dispatcher.ExecuteAsync(new(EditorCommandId.DeleteWindow)));

        var error = Assert.Single(hooks.Errors);
        Assert.Equal(EditorErrorId.CommandRejected, error.Id);
        Assert.Equal(EditorCommandId.DeleteWindow.ToString(), error.DetailCode);
        Assert.Equal("EDITOR_COMMAND_REJECTED", error.Code);
    }

    [Fact]
    public async Task FileIOException_IsReportedWithoutUsingExceptionTextAsUiMessage()
    {
        var exception = new IOException("diagnostic path should not become the UI contract");
        var hooks = new RecordingHooks();
        var session = new EditorSession(hooks, new ThrowingCodec(exception));
        session.CreateDocument("alpha");
        var dispatcher = new EditorCommandDispatcher(session);

        Assert.False(await dispatcher.ExecuteAsync(
            new(EditorCommandId.ReadFile, Text: "input.txt")));

        var error = Assert.Single(hooks.Errors);
        Assert.Equal(EditorErrorId.FileIoFailed, error.Id);
        Assert.Equal("EDITOR_FILE_IO_FAILED", error.Code);
        Assert.Equal("Editor.Errors.File.IoFailed", error.ResourceKey);
        Assert.Same(exception, error.Exception);
        Assert.NotEqual(exception.Message, error.Message);
    }

    [Fact]
    public void InputErrors_UseSameResourceModelAsCommandErrors()
    {
        var overflow = EditorErrorCatalog.TypeaheadOverflow();
        var aborted = EditorErrorCatalog.OperationAborted();

        Assert.Equal(EditorErrorId.TypeaheadOverflow, overflow.Id);
        Assert.Equal("EDITOR_TYPEAHEAD_OVERFLOW", overflow.Code);
        Assert.Equal(EditorErrorId.OperationAborted, aborted.Id);
        Assert.Equal("EDITOR_OPERATION_ABORTED", aborted.Code);
    }

    private sealed class RecordingHooks : IEditorHooks
    {
        public List<EditorError> Errors { get; } = [];

        public ValueTask OnErrorAsync(
            EditorSession session,
            EditorError error,
            CancellationToken cancellationToken)
        {
            Errors.Add(error);
            return ValueTask.CompletedTask;
        }
    }

    private sealed class ThrowingCodec(Exception exception) : IEditorFileCodec
    {
        public ValueTask<IReadOnlyList<EditorLineSnapshot>> ReadAsync(
            string path,
            CancellationToken cancellationToken = default) =>
            ValueTask.FromException<IReadOnlyList<EditorLineSnapshot>>(exception);

        public ValueTask WriteAsync(
            string path,
            IReadOnlyList<EditorLineSnapshot> lines,
            CancellationToken cancellationToken = default) =>
            ValueTask.FromException(exception);
    }
}
