using System.Text;
using System.Text.Json;
using Sasd.Editor.Hooks;

namespace Sasd.Editor.FirstEd.Sample;

/// <summary>
/// Optional, host-owned JSON message resource loader. A dictionary of stable
/// EditorError.ResourceKey strings is intentionally NOT a parser for the
/// historical Borland EDITERR.MSG file, whose full format is unverified.
/// The core knows nothing about this filesystem configuration.
/// </summary>
internal static class ConsoleFirstEdMessageResources
{
    internal const string EnvironmentVariable = "SASD_FIRSTED_MESSAGES_JSON";
    private const long MaximumBytes = 64 * 1024;

    /// <summary>
    /// Fails closed to built-in English messages. A missing or malformed
    /// resource file must never prevent the editor from starting or produce
    /// partial, misleading localization. The returned dictionary is only
    /// installed after every key and message has been validated.
    /// </summary>
    internal static IReadOnlyDictionary<string, string> Load(string? path)
    {
        if (string.IsNullOrWhiteSpace(path))
            return new Dictionary<string, string>();

        try
        {
            var file = new FileInfo(path);
            if (!file.Exists || file.Length > MaximumBytes)
                return new Dictionary<string, string>();

            // Reject byte-order ambiguity and invalid UTF-8 sequences rather
            // than silently replacing characters in safety-critical prompts.
            var json = new UTF8Encoding(false, true).GetString(File.ReadAllBytes(path));
            using var parsed = JsonDocument.Parse(json);
            if (parsed.RootElement.ValueKind != JsonValueKind.Object)
                return new Dictionary<string, string>();

            var messages = new Dictionary<string, string>(StringComparer.Ordinal);
            foreach (var property in parsed.RootElement.EnumerateObject())
            {
                if (property.Value.ValueKind != JsonValueKind.String)
                    return new Dictionary<string, string>();

                var knownKey = Enum.GetValues<EditorErrorId>()
                    .Any(id => EditorErrorCatalog.Describe(id).ResourceKey == property.Name);
                var value = property.Value.GetString();
                if (!knownKey || string.IsNullOrWhiteSpace(value)
                    || value.Length > 240 || value.Any(char.IsControl)
                    || value.Any(c => c is >= '\u007f' and <= '\u009f')
                    || !messages.TryAdd(property.Name, value))
                    return new Dictionary<string, string>();
            }
            return messages;
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException
            or System.Security.SecurityException or ArgumentException
            or JsonException or DecoderFallbackException)
        {
            // This opt-in resource must not bring down the interactive editor.
            // Never print provider exceptions: they may expose private paths.
            return new Dictionary<string, string>();
        }
    }
}
