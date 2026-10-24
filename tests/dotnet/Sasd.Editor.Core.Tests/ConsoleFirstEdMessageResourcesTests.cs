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
