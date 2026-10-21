using Sasd.Editor.Commands;
using Sasd.Editor.Editing;
using Sasd.Editor.FirstEd.Sample;
using Sasd.Editor.Model;
using Xunit;

namespace Sasd.Editor.Core.Tests;

/// <summary>
/// Protects the developer demo's launch contract independently of Console.
/// Every action it demonstrates must be backed by the REAL public editor model.
/// </summary>
public sealed class FirstEdDeveloperDemoTests
{
    [Fact]
    public void DefaultLaunchIsEmptyAndDemoRequiresExplicitOptIn()
    {
        Assert.Equal(FirstEdStartupMode.EmptyDocument,
            FirstEdSampleStartup.Parse(Array.Empty<string>()));
        Assert.Equal(FirstEdStartupMode.Demonstration,
            FirstEdSampleStartup.Parse(["--demo"]));
        Assert.Equal(FirstEdStartupMode.Help,
            FirstEdSampleStartup.Parse(["--help"]));
        Assert.Equal(FirstEdStartupMode.Help,
            FirstEdSampleStartup.Parse(["-h"]));

        // A misspelled or combined switch should never silently start a
        // different demo or overwrite the user's expectations.
        Assert.Equal(FirstEdStartupMode.InvalidArguments,
            FirstEdSampleStartup.Parse(["--deom"]));
        Assert.Equal(FirstEdStartupMode.InvalidArguments,
            FirstEdSampleStartup.Parse(["--demo", "--help"]));
        Assert.Throws<ArgumentNullException>(() =>
            FirstEdSampleStartup.Parse(null!));
    }

    [Fact]
    public void UsageIsUsefulWithoutAnInteractiveConsole()
    {
        // --help is handled *before* Program checks Console.IsInputRedirected.
        // This allows SDK users to discover and script the sample's launch
        // behavior without requiring a PTY in their build environment.
        Assert.Contains("--demo", FirstEdSampleStartup.Usage);
        Assert.Contains("--help", FirstEdSampleStartup.Usage);
        Assert.Contains("dotnet run", FirstEdSampleStartup.Usage);
        Assert.Contains("in-memory", FirstEdSampleStartup.Usage);
    }

    [Fact]
    public void DemoBuildsCleanRealDocumentWithNoFilesystemSideEffects()
    {
        var session = new EditorSession();
        FirstEdDemoScenario.Initialize(session);
        var first = Assert.Single(session.Windows);
        var document = first.Document;

        Assert.Equal(FirstEdDemoScenario.Text,
            string.Join('\n', document.Buffer.Snapshot().Select(x => x.Text)));
        Assert.Contains('\u001b', FirstEdDemoScenario.Text);
        Assert.Contains("WIDGET", FirstEdDemoScenario.Text);
        Assert.Null(document.FilePath);
        Assert.Equal("NONAME", document.DisplayName);
        Assert.False(document.IsDirty);
        Assert.Equal(0, document.Version);
        Assert.Equal(0, session.Undo.Count);
        Assert.Equal(new TextPosition(0, 0), first.Cursor);
    }

    [Fact]
    public async Task DemoUsesRealCommandDispatcherSearchEditAndUndo()
    {
        var session = new EditorSession();
        FirstEdDemoScenario.Initialize(session);
        var original = session.CurrentWindow.Document.Buffer.Snapshot();
        var dispatcher = new EditorCommandDispatcher(session);

        // A sample must not forge a screenshot or precompute a fake search
        // result. Find, navigation and editing must exercise the same public
        // EditorCommandRequest path a real embedding host uses.
        Assert.True(await dispatcher.ExecuteAsync(
            new(EditorCommandId.FindNext, Text: "WIDGET")));
        var current = session.CurrentWindow;
        Assert.Contains("WIDGET", current.Document.Buffer.GetLine(current.Cursor.Line).Text);
        Assert.Equal(0, session.Undo.Count);

        Assert.True(await dispatcher.ExecuteAsync(
            new(EditorCommandId.InsertText, Text: "TEST ")));
        Assert.True(current.Document.IsDirty);
        Assert.Equal(1, session.Undo.Count);

        Assert.True(await dispatcher.ExecuteAsync(
            new(EditorCommandId.Undo)));
        Assert.Equal(original, current.Document.Buffer.Snapshot());
        Assert.False(current.Document.IsDirty);
        Assert.Equal(0, session.Undo.Count);
        Assert.Null(current.Document.FilePath);
    }

    [Fact]
    public void DemoCanUseActualLinkedWindowsWithoutDuplicatingText()
    {
        var session = new EditorSession();
        FirstEdDemoScenario.Initialize(session);
        var first = session.CurrentWindow;
        var linked = session.LinkWindow(first);

        Assert.Same(first.Document, linked.Document);
        first.Cursor = new TextPosition(0, 0);
        linked.Cursor = new TextPosition(5, 3);

        Assert.NotEqual(first.Cursor, linked.Cursor);
        Assert.Same(first.Document.Buffer, linked.Document.Buffer);
        Assert.False(first.Document.IsDirty);
        Assert.Equal(0, session.Undo.Count);
    }
}
