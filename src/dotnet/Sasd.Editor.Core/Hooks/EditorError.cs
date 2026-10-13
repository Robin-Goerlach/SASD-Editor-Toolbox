using Sasd.Editor.Commands;

namespace Sasd.Editor.Hooks;

/// <summary>
/// Stable semantic categories for errors reported by the reusable editor core.
/// </summary>
/// <remarks>
/// The identifiers are intentionally modern and language-neutral. They are not a
/// copy of the historical Turbo Editor Toolbox numeric error catalog. Hosts can
/// translate these categories into localized text, telemetry or UI-specific
/// presentation without changing editor behavior.
/// </remarks>
public enum EditorErrorId
{
    InvalidCommandArgument,
    CommandRejected,
    OperationFailed,
    FileIoFailed,
    AccessDenied,
    UnsupportedOperation,
    TypeaheadOverflow,
    OperationAborted
}

/// <summary>
/// Immutable metadata associated with one <see cref="EditorErrorId"/>.
/// </summary>
/// <param name="Id">Typed semantic identifier.</param>
/// <param name="Code">Stable machine-readable code suitable for logs and cross-language tests.</param>
/// <param name="ResourceKey">Stable localization/resource lookup key owned by the host.</param>
/// <param name="DefaultMessage">English fallback text when the host has no resource for the key.</param>
public sealed record EditorErrorDescriptor(
    EditorErrorId Id,
    string Code,
    string ResourceKey,
    string DefaultMessage);

/// <summary>
/// Host-neutral error notification passed through <see cref="IEditorHooks.OnErrorAsync"/>.
/// </summary>
/// <remarks>
/// <see cref="Code"/> and <see cref="ResourceKey"/> are stable contract values.
/// <see cref="Message"/> is a fallback diagnostic, not a localization contract.
/// The original exception is retained for diagnostics but hosts should avoid
/// exposing exception text blindly when it can contain paths or environment data.
/// </remarks>
public sealed record EditorError
{
    internal EditorError(
        EditorErrorId id,
        string code,
        string resourceKey,
        string message,
        Exception? exception = null,
        string? detailCode = null,
        string? parameterName = null)
    {
        Id = id;
        Code = code;
        ResourceKey = resourceKey;
        Message = message;
        Exception = exception;
        DetailCode = detailCode;
        ParameterName = parameterName;
    }

    public EditorErrorId Id { get; }
    public string Code { get; }
    public string ResourceKey { get; }
    public string Message { get; }
    public Exception? Exception { get; }

    /// <summary>
    /// Optional finer-grained machine-readable detail. For command validation
    /// this carries the validation category without changing the top-level error.
    /// </summary>
    public string? DetailCode { get; }

    /// <summary>
    /// Optional semantic request field associated with the failure.
    /// </summary>
    public string? ParameterName { get; }
}

/// <summary>
/// Creates <see cref="EditorError"/> values from stable descriptors.
/// </summary>
/// <remarks>
/// Keeping creation in one catalog prevents hosts and command processors from
/// inventing slightly different codes for the same semantic error. Equivalent
/// C++/Java implementations can expose the same IDs/codes while using their own
/// native enum and resource mechanisms.
/// </remarks>
public static class EditorErrorCatalog
{
    public static EditorErrorDescriptor Describe(EditorErrorId id) =>
        id switch
        {
            EditorErrorId.InvalidCommandArgument => new(
                id,
                "EDITOR_COMMAND_INVALID_ARGUMENT",
                "Editor.Errors.Command.InvalidArgument",
                "The editor command arguments are invalid."),
            EditorErrorId.CommandRejected => new(
                id,
                "EDITOR_COMMAND_REJECTED",
                "Editor.Errors.Command.Rejected",
                "The editor command cannot be performed in the current editor state."),
            EditorErrorId.OperationFailed => new(
                id,
                "EDITOR_OPERATION_FAILED",
                "Editor.Errors.Operation.Failed",
                "The editor operation could not be completed."),
            EditorErrorId.FileIoFailed => new(
                id,
                "EDITOR_FILE_IO_FAILED",
                "Editor.Errors.File.IoFailed",
                "The editor could not complete the file operation."),
            EditorErrorId.AccessDenied => new(
                id,
                "EDITOR_ACCESS_DENIED",
                "Editor.Errors.Access.Denied",
                "Access to the requested resource was denied."),
            EditorErrorId.UnsupportedOperation => new(
                id,
                "EDITOR_OPERATION_UNSUPPORTED",
                "Editor.Errors.Operation.Unsupported",
                "The requested editor operation is not supported."),
            EditorErrorId.TypeaheadOverflow => new(
                id,
                "EDITOR_TYPEAHEAD_OVERFLOW",
                "Editor.Errors.Input.TypeaheadOverflow",
                "The typeahead buffer overflowed and pending input was cleared."),
            EditorErrorId.OperationAborted => new(
                id,
                "EDITOR_OPERATION_ABORTED",
                "Editor.Errors.Operation.Aborted",
                "The pending editor operation was aborted."),
            _ => throw new ArgumentOutOfRangeException(nameof(id), id, "Unknown editor error identifier.")
        };

    /// <summary>
    /// Creates a typed error using the catalog fallback message unless a more
    /// specific safe fallback is supplied by the caller.
    /// </summary>
    public static EditorError Create(
        EditorErrorId id,
        string? message = null,
        Exception? exception = null,
        string? detailCode = null,
        string? parameterName = null)
    {
        var descriptor = Describe(id);
        return new EditorError(
            descriptor.Id,
            descriptor.Code,
            descriptor.ResourceKey,
            message ?? descriptor.DefaultMessage,
            exception,
            detailCode,
            parameterName);
    }

    /// <summary>
    /// Converts a validated request failure into the common error contract.
    /// </summary>
    public static EditorError InvalidCommandArgument(EditorCommandValidationFailure failure)
    {
        ArgumentNullException.ThrowIfNull(failure);
        return Create(
            EditorErrorId.InvalidCommandArgument,
            failure.Message,
            detailCode: failure.Code.ToString(),
            parameterName: failure.ParameterName);
    }

    /// <summary>
    /// Reports a state-dependent compatibility rejection without treating normal
    /// no-op navigation commands as errors.
    /// </summary>
    public static EditorError CommandRejected(EditorCommandId commandId) =>
        Create(
            EditorErrorId.CommandRejected,
            detailCode: commandId.ToString());

    /// <summary>
    /// Maps expected operational exception families to stable editor error IDs.
    /// Cancellation is deliberately excluded and remains a control-flow signal.
    /// </summary>
    public static EditorError FromException(Exception exception)
    {
        ArgumentNullException.ThrowIfNull(exception);

        var id = exception switch
        {
            UnauthorizedAccessException => EditorErrorId.AccessDenied,
            IOException => EditorErrorId.FileIoFailed,
            NotSupportedException => EditorErrorId.UnsupportedOperation,
            ArgumentException => EditorErrorId.InvalidCommandArgument,
            InvalidOperationException => EditorErrorId.OperationFailed,
            _ => EditorErrorId.OperationFailed
        };

        // Use a catalog fallback rather than exception.Message. Exceptions are
        // retained for diagnostics, while user-facing hosts can localize by key.
        return Create(id, exception: exception);
    }

    public static EditorError TypeaheadOverflow() =>
        Create(EditorErrorId.TypeaheadOverflow);

    public static EditorError OperationAborted() =>
        Create(EditorErrorId.OperationAborted);
}
