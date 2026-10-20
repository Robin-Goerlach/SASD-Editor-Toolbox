using Sasd.Editor.Commands;
using Sasd.Editor.Input;

namespace Sasd.Editor.FirstEd.Sample;

/// <summary>
/// Chooses the semantic Save File request for the FIRST-ED terminal sample
/// without reading keys, touching the console, or writing any document data.
/// </summary>
/// <remarks>
/// Ctrl-K S is a no-argument key binding because an already associated document
/// can be saved immediately. A freshly created NONAME document has no target,
/// however; the *host* must ask for a filename before dispatch. This is a
/// documented modern host policy, not a reconstruction of DOS dialog internals.
/// </remarks>
internal static class ConsoleFirstEdSavePromptPolicy
{
    /// <summary>
    /// Reports whether the active document still requires a destination path.
    /// This decision is made for the selected view's document, not globally.
    /// </summary>
    internal static bool RequiresPath(string? associatedPath) =>
        string.IsNullOrWhiteSpace(associatedPath);

    /// <summary>
    /// Resolves either a direct Save or an explicitly named first Save.
    /// </summary>
    /// <param name="binding">The actual Ctrl-K S binding from the key map.</param>
    /// <param name="visibleRows">Text rows supplied by the current host.</param>
    /// <param name="associatedPath">File association of the initiating document.</param>
    /// <param name="enteredPath">
    /// Host-entered filename for an unnamed document; null means cancellation.
    /// A blank or whitespace-only name also abandons this Save.
    /// </param>
    /// <returns>
    /// A dispatchable SaveFile request or null if a required filename was not
    /// supplied. No request is ever emitted for a cancelled first-save dialog.
    /// </returns>
    internal static EditorCommandRequest? Resolve(
        EditorCommandBinding binding,
        int visibleRows,
        string? associatedPath,
        string? enteredPath)
    {
        ArgumentNullException.ThrowIfNull(binding);

        if (binding.CommandId != EditorCommandId.SaveFile ||
            binding.ArgumentKind != EditorCommandArgumentKind.None)
        {
            throw new ArgumentException(
                "This policy accepts only the no-argument FIRST-ED Save File binding.",
                nameof(binding));
        }

        if (!RequiresPath(associatedPath))
        {
            // The file service owns the existing document association. Do not
            // silently reinterpret an unsolicited prompt value as Save As.
            // The exact same request may come from any non-console host.
            return EditorCommandPromptResolver.Resolve(binding, visibleRows);
        }

        // Reuse the shared host-neutral filename resolver: it distinguishes
        // cancellation from a supplied name, trims outer whitespace, and
        // rejects a blank path. The terminal's Save prompt remains outside
        // Sasd.Editor.Core even though the resulting command is portable.
        var namedSaveBinding = new EditorCommandBinding(
            EditorCommandId.SaveFile,
            EditorCommandArgumentKind.FilePath);
        return EditorCommandPromptResolver.Resolve(
            namedSaveBinding, visibleRows, firstResponse: enteredPath);
    }
}
