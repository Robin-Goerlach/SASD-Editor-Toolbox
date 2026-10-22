namespace Sasd.Editor.FirstEd.Sample;

/// <summary>
/// A named acceptance gate that is still missing evidence. Names are stable
/// review identifiers, not translated UI strings or guesses about Borland.
/// </summary>
internal enum MicroStarAcceptanceGap
{
    HandbookBehavior,
    ImplementationTrace,
    AutomatedTests,
    InteractiveTerminalCheck,
    DivergenceReview,
    FinalParityReview
}

/// <summary>
/// Explains *why* a feature is not historically accepted. The pure evaluator
/// never executes tests or claims they passed; reviewers supply evidence
/// explicitly. Reports are suitable for CI summaries and future GUIs.
/// </summary>
internal static class MicroStarAcceptanceReport
{
    internal static IReadOnlyList<MicroStarAcceptanceGap> Missing(
        MicroStarAcceptanceEvidence evidence)
    {
        var gaps = new List<MicroStarAcceptanceGap>();

        // Keep the reporting order consistent with the documented review
        // workflow. Missing checks are returned independently so a reviewer
        // can work through more than one outstanding requirement at a time.
        if (!evidence.HandbookBehaviorReviewed)
            gaps.Add(MicroStarAcceptanceGap.HandbookBehavior);
        if (!evidence.ImplementationTraced)
            gaps.Add(MicroStarAcceptanceGap.ImplementationTrace);
        if (!evidence.AutomatedTestsPassed)
            gaps.Add(MicroStarAcceptanceGap.AutomatedTests);
        if (evidence.InteractiveCheckRequired && !evidence.InteractiveCheckPassed)
            gaps.Add(MicroStarAcceptanceGap.InteractiveTerminalCheck);
        if (!evidence.DivergencesReviewed)
            gaps.Add(MicroStarAcceptanceGap.DivergenceReview);
        if (!evidence.FinalParityReviewPassed)
            gaps.Add(MicroStarAcceptanceGap.FinalParityReview);

        return gaps;
    }

    internal static bool IsReady(MicroStarAcceptanceEvidence evidence) =>
        Missing(evidence).Count == 0;
}
