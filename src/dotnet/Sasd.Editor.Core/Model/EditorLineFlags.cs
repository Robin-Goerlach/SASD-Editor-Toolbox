namespace Sasd.Editor.Model;

/// <summary>
/// Metadata attached to a logical line. The first three flags intentionally
/// mirror concepts exposed by the historical Turbo Editor Toolbox.
/// </summary>
[Flags]
public enum EditorLineFlags
{
    None = 0,
    InBlock = 1 << 0,
    Wrapped = 1 << 1,
    UserColored = 1 << 2
}
