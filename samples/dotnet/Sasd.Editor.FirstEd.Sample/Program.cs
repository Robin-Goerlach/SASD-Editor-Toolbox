using Sasd.Editor.Editing;
using Sasd.Editor.Rendering;

var session = new EditorSession();
session.CreateDocument("SASD FIRST-ED sample");
session.Engine.MoveEndOfLine();
session.Engine.InsertNewLine();
session.Engine.InsertText("The reusable editor kernel is independent from the UI.");

var viewport = new EditorViewportBuilder(session).Build(height: 10, width: 80);
Console.WriteLine($"{viewport.Status.FileName}  Ln {viewport.Status.Line}, Col {viewport.Status.Column}");
foreach (var line in viewport.Lines)
{
    Console.WriteLine(line.Text);
}
