using Sasd.Editor.Model;

namespace Sasd.Editor.FirstEd.Sample;

/// <summary>
/// The five screen attribute roles described in the 1985 editor handbook,
/// chapter 11, pp. 69-72. These are DISPLAY roles, not file encodings.
/// </summary>
internal enum FirstEdColorRole
{
    Text,       // Txtcolor: ordinary document text
    Status,     // Bordcolor: window status/header rows
    Block,      // Blockcolor: the selected whole-line block
    Command,    // Cmdcolor: prompts and command/error line
    User        // Usercolor: lines explicitly marked Colored
}

internal readonly record struct FirstEdTerminalColors(
    ConsoleColor Foreground, ConsoleColor Background);

/// <summary>
/// Pure FIRST-ED console presentation rules, kept separate from physical
/// Console calls so the historical behavior is testable without a terminal.
/// Other hosts (WinForms, WPF, web, MicroStar) are free to use other colors.
/// </summary>
internal static class ConsoleFirstEdPresentationPolicy
{
    /// <summary>
    /// A Colored line overrides both ordinary text and block highlighting
    /// (handbook pp. 137-138). InBlock is a visible-state projection supplied
    /// by Core; a hidden block already has no InBlock flag.
    /// </summary>
    internal static FirstEdColorRole TextRole(EditorLineFlags flags)
    {
        if ((flags & EditorLineFlags.UserColored) != 0)
            return FirstEdColorRole.User;
        if ((flags & EditorLineFlags.InBlock) != 0)
            return FirstEdColorRole.Block;
        return FirstEdColorRole.Text;
    }

    /// <summary>
    /// The handbook defines the *roles*, not a universal ANSI/Windows palette.
    /// These conservative terminal colors are an explicitly modern host theme.
    /// </summary>
    internal static FirstEdTerminalColors Colors(FirstEdColorRole role) => role switch
    {
        FirstEdColorRole.Text => new(ConsoleColor.Gray, ConsoleColor.Black),
        FirstEdColorRole.Status => new(ConsoleColor.Black, ConsoleColor.Gray),
        FirstEdColorRole.Block => new(ConsoleColor.White, ConsoleColor.DarkBlue),
        FirstEdColorRole.Command => new(ConsoleColor.Yellow, ConsoleColor.DarkGray),
        FirstEdColorRole.User => new(ConsoleColor.Cyan, ConsoleColor.Black),
        _ => throw new ArgumentOutOfRangeException(nameof(role))
    };

    /// <summary>
    /// FIRST-ED's optional right-edge column distinguishes hard returns,
    /// soft Wrapped boundaries, and screen rows beyond the file (p. 69).
    /// A blank marker for Wrapped is intentional. MicroStar turns this
    /// feature off; disabled indicators do not reserve a terminal cell.
    /// </summary>
    internal static char? RightEdgeMarker(
        bool enabled, bool isDocumentLine, EditorLineFlags flags)
    {
        if (!enabled)
            return null;
        if (!isDocumentLine)
            return '.';
        return (flags & EditorLineFlags.Wrapped) != 0 ? ' ' : '<';
    }
}
