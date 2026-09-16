namespace Sasd.Editor.Model;

/// <summary>
/// Historical V1 text marker. Turbo Editor Toolbox markers identify a logical
/// line; jumping to a marker must not replace the target window's column.
/// </summary>
/// <remarks>
/// The first .NET implementation stores the line index rather than a raw linked
/// list pointer. Line-topology updates that move or delete a marked line are part
/// of the remaining low-level compatibility audit.
/// </remarks>
public sealed record EditorMarker(Guid DocumentId, int Line);
