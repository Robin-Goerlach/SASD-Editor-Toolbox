using System.Text.Json;
using Sasd.Editor.Commands;
using Sasd.Editor.Document;
using Sasd.Editor.Editing;
using Sasd.Editor.Hooks;
using Sasd.Editor.Model;
using Sasd.Editor.Rendering;
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
        var session = new EditorSession(hooks);
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

        return new TestContext(session, documents, windows, hooks);
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
            case "select_window":
                // Fixture view selection must not manufacture an editor key.
                context.Session.SetCurrentWindow(
                    context.Windows[RequireString(operation, "windowId")].WindowId);
                return true;
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
                    context.Windows[portableWindow.Name],
                    portableWindow.Value);
            }
        }

        if (expected.TryGetProperty("blockState", out var blockState))
        {
            AssertExpectedBlockState(context, blockState);
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

    private static void AssertExpectedWindow(EditorWindow window, JsonElement expected)
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
        RecordingHooks Hooks);

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
