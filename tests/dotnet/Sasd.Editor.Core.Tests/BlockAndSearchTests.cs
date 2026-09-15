using Sasd.Editor.Editing;
using Sasd.Editor.Search;
using Xunit;

namespace Sasd.Editor.Core.Tests;

public sealed class BlockAndSearchTests
{
    [Fact]
    public void DeleteBlock_RemovesWholeMarkedLines()
    {
        var session = new EditorSession();
        session.CreateDocument("a\nb\nc\nd");
        session.CurrentWindow.Cursor = new(1, 0);
        session.BeginBlock();
        session.CurrentWindow.Cursor = new(2, 0);
        session.EndBlock();
        session.Engine.DeleteBlock();
        Assert.Equal(2, session.CurrentWindow.Document.Buffer.LineCount);
        Assert.Equal("a", session.CurrentWindow.Document.Buffer.GetLine(0).Text);
        Assert.Equal("d", session.CurrentWindow.Document.Buffer.GetLine(1).Text);
    }

    [Fact]
    public void FindNext_WrapsToBeginning()
    {
        var session = new EditorSession();
        session.CreateDocument("alpha\nbeta alpha");
        session.CurrentWindow.Cursor = new(1, 10);
        var match = session.Search.FindNext("alpha", new SearchOptions(WrapAround: true));
        Assert.Equal(new Sasd.Editor.Model.TextPosition(0, 0), match);
    }
}
