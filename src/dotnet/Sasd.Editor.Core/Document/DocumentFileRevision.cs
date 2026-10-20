namespace Sasd.Editor.Document;

/// <summary>
/// Last confirmed on-disk byte revision for an editor document and path.
/// </summary>
/// <remarks>
/// SHA-256 of the RAW bytes (including BOM) is used instead of timestamps or
/// logical-line text. This is internal metadata, deliberately excluded from
/// Undo snapshots, public APIs, and historical FIRST-ED compatibility rules.
/// </remarks>
internal sealed record DocumentFileRevision(string FullPath, string Sha256);
