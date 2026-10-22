using Sasd.Editor.Commands;
using Sasd.Editor.FirstEd.Sample;
using Sasd.Editor.Input;
using Xunit;

namespace Sasd.Editor.Core.Tests;

/// <summary>
/// Verify that the optional MicroStar F10 layer delegates to the established
/// semantic pipeline instead of introducing its own editing implementation.
/// </summary>
public sealed class ConsoleMicroStarMenuTests
{
    [Theory]
    [InlineData("f", EditorCommandId.FindNext, EditorCommandArgumentKind.Text)]
    [InlineData("R", EditorCommandId.ReplaceNext, EditorCommandArgumentKind.FindReplace)]
    [InlineData("g", EditorCommandId.GoToLine, EditorCommandArgumentKind.Number)]
    [InlineData("s", EditorCommandId.SaveFile, EditorCommandArgumentKind.None)]
    [InlineData("w", EditorCommandId.WriteFile, EditorCommandArgumentKind.FilePath)]
    [InlineData("I", EditorCommandId.ReadFile, EditorCommandArgumentKind.FilePath)]
    [InlineData("u", EditorCommandId.Undo, EditorCommandArgumentKind.None)]
    [InlineData("b", EditorCommandId.BeginBlock, EditorCommandArgumentKind.None)]
    [InlineData("e", EditorCommandId.EndBlock, EditorCommandArgumentKind.None)]
    [InlineData("c", EditorCommandId.CopyBlock, EditorCommandArgumentKind.None)]
    [InlineData("m", EditorCommandId.MoveBlock, EditorCommandArgumentKind.None)]
    [InlineData("d", EditorCommandId.DeleteBlock, EditorCommandArgumentKind.None)]
    [InlineData("n", EditorCommandId.NextWindow, EditorCommandArgumentKind.None)]
    public void EverySupportedMenuEntryProducesExpectedSemanticBinding(
        string letter, EditorCommandId command, EditorCommandArgumentKind arguments)
    {
        var binding = ConsoleMicroStarMenuPolicy.Resolve(letter);
        Assert.NotNull(binding);
        Assert.Equal(command, binding.CommandId);
        Assert.Equal(arguments, binding.ArgumentKind);
        Assert.Null(EditorCommandValidator.Validate(
            binding.CreateRequest(text: command is EditorCommandId.FindNext ? "abc"
                : command is EditorCommandId.ReplaceNext ? "abc"
                : command is EditorCommandId.ReadFile or EditorCommandId.WriteFile ? "file.txt"
                : null,
                text2: command == EditorCommandId.ReplaceNext ? "x" : null,
                number: command == EditorCommandId.GoToLine ? 1 : null,
                pageSize: 4)));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData("q")]
    [InlineData("Q")]
    [InlineData("XX")]
    [InlineData("7")]
    public void UnknownOrCancelledMenuSelectionCannotRunEditorCommand(string? input)
    {
        Assert.Null(ConsoleMicroStarMenuPolicy.Resolve(input));
    }

    [Fact]
    public void HistoricalSampleMenuChoicesRemainReachableAndExplicit()
    {
        // The handbook supplement lists several MicroStar application
        // workflows beyond the reusable FIRST-ED semantic commands.
        // These entries deliberately return no Core binding because the
        // host must perform file selection, consent or print coordination.
        // Keeping this regression next to the ordinary command map helps
        // prevent a future menu refactor from silently losing access to
        // already implemented sample workflows.
        foreach (var choice in "TLPAHJK")
        {
            Assert.True(ConsoleMicroStarPopupLayout.IsMenuChoice(choice));
            Assert.Null(ConsoleMicroStarMenuPolicy.Resolve(choice.ToString()));
        }

        // Q is cancellation, not an executable sample or core command.
        Assert.True(ConsoleMicroStarPopupLayout.IsMenuChoice('Q'));
        Assert.Null(ConsoleMicroStarMenuPolicy.Resolve("Q"));
        Assert.Contains(ConsoleMicroStarPopupLayout.MenuLines,
            line => line.Contains("Save/Open", StringComparison.Ordinal));
        Assert.Contains(ConsoleMicroStarPopupLayout.MenuLines,
            line => line.Contains("Print status", StringComparison.Ordinal));
    }

    [Fact]
    public void MenuUsesReadableActionLabelsAndExposesF10()
    {
        Assert.Contains("F10", ConsoleMicroStarMenuPolicy.Prompt);
        Assert.Contains("Find", ConsoleMicroStarMenuPolicy.Prompt);
        Assert.Contains("Replace", ConsoleMicroStarMenuPolicy.Prompt);
        Assert.Contains("Cancel", ConsoleMicroStarMenuPolicy.Prompt);
    }
}
