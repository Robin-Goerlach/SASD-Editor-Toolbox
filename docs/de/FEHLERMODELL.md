# Typisiertes Fehler- und Ressourcenmodell

## Zweck

Der Editor-Core muss Fehler melden können, ohne festzulegen, wie Terminal, Desktop-Anwendung, Web-Host oder spätere C++-/Java-Implementierungen sie darstellen. V1 verwendet deshalb einen kleinen typisierten Fehlervertrag, statt den historischen numerischen Fehler-/Textkatalog zu kopieren.

Das ist eine Modernisierungsgrenze: Das Handbuch legt bei mehreren Befehlen fest, dass ein Fehler gemeldet wird. Historische Nummern und Wortlaut sind jedoch Präsentationsdetails und kein portables Editorverhalten.

## Fehlervertrag

Die .NET-Referenzimplementierung liefert `EditorError` über `IEditorHooks.OnErrorAsync`. Jeder katalogisierte Fehler enthält:

- `Id` — ein typisiertes semantisches `EditorErrorId`;
- `Code` — stabiler maschinenlesbarer Code für Logs, Automation und Conformance-Tests;
- `ResourceKey` — stabiler Schlüssel für die Lokalisierung im Host;
- `Message` — englischer Fallback-Diagnosetext, kein normativer UI-Wortlaut;
- optional `DetailCode` und `ParameterName`;
- bei Bedarf die ursprüngliche `Exception` für Diagnose, nicht zur ungefilterten Anzeige.

Der Katalog definiert derzeit folgende portable Kategorien:

| ID | Maschinen-Code | Ressourcenschlüssel |
|---|---|---|
| `InvalidCommandArgument` | `EDITOR_COMMAND_INVALID_ARGUMENT` | `Editor.Errors.Command.InvalidArgument` |
| `CommandRejected` | `EDITOR_COMMAND_REJECTED` | `Editor.Errors.Command.Rejected` |
| `OperationFailed` | `EDITOR_OPERATION_FAILED` | `Editor.Errors.Operation.Failed` |
| `FileIoFailed` | `EDITOR_FILE_IO_FAILED` | `Editor.Errors.File.IoFailed` |
| `AccessDenied` | `EDITOR_ACCESS_DENIED` | `Editor.Errors.Access.Denied` |
| `UnsupportedOperation` | `EDITOR_OPERATION_UNSUPPORTED` | `Editor.Errors.Operation.Unsupported` |
| `TypeaheadOverflow` | `EDITOR_TYPEAHEAD_OVERFLOW` | `Editor.Errors.Input.TypeaheadOverflow` |
| `OperationAborted` | `EDITOR_OPERATION_ABORTED` | `Editor.Errors.Operation.Aborted` |

## Befehlsfehler

Fehler in Form oder Wertebereich eines Requests werden von `EditorCommandValidationFailure` in `InvalidCommandArgument` überführt. Validierungskategorie und betroffenes Request-Feld bleiben als Detailmetadaten erhalten.

Zustandsabhängige Kompatibilitätsfehler, die das Handbuch tatsächlich als Fehler beschreibt, verwenden `CommandRejected`. Die ersten auditierten Beispiele sind Create Window bei nicht komprimierbarem Spender, Link eines Fensters auf sich selbst beziehungsweise bereits gemeinsamen Text, Löschen des einzigen angezeigten Fensters sowie unmögliche Änderungen von linkem/rechtem Rand.

Normale Navigations-No-ops bleiben hingegen gewöhnliche `false`-Ergebnisse. Das Erreichen des Dateianfangs ist beispielsweise kein Command-Rejected-Fehler.

## Operative Exceptions

Der Dispatcher ordnet erwartete Exception-Familien stabil zu, ohne deren Wortlaut zum UI-Vertrag zu machen:

- `IOException` -> `FileIoFailed`;
- `UnauthorizedAccessException` -> `AccessDenied`;
- `NotSupportedException` -> `UnsupportedOperation`;
- verbleibende `ArgumentException` -> `InvalidCommandArgument`;
- `InvalidOperationException` -> `OperationFailed`.

Cancellation wird hier bewusst nicht umgewandelt. `OperationCanceledException` bleibt Kontrollfluss für `CancellationToken`; der separate Audit zur Unterbrechbarkeit klärt das Zusammenspiel zwischen historischem Ctrl-U-Abortzustand und moderner Cancellation.

## Eingabefehler

Typeahead-Overflow und vom Host stammendes Ctrl-U verwenden jetzt denselben Fehlerkatalog wie Befehlsfehler. Das Terminalbeispiel leitet sie über den normalen Hook und entscheidet erst danach über die Darstellung. Der Typeahead-Puffer selbst bleibt hostunabhängig.

## Lokalisierung und Diagnose

Hosts sollten für lokalisierte Darstellung bevorzugt `ResourceKey` verwenden und dürfen auf `Message` zurückfallen. Logs und Automation sollten `Code` und `Id` bevorzugen.

Die ursprüngliche Exception bleibt für Diagnose erhalten. Ein Host sollte `Exception.Message` nicht automatisch anzeigen, da Datei- und Plattformfehler Pfade oder Umgebungsdetails enthalten können, die nicht Teil des Benutzervertrags werden sollen.

## Sprachübergreifende Conformance

Gemeinsame Conformance-Fälle dürfen Fehler-`id`, `code` und `resourceKey` prüfen. Der .NET-Adapter erfasst Fehler über `IEditorHooks`; C++ und Java sollen dieselben portablen Werte auf idiomatische native Enums/Ressourcen abbilden, statt die C#-Typstruktur nachzubauen.

Damit bleibt die Fehlersemantik portabel, während jede Sprache und jeder Host Darstellung und Lokalisierung selbst bestimmen kann.
