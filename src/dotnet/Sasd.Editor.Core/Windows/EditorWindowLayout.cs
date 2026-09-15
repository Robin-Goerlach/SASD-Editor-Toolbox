using Sasd.Editor.Editing;

namespace Sasd.Editor.Windows;

/// <summary>
/// Host-neutral vertical screen geometry for one displayed editor window.
/// <para>
/// <see cref="Height"/> includes the window's status row. Consequently,
/// <see cref="TextRows"/> is one row smaller, matching the Turbo Editor Toolbox
/// convention used by <c>EditWindowCreate</c>.
/// </para>
/// </summary>
public sealed record EditorWindowFrame(Guid WindowId, int TopRow, int Height)
{
    public const int StatusRowCount = 1;
    public const int MinimumHeight = 3;

    public int TextRows => Math.Max(0, Height - StatusRowCount);

    public int BottomRow => TopRow + Height - 1;
}

/// <summary>
/// Maintains the vertical allocation of displayed editor windows independently
/// of any concrete console, WinForms, WPF or browser rendering API.
/// </summary>
/// <remarks>
/// The historical editor kept screen boundaries in every window descriptor.
/// SASD keeps the same observable geometry but centralizes allocation rules so
/// renderers only consume frames and do not own editor semantics.
/// </remarks>
public sealed class EditorWindowLayout(EditorSession session)
{
    private readonly Dictionary<Guid, int> _heights = [];

    /// <summary>Total number of host rows currently assigned to editor windows.</summary>
    public int TotalRows { get; private set; }

    public bool IsConfigured => TotalRows > 0;

    /// <summary>
    /// Returns frames in the same order as <see cref="EditorSession.Windows"/>.
    /// Top rows are derived rather than stored, which avoids stale coordinates
    /// after insert/delete operations.
    /// </summary>
    public IReadOnlyList<EditorWindowFrame> Frames
    {
        get
        {
            var frames = new List<EditorWindowFrame>(session.Windows.Count);
            var top = 0;
            foreach (var window in session.Windows)
            {
                if (!_heights.TryGetValue(window.WindowId, out var height))
                {
                    continue;
                }

                frames.Add(new EditorWindowFrame(window.WindowId, top, height));
                top += height;
            }

            return frames;
        }
    }

    /// <summary>
    /// Assigns a complete workspace to all currently displayed windows. This is
    /// primarily used when a host first attaches to a session. With more than
    /// one pre-existing window, rows are distributed as evenly as possible.
    /// </summary>
    public bool Configure(int totalRows)
    {
        if (session.Windows.Count == 0 || totalRows < session.Windows.Count * EditorWindowFrame.MinimumHeight)
        {
            return false;
        }

        _heights.Clear();
        TotalRows = totalRows;

        var baseHeight = totalRows / session.Windows.Count;
        var remainder = totalRows % session.Windows.Count;
        for (var index = 0; index < session.Windows.Count; index++)
        {
            _heights[session.Windows[index].WindowId] = baseHeight + (index < remainder ? 1 : 0);
        }

        return true;
    }

    /// <summary>
    /// Adapts an already configured layout to a resized host workspace. Growth
    /// is given to the bottom window. Shrinking removes spare rows from bottom
    /// to top but never takes a window below the historical three-row minimum.
    /// </summary>
    public bool ResizeWorkspace(int totalRows)
    {
        if (!IsConfigured || _heights.Count != session.Windows.Count)
        {
            return Configure(totalRows);
        }

        if (totalRows < session.Windows.Count * EditorWindowFrame.MinimumHeight)
        {
            return false;
        }

        var delta = totalRows - TotalRows;
        if (delta == 0)
        {
            return true;
        }

        if (delta > 0)
        {
            var last = session.Windows[^1];
            _heights[last.WindowId] += delta;
            TotalRows = totalRows;
            return true;
        }

        var rowsToRemove = -delta;
        for (var index = session.Windows.Count - 1; index >= 0 && rowsToRemove > 0; index--)
        {
            var window = session.Windows[index];
            var currentHeight = _heights[window.WindowId];
            var removable = currentHeight - EditorWindowFrame.MinimumHeight;
            var take = Math.Min(removable, rowsToRemove);
            _heights[window.WindowId] = currentHeight - take;
            rowsToRemove -= take;
        }

        if (rowsToRemove != 0)
        {
            return false;
        }

        TotalRows = totalRows;
        return true;
    }

    public EditorWindowFrame? GetFrame(Guid windowId)
    {
        var top = 0;
        foreach (var window in session.Windows)
        {
            if (!_heights.TryGetValue(window.WindowId, out var height))
            {
                continue;
            }

            if (window.WindowId == windowId)
            {
                return new EditorWindowFrame(windowId, top, height);
            }

            top += height;
        }

        return null;
    }

    internal bool CanSplit(EditorWindow donor, int newWindowHeight)
    {
        ArgumentNullException.ThrowIfNull(donor);
        return IsConfigured &&
               newWindowHeight >= EditorWindowFrame.MinimumHeight &&
               _heights.TryGetValue(donor.WindowId, out var donorHeight) &&
               donorHeight - newWindowHeight >= EditorWindowFrame.MinimumHeight;
    }

    /// <summary>
    /// Gives the lower <paramref name="newWindowHeight"/> rows of the donor to a
    /// newly inserted window. Validation must be performed with <see cref="CanSplit"/>
    /// before the new logical window is created.
    /// </summary>
    internal void Split(EditorWindow donor, EditorWindow newWindow, int newWindowHeight)
    {
        ArgumentNullException.ThrowIfNull(donor);
        ArgumentNullException.ThrowIfNull(newWindow);

        if (!CanSplit(donor, newWindowHeight))
        {
            throw new InvalidOperationException("The requested window split is not valid for the current layout.");
        }

        _heights[donor.WindowId] -= newWindowHeight;
        _heights[newWindow.WindowId] = newWindowHeight;
    }

    /// <summary>
    /// Reclaims a removed window's rows according to the Toolbox rule: deleting
    /// window 1 gives its rows to window 2; otherwise the window immediately
    /// above the deleted window receives them.
    /// </summary>
    internal void OnWindowRemoving(EditorWindow target, int targetIndex)
    {
        ArgumentNullException.ThrowIfNull(target);
        if (!_heights.Remove(target.WindowId, out var freedHeight) || session.Windows.Count <= 1)
        {
            return;
        }

        var recipient = targetIndex == 0
            ? session.Windows[1]
            : session.Windows[targetIndex - 1];

        if (_heights.TryGetValue(recipient.WindowId, out var recipientHeight))
        {
            _heights[recipient.WindowId] = recipientHeight + freedHeight;
        }
    }
}
