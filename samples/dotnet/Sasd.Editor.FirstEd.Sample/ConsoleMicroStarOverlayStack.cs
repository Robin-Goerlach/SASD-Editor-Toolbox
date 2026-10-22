namespace Sasd.Editor.FirstEd.Sample;

/// <summary>
/// Owns the LIFO lifetime of temporary terminal overlays independently of
/// editor documents and physical console APIs. This is a host-only service.
/// </summary>
/// <remarks>
/// Historical popup restoration could copy DOS video-memory cells. Modern
/// terminals cannot safely expose their underlying Unicode/VT cell contents.
/// Consequently the renderer paints the editor model, then all active popups
/// from bottom to top. Closing a child restores its parent by recomposition.
/// </remarks>
internal sealed class ConsoleMicroStarOverlayStack
{
    private readonly List<ConsoleMicroStarPopupLayout> _active = [];

    /// <summary>Returns a snapshot, not the mutable ownership list.</summary>
    internal IReadOnlyList<ConsoleMicroStarPopupLayout> Active => _active.ToArray();
    internal int Count => _active.Count;

    /// <summary>
    /// The returned scope must be disposed in reverse creation order. This
    /// prevents closing a parent while a nested modal child still has focus.
    /// </summary>
    internal IDisposable Push(ConsoleMicroStarPopupLayout layout)
    {
        ArgumentNullException.ThrowIfNull(layout);
        _active.Add(layout);
        return new Lease(this, layout);
    }

    private void Pop(ConsoleMicroStarPopupLayout expected)
    {
        if (_active.Count == 0 || !ReferenceEquals(_active[^1], expected))
            throw new InvalidOperationException(
                "Popup overlays must be closed in reverse opening order.");
        _active.RemoveAt(_active.Count - 1);
    }

    private sealed class Lease(
        ConsoleMicroStarOverlayStack stack,
        ConsoleMicroStarPopupLayout layout) : IDisposable
    {
        private bool _disposed;
        public void Dispose()
        {
            if (_disposed) return;
            // Mark disposed only AFTER a successful pop. An out-of-order
            // attempt must allow a later correctly ordered retry.
            stack.Pop(layout);
            _disposed = true;
        }
    }
}
