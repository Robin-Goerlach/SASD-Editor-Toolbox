using System.Text.Json;
using Sasd.Editor.Commands;
using Sasd.Editor.Document;
using Sasd.Editor.Editing;
using Sasd.Editor.Model;
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
    public static IEnumerable<object[]> CoreSmokeCases()
    {
        var path = Path.Combine(
            AppContext.BaseDirectory,
            "conformance",
            "cases",
            "core-smoke.json");

        using var document = JsonDocument.Parse(File.ReadAllText(path));
        foreach (var testCase in document.RootElement.GetProperty("cases").EnumerateArray())
        {
            yield return
            [
                testCase.GetProperty("id").GetString()!,
                testCase.GetRawText()
            ];
        }
    }

    [Theory]
    [MemberData(nameof(CoreSmokeCases))]
    public async Task SharedCoreSmokeCase_MatchesDotNetImplementation(
        string caseId,
        string caseJson)
    {
        Assert.False(string.IsNullOrWhiteSpace(caseId));

        using var parsed = JsonDocument.Parse(caseJson);
        var testCase = parsed.RootElement;

        var context = BuildInitialState(testCase.GetProperty("initial"));
        var operation = testCase.GetProperty("operation");
        var request = BuildRequest(operation);
        var executed = await new EditorCommandDispatcher(context.Session).ExecuteAsync(request);

        AssertExpectedState(context, testCase.GetProperty("expect"), executed);
    }

    private static TestContext BuildInitialState(JsonElement initial)
    {
        var session = new EditorSession();
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

        return new TestContext(session, documents, windows);
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

        if (expected.TryGetProperty("undo", out var undo)
            && undo.TryGetProperty("limit", out var undoLimit))
        {
            Assert.Equal(undoLimit.GetInt32(), context.Session.Undo.Limit);
        }
    }

    private static void AssertExpectedDocument(EditorDocument document, JsonElement expected)
    {
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
        IReadOnlyDictionary<string, EditorWindow> Windows);
}
