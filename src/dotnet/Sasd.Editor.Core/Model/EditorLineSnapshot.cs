namespace Sasd.Editor.Model;

/// <summary>
/// Immutable representation of one logical line.
/// </summary>
public sealed record EditorLineSnapshot(string Text, EditorLineFlags Flags = EditorLineFlags.None);
