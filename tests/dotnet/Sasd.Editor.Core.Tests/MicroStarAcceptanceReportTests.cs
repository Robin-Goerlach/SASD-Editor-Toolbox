using Sasd.Editor.FirstEd.Sample;
using Xunit;

namespace Sasd.Editor.Core.Tests;

/// <summary>
/// A feature audit must show exactly which proof is missing. The report
/// cannot silently replace missing historical evidence with the mere
/// existence of modern code, documentation or a green CI pipeline.
/// </summary>
public sealed class MicroStarAcceptanceReportTests
{
    [Fact]
    public void DefaultEvidenceReportsAllMandatoryGaps()
    {
        Assert.Equal(
            [
                MicroStarAcceptanceGap.HandbookBehavior,
                MicroStarAcceptanceGap.ImplementationTrace,
                MicroStarAcceptanceGap.AutomatedTests,
                MicroStarAcceptanceGap.DivergenceReview,
                MicroStarAcceptanceGap.FinalParityReview
            ],
            MicroStarAcceptanceReport.Missing(default));
        Assert.False(MicroStarAcceptanceReport.IsReady(default));
    }

    [Fact]
    public void InteractiveTerminalGateAppearsOnlyWhenRequired()
    {
        var evidence = new MicroStarAcceptanceEvidence(
            HandbookBehaviorReviewed: true,
            ImplementationTraced: true,
            AutomatedTestsPassed: true,
            InteractiveCheckRequired: true,
            InteractiveCheckPassed: false,
            DivergencesReviewed: true,
            FinalParityReviewPassed: true);

        Assert.Equal([MicroStarAcceptanceGap.InteractiveTerminalCheck],
            MicroStarAcceptanceReport.Missing(evidence));
        Assert.False(MicroStarAcceptanceReport.IsReady(evidence));

        // Pure or noninteractive routines need no artificial terminal
        // session. This exemption must be explicit in the evidence model.
        Assert.True(MicroStarAcceptanceReport.IsReady(
            evidence with { InteractiveCheckRequired = false }));
    }

    [Fact]
    public void ReportAndPolicyAgreeOnCompleteEvidence()
    {
        var complete = new MicroStarAcceptanceEvidence(true, true, true,
            true, true, true, true);
        Assert.Empty(MicroStarAcceptanceReport.Missing(complete));
        Assert.True(MicroStarAcceptanceReport.IsReady(complete));
        Assert.True(MicroStarAcceptancePolicy.IsHistoricallyAccepted(complete));
    }
}
