using Sasd.Editor.Commands;
using Sasd.Editor.Input;

namespace Sasd.Editor.FirstEd.Sample;

/// <summary>
/// Decides whether the FIRST-ED terminal sample needs an explicit overwrite
/// confirmation for a host-selected output filename.
/// </summary>
/// <remarks>
/// This is deliberately *sample-host* behavior, not a new core command or an
/// assertion about the exact 1985 DOS prompt. The host checks file existence;
/// this pure policy only makes the non-mutating decision testable without a
/// terminal or filesystem. There is no process-wide last-used filename.
/// </remarks>
internal static class ConsoleFirstEdOverwritePolicy
{
    /// <summary>
    /// Decides whether an existing path was explicitly selected as a new
    /// output target. A normal Save of an already associated document must
    /// continue without an unexpected secondary confirmation.
    /// </summary>
    /// <param name="request">Fully resolved semantic file command.</param>
    /// <param name="targetExists">Host observation from File.Exists.</param>
    internal static bool RequiresConfirmation(
        EditorCommandRequest request, bool targetExists)
    {
        ArgumentNullException.ThrowIfNull(request);

        // Unlike the core's direct file service, a terminal dialog can ask
        // for consent. Restrict confirmation to a fully named Save As or
        // Write File request: Read File is never an overwrite, and an
        // associated Save has no new filename in Text.
        return targetExists
            && request.Id is EditorCommandId.SaveFile or EditorCommandId.WriteFile
            && !string.IsNullOrWhiteSpace(request.Text);
    }

    /// <summary>
    /// Returns the *original* file command only after an affirmative answer.
    /// Every other response, including a cancelled or blank prompt, returns
    /// null so the host cannot accidentally dispatch a destructive write.
    /// </summary>
    internal static EditorCommandRequest? Confirm(
        EditorCommandRequest request, string? answer)
    {
        ArgumentNullException.ThrowIfNull(request);
        if (request.Id is not (EditorCommandId.SaveFile or EditorCommandId.WriteFile)
            || string.IsNullOrWhiteSpace(request.Text))
        {
            throw new ArgumentException(
                "An overwrite confirmation requires a named output file command.",
                nameof(request));
        }

        // Reuse the existing host-neutral YES parser. The resolved response
        // here is only a boolean authorization: returning its new request
        // would drop the filename. Preserve the caller's ORIGINAL request
        // (including Text and PageSize) once confirmation succeeds.
        var confirmation = new EditorCommandBinding(
            request.Id, EditorCommandArgumentKind.Confirmation);
        return EditorCommandPromptResolver.Resolve(
            confirmation, request.PageSize, firstResponse: answer) is not null
            ? request
            : null;
    }
}
