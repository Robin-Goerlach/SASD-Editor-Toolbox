# NuGet-only Core package consumer (RC-09)

This is an **independent package consumer**, not a source-project consumer.
Its only editor dependency is `PackageReference` to
`Sasd.Editor.Core 1.0.0-rc.1`. The application exercises public document
editing, search, undo and safe disposal of a temporary legacy-format export.

From the repository root, produce the **unpublished** candidate package:

```sh
dotnet pack src/dotnet/Sasd.Editor.Core/Sasd.Editor.Core.csproj -c Release -o artifacts/rc-package -p:PackageVersion=1.0.0-rc.1
dotnet restore samples/dotnet/Sasd.Editor.PackageConsumer.Sample/Sasd.Editor.PackageConsumer.Sample.csproj --source artifacts/rc-package
dotnet run --project samples/dotnet/Sasd.Editor.PackageConsumer.Sample -c Release --no-restore
```

Expected output: `RC-03 public Core consumer: PASS` (the shared functional
scenario). The GitHub Actions Windows/Ubuntu jobs run these steps with a
fresh package and no NuGet publication. This check does not grant V1.0
historical-parity approval.
