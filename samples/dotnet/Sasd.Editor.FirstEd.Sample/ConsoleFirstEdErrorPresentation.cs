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
    internal static string Format(EditorError error)
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
}
