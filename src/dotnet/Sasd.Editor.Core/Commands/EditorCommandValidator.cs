namespace Sasd.Editor.Commands;

/// <summary>
/// Stable categories for semantic command request validation failures.
/// </summary>
/// <remarks>
/// These categories describe request problems without prescribing localized
/// user-facing wording. Hosts can later map them to resources or diagnostics.
/// </remarks>
public enum EditorCommandValidationCode
{
    UnknownCommand,
    InvalidPageSize,
    MissingText,
    InvalidControlCharacter,
    MissingNumber,
    MissingSecondNumber,
    NumberOutOfRange,
    MissingReplacement,
    InvalidFilePath
}

/// <summary>
/// Describes why a host-supplied semantic command request cannot be dispatched.
/// </summary>
public sealed record EditorCommandValidationFailure(
    EditorCommandValidationCode Code,
    string? ParameterName,
    string Message);

/// <summary>
/// Validates the host-facing semantic command envelope before editor state is
/// mutated or file I/O begins.
/// </summary>
/// <remarks>
/// Stateful rules such as margin ordering and donor-window capacity remain in
/// the compatibility service that owns that mutable state. This validator owns
/// request shape and stable value ranges only.
/// </remarks>
public static class EditorCommandValidator
{
    public const int FirstMarkerNumber = 1;
    public const int LastMarkerNumber = 20;

    public static EditorCommandValidationFailure? Validate(EditorCommandRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (!Enum.IsDefined(typeof(EditorCommandId), request.Id))
        {
            return Failure(
                EditorCommandValidationCode.UnknownCommand,
                nameof(EditorCommandRequest.Id),
                $"Unknown editor command value '{request.Id}'.");
        }

        if (request.PageSize.HasValue && request.PageSize.Value < 1)
        {
            return Failure(
                EditorCommandValidationCode.InvalidPageSize,
                nameof(EditorCommandRequest.PageSize),
                "Visible page size must be at least one text row.");
        }

        return request.Id switch
        {
            EditorCommandId.GoToLine
                or EditorCommandId.GoToColumn
                or EditorCommandId.GoToWindow
                or EditorCommandId.SetLeftMargin
                or EditorCommandId.SetRightMargin
                or EditorCommandId.SetTabWidth
                or EditorCommandId.SetUndoLimit =>
                RequireNumberAtLeast(request, 1),

            EditorCommandId.SetMarker
                or EditorCommandId.JumpMarker =>
                RequireNumberInRange(request, FirstMarkerNumber, LastMarkerNumber),

            EditorCommandId.CreateWindow =>
                RequireCreateWindowArguments(request),

            EditorCommandId.LinkWindow =>
                RequireTwoPositiveNumbers(request),

            EditorCommandId.DeleteWindow =>
                request.Number.HasValue ? RequireNumberAtLeast(request, 1) : null,

            EditorCommandId.InsertControlCharacter =>
                ValidateControlCharacter(request),

            EditorCommandId.FindNext =>
                RequirePresentText(request),

            EditorCommandId.ReplaceNext =>
                ValidateReplaceArguments(request),

            EditorCommandId.ReadFile
                or EditorCommandId.WriteFile =>
                RequireFilePath(request),

            _ => null
        };
    }

    /// <summary>
    /// Resolves replace arguments after <see cref="Validate"/> has succeeded.
    /// </summary>
    /// <remarks>
    /// <see cref="EditorCommandRequest.Text2"/> is the portable request shape.
    /// The older .NET-only NUL-packed payload remains accepted temporarily so
    /// existing callers are not broken during the transition.
    /// </remarks>
    internal static (string Pattern, string Replacement) GetReplaceArguments(EditorCommandRequest request)
    {
        if (request.Text2 is not null)
        {
            return (request.Text!, request.Text2);
        }

        var separator = request.Text!.IndexOf('\0');
        return (request.Text[..separator], request.Text[(separator + 1)..]);
    }

    private static EditorCommandValidationFailure? RequireCreateWindowArguments(EditorCommandRequest request)
    {
        var sizeFailure = RequireNumberAtLeast(request, 3);
        return sizeFailure ?? RequireSecondNumberAtLeast(request, 1);
    }

    private static EditorCommandValidationFailure? RequireTwoPositiveNumbers(EditorCommandRequest request)
    {
        var firstFailure = RequireNumberAtLeast(request, 1);
        return firstFailure ?? RequireSecondNumberAtLeast(request, 1);
    }

    private static EditorCommandValidationFailure? ValidateControlCharacter(EditorCommandRequest request)
    {
        if (request.Text is null)
        {
            return Failure(
                EditorCommandValidationCode.MissingText,
                nameof(EditorCommandRequest.Text),
                $"Command {request.Id} requires one character.");
        }

        return request.Text.Length == 1
            ? null
            : Failure(
                EditorCommandValidationCode.InvalidControlCharacter,
                nameof(EditorCommandRequest.Text),
                $"Command {request.Id} requires exactly one UTF-16 character.");
    }

    /// <summary>
    /// Distinguish an omitted search prompt (null, invalid request) from an
    /// explicitly submitted empty string (repeat the saved FIRST-ED pattern).
    /// </summary>
    private static EditorCommandValidationFailure? RequirePresentText(EditorCommandRequest request) =>
        request.Text is null
            ? Failure(
                EditorCommandValidationCode.MissingText,
                nameof(EditorCommandRequest.Text),
                $"Command {request.Id} requires a search value (empty repeats the previous pattern).")
            : null;

    private static EditorCommandValidationFailure? RequireNonEmptyText(EditorCommandRequest request) =>
        string.IsNullOrEmpty(request.Text)
            ? Failure(
                EditorCommandValidationCode.MissingText,
                nameof(EditorCommandRequest.Text),
                $"Command {request.Id} requires non-empty text.")
            : null;

    private static EditorCommandValidationFailure? ValidateReplaceArguments(EditorCommandRequest request)
    {
        if (request.Text2 is not null)
        {
            return string.IsNullOrEmpty(request.Text)
                ? Failure(
                    EditorCommandValidationCode.MissingText,
                    nameof(EditorCommandRequest.Text),
                    "ReplaceNext requires a non-empty search pattern.")
                : null;
        }

        if (string.IsNullOrEmpty(request.Text))
        {
            return Failure(
                EditorCommandValidationCode.MissingText,
                nameof(EditorCommandRequest.Text),
                "ReplaceNext requires a non-empty search pattern.");
        }

        var separator = request.Text.IndexOf('\0');
        return separator > 0
            ? null
            : Failure(
                EditorCommandValidationCode.MissingReplacement,
                nameof(EditorCommandRequest.Text2),
                "ReplaceNext requires a replacement value; an empty replacement is valid.");
    }

    private static EditorCommandValidationFailure? RequireFilePath(EditorCommandRequest request) =>
        string.IsNullOrWhiteSpace(request.Text)
            ? Failure(
                EditorCommandValidationCode.InvalidFilePath,
                nameof(EditorCommandRequest.Text),
                $"Command {request.Id} requires a non-blank file path.")
            : null;

    private static EditorCommandValidationFailure? RequireNumberAtLeast(
        EditorCommandRequest request,
        int minimum)
    {
        if (!request.Number.HasValue)
        {
            return Failure(
                EditorCommandValidationCode.MissingNumber,
                nameof(EditorCommandRequest.Number),
                $"Command {request.Id} requires a numeric value.");
        }

        return request.Number.Value >= minimum
            ? null
            : Failure(
                EditorCommandValidationCode.NumberOutOfRange,
                nameof(EditorCommandRequest.Number),
                $"Command {request.Id} requires Number >= {minimum}.");
    }

    private static EditorCommandValidationFailure? RequireSecondNumberAtLeast(
        EditorCommandRequest request,
        int minimum)
    {
        if (!request.Number2.HasValue)
        {
            return Failure(
                EditorCommandValidationCode.MissingSecondNumber,
                nameof(EditorCommandRequest.Number2),
                $"Command {request.Id} requires a second numeric value.");
        }

        return request.Number2.Value >= minimum
            ? null
            : Failure(
                EditorCommandValidationCode.NumberOutOfRange,
                nameof(EditorCommandRequest.Number2),
                $"Command {request.Id} requires Number2 >= {minimum}.");
    }

    private static EditorCommandValidationFailure? RequireNumberInRange(
        EditorCommandRequest request,
        int minimum,
        int maximum)
    {
        if (!request.Number.HasValue)
        {
            return Failure(
                EditorCommandValidationCode.MissingNumber,
                nameof(EditorCommandRequest.Number),
                $"Command {request.Id} requires a numeric value.");
        }

        return request.Number.Value >= minimum && request.Number.Value <= maximum
            ? null
            : Failure(
                EditorCommandValidationCode.NumberOutOfRange,
                nameof(EditorCommandRequest.Number),
                $"Command {request.Id} requires Number in the range {minimum}..{maximum}.");
    }

    private static EditorCommandValidationFailure Failure(
        EditorCommandValidationCode code,
        string? parameterName,
        string message) =>
        new(code, parameterName, message);
}
