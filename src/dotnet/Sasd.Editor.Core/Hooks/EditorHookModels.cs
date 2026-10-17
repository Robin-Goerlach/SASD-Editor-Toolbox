using Sasd.Editor.Model;

namespace Sasd.Editor.Hooks;

/// <summary>
/// The exact on-document text range offered to the host for confirmation.
/// </summary>
/// <param name="Position">Logical position of the candidate within its document.</param>
/// <param name="ExistingText">
/// Original matched document content, preserving capitalization and spelling.
/// This is deliberately not the user's possibly case-insensitive query.
/// </param>
/// <param name="Replacement">Text requested as the replacement.</param>
public sealed record ReplaceCandidate(TextPosition Position, string ExistingText, string Replacement);

public enum ReplaceDecision
{
    Replace,
    Skip,
    Cancel
}
