using Sasd.Editor.Commands;
using Sasd.Editor.Input;
using Sasd.Editor.Editing;

namespace Sasd.Editor.FirstEd.Sample;

/// <summary>
/// Resolves prompt metadata produced by the core key map. The key map declares
/// what it needs; this terminal host decides how to ask for it.
/// </summary>
internal sealed class ConsoleFirstEdPromptService(ConsoleFirstEdRenderer renderer, EditorSession session)
{
    public EditorCommandRequest? Resolve(EditorCommandBinding binding, int visibleRows)
    {
        ArgumentNullException.ThrowIfNull(binding);

        // Save File has a conditional host prompt: a new NONAME document has
        // no target, while an already associated document can save directly.
        // Do not put this dialog decision into the reusable key map or core.
        if (binding.CommandId == EditorCommandId.SaveFile)
        {
            return ResolveSaveFile(binding, visibleRows);
        }

        return binding.ArgumentKind switch
        {
            EditorCommandArgumentKind.None => EditorCommandPromptResolver.Resolve(binding, visibleRows),
            EditorCommandArgumentKind.Number => ResolveNumber(binding, visibleRows),
            EditorCommandArgumentKind.TwoNumbers => ResolveTwoNumbers(binding, visibleRows),
            EditorCommandArgumentKind.Text => ResolveText(binding, visibleRows),
            EditorCommandArgumentKind.FindReplace => ResolveFindReplace(binding, visibleRows),
            EditorCommandArgumentKind.Character => ResolveCharacter(binding, visibleRows),
            EditorCommandArgumentKind.FilePath => ResolveFilePath(binding, visibleRows),
            EditorCommandArgumentKind.Confirmation => ResolveConfirmation(binding, visibleRows),
            _ => null
        };
    }

    private EditorCommandRequest? ResolveSaveFile(EditorCommandBinding binding, int visibleRows)
    {
        // Capture the active document association before collecting input.
        // Prompting is synchronous in this terminal sample; the selected view
        // must never inherit another view's previously used file name.
        var associatedPath = session.CurrentWindow.Document.FilePath;
        var enteredPath = ConsoleFirstEdSavePromptPolicy.RequiresPath(associatedPath)
            ? renderer.ReadPrompt("Save as file path: ")
            : null;

        var request = ConsoleFirstEdSavePromptPolicy.Resolve(
            binding, visibleRows, associatedPath, enteredPath);

        // For a first Save As, the user's chosen name may already belong to
        // another file. The historical core's Save operation deliberately
        // contains no terminal prompts: ask here, before a semantic write can
        // reach the dispatcher. Existing *associated* files save directly,
        // as they were already selected by an earlier Save.
        return ConfirmExistingTargetIfNeeded(
            request, isNewDestination: ConsoleFirstEdSavePromptPolicy.RequiresPath(associatedPath));
    }

    private EditorCommandRequest? ResolveNumber(EditorCommandBinding binding, int visibleRows)
    {
        // Capture the initiating view's column before the synchronous prompt
        // begins. Unlike the generic numeric prompts, the historical
        // EditCptabdef accepts deliberately empty Enter as "tab interval =
        // displayed column minus one". Cursor columns are already zero-based
        // in our model. This is terminal wrapper behavior, not a core rule.
        var cursorColumn = binding.CommandId == EditorCommandId.SetTabWidth
            ? session.CurrentWindow.Cursor.Column
            : 0;
        var raw = renderer.ReadPrompt(NumberPrompt(binding.CommandId));
        return binding.CommandId == EditorCommandId.SetTabWidth
            ? ConsoleFirstEdTabWidthPromptPolicy.Resolve(
                binding, visibleRows, cursorColumn, raw)
            : EditorCommandPromptResolver.Resolve(
                binding, visibleRows, firstResponse: raw);
    }

    private EditorCommandRequest? ResolveTwoNumbers(EditorCommandBinding binding, int visibleRows)
    {
        var prompts = binding.CommandId switch
        {
            EditorCommandId.CreateWindow => ("New window screen rows: ", "Window number to compress: "),
            EditorCommandId.LinkWindow => ("Destination window number: ", "Source window number: "),
            _ => ("First number: ", "Second number: ")
        };

        var first = renderer.ReadPrompt(prompts.Item1);
        // No second prompt after invalid/cancelled first input.
        if (!int.TryParse(first, System.Globalization.NumberStyles.Integer,
            System.Globalization.CultureInfo.InvariantCulture, out _))
            return null;

        var second = renderer.ReadPrompt(prompts.Item2);
        return EditorCommandPromptResolver.Resolve(
            binding, visibleRows, firstResponse: first, secondResponse: second);
    }

    private EditorCommandRequest? ResolveText(EditorCommandBinding binding, int visibleRows)
    {
        var text = renderer.ReadPrompt(binding.CommandId == EditorCommandId.FindNext
            ? "Find (empty = find again): "
            : "Text: ");

        // A cancelled (null) Find is not an explicitly empty Find Again.
        return EditorCommandPromptResolver.Resolve(
            binding, visibleRows, firstResponse: text);
    }

    private EditorCommandRequest? ResolveFindReplace(EditorCommandBinding binding, int visibleRows)
    {
        var find = renderer.ReadPrompt("Find: ");
        if (string.IsNullOrEmpty(find))
        {
            return null;
        }

        // Null means cancel; the explicitly empty string means deletion.
        // Coalescing null to "" here previously made an abandoned prompt
        // unintentionally destructive.
        var replacement = renderer.ReadPrompt("Replace with: ");
        return EditorCommandPromptResolver.Resolve(
            binding, visibleRows, firstResponse: find, secondResponse: replacement);
    }

    private EditorCommandRequest? ResolveCharacter(EditorCommandBinding binding, int visibleRows)
    {
        // Ctrl-P is not ordinary character insertion: the next printable
        // ASCII key *names* a control code. Describe the accepted alphabet
        // in the prompt so users are not forced to guess whether to press
        // Control a second time. Physical Control+A also works when the
        // terminal reports its already-translated 0x01 code.
        var value = renderer.ReadCharacterPrompt(
            "Control code (A-Z, @ [ \\ ] ^ _): ");
        return EditorCommandPromptResolver.Resolve(
            binding, visibleRows, characterResponse: value);
    }

    private EditorCommandRequest? ResolveFilePath(EditorCommandBinding binding, int visibleRows)
    {
        var path = renderer.ReadPrompt("File path: ");
        var request = EditorCommandPromptResolver.Resolve(
            binding, visibleRows, firstResponse: path);

        // Ctrl-K W is an *export* and may name an existing destination even
        // when the editor document itself is unrelated to that file. Ask
        // before an overwrite, but preserve direct Write File semantics:
        // after a confirmed write the document remains Dirty and unnamed.
        // Read File never writes its input and never needs confirmation.
        return ConfirmExistingTargetIfNeeded(
            request, isNewDestination: binding.CommandId == EditorCommandId.WriteFile);
    }

    private EditorCommandRequest? ConfirmExistingTargetIfNeeded(
        EditorCommandRequest? request, bool isNewDestination)
    {
        if (request is null)
        {
            // Cancelled filename prompt: no File.Exists call, no I/O and no
            // accidental empty-path dispatch.
            return null;
        }

        // The probe is intentionally in the host, never inside reusable Core.
        // File.Exists is a usability guard, *not* a race-free file lock:
        // another process can create/replace a target after this check.
        var exists = isNewDestination && request.Text is not null
            && File.Exists(request.Text);

        if (!ConsoleFirstEdOverwritePolicy.RequiresConfirmation(request, exists))
        {
            return request;
        }

        // An explicit YES is required. Escape/Ctrl-U/null and plain Enter all
        // cancel before command dispatch, leaving disk and Undo untouched.
        var response = renderer.ReadPrompt("File exists. Type YES to overwrite: ");
        return ConsoleFirstEdOverwritePolicy.Confirm(request, response);
    }

    private EditorCommandRequest? ResolveConfirmation(EditorCommandBinding binding, int visibleRows)
    {
        var answer = renderer.ReadPrompt(binding.CommandId == EditorCommandId.Exit
            ? "Exit editor? Type YES to confirm: "
            : "Confirm? Type YES: ");

        return EditorCommandPromptResolver.Resolve(
            binding, visibleRows, firstResponse: answer);
    }

    private static string NumberPrompt(EditorCommandId commandId) => commandId switch
    {
        EditorCommandId.GoToLine => "Line number: ",
        EditorCommandId.GoToColumn => "Column number: ",
        EditorCommandId.GoToWindow => "Window number: ",
        EditorCommandId.DeleteWindow => "Window number to delete: ",
        EditorCommandId.SetLeftMargin => "Left margin column: ",
        EditorCommandId.SetRightMargin => "Right margin column: ",
        EditorCommandId.SetTabWidth => "Tab width: ",
        EditorCommandId.SetUndoLimit => "Undo limit: ",
        EditorCommandId.SetMarker => "Marker number: ",
        EditorCommandId.JumpMarker => "Marker number: ",
        _ => "Number: "
    };
}
