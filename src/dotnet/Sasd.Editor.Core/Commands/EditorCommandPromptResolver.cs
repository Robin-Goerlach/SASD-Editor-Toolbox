using System.Globalization;
using Sasd.Editor.Input;

namespace Sasd.Editor.Commands;

/// <summary>
/// Resolves responses that a host has *already collected* for a FIRST-ED
/// binding. The reusable core never opens a prompt or touches UI APIs.
/// </summary>
/// <remarks>
/// A null response means cancellation/end-of-input. The empty string means a
/// deliberately submitted value. In particular, an empty replacement deletes
/// a match; a null replacement cancels without ever reaching the dispatcher.
/// The normal command validator still owns argument range validation.
/// </remarks>
public static class EditorCommandPromptResolver
{
    public static EditorCommandRequest? Resolve(
        EditorCommandBinding binding,
        int? pageSize = null,
        string? firstResponse = null,
        string? secondResponse = null,
        char? characterResponse = null)
    {
        ArgumentNullException.ThrowIfNull(binding);
        switch (binding.ArgumentKind)
        {
            case EditorCommandArgumentKind.None:
                return binding.CreateRequest(pageSize: pageSize);
            case EditorCommandArgumentKind.Number:
                return TryNumber(firstResponse, out var number)
                    ? binding.CreateRequest(number: number, pageSize: pageSize)
                    : null;
            case EditorCommandArgumentKind.TwoNumbers:
                return TryNumber(firstResponse, out var first)
                    && TryNumber(secondResponse, out var second)
                    ? binding.CreateRequest(number: first, number2: second,
                        pageSize: pageSize)
                    : null;
            case EditorCommandArgumentKind.Text:
                // Explicit empty Find repeats the last pattern; null Find
                // means cancellation and must not initiate Find Again.
                if (firstResponse is null)
                    return null;
                if (binding.CommandId == EditorCommandId.FindNext
                    && firstResponse.Length == 0)
                    return new EditorCommandRequest(EditorCommandId.FindAgain,
                        PageSize: pageSize);
                return firstResponse.Length == 0
                    ? null
                    : binding.CreateRequest(text: firstResponse, pageSize: pageSize);
            case EditorCommandArgumentKind.FindReplace:
                // Never use secondResponse ?? string.Empty; doing so would
                // silently transform a cancelled prompt into a deletion.
                return !string.IsNullOrEmpty(firstResponse) && secondResponse is not null
                    ? binding.CreateRequest(text: firstResponse,
                        text2: secondResponse, pageSize: pageSize)
                    : null;
            case EditorCommandArgumentKind.Character:
                return characterResponse.HasValue
                    ? binding.CreateRequest(
                        text: characterResponse.Value.ToString(), pageSize: pageSize)
                    : null;
            case EditorCommandArgumentKind.FilePath:
                return string.IsNullOrWhiteSpace(firstResponse)
                    ? null
                    : binding.CreateRequest(
                        text: firstResponse.Trim(), pageSize: pageSize);
            case EditorCommandArgumentKind.Confirmation:
                return string.Equals(firstResponse?.Trim(), "YES",
                    StringComparison.OrdinalIgnoreCase)
                    ? binding.CreateRequest(pageSize: pageSize)
                    : null;
            default:
                // Unknown prompt kinds must not guess a command payload.
                return null;
        }
    }

    private static bool TryNumber(string? value, out int number) =>
        int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture,
            out number);
}
