using System.Security;
using System.Text;
using Sasd.Editor.FirstEd.Sample;
using Sasd.Editor.Hooks;
using Xunit;

namespace Sasd.Editor.Core.Tests;

/// <summary>
/// Pure host-output tests: a console device and real permissions are not
/// required. They verify useful categories without revealing raw provider
/// diagnostics that may include sensitive absolute filesystem paths.
/// </summary>
public sealed class ConsoleFirstEdErrorPresentationTests
{
    [Fact]
    public void UnsupportedLegacyCharacterHasActionableSafeMessage()
    {
        var error = EditorErrorCatalog.FromException(
            new EncoderFallbackException("secret /home/person/private.txt"));
        var displayed = ConsoleFirstEdErrorPresentation.Format(error);

        Assert.Contains("legacy file format", displayed);
        Assert.Contains("EDITOR_FILE_IO_FAILED", displayed);
        Assert.DoesNotContain("/home/person", displayed);
        Assert.DoesNotContain("secret", displayed);
    }

    [Fact]
    public void GenericIoAndDeniedAccessHaveDifferentSafeMessages()
    {
        var io = ConsoleFirstEdErrorPresentation.Format(
            EditorErrorCatalog.FromException(
                new IOException("private /tmp/internal.file")));
        var denied = ConsoleFirstEdErrorPresentation.Format(
            EditorErrorCatalog.FromException(
                new UnauthorizedAccessException("private C:\\Users\\secret")));
        var security = ConsoleFirstEdErrorPresentation.Format(
            EditorErrorCatalog.FromException(new SecurityException("private")));

        Assert.Contains("check path and storage", io);
        Assert.Contains("Access denied", denied);
        Assert.Equal(denied, security);
        Assert.DoesNotContain("internal.file", io);
        Assert.DoesNotContain("secret", denied);
    }

    [Fact]
    public void OtherTypedErrorsRetainCatalogFallback()
    {
        var error = EditorErrorCatalog.Create(EditorErrorId.OperationAborted);
        Assert.Equal($"{error.Code}: {error.Message}",
            ConsoleFirstEdErrorPresentation.Format(error));
    }
}
