namespace Sasd.Editor.FirstEd.Sample;

/// <summary>
/// Detects changes in the terminal's cell dimensions independently of the
/// keyboard. Kept in the console host so that the portable editor core is not
/// coupled to Windows Terminal, Linux PTYs, or any Console API.
/// </summary>
internal sealed class ConsoleTerminalSizeTracker
{
    private (int Width, int Height)? _last;

    /// <summary>
    /// Records the observed geometry and reports whether an already observed
    /// size has changed. The first observation establishes a baseline instead
    /// of treating startup as an unexpected resize event.
    /// </summary>
    internal bool Observe(int width, int height)
    {
        var current = (Math.Max(1, width), Math.Max(1, height));
        var changed = _last.HasValue && _last.Value != current;
        _last = current;
        return changed;
    }
}
