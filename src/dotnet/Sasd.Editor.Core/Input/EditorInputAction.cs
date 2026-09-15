using Sasd.Editor.Commands;

namespace Sasd.Editor.Input;

/// <summary>
/// Describes which additional value a host must collect before a historical
/// command binding can become an <see cref="EditorCommandRequest"/>.
/// </summary>
public enum EditorCommandArgumentKind
{
    None,
    Number,
    Text,
    FindReplace,
    Character,
    FilePath,
    TwoNumbers,
    Confirmation
}

/// <summary>
/// A command binding produced by a key map. A fixed number is used for direct
/// numbered commands such as Ctrl-K, 3 (set marker 3).
/// </summary>
public sealed record EditorCommandBinding(
    EditorCommandId CommandId,
    EditorCommandArgumentKind ArgumentKind = EditorCommandArgumentKind.None,
    int? FixedNumber = null)
{
    /// <summary>
    /// Creates a dispatcher request after the host has supplied any required
    /// prompt values. This method intentionally performs no prompting itself.
    /// </summary>
    public EditorCommandRequest CreateRequest(
        string? text = null,
        int? number = null,
        int? number2 = null,
        int? pageSize = null) =>
        new(CommandId, text, FixedNumber ?? number, pageSize, number2);
}

public enum EditorInputActionKind
{
    Ignored,
    InsertText,
    Command,
    PrefixPending
}

/// <summary>
/// Result of translating a normalized key stroke.
/// </summary>
public sealed record EditorInputAction(
    EditorInputActionKind Kind,
    string? Text = null,
    EditorCommandBinding? Binding = null,
    string? PrefixDisplay = null)
{
    public static EditorInputAction Ignored { get; } = new(EditorInputActionKind.Ignored);

    public static EditorInputAction Insert(string text) =>
        new(EditorInputActionKind.InsertText, Text: text);

    public static EditorInputAction Execute(EditorCommandBinding binding) =>
        new(EditorInputActionKind.Command, Binding: binding);

    public static EditorInputAction AwaitPrefix(string display) =>
        new(EditorInputActionKind.PrefixPending, PrefixDisplay: display);
}
