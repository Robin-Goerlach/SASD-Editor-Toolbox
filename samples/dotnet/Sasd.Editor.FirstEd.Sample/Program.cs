using System.Text;
using Sasd.Editor.Editing;
using Sasd.Editor.FirstEd.Sample;

if (Console.IsInputRedirected || Console.IsOutputRedirected)
{
    Console.WriteLine("SASD FIRST-ED sample requires an interactive terminal.");
    return;
}

Console.OutputEncoding = Encoding.UTF8;

var hooks = new ConsoleFirstEdHooks();
var session = new EditorSession(hooks);
session.CreateDocument();
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
    host.Render("FIRST-ED ready. Ctrl-K X exits; Escape performs Undo.");
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
