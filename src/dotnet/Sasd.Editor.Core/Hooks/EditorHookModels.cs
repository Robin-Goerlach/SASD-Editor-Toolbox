using Sasd.Editor.Model;

namespace Sasd.Editor.Hooks;

public sealed record ReplaceCandidate(TextPosition Position, string ExistingText, string Replacement);

public enum ReplaceDecision
{
    Replace,
    Skip,
    Cancel
}
