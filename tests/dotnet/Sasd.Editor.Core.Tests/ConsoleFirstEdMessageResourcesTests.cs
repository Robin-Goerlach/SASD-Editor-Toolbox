using System.Text;
using Sasd.Editor.FirstEd.Sample;
using Sasd.Editor.Hooks;
using Xunit;

namespace Sasd.Editor.Core.Tests;

/// <summary>
/// Portable file-based tests for the optional modern host resource boundary.
/// These do not assert support for the historical EDITERR.MSG file format.
/// </summary>
public sealed class ConsoleFirstEdMessageResourcesTests
{
    [Fact]
    public void MissingFileLeavesBuiltInMessagesAvailable()
    {
        var missing = Path.Combine(Path.GetTempPath(), Guid.NewGuid() + ".json");
        Assert.Empty(ConsoleFirstEdMessageResources.Load(missing));
    }

    [Fact]
    public void ValidResourceFileChangesHostMessageNotCoreIdentity()
    {
        var path = Path.GetTempFileName();
        try
        {
            File.WriteAllText(path,
                "{\"Editor.Errors.Operation.Aborted\":\"Vorgang abgebrochen.\"}",
                new UTF8Encoding(false));
            var resources = ConsoleFirstEdMessageResources.Load(path);
            var error = EditorErrorCatalog.OperationAborted();
            Assert.Equal("Vorgang abgebrochen.", resources[error.ResourceKey]);
            Assert.Equal($"{error.Code}: Vorgang abgebrochen.",
                ConsoleFirstEdErrorPresentation.Format(error, resources));
            Assert.Equal(EditorErrorId.OperationAborted, error.Id);
        }
        finally { File.Delete(path); }
    }

    [Fact]
    public async Task LoadedResourceFlowsThroughActualHostHookAndKeepsFallback()
    {
        var path = Path.GetTempFileName();
        try
        {
            // Test the real boundary rather than formatting a dictionary
            // directly: the host's OnErrorAsync callback must actually use
            // resources returned by the file loader. A partial translation
            // retains the built-in fallback for a different error category.
            File.WriteAllText(path,
                "{\\"Editor.Errors.Operation.Aborted\\":\\"Vorgang abgebrochen.\\"}",
                new UTF8Encoding(false));
            var hooks = new ConsoleFirstEdHooks(
                ConsoleFirstEdMessageResources.Load(path));
            var session = new Sasd.Editor.Editing.EditorSession(hooks);
            var translated = EditorErrorCatalog.OperationAborted();

            await hooks.OnErrorAsync(session, translated, CancellationToken.None);
            Assert.Equal($"{translated.Code}: Vorgang abgebrochen.",
                hooks.TakeMessage());
            Assert.Null(hooks.TakeMessage()); // One-shot host status.

            var untranslated = EditorErrorCatalog.Create(EditorErrorId.CommandRejected);
            await hooks.OnErrorAsync(session, untranslated, CancellationToken.None);
            Assert.Equal($"{untranslated.Code}: {untranslated.Message}",
                hooks.TakeMessage());
        }
        finally { File.Delete(path); }
    }

    [Fact]
    public void DuplicateResourceKeysInvalidateTheWholeFile()
    {
        // JSON allows repeated property names syntactically, but accepting
        // the last value would make configuration precedence ambiguous.
        var path = Path.GetTempFileName();
        try
        {
            File.WriteAllText(path,
                "{\"Editor.Errors.Operation.Aborted\":\"First\",\"Editor.Errors.Operation.Aborted\":\"Second\"}",
                new UTF8Encoding(false));
            Assert.Empty(ConsoleFirstEdMessageResources.Load(path));
        }
        finally { File.Delete(path); }
    }

    [Fact]
    public void InvalidUtf8BytesDoNotInstallReplacementCharacters()
    {
        var path = Path.GetTempFileName();
        try
        {
            // 0xFF is not valid UTF-8. Lossy decoder substitution would hide
            // corruption in a user-provided status message resource.
            File.WriteAllBytes(path, [0x7B, 0x22, 0xFF, 0x22, 0x7D]);
            Assert.Empty(ConsoleFirstEdMessageResources.Load(path));
        }
        finally { File.Delete(path); }
    }

    [Fact]
    public void OversizedResourceFileIsRejectedBeforeJsonParsing()
    {
        var path = Path.GetTempFileName();
        try
        {
            // The 64-KiB cap is a host resource limit, not a document limit.
            // An oversized optional translation must never block startup.
            File.WriteAllBytes(path, new byte[65 * 1024]);
            Assert.Empty(ConsoleFirstEdMessageResources.Load(path));
        }
        finally { File.Delete(path); }
    }

    [Theory]
    [InlineData("\u202E")] // Right-to-left override can spoof error-code order.
    [InlineData("\u200B")] // Zero-width space is invisible formatting.
    public void UnicodeFormatCharactersInvalidateEntireResourceFile(string invisible)
    {
        var path = Path.GetTempFileName();
        try
        {
            var payload = "{\"Editor.Errors.Operation.Aborted\":\"Valid\","
                + "\"Editor.Errors.File.IoFailed\":\"unsafe"
                + invisible + "\"}";
            File.WriteAllText(path, payload, new UTF8Encoding(false));
            Assert.Empty(ConsoleFirstEdMessageResources.Load(path));
        }
        finally { File.Delete(path); }
    }

    [Theory]
    [InlineData("{")]
    [InlineData("[]")]
    [InlineData("{\"Editor.Errors.Operation.Aborted\": 1}")]
    [InlineData("{\"Unknown.Key\":\"Ignored\"}")]
    [InlineData("{\"Editor.Errors.Operation.Aborted\":\"Bad\\nline\"}")]
    [InlineData("{\"Editor.Errors.Operation.Aborted\":\"OK\",\"Unknown.Key\":\"bad\"}")]
    public void InvalidOrPartlyValidFilesDoNotInstallPartialResources(string json)
    {
        var path = Path.GetTempFileName();
        try
        {
            File.WriteAllText(path, json, new UTF8Encoding(false));
            Assert.Empty(ConsoleFirstEdMessageResources.Load(path));
        }
        finally { File.Delete(path); }
    }
}
