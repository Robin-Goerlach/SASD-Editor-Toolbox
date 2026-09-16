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

    /// <summary>
    /// The boundary after this logical line is a soft word-wrap boundary rather
    /// than an explicit paragraph/newline boundary. This outgoing-boundary model
    /// matches the legacy high-bit carriage-return file convention and allows a
    /// reformatter to follow a paragraph without relying on storage pointers.
    /// </summary>
    Wrapped = 1 << 1,

    UserColored = 1 << 2
}
