namespace Sasd.Editor.FirstEd.Sample;

/// <summary>
/// Identifies the role of a focusable control without depending on a concrete
/// user-interface toolkit. The first consumer is the terminal MicroStar host;
/// the definition intentionally has no dependency on EditorSession.
/// </summary>
internal enum MicroStarDialogControlKind
{
    Input,
    Choice,
    Action
}

/// <summary>
/// One immutable control descriptor. Enabled=false keeps disabled controls in
/// the visual layout but excludes them from keyboard focus and activation.
/// </summary>
internal sealed record MicroStarDialogControl(
    string Id, MicroStarDialogControlKind Kind, bool Enabled = true);

internal enum MicroStarDialogNavigationResult
{
    Ignored,
    FocusChanged,
    Activate,
    Cancel
}

/// <summary>
/// Pure focus policy for modal dialogs. Every dialog owns its own instance,
/// so a nested dialog cannot accidentally replace its parent's focus.
/// </summary>
/// <remarks>
/// This is deliberate modern SASD behavior, not an assertion that the 1985
/// MicroStar application used this exact keyboard mapping. Rendering and
/// keyboard capture belong to a host adapter; editor mutations belong to
/// semantic commands outside this policy.
/// </remarks>
internal sealed class MicroStarDialogFocus
{
    private readonly MicroStarDialogControl[] _controls;
    private int _focusedIndex;

    internal MicroStarDialogFocus(IEnumerable<MicroStarDialogControl> controls)
    {
        ArgumentNullException.ThrowIfNull(controls);
        _controls = controls.ToArray();

        // Duplicate identifiers would make it impossible for a host to map a
        // committed selection back to one unambiguous action or input field.
        if (_controls.Any(control => control is null ||
            string.IsNullOrWhiteSpace(control.Id)) ||
            _controls.Select(control => control.Id).Distinct(StringComparer.Ordinal).Count()
                != _controls.Length)
            throw new ArgumentException("Dialog controls require unique, nonempty IDs.", nameof(controls));

        if (_controls.Length == 0 || !_controls.Any(control => control.Enabled))
            throw new ArgumentException("A dialog requires an enabled control.", nameof(controls));

        _focusedIndex = Array.FindIndex(_controls, control => control.Enabled);
    }

    internal string FocusedId => _controls[_focusedIndex].Id;
    internal MicroStarDialogControlKind FocusedKind => _controls[_focusedIndex].Kind;
    internal IReadOnlyList<MicroStarDialogControl> Controls => Array.AsReadOnly(_controls);

    /// <summary>
    /// Move only among enabled controls; wrap in both directions. Do not
    /// translate printable text here: text entry must remain with the owning
    /// input widget rather than activating a neighboring control.
    /// </summary>
    internal MicroStarDialogNavigationResult Handle(ConsoleKey key, ConsoleModifiers modifiers = 0)
    {
        if (key == ConsoleKey.Escape ||
            (key == ConsoleKey.U && modifiers.HasFlag(ConsoleModifiers.Control)))
            return MicroStarDialogNavigationResult.Cancel;

        // Alt/Ctrl combinations are owned by the application or terminal,
        // except for the explicit abort shortcut handled above.
        if ((modifiers & (ConsoleModifiers.Alt | ConsoleModifiers.Control)) != 0)
            return MicroStarDialogNavigationResult.Ignored;

        if (key == ConsoleKey.Enter)
            return MicroStarDialogNavigationResult.Activate;

        if (key != ConsoleKey.Tab && key != ConsoleKey.DownArrow &&
            key != ConsoleKey.UpArrow)
            return MicroStarDialogNavigationResult.Ignored;

        var direction = key == ConsoleKey.UpArrow ||
            (key == ConsoleKey.Tab && modifiers.HasFlag(ConsoleModifiers.Shift))
            ? -1 : 1;

        var originalIndex = _focusedIndex;
        for (var scanned = 0; scanned < _controls.Length; scanned++)
        {
            _focusedIndex = (_focusedIndex + direction + _controls.Length) % _controls.Length;
            if (_controls[_focusedIndex].Enabled)
            {
                // A dialog may intentionally expose just one enabled field
                // while displaying several disabled controls. Wrapping back
                // to that same field is not a focus transition; reporting a
                // change would trigger unnecessary host redraw/focus events.
                return _focusedIndex == originalIndex
                    ? MicroStarDialogNavigationResult.Ignored
                    : MicroStarDialogNavigationResult.FocusChanged;
            }
        }

        // The constructor guarantees at least one enabled item.
        throw new InvalidOperationException("Dialog focus invariant violated.");
    }
}
