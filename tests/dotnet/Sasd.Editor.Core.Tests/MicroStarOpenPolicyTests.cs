using Sasd.Editor.FirstEd.Sample;
using Xunit;

namespace Sasd.Editor.Core.Tests;

/// <summary>
/// Covers the dangerous branching logic WITHOUT filesystem side effects.
/// A later integration test can use these same invariants around Core IO.
/// </summary>
public sealed class MicroStarOpenPolicyTests
{
    [Fact]
    public void CancelNeverAllowsAWindowTransition()
    {
        var decision = MicroStarOpenPolicy.Plan(
            MicroStarOpenDisposition.Cancel, true, true);
        Assert.False(decision.MayOpen);
        Assert.False(decision.MustSave);
        Assert.False(MicroStarOpenPolicy.CanCommit(decision, 1, 1, false, true));
    }

    [Fact]
    public void SaveThenOpenCannotBecomeImplicitAbandon()
    {
        var plan = MicroStarOpenPolicy.Plan(
            MicroStarOpenDisposition.SaveThenOpen, true, false);
        Assert.True(plan.MayOpen);
        Assert.True(plan.MustSave);
        Assert.False(MicroStarOpenPolicy.CanCommit(plan, 1, 1, true, true));
        Assert.False(MicroStarOpenPolicy.CanCommit(plan, 1, 1, false, false));
        Assert.True(MicroStarOpenPolicy.CanCommit(plan, 1, 1, false, true));
    }

    [Fact]
    public void AbandonDirtyDocumentRequiresExplicitSeparateApproval()
    {
        var denied = MicroStarOpenPolicy.Plan(
            MicroStarOpenDisposition.AbandonThenOpen, true, false);
        Assert.True(denied.RequiresExplicitDiscardConfirmation);
        Assert.False(denied.MayOpen);

        var accepted = MicroStarOpenPolicy.Plan(
            MicroStarOpenDisposition.AbandonThenOpen, true, true);
        Assert.True(accepted.MayOpen);
        Assert.False(accepted.MustSave);
        Assert.True(MicroStarOpenPolicy.CanCommit(accepted, 7, 7, true, false));
    }

    [Fact]
    public void CleanDocumentDoesNotDemandDiscardPermission()
    {
        var plan = MicroStarOpenPolicy.Plan(
            MicroStarOpenDisposition.AbandonThenOpen, false, false);
        Assert.True(plan.MayOpen);
        Assert.False(plan.RequiresExplicitDiscardConfirmation);
        Assert.True(MicroStarOpenPolicy.CanCommit(plan, 0, 0, false, false));
    }

    [Fact]
    public void ConcurrentTextEditRejectsBothPreviouslyApprovedPaths()
    {
        foreach (var disposition in new[] {
            MicroStarOpenDisposition.SaveThenOpen,
            MicroStarOpenDisposition.AbandonThenOpen })
        {
            var plan = MicroStarOpenPolicy.Plan(disposition, true, true);
            Assert.False(MicroStarOpenPolicy.CanCommit(
                plan, versionAtPreflight: 10, currentVersion: 11,
                currentDirty: true, saveCompletedSuccessfully: true));
        }
    }

    [Fact]
    public void InvalidDispositionDoesNotDefaultToDiscard()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            MicroStarOpenPolicy.Plan((MicroStarOpenDisposition)42, true, true));
    }
}
