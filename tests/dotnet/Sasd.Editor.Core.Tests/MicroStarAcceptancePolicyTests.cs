using Sasd.Editor.FirstEd.Sample;
using Xunit;

namespace Sasd.Editor.Core.Tests;

/// <summary>
/// Historical parity is a separate claim from the existence of C# code
/// or a green test run. These tests lock down the review gates so future
/// feature additions cannot silently downgrade acceptance requirements.
/// </summary>
public sealed class MicroStarAcceptancePolicyTests
{
    private static readonly MicroStarAcceptanceEvidence Complete = new(
        HandbookBehaviorReviewed: true,
        ImplementationTraced: true,
        AutomatedTestsPassed: true,
        InteractiveCheckRequired: true,
        InteractiveCheckPassed: true,
        DivergencesReviewed: true,
        FinalParityReviewPassed: true);

    [Fact]
    public void CompleteEvidenceCanBeHistoricallyAccepted()
    {
        Assert.True(MicroStarAcceptancePolicy.IsHistoricallyAccepted(Complete));
    }

    [Fact]
    public void EveryRequiredEvidenceFieldCanBlockHistoricalAcceptance()
    {
        // Changing each requirement independently guards against a future
        // accidental relaxation, such as treating green unit tests alone
        // as proof of exact Borland behavior.
        Assert.False(MicroStarAcceptancePolicy.IsHistoricallyAccepted(
            Complete with { HandbookBehaviorReviewed = false }));
        Assert.False(MicroStarAcceptancePolicy.IsHistoricallyAccepted(
            Complete with { ImplementationTraced = false }));
        Assert.False(MicroStarAcceptancePolicy.IsHistoricallyAccepted(
            Complete with { AutomatedTestsPassed = false }));
        Assert.False(MicroStarAcceptancePolicy.IsHistoricallyAccepted(
            Complete with { InteractiveCheckPassed = false }));
        Assert.False(MicroStarAcceptancePolicy.IsHistoricallyAccepted(
            Complete with { DivergencesReviewed = false }));
        Assert.False(MicroStarAcceptancePolicy.IsHistoricallyAccepted(
            Complete with { FinalParityReviewPassed = false }));
    }

    [Fact]
    public void NonInteractiveRoutineDoesNotRequireTerminalTesting()
    {
        var evidence = Complete with {
            InteractiveCheckRequired = false,
            InteractiveCheckPassed = false
        };
        Assert.True(MicroStarAcceptancePolicy.IsHistoricallyAccepted(evidence));
    }

    [Fact]
    public void DefaultEvidenceNeverImpliesParity()
    {
        Assert.False(MicroStarAcceptancePolicy.IsHistoricallyAccepted(default));
    }
}
