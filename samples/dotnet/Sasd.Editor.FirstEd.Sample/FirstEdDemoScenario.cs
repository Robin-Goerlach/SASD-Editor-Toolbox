using Sasd.Editor.Editing;

namespace Sasd.Editor.FirstEd.Sample;

/// <summary>
/// Builds a ready-to-edit, in-memory developer demonstration using the same
/// public editor API that a third-party terminal/desktop host would consume.
/// </summary>
/// <remarks>
/// Deliberately NOT an alternative editor model: no custom text buffer,
/// private command processor, fake window or sample-specific Undo machinery.
/// A plain EditorSession and its public CreateDocument API are enough.
///
/// The fixture uses only Latin-1-compatible ASCII because the FIRST-ED
/// reference host saves via the historical 8-bit codec. The embedded ESC
/// is intentional and safe to display only because every terminal row
/// passes through ConsoleFirstEdRowProjection before Console.Write.
/// </remarks>
internal static class FirstEdDemoScenario
{
    internal static string Text { get; } = string.Join('\n',
    [
        "SASD Editor Toolbox - FIRST-ED interactive developer demo",
        "=====================================================",
        "",
        "This is REAL editable text, not a mockup or a simulated editor.",
        "The application uses EditorSession + EditorCommandDispatcher.",
        "",
        "1. NAVIGATION: arrows, Home, End, PgUp, PgDn; Ctrl-Q R = top.",
        "2. EDITING: type text, Enter = newline, Ctrl-N = insert line.",
        "3. UNDO: Escape restores the last editing transaction.",
        "4. SEARCH: Ctrl-Q F then type a search word, e.g. WIDGET.",
        "5. BLOCK: Ctrl-K B begins; Ctrl-K K ends; Ctrl-K C copies.",
        "6. WINDOWS: Ctrl-O O prompts for two window numbers.",
        "7. MARKERS: Ctrl-K 1 stores, Ctrl-Q 1 revisits a marker.",
        "8. SAVE: Ctrl-K S uses the historical 8-bit FIRST-ED codec.",
        "9. EXIT: Ctrl-K X opens an exit confirmation.",
        "",
        "WIDGET: edit this line, duplicate it or search for WIDGET.",
        "Sample block line 1: alpha beta gamma",
        "Sample block line 2: delta epsilon zeta",
        "",
        // Deliberately stored control character: if any renderer bypasses
        // the host's safe output projection, this can become an ANSI
        // clear-screen instruction. The screen MUST show '^[[' instead.
        "SAFE CONTROL DISPLAY: " + '\u001b' + "[2J must be visible, not executed.",
        "",
        "Create and change windows or markers to exercise session topology.",
        "The initial document is not associated with any real disk file.",
        "Consult docs/en/DEMO-EDITOR-AND-PUBLIC-API-ACCEPTANCE.md.",
    ]);

    internal static void Initialize(EditorSession session)
    {
        ArgumentNullException.ThrowIfNull(session);

        // Use precisely the public API expected of downstream applications.
        // No special persistence state: an untampered demonstration starts
        // clean with no Undo entries or associated filename. A later Save
        // must be a deliberate user action, not an implicit file overwrite.
        session.CreateDocument(Text);
    }
}
