namespace Sasd.Editor.FirstEd.Sample;

/// <summary>
/// Defines the intentionally small public launch contract of the FIRST-ED
/// developer reference application. Parsing is separated from Program.cs so
/// the supported invocation forms can be checked without a real terminal.
/// </summary>
/// <remarks>
/// The sample is an executable *host*, not a second editor implementation.
/// Both an empty session and a prefilled demo use exactly the same
/// EditorSession, EditorCommandDispatcher and IEditorInputPump pipeline.
/// Unknown arguments are rejected, not silently ignored: a developer
/// debugging a failed --demo launch should get an actionable usage message.
/// </remarks>
internal enum FirstEdStartupMode
{
    EmptyDocument,
    Demonstration,
    Help,
    InvalidArguments
}

internal static class FirstEdSampleStartup
{
    internal const string Usage = """
        SASD Editor Toolbox - FIRST-ED developer reference editor (.NET 10)

        Usage:
          dotnet run --project samples/dotnet/Sasd.Editor.FirstEd.Sample -- [--demo | --help]

          (no argument)  Start with an empty in-memory document
          --demo         Start a guided, editable in-memory demonstration
          --help         Print usage (also works without an interactive console)

        Important:
          The --demo document is not loaded from or written to disk.
          Ctrl-K S saves with the historical 8-bit FIRST-ED codec.
          Ctrl-K X leaves the editor after its confirmation prompt.
          Read docs/en/DEMO-EDITOR-AND-PUBLIC-API-ACCEPTANCE.md for the
          developer walkthrough and the remaining manual V1 checks.
        """;

    internal static FirstEdStartupMode Parse(IReadOnlyList<string> arguments)
    {
        ArgumentNullException.ThrowIfNull(arguments);

        return arguments.Count switch
        {
            0 => FirstEdStartupMode.EmptyDocument,
            1 when string.Equals(arguments[0], "--demo", StringComparison.Ordinal)
                => FirstEdStartupMode.Demonstration,
            1 when arguments[0] is "--help" or "-h"
                => FirstEdStartupMode.Help,
            _ => FirstEdStartupMode.InvalidArguments
        };
    }
}
