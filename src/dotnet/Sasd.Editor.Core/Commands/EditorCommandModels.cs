namespace Sasd.Editor.Commands;

/// <summary>
/// Semantic editor commands understood by the reusable editor layer.
///
/// The semantic vocabulary is intentionally independent of keyboard mappings so
/// historical compatibility hosts, modern menus and automation can invoke the
/// same operations without coupling the engine to a UI framework.
/// </summary>
public enum EditorCommandId
{
    CursorLeft,
    CursorRight,
    CursorUp,
    CursorDown,
    PageUp,
    PageDown,
    ScrollUp,
    ScrollDown,
    WordLeft,
    WordRight,
    BeginningOfLine,
    EndOfLine,
    BeginningOrEndOfLine,
    TopOfFile,
    BottomOfFile,
    TopOfBlock,
    BottomOfBlock,
    GoToLine,
    GoToColumn,
    InsertText,
    InsertLine,
    NewLine,
    InsertControlCharacter,
    Tab,
    DeleteLeftCharacter,
    DeleteRightCharacter,
    DeleteRightWord,
    DeleteLine,
    DeleteToEndOfLine,
    ChangeCase,
    CenterLine,
    ReformatParagraph,
    ToggleInsert,
    ToggleWordWrap,
    ToggleAutoIndent,
    Undo,
    BeginBlock,
    EndBlock,
    CopyBlock,
    MoveBlock,
    DeleteBlock,
    HideBlock,
    CreateWindow,
    LinkWindow,
    PreviousWindow,
    NextWindow,
    GoToWindow,
    DeleteWindow,
    DeleteWindowText,
    SetLeftMargin,
    SetRightMargin,
    SetTabWidth,
    SetUndoLimit,
    SetMarker,
    JumpMarker,
    FindNext,
    FindAgain,
    ReplaceNext,
    ReadFile,
    WriteFile,
    SaveFile,
    Exit
}

/// <summary>
/// Fully resolved semantic command request passed to the command dispatcher.
/// Hosts may build this request from keyboard bindings, menus, buttons, scripts
/// or another automation surface.
/// </summary>
/// <param name="Id">The semantic operation to execute.</param>
/// <param name="Text">Optional textual argument such as a search pattern or file path.</param>
/// <param name="Number">Optional first numeric argument.</param>
/// <param name="PageSize">Optional host-specific visible page size.</param>
/// <param name="Number2">Optional second numeric argument used by two-value compatibility commands.</param>
public sealed record EditorCommandRequest(
    EditorCommandId Id,
    string? Text = null,
    int? Number = null,
    int? PageSize = null,
    int? Number2 = null);
