namespace Sasd.Editor.FirstEd.Sample;

/// <summary>
/// Explicit evidence supplied by an acceptance reviewer for one historical
/// feature. This model does NOT infer parity from a class name or a passing
/// build: each field represents a separate documented review decision.
/// </summary>
/// <remarks>
/// MicroStar sample acceptance is deliberately separate from FIRST-ED core
/// conformance. A Windows/Linux terminal check matters for menu behavior,
/// but may be inapplicable to a purely headless routine; callers declare
/// that explicitly rather than silently omitting the check.
/// </remarks>
internal readonly record struct MicroStarAcceptanceEvidence(
    bool HandbookBehaviorReviewed,
    bool ImplementationTraced,
    bool AutomatedTestsPassed,
    bool InteractiveCheckRequired,
    bool InteractiveCheckPassed,
    bool DivergencesReviewed,
    bool FinalParityReviewPassed);

/// <summary>
/// Conservative historical-acceptance decision: no missing evidence is
/// converted into success. A recorded modernization can be accepted only
/// after its behavioral difference is reviewed and explicitly approved.
/// </summary>
internal static class MicroStarAcceptancePolicy
{
    internal static bool IsHistoricallyAccepted(MicroStarAcceptanceEvidence evidence)
    {
        // Keep every independent gate readable: reviewers should be able
        // to see exactly which proof obligations define full acceptance.
        // This method is a policy for future sign-off, not a claim that
        // today's MicroStar sample already satisfies all of them.
        // One source of truth: the detailed report and yes/no decision must
        // never disagree after a new evidence gate is introduced. Future
        // policy changes should be implemented in Missing(), where the
        // reviewer can also see the precise reason for non-acceptance.
        return MicroStarAcceptanceReport.IsReady(evidence);
    }
}
