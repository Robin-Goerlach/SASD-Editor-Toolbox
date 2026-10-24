using System.Text;
using Sasd.Editor.Hooks;

namespace Sasd.Editor.FirstEd.Sample;

/// <summary>
/// Maps typed, portable editor failures to short messages for a physical
/// FIRST-ED terminal. This is a presentation adapter: no file I/O, prompting,
/// command authorization or document mutations belong here.
/// </summary>
/// <remarks>
/// Core error codes and resource keys remain the stable cross-language API.
/// Exception messages may contain absolute filenames or provider diagnostics,
/// so the terminal must never print them verbatim. The exception *type* is
/// sufficient to distinguish an unencodable legacy document from an ordinary
/// disk I/O failure without exposing user data.
/// </remarks>
internal static class ConsoleFirstEdErrorPresentation
{
    /// <summary>
    /// Formats an error with optional host-owned resource overrides. Keys are
    /// the stable EditorError.ResourceKey values, not historical numeric DOS
    /// message identifiers. Loading and validating an actual resource file
    /// belongs to a future host adapter, never to the portable Core.
    /// </summary>
    internal static string Format(
        EditorError error, IReadOnlyDictionary<string, string>? resources = null)
    {
        ArgumentNullException.ThrowIfNull(error);

        if (error.Id == EditorErrorId.FileIoFailed &&
            error.Exception is EncoderFallbackException)
        {
            // The historical FIRST-ED codec only writes representable 8-bit
            // characters. Keep the document dirty: the user must choose a
            // compatible text or an explicitly modern file format.
            return $"{error.Code}: Text contains characters unsupported by the legacy file format.";
        }

        // A host may localize a semantic resource key, but resource data
        // must not be allowed to inject cursor-control bytes or newlines into
        // a one-line terminal status area. Ignore malformed entries rather
        // than risking display corruption; retaining the stable machine code
        // allows diagnostics even when a translation is unavailable.
        //
        // Encoding failures keep their explicit diagnostic so the user is
        // told about actual format limitations instead of generic I/O.
        if (resources is not null
            && resources.TryGetValue(error.ResourceKey, out var translated)
            && IsSafeSingleLine(translated))
            return $"{error.Code}: {translated}";

        // A denied file operation is distinct from a malformed encoding.
        // Do not expose exception.Message: OS diagnostics may include paths,
        // usernames, internal server addresses or other private details.
        if (error.Id == EditorErrorId.AccessDenied)
            return $"{error.Code}: Access denied.";

        if (error.Id == EditorErrorId.FileIoFailed)
            return $"{error.Code}: File operation failed; check path and storage.";

        // Preserve the catalog's general, safe default for non-file errors.
        return $"{error.Code}: {error.Message}";
    }

    private static bool IsSafeSingleLine(string? text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return false;

        // CR/LF, ESC, C1 controls and DEL can manipulate terminal state.
        // Reject these entirely, rather than attempting lossy sanitation of
        // a potentially externally supplied localization value.
        foreach (var character in text)
            if (char.IsControl(character) || character is >= '\u007f' and <= '\u009f')
                return false;
        return true;
    }
}
