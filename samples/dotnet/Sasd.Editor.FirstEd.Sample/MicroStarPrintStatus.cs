namespace Sasd.Editor.FirstEd.Sample;

/// <summary>
/// Read-only projection of the cooperative MicroStar print job.
/// This class has no console, file-system or scheduler side effects,
/// so both terminal and future GUI hosts can share its semantics.
/// </summary>
internal static class MicroStarPrintStatus
{
    /// <summary>
    /// Report terminal states before progress: printer failure and explicit
    /// cancellation are never mistaken for successful completion.
    /// Counters are output records, including pagination separators.
    /// </summary>
    internal static string Describe(MicroStarPrintJob? job)
    {
        if (job is null) return "No print job has been started.";
        if (job.Failure is not null)
            return $"Print failed after {job.CompletedLines}/{job.TotalLines} records.";
        if (job.IsCancelled)
            return $"Print cancelled after {job.CompletedLines}/{job.TotalLines} records.";
        if (job.IsFinished)
            return $"Print complete: {job.CompletedLines}/{job.TotalLines} records.";
        return $"Printing: {job.CompletedLines}/{job.TotalLines} records.";
    }
}
