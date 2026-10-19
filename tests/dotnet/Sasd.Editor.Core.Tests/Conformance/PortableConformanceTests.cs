using System.Text.Json;
using Sasd.Editor.Commands;
using Sasd.Editor.Document;
using Sasd.Editor.Editing;
using Sasd.Editor.Hooks;
using Sasd.Editor.IO;
using Sasd.Editor.Input;
using Sasd.Editor.Model;
using Sasd.Editor.Rendering;
using Sasd.Editor.Search;
using Sasd.Editor.Windows;
using Xunit;

namespace Sasd.Editor.Core.Tests.Conformance;

/// <summary>
/// Executes the shared implementation-neutral smoke cases against the .NET
/// reference implementation.
/// </summary>
/// <remarks>
/// The JSON vocabulary belongs to the repository conformance harness, not to the
/// public C# API. This adapter deliberately translates conceptual operation and
/// state names into the native semantic command/session model. C++ and Java can
/// therefore implement equivalent adapters without copying C# type names.
/// </remarks>
public sealed class PortableConformanceTests
{
    /// <summary>
    /// Discovers shared conformance files from the normative manifest.
    /// Reading only core-smoke.json would silently skip new cross-language
    /// behavior cases even when the repository contract validator accepts them.
    /// </summary>
    public static IEnumerable<object[]> PortableCases()
    {
        var root = Path.Combine(AppContext.BaseDirectory, "conformance");
        using var manifest = JsonDocument.Parse(
            File.ReadAllText(Path.Combine(root, "manifest.json")));

        const string sourcePrefix = "conformance/v1/";
        foreach (var entry in manifest.RootElement.GetProperty("caseFiles").EnumerateArray())
        {
            var sourcePath = entry.GetString()
                ?? throw new InvalidDataException("Conformance manifest contains a null path.");

            // The output layout deliberately omits v1/, because the test
            // project's Link metadata copies cases under conformance/cases/.
            // Reject traversal components rather than allowing arbitrary
            // manifest paths to escape the packaged conformance directory.
            if (!sourcePath.StartsWith(sourcePrefix, StringComparison.Ordinal))
            {
                throw new InvalidDataException($"Unexpected conformance path: {sourcePath}");
            }

            var relative = sourcePath[sourcePrefix.Length..];
            var parts = relative.Split('/');
            if (parts.Any(static part => part is "" or "." or ".." || part.Contains('\\')))
            {
                throw new InvalidDataException($"Unsafe conformance path: {sourcePath}");
            }

            var caseFile = Path.Combine(root, Path.Combine(parts));
            using var document = JsonDocument.Parse(File.ReadAllText(caseFile));
            foreach (var testCase in document.RootElement.GetProperty("cases").EnumerateArray())
            {
                yield return
                [
                    testCase.GetProperty("id").GetString()!,
                    testCase.GetRawText()
                ];
            }
        }
    }

    [Theory]
    [MemberData(nameof(PortableCases))]
    public async Task SharedPortableCase_MatchesDotNetImplementation(
        string caseId,
        string caseJson)
    {
        Assert.False(string.IsNullOrWhiteSpace(caseId));

        using var parsed = JsonDocument.Parse(caseJson);
        var testCase = parsed.RootElement;

        var context = BuildInitialState(testCase.GetProperty("initial"));
        var executed = await ExecuteCaseAsync(context, testCase);

        AssertExpectedState(context, testCase.GetProperty("expect"), executed);
    }

    private static TestContext BuildInitialState(JsonElement initial)
    {
        var hooks = new RecordingHooks();

        // Portable file fixtures use an in-memory codec: decoded input
        // supports ReadFile and a recording sink supports WriteFile/SaveFile.
        // No fixture creates a real file or depends on OS path separators.
        var hasReadInput = initial.TryGetProperty("fileReadLines", out var decoded);
        var hasWriteSink = initial.TryGetProperty("fileWriteSink", out var sink)
            && sink.GetBoolean();
        var fixtureCodec = hasReadInput || hasWriteSink
            ? new FixtureFileCodec(
                hasReadInput
                    ? decoded.EnumerateArray().Select(ParseLine).ToArray()
                    : Array.Empty<EditorLineSnapshot>())
            : null;
        var session = new EditorSession(hooks, fixtureCodec);
        var documents = new Dictionary<string, EditorDocument>(StringComparer.Ordinal);
        var windows = new Dictionary<string, EditorWindow>(StringComparer.Ordinal);

        foreach (var portableDocument in initial.GetProperty("documents").EnumerateArray())
        {
            var id = RequireString(portableDocument, "id");
            var lines = portableDocument.GetProperty("lines")
                .EnumerateArray()
                .Select(ParseLine)
                .ToArray();

            documents.Add(id, new EditorDocument(new LinkedLineTextBuffer(lines)));
        }

        foreach (var portableWindow in initial.GetProperty("windows").EnumerateArray())
        {
            var id = RequireString(portableWindow, "id");
            var documentId = RequireString(portableWindow, "documentId");
            var document = documents[documentId];
            var options = ParseOptions(portableWindow);

            var window = session.CreateWindow(document, options);
            var cursor = portableWindow.GetProperty("cursor");
            window.Cursor = new TextPosition(
                cursor.GetProperty("line").GetInt32(),
                cursor.GetProperty("column").GetInt32());

            if (portableWindow.TryGetProperty("topLine", out var topLine))
            {
                window.TopLine = topLine.GetInt32();
            }

            if (portableWindow.TryGetProperty("leftColumn", out var leftColumn))
            {
                window.LeftColumn = leftColumn.GetInt32();
            }

            windows.Add(id, window);
        }

        var currentWindowId = RequireString(initial, "currentWindowId");
        session.SetCurrentWindow(windows[currentWindowId].WindowId);

        // Physical rows are a host-provided layout constraint rather than an
        // intrinsic property of editor text. Only cases exercising actual
        // FIRST-ED window splits/deletions need to opt into a frame layout.
        if (initial.TryGetProperty("layoutRows", out var layoutRows))
        {
            Assert.True(session.WindowLayout.Configure(layoutRows.GetInt32()));
        }

        return new TestContext(session, documents, windows, hooks, fixtureCodec);
    }

    private static EditorWindowOptions ParseOptions(JsonElement portableWindow)
    {
        var options = new EditorWindowOptions();
        if (!portableWindow.TryGetProperty("options", out var source))
        {
            return options;
        }

        if (source.TryGetProperty("insertMode", out var insertMode))
        {
            options.InsertMode = insertMode.GetBoolean();
        }

        if (source.TryGetProperty("wordWrap", out var wordWrap))
        {
            options.WordWrap = wordWrap.GetBoolean();
        }

        if (source.TryGetProperty("autoIndent", out var autoIndent))
        {
            options.AutoIndent = autoIndent.GetBoolean();
        }

        if (source.TryGetProperty("rightMargin", out var rightMargin))
        {
            options.RightMargin = rightMargin.GetInt32();
        }

        if (source.TryGetProperty("leftMargin", out var leftMargin))
        {
            options.LeftMargin = leftMargin.GetInt32();
        }

        if (source.TryGetProperty("tabSize", out var tabSize))
        {
            options.TabSize = tabSize.GetInt32();
        }

        return options;
    }

    private static EditorLineSnapshot ParseLine(JsonElement line) =>
        new(
            RequireString(line, "text"),
            line.TryGetProperty("flags", out var flags)
                ? ParseFlags(flags)
                : EditorLineFlags.None);

    private static EditorLineFlags ParseFlags(JsonElement flags)
    {
        var result = EditorLineFlags.None;
        foreach (var item in flags.EnumerateArray())
        {
            result |= item.GetString() switch
            {
                "in_block" => EditorLineFlags.InBlock,
                "wrapped" => EditorLineFlags.Wrapped,
                "user_colored" => EditorLineFlags.UserColored,
                var unknown => throw new InvalidDataException($"Unknown portable line flag '{unknown}'.")
            };
        }

        return result;
    }

    /// <summary>
    /// Command sequences run against one session so linked-window realignment,
    /// independent block endpoints and undo can be tested portably. A rejected
    /// intermediate command must be explicitly marked as expected; otherwise
    /// it fails immediately instead of being hidden by a later success.
    /// </summary>
    private static async ValueTask<bool> ExecuteCaseAsync(TestContext context, JsonElement testCase)
    {
        if (!testCase.TryGetProperty("operations", out var operations))
        {
            return await ExecuteOperationAsync(context, testCase.GetProperty("operation"));
        }

        var succeeded = true;
        foreach (var operation in operations.EnumerateArray())
        {
            succeeded = await ExecuteOperationAsync(context, operation);
            if (operation.TryGetProperty("commandSucceeded", out var expected))
            {
                Assert.Equal(expected.GetBoolean(), succeeded);
            }
            else
            {
                Assert.True(succeeded, $"Portable command failed: {RequireString(operation, "id")}");
            }
        }

        return succeeded;
    }

    /// <summary>
    /// Some portable operations are session/view services rather than keyboard
    /// commands. Keep them out of EditorCommandId to preserve the separation
    /// between host-neutral behavior and the historical FIRST-ED key map.
    /// </summary>
    private static async ValueTask<bool> ExecuteOperationAsync(
        TestContext context,
        JsonElement operation)
    {
        switch (RequireString(operation, "id"))
        {
            case "translate_firsted_key":
            {
                // Drive the actual FIRST-ED key map, but describe key events
                // using portable character/specialKey/modifier vocabulary.
                // No terminal, browser, WinForms or DOS scan-code types leak
                // into the cross-language test fixtures.
                var key = ParsePortableKeyStroke(operation);
                var action = context.KeyMap.Translate(key);
                context.LastKeyAction = action;
                Assert.Equal(
                    RequireString(operation, "expectAction"),
                    PortableEnumName(action.Kind.ToString()));

                if (operation.TryGetProperty("expectCommand", out var command))
                {
                    Assert.NotNull(action.Binding);
                    Assert.Equal(command.GetString(),
                        PortableEnumName(action.Binding!.CommandId.ToString()));
                }

                if (operation.TryGetProperty("expectArgument", out var argument))
                {
                    Assert.NotNull(action.Binding);
                    Assert.Equal(argument.GetString(),
                        PortableEnumName(action.Binding!.ArgumentKind.ToString()));
                }

                if (operation.TryGetProperty("expectFixedNumber", out var fixedNumber))
                {
                    Assert.NotNull(action.Binding);
                    Assert.Equal(fixedNumber.GetInt32(), action.Binding!.FixedNumber);
                }

                if (operation.TryGetProperty("expectText", out var insertedText))
                {
                    Assert.Equal(insertedText.GetString(), action.Text);
                }

                if (operation.TryGetProperty("expectPrefixDisplay", out var prefixDisplay))
                {
                    Assert.Equal(prefixDisplay.GetString(), action.PrefixDisplay);
                }

                if (operation.TryGetProperty("expectPendingPrefix", out var pending))
                {
                    Assert.Equal(pending.GetString(),
                        PortableEnumName(context.KeyMap.PendingPrefix.ToString()));
                }

                return true;
            }
            case "resolve_host_prompt":
            {
                // The key map merely declares required parameters. This
                // adapter supplies the recorded host responses, including
                // null cancellation, to the shared pure prompt resolver.
                var binding = context.LastKeyAction?.Binding
                    ?? throw new InvalidDataException("No pending host command binding.");
                string? first = operation.TryGetProperty("firstResponse", out var a)
                    ? a.GetString() : null;
                string? second = operation.TryGetProperty("secondResponse", out var b)
                    ? b.GetString() : null;
                char? character = null;
                if (operation.TryGetProperty("characterResponse", out var ch)
                    && ch.ValueKind != JsonValueKind.Null)
                {
                    var value = ch.GetString()!;
                    Assert.Single(value);
                    character = value[0];
                }

                var request = EditorCommandPromptResolver.Resolve(
                    binding,
                    operation.TryGetProperty("pageSize", out var page) ? page.GetInt32() : null,
                    first, second, character);
                context.LastResolvedPrompt = request;
                Assert.Equal(operation.GetProperty("expectCancelled").GetBoolean(),
                    request is null);

                if (operation.TryGetProperty("expectRequest", out var expected))
                {
                    Assert.NotNull(request);
                    Assert.Equal(RequireString(expected, "id"),
                        PortableEnumName(request.Id.ToString()));
                    if (expected.TryGetProperty("text", out var expectedText))
                        Assert.Equal(expectedText.GetString(), request.Text);
                    if (expected.TryGetProperty("text2", out var expectedText2))
                        Assert.Equal(expectedText2.GetString(), request.Text2);
                    if (expected.TryGetProperty("number", out var expectedNumber))
                        Assert.Equal(expectedNumber.GetInt32(), request.Number);
                    if (expected.TryGetProperty("number2", out var expectedNumber2))
                        Assert.Equal(expectedNumber2.GetInt32(), request.Number2);
                    if (expected.TryGetProperty("pageSize", out var expectedPage))
                        Assert.Equal(expectedPage.GetInt32(), request.PageSize);
                }

                return true;
            }
            case "execute_resolved_prompt":
                // A cancelled host prompt must not even enter the dispatcher.
                return context.LastResolvedPrompt is not null
                    && await new EditorCommandDispatcher(context.Session)
                        .ExecuteAsync(context.LastResolvedPrompt);
            case "execute_firsted_key":
            {
                // The key map itself does not change text or collect prompts:
                // the host would supply these values, then dispatch the
                // binding. These fixtures verify the complete public path
                // without implementing any real user-facing dialog.
                var action = context.LastKeyAction
                    ?? throw new InvalidDataException("No key action has been translated.");
                EditorCommandRequest request;
                if (action.Kind == EditorInputActionKind.InsertText)
                {
                    request = new EditorCommandRequest(
                        EditorCommandId.InsertText, Text: action.Text);
                }
                else if (action.Kind == EditorInputActionKind.Command)
                {
                    var binding = action.Binding
                        ?? throw new InvalidDataException("Command action lacks a binding.");
                    request = binding.CreateRequest(
                        text: operation.TryGetProperty("text", out var text) ? text.GetString() : null,
                        text2: operation.TryGetProperty("text2", out var text2) ? text2.GetString() : null,
                        number: operation.TryGetProperty("number", out var number) ? number.GetInt32() : null,
                        number2: operation.TryGetProperty("number2", out var number2) ? number2.GetInt32() : null,
                        pageSize: operation.TryGetProperty("pageSize", out var pageSize) ? pageSize.GetInt32() : null);
                }
                else
                {
                    throw new InvalidDataException(
                        "Only translated command or insert-text actions can execute.");
                }

                return await new EditorCommandDispatcher(context.Session)
                    .ExecuteAsync(request);
            }
            case "reset_firsted_keymap":
                context.KeyMap.Reset();
                context.LastKeyAction = null;
                return true;
            case "host_typeahead":
            case "push_typeahead":
            {
                // The origin of a normalized keystroke matters: physical
                // Ctrl-U aborts immediately; macro Ctrl-U is queued normally.
                // Keep this below the language-neutral buffer adapter rather
                // than synthesizing terminal/DOS scan codes.
                var symbol = RequireString(operation, "character");
                Assert.Single(symbol);
                var key = operation.TryGetProperty("control", out var control)
                    && control.GetBoolean()
                    ? EditorKeyStroke.Control(symbol[0])
                    : EditorKeyStroke.ForCharacter(symbol[0]);
                var result = RequireString(operation, "id") == "host_typeahead"
                    ? context.Session.Typeahead.EnqueueFromHost(key)
                    : context.Session.Typeahead.PushNext(key);
                if (operation.TryGetProperty("expectResult", out var expectedResult))
                {
                    Assert.Equal(expectedResult.GetString(), result.ToString().ToLowerInvariant());
                }

                return true;
            }
            case "push_typeahead_text":
            {
                var result = context.Session.Typeahead.PushText(
                    RequireString(operation, "text"));
                if (operation.TryGetProperty("expectResult", out var expectedResult))
                {
                    Assert.Equal(expectedResult.GetString(), result.ToString().ToLowerInvariant());
                }

                return true;
            }
            case "read_typeahead":
            {
                var available = context.Session.Typeahead.TryRead(out var key);
                if (operation.TryGetProperty("expectAvailable", out var expectedAvailable))
                {
                    Assert.Equal(expectedAvailable.GetBoolean(), available);
                }
                else
                {
                    Assert.True(available);
                }

                if (available)
                {
                    // Enum names are converted to lowercase conceptual
                    // categories, independent of any host physical key codes.
                    if (operation.TryGetProperty("expectKind", out var expectedKind))
                        Assert.Equal(expectedKind.GetString(), key.Key.ToString().ToLowerInvariant());
                    if (operation.TryGetProperty("expectCharacter", out var expectedCharacter))
                        Assert.Equal(expectedCharacter.GetString(), key.Character.ToString());
                    if (operation.TryGetProperty("expectControl", out var expectedControl))
                    {
                        var symbol = expectedControl.GetString()!;
                        Assert.Single(symbol);
                        Assert.True(key.IsControlLetter(symbol[0]));
                    }
                }

                return true;
            }
            case "consume_typeahead_abort":
            {
                var consumed = context.Session.Typeahead.ConsumeAbortRequest();
                Assert.Equal(
                    operation.GetProperty("expectAbort").GetBoolean(), consumed);
                return true;
            }
            case "select_window":
                // Fixture view selection must not manufacture an editor key.
                context.Session.SetCurrentWindow(
                    context.Windows[RequireString(operation, "windowId")].WindowId);
                return true;
            case "find_next_with_options":
            {
                // The FIRST-ED semantic command uses its historical default
                // options. This portable adapter instead addresses the public
                // host-neutral search service so modern optional wrap/word
                // behavior can be tested without changing the key map.
                var flags = operation.GetProperty("searchOptions");
                var options = new SearchOptions(
                    CaseSensitive: flags.TryGetProperty("caseSensitive", out var sensitive)
                        && sensitive.GetBoolean(),
                    WholeWord: flags.TryGetProperty("wholeWord", out var whole)
                        && whole.GetBoolean(),
                    WrapAround: flags.TryGetProperty("wrapAround", out var wrap)
                        && wrap.GetBoolean());
                return await context.Session.Search.FindNextAsync(
                    RequireString(operation, "text"), options) is not null;
            }
            case "position_cursor":
            {
                var window = context.Session.CurrentWindow;
                var line = operation.GetProperty("line").GetInt32();
                var column = operation.GetProperty("column").GetInt32();
                if (line < 0 || line >= window.Document.Buffer.LineCount || column < 0)
                {
                    throw new InvalidDataException("Portable cursor line/column is out of range.");
                }

                // Historical virtual columns beyond EOL are valid view state.
                window.Cursor = new TextPosition(line, column);
                return true;
            }
            case "color_current_line":
                context.Session.ColorCurrentLine();
                return true;
            case "color_current_document":
                context.Session.ColorCurrentDocument();
                return true;
            case "ensure_horizontal_visibility":
                context.Session.EnsureAllWindowCursorsHorizontallyVisible(
                    operation.GetProperty("visibleColumns").GetInt32());
                return true;
            case "project_viewports":
                // Building a viewport is observational: it must not change
                // the cursor, text, undo history or currently selected view.
                return true;
            default:
                return await new EditorCommandDispatcher(context.Session)
                    .ExecuteAsync(BuildRequest(operation));
        }
    }

    /// <summary>
    /// Convert a portable key to the .NET normalized representation. The
    /// fixture format deliberately avoids CLR enum identifiers and OS codes.
    /// </summary>
    private static EditorKeyStroke ParsePortableKeyStroke(JsonElement operation)
    {
        var modifiers = EditorKeyModifiers.None;
        if (operation.TryGetProperty("control", out var control) && control.GetBoolean())
            modifiers |= EditorKeyModifiers.Control;
        if (operation.TryGetProperty("alt", out var alt) && alt.GetBoolean())
            modifiers |= EditorKeyModifiers.Alt;
        if (operation.TryGetProperty("shift", out var shift) && shift.GetBoolean())
            modifiers |= EditorKeyModifiers.Shift;

        if (operation.TryGetProperty("specialKey", out var specialKey))
        {
            var key = specialKey.GetString() switch
            {
                "enter" => EditorKey.Enter,
                "escape" => EditorKey.Escape,
                "backspace" => EditorKey.Backspace,
                "delete" => EditorKey.Delete,
                "tab" => EditorKey.Tab,
                "arrow_left" => EditorKey.ArrowLeft,
                "arrow_right" => EditorKey.ArrowRight,
                "arrow_up" => EditorKey.ArrowUp,
                "arrow_down" => EditorKey.ArrowDown,
                "page_up" => EditorKey.PageUp,
                "page_down" => EditorKey.PageDown,
                "home" => EditorKey.Home,
                "end" => EditorKey.End,
                "f1" => EditorKey.F1,
                _ => throw new InvalidDataException("Unsupported portable special key.")
            };
            return new EditorKeyStroke(key, modifiers: modifiers);
        }

        var character = RequireString(operation, "character");
        Assert.Single(character);
        return EditorKeyStroke.ForCharacter(character[0], modifiers);
    }

    /// <summary>
    /// Deterministic snake_case projection for cross-language action and
    /// binding labels, rather than serializing any .NET enum types.
    /// </summary>
    private static string PortableEnumName(string value)
    {
        var builder = new System.Text.StringBuilder();
        for (var index = 0; index < value.Length; index++)
        {
            var ch = value[index];
            if (char.IsUpper(ch) && index > 0
                && (char.IsLower(value[index - 1]) || char.IsDigit(value[index - 1])))
                builder.Append('_');
            builder.Append(char.ToLowerInvariant(ch));
        }
        return builder.ToString();
    }

    private static EditorCommandRequest BuildRequest(JsonElement operation)
    {
        var operationId = RequireString(operation, "id");
        return new EditorCommandRequest(
            MapOperation(operationId),
            Text: operation.TryGetProperty("text", out var text) ? text.GetString() : null,
            Number: operation.TryGetProperty("number", out var number) ? number.GetInt32() : null,
            PageSize: operation.TryGetProperty("pageSize", out var pageSize) ? pageSize.GetInt32() : null,
            Number2: operation.TryGetProperty("number2", out var number2) ? number2.GetInt32() : null,
            Text2: operation.TryGetProperty("text2", out var text2) ? text2.GetString() : null);
    }

    private static EditorCommandId MapOperation(string operation) =>
        operation switch
        {
            "move_right_character" => EditorCommandId.CursorRight,
            // Use the public semantic dispatcher, not implementation-private
            // compatibility processors or DOS keyboard scan codes.
            "beginning_of_line" => EditorCommandId.BeginningOfLine,
            "end_of_line" => EditorCommandId.EndOfLine,
            "beginning_or_end_of_line" => EditorCommandId.BeginningOrEndOfLine,
            "goto_column" => EditorCommandId.GoToColumn,
            "move_left_word" => EditorCommandId.WordLeft,
            "move_right_word" => EditorCommandId.WordRight,
            "toggle_insert" => EditorCommandId.ToggleInsert,
            "toggle_word_wrap" => EditorCommandId.ToggleWordWrap,
            "toggle_auto_indent" => EditorCommandId.ToggleAutoIndent,
            "set_left_margin" => EditorCommandId.SetLeftMargin,
            "set_right_margin" => EditorCommandId.SetRightMargin,
            "change_case" => EditorCommandId.ChangeCase,
            "delete_to_end_of_line" => EditorCommandId.DeleteToEndOfLine,
            // These remain distinct historical semantics even if a modern
            // host binds them to different keys or menu actions.
            "tab" => EditorCommandId.Tab,
            "delete_left_character" => EditorCommandId.DeleteLeftCharacter,
            "delete_right_character" => EditorCommandId.DeleteRightCharacter,
            "delete_right_word" => EditorCommandId.DeleteRightWord,
            // Cross-language reflow is a semantic command, not a public
            // copy of the Pascal compression/splicing helper functions.
            "reformat_paragraph" => EditorCommandId.ReformatParagraph,
            // The semantic window/block operations remain UI-neutral.
            "hide_block" => EditorCommandId.HideBlock,
            "top_of_block" => EditorCommandId.TopOfBlock,
            "bottom_of_block" => EditorCommandId.BottomOfBlock,
            "create_window" => EditorCommandId.CreateWindow,
            "delete_window" => EditorCommandId.DeleteWindow,
            "delete_window_text" => EditorCommandId.DeleteWindowText,
            "next_window" => EditorCommandId.NextWindow,
            "previous_window" => EditorCommandId.PreviousWindow,
            "goto_window" => EditorCommandId.GoToWindow,
            "link_window" => EditorCommandId.LinkWindow,
            // Search operations use the public semantic-command boundary.
            // In particular, text:"" for find_next has historical meaning;
            // it must not be converted to an omitted/null request argument.
            "read_file" => EditorCommandId.ReadFile,
            "write_file" => EditorCommandId.WriteFile,
            "save_file" => EditorCommandId.SaveFile,
            "find_next" => EditorCommandId.FindNext,
            "find_again" => EditorCommandId.FindAgain,
            "replace_next" => EditorCommandId.ReplaceNext,
            // Shared historical viewport commands: PageSize is the positive
            // number of host-visible text rows, never a DOS screen constant.
            "move_up_line" => EditorCommandId.CursorUp,
            "move_down_line" => EditorCommandId.CursorDown,
            "scroll_up" => EditorCommandId.ScrollUp,
            "scroll_down" => EditorCommandId.ScrollDown,
            "page_up" => EditorCommandId.PageUp,
            "page_down" => EditorCommandId.PageDown,
            "top_of_file" => EditorCommandId.TopOfFile,
            "bottom_of_file" => EditorCommandId.BottomOfFile,
            "insert_line" => EditorCommandId.InsertLine,
            "end_block" => EditorCommandId.EndBlock,
            "copy_block" => EditorCommandId.CopyBlock,
            "move_block" => EditorCommandId.MoveBlock,
            "delete_block" => EditorCommandId.DeleteBlock,
            "set_marker" => EditorCommandId.SetMarker,
            "jump_marker" => EditorCommandId.JumpMarker,
            "undo" => EditorCommandId.Undo,
            "delete_line" => EditorCommandId.DeleteLine,
            "begin_block" => EditorCommandId.BeginBlock,
            "new_line" => EditorCommandId.NewLine,
            "goto_line" => EditorCommandId.GoToLine,
            "set_undo_limit" => EditorCommandId.SetUndoLimit,
            _ => throw new InvalidDataException(
                $"The .NET conformance adapter does not support operation '{operation}'.")
        };

    private static void AssertExpectedState(
        TestContext context,
        JsonElement expected,
        bool commandSucceeded)
    {
        if (expected.TryGetProperty("commandSucceeded", out var expectedCommandSucceeded))
        {
            Assert.Equal(expectedCommandSucceeded.GetBoolean(), commandSucceeded);
        }
        else
        {
            Assert.True(commandSucceeded);
        }

        if (expected.TryGetProperty("documents", out var documents))
        {
            foreach (var portableDocument in documents.EnumerateObject())
            {
                AssertExpectedDocument(
                    context.Documents[portableDocument.Name],
                    portableDocument.Value);
            }
        }

        if (expected.TryGetProperty("windows", out var windows))
        {
            foreach (var portableWindow in windows.EnumerateObject())
            {
                AssertExpectedWindow(
                    context,
                    context.Windows[portableWindow.Name],
                    portableWindow.Value);
            }
        }

        if (expected.TryGetProperty("blockState", out var blockState))
        {
            AssertExpectedBlockState(context, blockState);
        }

        if (expected.TryGetProperty("windowCount", out var windowCount))
        {
            Assert.Equal(windowCount.GetInt32(), context.Session.Windows.Count);
        }

        if (expected.TryGetProperty("currentWindowIndex", out var windowIndex))
        {
            // Index is zero-based in portable fixtures, unlike FIRST-ED's
            // user-facing one-based displayed-window command arguments.
            Assert.Equal(windowIndex.GetInt32(), context.Session.Windows
                .ToList().FindIndex(x => x.WindowId == context.Session.CurrentWindow.WindowId));
        }

        if (expected.TryGetProperty("layoutFrames", out var frames))
        {
            var actual = context.Session.WindowLayout.Frames;
            Assert.Equal(frames.GetArrayLength(), actual.Count);
            var index = 0;
            foreach (var frame in frames.EnumerateArray())
            {
                if (frame.TryGetProperty("topRow", out var top))
                    Assert.Equal(top.GetInt32(), actual[index].TopRow);
                if (frame.TryGetProperty("height", out var height))
                    Assert.Equal(height.GetInt32(), actual[index].Height);
                index++;
            }
        }

        if (expected.TryGetProperty("rundownRequested", out var rundown))
        {
            Assert.Equal(rundown.GetBoolean(), context.Session.RundownRequested);
        }

        if (expected.TryGetProperty("firstEdKeymap", out var keymap))
        {
            if (keymap.TryGetProperty("pendingPrefix", out var pending))
                Assert.Equal(pending.GetString(),
                    PortableEnumName(context.KeyMap.PendingPrefix.ToString()));
        }

        if (expected.TryGetProperty("typeahead", out var typeahead))
        {
            // A portable projection of the queue's size and immediate Ctrl-U
            // state. Sequence order is checked by read_typeahead operations.
            if (typeahead.TryGetProperty("count", out var count))
                Assert.Equal(count.GetInt32(), context.Session.Typeahead.Count);
            if (typeahead.TryGetProperty("abortRequested", out var abort))
                Assert.Equal(abort.GetBoolean(), context.Session.Typeahead.AbortRequested);
        }

        if (expected.TryGetProperty("undo", out var undo))
        {
            if (undo.TryGetProperty("limit", out var undoLimit))
            {
                Assert.Equal(undoLimit.GetInt32(), context.Session.Undo.Limit);
            }

            if (undo.TryGetProperty("count", out var undoCount))
            {
                Assert.Equal(undoCount.GetInt32(), context.Session.Undo.Count);
            }
        }

        if (expected.TryGetProperty("currentWindowId", out var current))
        {
            var selected = context.Windows[RequireString(expected, "currentWindowId")];
            Assert.Equal(selected.WindowId, context.Session.CurrentWindow.WindowId);
        }

        if (expected.TryGetProperty("viewports", out var viewports))
        {
            AssertExpectedViewports(context, viewports);
        }

        if (expected.TryGetProperty("fileWrites", out var expectedWrites))
        {
            // Portable output assertion compares captured logical snapshots,
            // not historical byte encoding or OS-specific full paths.
            var captured = Assert.IsType<FixtureFileCodec>(context.FileCodec).Writes;
            Assert.Equal(expectedWrites.GetArrayLength(), captured.Count);
            var index = 0;
            foreach (var written in expectedWrites.EnumerateArray())
            {
                Assert.Equal(
                    RequireString(written, "path"),
                    Path.GetFileName(captured[index].Path));
                var expectedLines = written.GetProperty("lines").EnumerateArray()
                    .Select(ParseLine).ToArray();
                Assert.Equal(expectedLines, captured[index].Lines);
                index++;
            }
        }

        if (expected.TryGetProperty("error", out var error))
        {
            AssertExpectedError(context, error);
        }
    }

    /// <summary>
    /// Portable projections check only the requested fields, so other
    /// language implementations can have independent renderer/status types.
    /// </summary>
    private static void AssertExpectedViewports(TestContext context, JsonElement viewports)
    {
        var builder = new EditorViewportBuilder(context.Session);
        foreach (var portableView in viewports.EnumerateObject())
        {
            var requested = portableView.Value;
            var window = context.Windows[portableView.Name];
            var viewport = builder.Build(
                window,
                requested.GetProperty("height").GetInt32(),
                requested.GetProperty("width").GetInt32());

            if (requested.TryGetProperty("statusLine", out var line))
            {
                Assert.Equal(line.GetInt32(), viewport.Status.Line);
            }

            if (requested.TryGetProperty("statusColumn", out var column))
            {
                Assert.Equal(column.GetInt32(), viewport.Status.Column);
            }

            if (requested.TryGetProperty("lines", out var lines))
            {
                var expectedLines = lines.EnumerateArray()
                    .Select(static item => item.GetString()!)
                    .ToArray();
                Assert.Equal(expectedLines, viewport.Lines.Select(static item => item.Text));
            }
        }
    }

    private static void AssertExpectedError(TestContext context, JsonElement expected)
    {
        var actual = Assert.Single(context.Hooks.Errors);

        if (expected.TryGetProperty("id", out var id))
        {
            Assert.Equal(RequireString(expected, "id"), PortableErrorId(actual.Id));
        }

        if (expected.TryGetProperty("code", out var code))
        {
            Assert.Equal(RequireString(expected, "code"), actual.Code);
        }

        if (expected.TryGetProperty("resourceKey", out var resourceKey))
        {
            Assert.Equal(RequireString(expected, "resourceKey"), actual.ResourceKey);
        }
    }

    private static string PortableErrorId(EditorErrorId id) =>
        id switch
        {
            EditorErrorId.InvalidCommandArgument => "invalid_command_argument",
            EditorErrorId.CommandRejected => "command_rejected",
            EditorErrorId.OperationFailed => "operation_failed",
            EditorErrorId.FileIoFailed => "file_io_failed",
            EditorErrorId.AccessDenied => "access_denied",
            EditorErrorId.UnsupportedOperation => "unsupported_operation",
            EditorErrorId.TypeaheadOverflow => "typeahead_overflow",
            EditorErrorId.OperationAborted => "operation_aborted",
            _ => throw new InvalidDataException($"Unsupported portable editor error id '{id}'.")
        };

    private static void AssertExpectedDocument(EditorDocument document, JsonElement expected)
    {
        if (expected.TryGetProperty("isDirty", out var dirty))
        {
            Assert.Equal(dirty.GetBoolean(), document.IsDirty);
        }

        if (expected.TryGetProperty("version", out var version))
        {
            Assert.Equal(version.GetInt64(), document.Version);
        }

        if (expected.TryGetProperty("associatedFileName", out var associatedFileName))
        {
            Assert.Equal(associatedFileName.GetString(),
                document.FilePath is null ? null : Path.GetFileName(document.FilePath));
        }

        if (!expected.TryGetProperty("lines", out var expectedLines))
        {
            return;
        }

        var actualLines = document.Buffer.Snapshot();
        Assert.Equal(expectedLines.GetArrayLength(), actualLines.Count);

        var index = 0;
        foreach (var expectedLine in expectedLines.EnumerateArray())
        {
            Assert.Equal(RequireString(expectedLine, "text"), actualLines[index].Text);
            if (expectedLine.TryGetProperty("flags", out var flags))
            {
                Assert.Equal(ParseFlags(flags), actualLines[index].Flags);
            }

            index++;
        }
    }

    private static void AssertExpectedWindow(TestContext context, EditorWindow window, JsonElement expected)
    {
        if (expected.TryGetProperty("cursor", out var cursor))
        {
            Assert.Equal(
                new TextPosition(
                    cursor.GetProperty("line").GetInt32(),
                    cursor.GetProperty("column").GetInt32()),
                window.Cursor);
        }

        if (expected.TryGetProperty("topLine", out var topLine))
        {
            Assert.Equal(topLine.GetInt32(), window.TopLine);
        }

        if (expected.TryGetProperty("leftColumn", out var leftColumn))
        {
            Assert.Equal(leftColumn.GetInt32(), window.LeftColumn);
        }

        if (expected.TryGetProperty("documentId", out var documentId))
        {
            Assert.Same(context.Documents[RequireString(expected, "documentId")], window.Document);
        }

        if (expected.TryGetProperty("documentLines", out var viewLines))
        {
            var actualLines = window.Document.Buffer.Snapshot();
            var expectedLines = viewLines.EnumerateArray().Select(ParseLine).ToArray();
            Assert.Equal(expectedLines, actualLines);
        }

        if (expected.TryGetProperty("sameDocumentAs", out var linked))
        {
            var other = context.Windows[linked.GetString()!];
            Assert.Same(other.Document, window.Document);
        }

        if (expected.TryGetProperty("differentDocumentFrom", out var separate))
        {
            var other = context.Windows[separate.GetString()!];
            Assert.NotSame(other.Document, window.Document);
        }

        if (expected.TryGetProperty("options", out var options))
        {
            // Linked views have independent modes despite shared document text.
            // A portable fixture asserts only fields explicitly present.
            var actual = window.Options;
            if (options.TryGetProperty("insertMode", out var insert))
                Assert.Equal(insert.GetBoolean(), actual.InsertMode);
            if (options.TryGetProperty("wordWrap", out var wrap))
                Assert.Equal(wrap.GetBoolean(), actual.WordWrap);
            if (options.TryGetProperty("autoIndent", out var indent))
                Assert.Equal(indent.GetBoolean(), actual.AutoIndent);
            if (options.TryGetProperty("leftMargin", out var left))
                Assert.Equal(left.GetInt32(), actual.LeftMargin);
            if (options.TryGetProperty("rightMargin", out var right))
                Assert.Equal(right.GetInt32(), actual.RightMargin);
            if (options.TryGetProperty("tabSize", out var tab))
                Assert.Equal(tab.GetInt32(), actual.TabSize);
        }
    }

    private static void AssertExpectedBlockState(TestContext context, JsonElement expected)
    {
        var state = context.Session.BlockState;

        AssertEndpoint(context, state.Start, expected.GetProperty("start"));
        AssertEndpoint(context, state.End, expected.GetProperty("end"));

        if (expected.TryGetProperty("hidden", out var hidden))
        {
            Assert.Equal(hidden.GetBoolean(), state.Hidden);
        }

        if (expected.TryGetProperty("complete", out var complete))
        {
            Assert.Equal(complete.GetBoolean(), context.Session.Block is not null);
        }
    }

    private static void AssertEndpoint(
        TestContext context,
        EditorBlockEndpoint? actual,
        JsonElement expected)
    {
        if (expected.ValueKind == JsonValueKind.Null)
        {
            Assert.Null(actual);
            return;
        }

        Assert.NotNull(actual);
        var documentId = RequireString(expected, "documentId");
        Assert.Equal(context.Documents[documentId].DocumentId, actual!.DocumentId);
        Assert.Equal(expected.GetProperty("line").GetInt32(), actual.Line);
    }

    private static string RequireString(JsonElement element, string propertyName) =>
        element.GetProperty(propertyName).GetString()
        ?? throw new InvalidDataException($"Portable field '{propertyName}' must be a string.");

    private sealed record TestContext(
        EditorSession Session,
        IReadOnlyDictionary<string, EditorDocument> Documents,
        IReadOnlyDictionary<string, EditorWindow> Windows,
        RecordingHooks Hooks,
        FixtureFileCodec? FileCodec)
    {
        // A stateful map belongs to a *single* portable case and survives
        // its input sequence. No state is shared between parallel xUnit runs.
        public FirstEdKeyMap KeyMap { get; } = new();
        public EditorInputAction? LastKeyAction { get; set; }
        public EditorCommandRequest? LastResolvedPrompt { get; set; }
    }

    private sealed record WrittenFile(string Path, IReadOnlyList<EditorLineSnapshot> Lines);

    /// <summary>
    /// The shared file fixture codec accepts decoded logical lines on Read,
    /// and records writes as exact logical line snapshots without filesystem
    /// I/O. Native C++/Java adapters can implement the same observable seam.
    /// Historical 0x8D byte-level compatibility remains independently tested.
    /// </summary>
    private sealed class FixtureFileCodec(IReadOnlyList<EditorLineSnapshot> lines) : IEditorFileCodec
    {
        public List<WrittenFile> Writes { get; } = [];

        public ValueTask<IReadOnlyList<EditorLineSnapshot>> ReadAsync(
            string path, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return ValueTask.FromResult(lines);
        }

        public ValueTask WriteAsync(
            string path,
            IReadOnlyList<EditorLineSnapshot> snapshots,
            CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            Writes.Add(new WrittenFile(path, snapshots.ToArray()));
            return ValueTask.CompletedTask;
        }
    }

    private sealed class RecordingHooks : IEditorHooks
    {
        public List<EditorError> Errors { get; } = [];

        public ValueTask OnErrorAsync(
            EditorSession session,
            EditorError error,
            CancellationToken cancellationToken)
        {
            Errors.Add(error);
            return ValueTask.CompletedTask;
        }
    }
}
