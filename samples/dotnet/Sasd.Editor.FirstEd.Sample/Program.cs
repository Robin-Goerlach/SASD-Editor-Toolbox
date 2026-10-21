using System.Text;
using Sasd.Editor.Editing;
using Sasd.Editor.FirstEd.Sample;

// Interpret startup arguments BEFORE checking for an interactive console.
 // Help is essential to developers running CI or a redirected terminal:
 // they must be able to learn the reference-host launch contract without
 // constructing editor state or opening a physical input device.
var startupMode = FirstEdSampleStartup.Parse(args);
if (startupMode == FirstEdStartupMode.Help)
{
    Console.WriteLine(FirstEdSampleStartup.Usage);
    return;
}

if (startupMode == FirstEdStartupMode.InvalidArguments)
{
    Console.Error.WriteLine("Unrecognized FIRST-ED sample arguments.");
    Console.Error.WriteLine(FirstEdSampleStartup.Usage);
    Environment.ExitCode = 2;
    return;
}

if (Console.IsInputRedirected || Console.IsOutputRedirected)
{
    Console.WriteLine("SASD FIRST-ED sample requires an interactive terminal.");
    Console.WriteLine("Run with --help for usage or --demo inside a terminal.");
    Environment.ExitCode = 1;
    return;
}

Console.OutputEncoding = Encoding.UTF8;

var hooks = new ConsoleFirstEdHooks();
var session = new EditorSession(hooks);

// --demo is a real, editable document in the normal public model. It uses
// the same host, commands, file codec and Undo implementation as the empty
// session. Never write a demo file or introduce a sample-only editor core.
if (startupMode == FirstEdStartupMode.Demonstration)
{
    FirstEdDemoScenario.Initialize(session);
}
else
{
    session.CreateDocument();
}

var host = new ConsoleFirstEdHost(session, hooks);

var restoreTreatControlCAsInput = false;
var oldTreatControlCAsInput = false;
try
{
    oldTreatControlCAsInput = Console.TreatControlCAsInput;
    Console.TreatControlCAsInput = true;
    restoreTreatControlCAsInput = true;
}
catch (PlatformNotSupportedException)
{
    // Some terminals do not expose Ctrl-C as ordinary input. The editor still
    // works; only terminal-reserved control combinations may be unavailable.
}

try
{
    host.Render(startupMode == FirstEdStartupMode.Demonstration
        ? "DEMO: edit text, Ctrl-Q F finds WIDGET; Ctrl-K X exits."
        : "FIRST-ED ready. Ctrl-K X exits; Escape performs Undo.");
    await session.SystemLoop.RunAsync(host);
}
finally
{
    host.Shutdown();
    if (restoreTreatControlCAsInput)
    {
        Console.TreatControlCAsInput = oldTreatControlCAsInput;
    }
}
