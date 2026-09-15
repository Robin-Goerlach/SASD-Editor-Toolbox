using Sasd.Editor.Commands;
using Sasd.Editor.Input;

namespace Sasd.Editor.FirstEd.Sample;

/// <summary>
/// Resolves prompt metadata produced by the core key map. The key map declares
/// what it needs; this terminal host decides how to ask for it.
/// </summary>
internal sealed class ConsoleFirstEdPromptService(ConsoleFirstEdRenderer renderer)
{
    public EditorCommandRequest? Resolve(EditorCommandBinding binding, int visibleRows)
    {
        ArgumentNullException.ThrowIfNull(binding);

        return binding.ArgumentKind switch
        {
            EditorCommandArgumentKind.None => binding.CreateRequest(pageSize: visibleRows),
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

    private EditorCommandRequest? ResolveNumber(EditorCommandBinding binding, int visibleRows)
    {
        var raw = renderer.ReadPrompt(NumberPrompt(binding.CommandId));
        return int.TryParse(raw, out var value)
            ? binding.CreateRequest(number: value, pageSize: visibleRows)
            : null;
    }

    private EditorCommandRequest? ResolveTwoNumbers(EditorCommandBinding binding, int visibleRows)
    {
        var prompts = binding.CommandId switch
        {
            EditorCommandId.CreateWindow => ("New window screen rows: ", "Window number to compress: "),
            EditorCommandId.LinkWindow => ("Destination window number: ", "Source window number: "),
            _ => ("First number: ", "Second number: ")
        };

        if (!int.TryParse(renderer.ReadPrompt(prompts.Item1), out var first) ||
            !int.TryParse(renderer.ReadPrompt(prompts.Item2), out var second))
        {
            return null;
        }

        return binding.CreateRequest(number: first, number2: second, pageSize: visibleRows);
    }

    private EditorCommandRequest? ResolveText(EditorCommandBinding binding, int visibleRows)
    {
        var text = renderer.ReadPrompt(binding.CommandId == EditorCommandId.FindNext
            ? "Find (empty = find again): "
            : "Text: ");

        if (binding.CommandId == EditorCommandId.FindNext && string.IsNullOrEmpty(text))
        {
            return new EditorCommandRequest(EditorCommandId.FindAgain, PageSize: visibleRows);
        }

        return string.IsNullOrEmpty(text)
            ? null
            : binding.CreateRequest(text: text, pageSize: visibleRows);
    }

    private EditorCommandRequest? ResolveFindReplace(EditorCommandBinding binding, int visibleRows)
    {
        var find = renderer.ReadPrompt("Find: ");
        if (string.IsNullOrEmpty(find))
        {
            return null;
        }

        // Empty replacement is valid and means deletion of the matched text.
        var replacement = renderer.ReadPrompt("Replace with: ") ?? string.Empty;
        return binding.CreateRequest(text: find + '\0' + replacement, pageSize: visibleRows);
    }

    private EditorCommandRequest? ResolveCharacter(EditorCommandBinding binding, int visibleRows)
    {
        var value = renderer.ReadCharacterPrompt("Character to insert: ");
        return value.HasValue
            ? binding.CreateRequest(text: value.Value.ToString(), pageSize: visibleRows)
            : null;
    }

    private EditorCommandRequest? ResolveFilePath(EditorCommandBinding binding, int visibleRows)
    {
        var path = renderer.ReadPrompt("File path: ");
        return string.IsNullOrWhiteSpace(path)
            ? null
            : binding.CreateRequest(text: path.Trim(), pageSize: visibleRows);
    }

    private EditorCommandRequest? ResolveConfirmation(EditorCommandBinding binding, int visibleRows)
    {
        var answer = renderer.ReadPrompt(binding.CommandId == EditorCommandId.Exit
            ? "Exit editor? Type YES to confirm: "
            : "Confirm? Type YES: ");

        return string.Equals(answer?.Trim(), "YES", StringComparison.OrdinalIgnoreCase)
            ? binding.CreateRequest(pageSize: visibleRows)
            : null;
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
