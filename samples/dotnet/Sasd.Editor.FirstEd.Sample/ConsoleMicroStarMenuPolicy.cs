using Sasd.Editor.Commands;
using Sasd.Editor.Input;

namespace Sasd.Editor.FirstEd.Sample;

/// <summary>
/// MicroStar-inspired F10 command-menu policy. The handbook explicitly
/// describes an F10 pull-down application menu, but the available description
/// does not establish the complete original item order or keyboard lettering.
/// These letter assignments therefore are a documented SASD sample-host
/// choice. Each entry delegates to an EXISTING editor semantic command.
/// </summary>
internal static class ConsoleMicroStarMenuPolicy
{
    // A single line works even in the minimum three-row console. A later
    // actual pop-up/window layer can present the same bindings as a menu grid.
    internal const string Prompt =
        "F10: F=Find R=Replace G=Goto S=Save W=Write I=Read U=Undo B=Begin E=End C=Copy M=Move D=Delete N=Next T=WriteBlock L=ReadBlock P=SaveOpen A=AbandonOpen H=Print K=StopPrint J=Status Q=Cancel: ";

    /// <summary>
    /// Normalize only a single user-chosen menu letter. Enter, Escape,
    /// Ctrl-U, whitespace, or other input does not dispatch a command.
    /// </summary>
    internal static EditorCommandBinding? Resolve(string? input)
    {
        if (input is null || input.Length != 1)
            return null;

        return char.ToUpperInvariant(input[0]) switch
        {
            'F' => new(EditorCommandId.FindNext, EditorCommandArgumentKind.Text),
            'R' => new(EditorCommandId.ReplaceNext, EditorCommandArgumentKind.FindReplace),
            'G' => new(EditorCommandId.GoToLine, EditorCommandArgumentKind.Number),
            'S' => new(EditorCommandId.SaveFile),
            'W' => new(EditorCommandId.WriteFile, EditorCommandArgumentKind.FilePath),
            'I' => new(EditorCommandId.ReadFile, EditorCommandArgumentKind.FilePath),
            'U' => new(EditorCommandId.Undo),
            'B' => new(EditorCommandId.BeginBlock),
            'E' => new(EditorCommandId.EndBlock),
            'C' => new(EditorCommandId.CopyBlock),
            'M' => new(EditorCommandId.MoveBlock),
            'D' => new(EditorCommandId.DeleteBlock),
            'N' => new(EditorCommandId.NextWindow),
            // T and L are host-only block-file operations; neither is
            // a regular FIRST-ED command binding.
            'Q' => null,
            _ => null
        };
    }
}
