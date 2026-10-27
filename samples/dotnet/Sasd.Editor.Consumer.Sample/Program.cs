using Sasd.Editor.Commands;
using Sasd.Editor.Editing;
using Sasd.Editor.Model;

// RC-03: This program is intentionally a separate consumer. It does not
// reference FIRST-ED, MicroStar, or any internal implementation types.
// Other SASD projects can follow the same public Core integration pattern.
var session = new EditorSession();
var view = session.CreateDocument("alpha beta");
var commands = new EditorCommandDispatcher(session);

// Editing always goes through the semantic command layer, not through
// terminal keyboard mappings. A UI can dispatch the same request.
view.Cursor = new TextPosition(0, 10);
if (!await commands.ExecuteAsync(
        new EditorCommandRequest(EditorCommandId.InsertText, Text: " gamma")))
    throw new InvalidOperationException("Public insert command did not succeed.");

if (view.Document.Buffer.GetLine(0).Text != "alpha beta gamma")
    throw new InvalidOperationException("Inserted text did not match.");

// Searching has no dependency on a console or the FIRST-ED options prompt.
view.Cursor = new TextPosition(0, 0);
if (!await commands.ExecuteAsync(
        new EditorCommandRequest(EditorCommandId.FindNext, Text: "beta")))
    throw new InvalidOperationException("Public find command did not succeed.");
if (view.Cursor != new TextPosition(0, 6))
    throw new InvalidOperationException("Find returned an unexpected cursor.");

// Undo must restore the original document even after a non-mutating search.
if (!await commands.ExecuteAsync(new EditorCommandRequest(EditorCommandId.Undo)))
    throw new InvalidOperationException("Undo did not succeed.");
if (view.Document.Buffer.GetLine(0).Text != "alpha beta")
    throw new InvalidOperationException("Undo did not restore the document.");

// Export only to a disposable path owned by this sample. FIRST-ED's default
// codec uses legacy 8-bit data; do not treat its output as general UTF-8.
// We verify the file was written without assuming a platform code page.
var target = Path.Combine(Path.GetTempPath(),
    "sasd-editor-consumer-" + Guid.NewGuid().ToString("N") + ".txt");
try
{
    await session.Files.WriteCurrentWindowAsync(target);
    if (!File.Exists(target) || new FileInfo(target).Length == 0)
        throw new InvalidOperationException("Public file export produced no data.");
}
finally
{
    if (File.Exists(target))
        File.Delete(target);
}
Console.WriteLine("RC-03 public Core consumer: PASS");
