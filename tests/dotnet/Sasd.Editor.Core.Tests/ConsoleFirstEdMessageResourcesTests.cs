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
