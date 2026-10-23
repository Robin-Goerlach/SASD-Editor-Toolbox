using Sasd.Editor.Rendering;

namespace Sasd.Editor.FirstEd.Sample;

/// <summary>
/// Builds the FIRST-ED sample's window header without touching the terminal.
/// The portable viewport supplies factual editor status, while the console
/// host owns the historic-looking labels and the active-window indicator.
/// </summary>
internal static class ConsoleFirstEdStatusLinePolicy
{
    internal static string Format(EditorStatus status, int windowNumber, bool active)
    {
        ArgumentNullException.ThrowIfNull(status);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(windowNumber);

        // Never inspect session.CurrentWindow here. The caller provides the
        // viewport for EACH displayed window and its own active flag. In a
        // linked-document layout, cursors and editing options belong to the
        // individual views even when both show the same underlying text.
        var marker = active ? ">" : " ";
        var mode = status.InsertMode ? "INS" : "OVR";
        var dirty = status.IsDirty ? " *" : string.Empty;
        return $"{marker}W{windowNumber} {status.FileName}  Ln {status.Line}  Col {status.Column}  {mode}" +
            $"  WW:{OnOff(status.WordWrap)} AI:{OnOff(status.AutoIndent)}{dirty}";
    }

    private static string OnOff(bool value) => value ? "ON" : "OFF";
}
