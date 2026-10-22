namespace Sasd.Editor.FirstEd.Sample;

/// <summary>
/// Pure, viewport-aware selection model for MicroStar lists (files, commands,
/// later directory entries). The model neither reads Console input nor opens
/// files and therefore can be tested without a physical terminal.
/// </summary>
/// <remarks>
/// The editor's reusable Core deliberately has no dependency on this UI
/// component. The item IDs are stable semantic values; the selection is an
/// index into an immutable snapshot, never a mutable filesystem path.
/// Performance optimizations such as virtualized backing stores come later.
/// </remarks>
internal sealed record MicroStarListItem(string Id, string Caption, bool Enabled = true);

internal enum MicroStarListResult
{
    Ignored,
    SelectionChanged,
    Accepted,
    Cancelled
}

internal sealed class MicroStarSelectionList
{
    private readonly MicroStarListItem[] _items;
    private readonly int _visibleRows;
    private int _selected;
    private int _firstVisible;

    internal MicroStarSelectionList(IEnumerable<MicroStarListItem> items, int visibleRows)
    {
        ArgumentNullException.ThrowIfNull(items);
        if (visibleRows < 1) throw new ArgumentOutOfRangeException(nameof(visibleRows));
        _items = items.ToArray();

        // Identifiers, not caption strings, are the application-facing
        // contract. Duplicate IDs would make a selected action ambiguous.
        if (_items.Any(x => x is null || string.IsNullOrWhiteSpace(x.Id) ||
            x.Caption is null) ||
            _items.Select(x => x.Id).Distinct(StringComparer.Ordinal).Count() != _items.Length)
            throw new ArgumentException("List entries require unique nonempty IDs and captions.", nameof(items));

        _visibleRows = visibleRows;
        _selected = Array.FindIndex(_items, item => item.Enabled);
        if (_selected >= 0) RevealSelection();
    }

    internal int Count => _items.Length;
    internal int FirstVisible => _firstVisible;
    internal int VisibleRows => _visibleRows;
    internal MicroStarListItem? Selected => _selected < 0 ? null : _items[_selected];
    internal IReadOnlyList<MicroStarListItem> Visible =>
        Array.AsReadOnly(_items.Skip(_firstVisible).Take(_visibleRows).ToArray());

    internal MicroStarListResult Handle(ConsoleKey key, ConsoleModifiers modifiers = 0)
    {
        if (key == ConsoleKey.Escape ||
            (key == ConsoleKey.U && modifiers.HasFlag(ConsoleModifiers.Control)))
            return MicroStarListResult.Cancelled;
        if ((modifiers & (ConsoleModifiers.Control | ConsoleModifiers.Alt)) != 0)
            return MicroStarListResult.Ignored;
        if (key == ConsoleKey.Enter)
            return Selected is null ? MicroStarListResult.Ignored : MicroStarListResult.Accepted;

        // Empty or all-disabled lists have no actionable position. They can
        // still be shown and cancelled; moving must never fabricate an item.
        if (_selected < 0) return MicroStarListResult.Ignored;

        var direction = key switch
        {
            ConsoleKey.UpArrow => -1,
            ConsoleKey.DownArrow => 1,
            ConsoleKey.PageUp => -_visibleRows,
            ConsoleKey.PageDown => _visibleRows,
            ConsoleKey.Home => int.MinValue,
            ConsoleKey.End => int.MaxValue,
            _ => 0
        };
        if (direction == 0) return MicroStarListResult.Ignored;

        var target = direction switch
        {
            int.MinValue => 0,
            int.MaxValue => _items.Length - 1,
            _ => (int)Math.Clamp((long)_selected + direction, 0, _items.Length - 1)
        };

        // Moving toward an unavailable row scans in the same direction.
        // A disabled terminal row stays visible, but cannot be accepted.
        var step = Math.Sign(target - _selected);
        if (step == 0) return MicroStarListResult.Ignored;
        for (var index = target; index >= 0 && index < _items.Length; index += step)
        {
            if (!_items[index].Enabled) continue;
            if (index == _selected) return MicroStarListResult.Ignored;
            _selected = index;
            RevealSelection();
            return MicroStarListResult.SelectionChanged;
        }
        return MicroStarListResult.Ignored;
    }

    private void RevealSelection()
    {
        if (_selected < _firstVisible) _firstVisible = _selected;
        if (_selected >= _firstVisible + _visibleRows)
            _firstVisible = _selected - _visibleRows + 1;
    }
}
