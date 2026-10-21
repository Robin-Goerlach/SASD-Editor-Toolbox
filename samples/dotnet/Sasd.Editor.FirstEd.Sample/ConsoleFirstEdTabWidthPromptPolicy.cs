using Sasd.Editor.Commands;
using Sasd.Editor.Input;

namespace Sasd.Editor.FirstEd.Sample;

/// <summary>
/// Resolves the historical FIRST-ED Set Tab Width prompt without exposing
/// terminal input or document state to the reusable editor core.
/// </summary>
/// <remarks>
/// The handbook's EditCptabdef wrapper treats an *empty submitted string*
/// specially: its tab interval is the displayed cursor column minus one.
/// The .NET window stores columns zero-based, so that number is exactly
/// the initiating window's Cursor.Column. A null response is a cancelled
/// prompt and must never invoke SetTabWidth.
/// </remarks>
internal static class ConsoleFirstEdTabWidthPromptPolicy
{
    /// <summary>
    /// Converts a host-collected response to the existing semantic request.
    /// Input collection, command validation and editor mutation are separate.
    /// </summary>
    /// <param name="binding">The actual numeric SetTabWidth binding.</param>
    /// <param name="visibleRows">Text-row count from the current host.</param>
    /// <param name="cursorColumn">
    /// Zero-based cursor column captured *before* opening the prompt.
    /// </param>
    /// <param name="answer">
    /// Null cancels; empty Enter means derive the width from the cursor;
    /// an explicitly supplied integer keeps the usual numeric semantics.
    /// </param>
    internal static EditorCommandRequest? Resolve(
        EditorCommandBinding binding,
        int visibleRows,
        int cursorColumn,
        string? answer)
    {
        ArgumentNullException.ThrowIfNull(binding);
        if (binding.CommandId != EditorCommandId.SetTabWidth
            || binding.ArgumentKind != EditorCommandArgumentKind.Number)
        {
            throw new ArgumentException(
                "This policy accepts only the numeric FIRST-ED tab-width binding.",
                nameof(binding));
        }

        if (answer is null)
        {
            // An escaped prompt is not the handbook's deliberate empty answer.
            return null;
        }

        if (answer.Length == 0)
        {
            // At the first displayed column, the historical formula yields
            // width zero. The reusable command requires a positive interval.
            // Avoid dispatching an invalid command or changing TabSize.
            // Later columns translate directly from one-based display to
            // the zero-based column held by EditorWindow.
            if (cursorColumn <= 0)
                return null;

            return binding.CreateRequest(number: cursorColumn, pageSize: visibleRows);
        }

        // All nonempty responses use the existing shared, invariant numeric
        // parser. In particular whitespace-only is NOT a deliberate empty
        // string, and overflow must not silently wrap into a tab interval.
        return EditorCommandPromptResolver.Resolve(
            binding, visibleRows, firstResponse: answer);
    }
}
