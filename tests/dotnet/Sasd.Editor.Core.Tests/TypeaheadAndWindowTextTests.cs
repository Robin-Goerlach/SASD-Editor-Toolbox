using Sasd.Editor.Commands;
using Sasd.Editor.Editing;
using Sasd.Editor.Input;
using Sasd.Editor.Model;
using Xunit;

namespace Sasd.Editor.Core.Tests;

public sealed class TypeaheadAndWindowTextTests
{
    [Fact]
    public void Typeahead_BackInsertionIsFifoAndFrontInsertionRunsNext()
    {
        var buffer = new EditorTypeaheadBuffer();
        Assert.Equal(EditorTypeaheadWriteResult.Queued, buffer.EnqueueFromHost(EditorKeyStroke.ForCharacter('A')));
        Assert.Equal(EditorTypeaheadWriteResult.Queued, buffer.EnqueueFromHost(EditorKeyStroke.ForCharacter('B')));
        Assert.Equal(EditorTypeaheadWriteResult.Queued, buffer.PushNext(EditorKeyStroke.ForCharacter('X')));

        Assert.True(buffer.TryRead(out var first));
        Assert.True(buffer.TryRead(out var second));
        Assert.True(buffer.TryRead(out var third));

        Assert.Equal('X', first.Character);
        Assert.Equal('A', second.Character);
        Assert.Equal('B', third.Character);
        Assert.False(buffer.TryRead(out _));
    }

    [Fact]
    public void Typeahead_PushSequencePreservesNaturalReadOrder()
    {
        var buffer = new EditorTypeaheadBuffer();
        var sequence = new[]
        {
            EditorKeyStroke.Control('K'),
            EditorKeyStroke.ForCharacter('X')
        };

        Assert.Equal(EditorTypeaheadWriteResult.Queued, buffer.PushSequence(sequence));
        Assert.True(buffer.TryRead(out var prefix));
        Assert.True(buffer.TryRead(out var command));

        Assert.True(prefix.IsControlLetter('K'));
        Assert.Equal('X', command.Character);
    }

    [Fact]
    public void Typeahead_PushTextNormalizesClassicControlCharacters()
    {
        var buffer = new EditorTypeaheadBuffer();

        Assert.Equal(EditorTypeaheadWriteResult.Queued, buffer.PushText("\u000bX"));
        Assert.True(buffer.TryRead(out var prefix));
        Assert.True(buffer.TryRead(out var command));

        Assert.True(prefix.IsControlLetter('K'));
        Assert.Equal(EditorKeyModifiers.None, command.Modifiers);
        Assert.Equal('X', command.Character);
    }

    [Fact]
    public void Typeahead_HostCtrlUClearsPendingInputAndRaisesAbortFlag()
    {
        var buffer = new EditorTypeaheadBuffer();
        buffer.EnqueueFromHost(EditorKeyStroke.ForCharacter('A'));
        buffer.EnqueueFromHost(EditorKeyStroke.ForCharacter('B'));

        var result = buffer.EnqueueFromHost(EditorKeyStroke.Control('U'));

        Assert.Equal(EditorTypeaheadWriteResult.Aborted, result);
        Assert.Equal(0, buffer.Count);
        Assert.True(buffer.AbortRequested);
        Assert.False(buffer.TryRead(out _));
        Assert.True(buffer.ConsumeAbortRequest());
        Assert.False(buffer.AbortRequested);
        Assert.False(buffer.ConsumeAbortRequest());
    }

    [Fact]
    public void Typeahead_OverflowClearsBufferInsteadOfKeepingPartialInput()
    {
        var buffer = new EditorTypeaheadBuffer(capacity: 2);
        buffer.EnqueueFromHost(EditorKeyStroke.ForCharacter('A'));
        buffer.EnqueueFromHost(EditorKeyStroke.ForCharacter('B'));

        var result = buffer.EnqueueFromHost(EditorKeyStroke.ForCharacter('C'));

        Assert.Equal(EditorTypeaheadWriteResult.Overflow, result);
        Assert.Equal(0, buffer.Count);
        Assert.False(buffer.TryRead(out _));
    }

    [Fact]
    public void Typeahead_UserPushCtrlUDoesNotUsePhysicalImmediateAbortPath()
    {
        var buffer = new EditorTypeaheadBuffer();

        Assert.Equal(EditorTypeaheadWriteResult.Queued, buffer.PushNext(EditorKeyStroke.Control('U')));

        Assert.False(buffer.AbortRequested);
        Assert.True(buffer.TryRead(out var keyStroke));
        Assert.True(keyStroke.IsControlLetter('U'));
    }

    [Fact]
    public async Task DeleteWindowText_IsDestructiveNonUndoableAndBreaksLinkedViews()
    {
        var session = new EditorSession();
        var original = session.CreateDocument("alpha\nbeta");
        var originalDocument = original.Document;
        original.Document.SetPersistenceMetadata("old-name.txt");
        session.Engine.InsertText("X");
        Assert.True(session.Undo.Count > 0);

        session.SetMarker(1);
        session.BeginBlock();
        var linked = session.LinkWindow(original);
        linked.Cursor = new TextPosition(1, 2);
        linked.TopLine = 1;
        linked.LeftColumn = 4;

        var unrelated = session.CreateDocument("keep me");
        var unrelatedDocument = unrelated.Document;
        session.SetCurrentWindow(linked.WindowId);

        var dispatcher = new EditorCommandDispatcher(session);
        Assert.True(await dispatcher.ExecuteAsync(new(EditorCommandId.DeleteWindowText)));

        Assert.Equal(3, session.Windows.Count);
        Assert.Same(linked, session.CurrentWindow);
        Assert.NotSame(originalDocument, original.Document);
        Assert.NotSame(originalDocument, linked.Document);
        Assert.NotSame(original.Document, linked.Document);
        Assert.Same(unrelatedDocument, unrelated.Document);
        Assert.Equal("keep me", unrelated.Document.Buffer.GetLine(0).Text);

        foreach (var window in new[] { original, linked })
        {
            Assert.Equal("NONAME", window.Document.DisplayName);
            Assert.Equal(1, window.Document.Buffer.LineCount);
            Assert.Equal(string.Empty, window.Document.Buffer.GetLine(0).Text);
            Assert.False(window.Document.IsDirty);
            Assert.Equal(default, window.Cursor);
            Assert.Equal(0, window.TopLine);
            Assert.Equal(0, window.LeftColumn);
        }

        Assert.Null(session.Block);
        Assert.False(session.JumpToMarker(1));
        Assert.Equal(0, session.Undo.Count);
        Assert.False(session.Engine.Undo());
    }
}
