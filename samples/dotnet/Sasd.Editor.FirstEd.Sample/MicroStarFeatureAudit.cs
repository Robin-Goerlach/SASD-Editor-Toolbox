namespace Sasd.Editor.FirstEd.Sample;

/// <summary>
/// One named historical behavior and its independently reviewed evidence.
/// Source references are review breadcrumbs, not proof by themselves.
/// </summary>
internal sealed record MicroStarFeatureReview(
    string FeatureId,
    string HandbookReference,
    string ImplementationReference,
    string TestReference,
    MicroStarAcceptanceEvidence Evidence);

/// <summary>
/// Summarizes a whole feature-review set without confusing a successful
/// build with complete Borland compatibility. Review IDs must be unique,
/// and an empty checklist must never be interpreted as full acceptance.
/// </summary>
internal static class MicroStarFeatureAudit
{
    internal static IReadOnlyList<(string FeatureId,
        IReadOnlyList<MicroStarAcceptanceGap> Missing)> Evaluate(
        IEnumerable<MicroStarFeatureReview> reviews)
    {
        ArgumentNullException.ThrowIfNull(reviews);
        var result = new List<(string FeatureId,
            IReadOnlyList<MicroStarAcceptanceGap> Missing)>();
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var review in reviews)
        {
            ArgumentNullException.ThrowIfNull(review);
            // Detect accidental duplicate coverage. Otherwise a report
            // could inflate its completion count by listing a feature twice.
            if (string.IsNullOrWhiteSpace(review.FeatureId))
                throw new ArgumentException("Feature ID must not be empty.", nameof(reviews));
            if (!seen.Add(review.FeatureId))
                throw new ArgumentException("Duplicate feature ID: " + review.FeatureId,
                    nameof(reviews));
            if (string.IsNullOrWhiteSpace(review.HandbookReference)
                || string.IsNullOrWhiteSpace(review.ImplementationReference)
                || string.IsNullOrWhiteSpace(review.TestReference))
                throw new ArgumentException("Every review requires explicit references.",
                    nameof(reviews));

            result.Add((review.FeatureId,
                MicroStarAcceptanceReport.Missing(review.Evidence)));
        }

        return result;
    }

    internal static bool AllAccepted(IEnumerable<MicroStarFeatureReview> reviews)
    {
        var report = Evaluate(reviews);
        // No supplied reviews means no coverage, not a vacuous success.
        return report.Count > 0 && report.All(row => row.Missing.Count == 0);
    }
}
