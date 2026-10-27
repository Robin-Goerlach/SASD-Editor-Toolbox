# Independent .NET Core consumer (RC-03)

This tiny executable exercises **only the public** `Sasd.Editor.Core`
assembly. It creates a document, inserts text, searches, undoes an edit and
exports a disposable file. It does not instantiate FIRST-ED/MicroStar host
classes or require an interactive terminal.

From the repository root:

```bash
dotnet run --project samples/dotnet/Sasd.Editor.Consumer.Sample -c Release
```

Expected output: `RC-03 public Core consumer: PASS`.

The exported file uses the library's default **legacy** file codec and is
removed after the example completes. This is an integration smoke, not yet
proof of NuGet packaging, all historical semantics, or a complete V1 release.
The sample is in the .NET solution so both Windows and Ubuntu CI compile it.
