namespace Sasd.Editor.FirstEd.Sample;

/// <summary>
/// Explicit user intent for leaving one document before opening another.
/// These are distinct operations: saving cannot be inferred from a Yes/No
/// response to discarding edits, nor vice versa.
/// </summary>
internal enum MicroStarOpenDisposition
{
    Cancel,
    SaveThenOpen,
    AbandonThenOpen
}

/// <summary>
/// One immutable preflight decision. The host must execute the returned plan
/// before changing the editor's document/window identity.
/// </summary>
internal sealed record MicroStarOpenPlan(
    MicroStarOpenDisposition Disposition,
    bool MustSave,
    bool RequiresExplicitDiscardConfirmation,
    bool MayOpen);

/// <summary>
/// Pure application policy for the original MicroStar-style Save &amp; Open and
/// Abandon &amp; Open workflows. This intentionally does NOT read files, move
/// windows, prompt users or mutate EditorSession.
/// </summary>
/// <remarks>
/// The work is split into preflight/IO/commit. Preflight proves whether the
/// requested transition is allowed. Future IO stages the incoming document
/// without losing the old one; commit may reattach the original view only
/// after successful decoding and a final concurrency guard. Keeping the
/// decision policy independent lets the browser and a future WinForms host
/// make the same safety choice without copying UI code.
/// </remarks>
internal static class MicroStarOpenPolicy
{
    internal static MicroStarOpenPlan Plan(
        MicroStarOpenDisposition disposition,
        bool isDirty,
        bool discardWasExplicitlyConfirmed)
    {
        if (!Enum.IsDefined(disposition))
            throw new ArgumentOutOfRangeException(nameof(disposition));

        return disposition switch
        {
            MicroStarOpenDisposition.Cancel =>
                new(disposition, MustSave: false,
                    RequiresExplicitDiscardConfirmation: false, MayOpen: false),

            // Save is a mandatory prerequisite even for a clean source in
            // this conservative version: the workflow name is a promise to
            // persist before switching. A missing path or failed write must
            // therefore block opening, never silently become Discard.
            MicroStarOpenDisposition.SaveThenOpen =>
                new(disposition, MustSave: true,
                    RequiresExplicitDiscardConfirmation: false, MayOpen: true),

            // A clean document contains no unsaved edits to discard. Dirty
            // documents demand an *explicit* second confirmation, not merely
            // pressing the Abandon item or Enter on a default button.
            MicroStarOpenDisposition.AbandonThenOpen =>
                new(disposition, MustSave: false,
                    RequiresExplicitDiscardConfirmation: isDirty,
                    MayOpen: !isDirty || discardWasExplicitlyConfirmed),

            _ => throw new ArgumentOutOfRangeException(nameof(disposition))
        };
    }

    /// <summary>
    /// The state may have changed while Save or file decoding awaited IO.
    /// Recheck the original source version and actual dirty state immediately
    /// before committing. Never let a stale preflight replace newer edits.
    /// </summary>
    internal static bool CanCommit(
        MicroStarOpenPlan plan,
        long versionAtPreflight,
        long currentVersion,
        bool currentDirty,
        bool saveCompletedSuccessfully)
    {
        ArgumentNullException.ThrowIfNull(plan);
        if (!plan.MayOpen || versionAtPreflight != currentVersion)
            return false;

        return plan.Disposition switch
        {
            MicroStarOpenDisposition.SaveThenOpen =>
                saveCompletedSuccessfully && !currentDirty,
            MicroStarOpenDisposition.AbandonThenOpen =>
                !currentDirty || plan.RequiresExplicitDiscardConfirmation,
            _ => false
        };
    }
}
