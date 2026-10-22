using Sasd.Editor.FirstEd.Sample;
using Xunit;

namespace Sasd.Editor.Core.Tests;

/// <summary>
/// Coverage accounting must not accidentally declare historical parity
/// merely because every item in an empty or duplicated list looks green.
/// These tests use fabricated review evidence, not claimed Borland results.
/// </summary>
public sealed class MicroStarFeatureAuditTests
{
    private static MicroStarFeatureReview Review(string id, bool approved) =>
        new(id, "Handbook p. 45", "Sample host", "Sample tests",
            new MicroStarAcceptanceEvidence(
                HandbookBehaviorReviewed: approved,
                ImplementationTraced: true,
                AutomatedTestsPassed: true,
                InteractiveCheckRequired: false,
                InteractiveCheckPassed: false,
                DivergencesReviewed: true,
                FinalParityReviewPassed: approved));

    [Fact]
    public void EmptyFeatureListNeverMeansParity()
    {
        Assert.Empty(MicroStarFeatureAudit.Evaluate([]));
        Assert.False(MicroStarFeatureAudit.AllAccepted([]));
    }

    [Fact]
    public void IndividualMissingEvidenceRemainsVisible()
    {
        var report = MicroStarFeatureAudit.Evaluate(
            [Review("menu", true), Review("printing", false)]);
        Assert.Equal(2, report.Count);
        Assert.Empty(report[0].Missing);
        Assert.Contains(MicroStarAcceptanceGap.HandbookBehavior, report[1].Missing);
        Assert.Contains(MicroStarAcceptanceGap.FinalParityReview, report[1].Missing);
        Assert.False(MicroStarFeatureAudit.AllAccepted(
            [Review("menu", true), Review("printing", false)]));
    }

    [Fact]
    public void DuplicateFeatureIdsAreRejectedCaseInsensitively()
    {
        Assert.Throws<ArgumentException>(() =>
            MicroStarFeatureAudit.Evaluate([Review("menu", true), Review("MENU", true)]));
    }

    [Fact]
    public void IncompleteEvidenceReferencesAreRejected()
    {
        var incomplete = Review("menu", true) with { TestReference = "" };
        Assert.Throws<ArgumentException>(() =>
            MicroStarFeatureAudit.Evaluate([incomplete]));
    }

    [Fact]
    public void CompleteNonemptySyntheticReviewSetCanPass()
    {
        Assert.True(MicroStarFeatureAudit.AllAccepted([Review("menu", true)]));
    }
}
