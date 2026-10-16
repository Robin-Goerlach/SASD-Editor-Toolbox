# FIRST-ED-Such- und Datei-Befehle

## Umfang

Dieser Meilenstein überträgt die nächste Gruppe von Verhaltensweisen der Turbo Editor Toolbox nach C#, ohne Host-Abfragen, Editorzustand und das historische Byte-Dateiformat in einer monolithischen Klasse zu vermischen.

Das Handbuch bleibt die Verhaltensreferenz; der Code wird unabhängig neu implementiert.

## Suchzustand und Find Again

Das historische `EditFind` fragt nach einem Suchmuster. Wird dabei eine leere Zeichenfolge eingegeben, wird das vorherige Suchmuster erneut verwendet. FIRST-ED besitzt außerdem einen Befehl zum Wiederholen der Suche. SASD bildet das explizit im `EditorSearchService` ab:

- `FindNext(pattern, options)` merkt sich Muster und Optionen;
- eine explizit leere Suchzeichenfolge (`FindNext("")` bzw. `FindNextAsync("")`, auch im Command Dispatcher) wiederholt das gespeicherte Muster einschließlich der ursprünglichen Suchoptionen. Ein fehlender/null-Wert ist weiterhin ein ungültiger Befehl; ohne zuvor gesetztes Muster entsteht bei leerer Eingabe einfach kein Treffer;
- `FindAgain()` wiederholt die gemerkte Suche;
- steht der Cursor noch auf dem vorherigen Treffer, beginnt Find Again hinter diesem Treffer und liefert nicht immer wieder dieselbe Fundstelle;
- die FIRST-ED-kompatible Vorwärtssuche springt standardmäßig nicht zum Dateianfang zurück;
- Wrap-around bleibt als explizite `SearchOptions`-Option für moderne Hosts verfügbar.

Die Suche ist weiterhin von der Tastaturbelegung getrennt. `FirstEdKeyMap` ordnet Ctrl-L `FindAgain` zu; der Command Dispatcher führt anschließend den gemerkten Suchzustand aus.

## Trennung der Datei-Befehle

Das Handbuch unterscheidet zwischen Befehlen, die den Benutzer nach einem Dateinamen fragen (`EditCprfw`, `EditCpwfw`), und Routinen, die einen Dateinamen als Parameter erhalten (`EditFileRead`, `EditFileWrite`). SASD erhält diese Grenze:

1. der Host fragt den Pfad ab;
2. die Keymap beschreibt nur, dass ein Pfad benötigt wird;
3. `EditorCommandDispatcher` erhält einen vollständig aufgelösten `EditorCommandRequest`;
4. `EditorFileService` führt die Operation aus;
5. `IEditorFileCodec` kapselt das Byte-Dateiformat.

Damit können WPF-Dialog, Terminal-Prompt, Web-UI und Skript dieselbe Kernfunktion verwenden.

## Historische Read-Semantik

Laut Handbuch fügt `EditReatxtfil` den Inhalt einer Datei **hinter der aktuellen Zeile** ein und verändert die Cursorposition nicht. `EditorFileService.ReadIntoCurrentWindowAsync` erhält genau dieses beobachtbare Verhalten. Die Operation ist Undo-fähig und setzt den Dirty-Zustand.

Der Dateiname des aktuellen Dokuments wird dadurch nicht geändert: Es handelt sich um eine Einfügeoperation und nicht um ein modernes "Dokument öffnen".

## Wrapped-Zeilen im Dateiformat

Die historischen Datei-Routinen verwenden ein Carriage-Return-Byte mit gesetztem High-Bit, dezimal 141 bzw. hexadezimal `0x8D`, um eine Zeile mit `Wrapped`-Attribut zu kennzeichnen. `FirstEdLegacyFileCodec` bildet diese Konvention ab:

- `0x8D` beendet eine logische Zeile und setzt `EditorLineFlags.Wrapped`;
- normale CR/LF- und LF-Zeilenenden werden als normale Zeilengrenzen akzeptiert;
- beim Schreiben wird für Wrapped-Zeilen `0x8D` statt des normalen CR/LF-Trenners ausgegeben;
- der Codec ist bewusst ein 8-Bit-Latin-1-Kompatibilitätscodec.
- CR, LF und `0x8D` sind ausschließlich Zeilentrenner und dürfen nicht als Klartextzeichen **innerhalb** eines `EditorLineSnapshot.Text` vorkommen. Der Encoder lehnt solche und nicht in Latin-1 darstellbare Zeichen vor dem Öffnen/Überschreiben der Zieldatei ab. Regressionstests prüfen, dass vorhandene Datei-Bytes erhalten bleiben.

`FileTextStorage` bleibt der moderne UTF-8-Provider für vollständige Dokumente. Sein Byteformat und die Behandlung der Zeilenenden bleiben vom historischen 8-Bit-Codec unabhängig. Beide Speicherwege verwenden aber inzwischen dieselbe sichere Bestätigung eines gespeicherten Snapshots über `EditorDocument.MarkSnapshotSaved`, ohne die Dateiformate zu vermischen.

## Write- und Save-Semantik

`WriteFile` schreibt den aktuellen Text-Stream an einen expliziten Pfad, benennt das Dokument aber nicht um und löscht das Dirty-Flag nicht. Das entspricht der direkten historischen Write-Operation mit Dateinamen.

`SaveFile` schreibt an den dem Dokument zugeordneten Pfad und löscht anschließend das Dirty-Flag **nur, wenn während des Schreibens keine neueren Dokumentänderungen eingetroffen sind**. Ein moderner Host darf auch explizit einen Pfad mitgeben. Das ist bewusst als moderne Integrationsentscheidung dokumentiert, weil die Dokumentidentität in SASD eine moderne Abstraktion und kein Pascal-Globalzustand ist.

## Weitere V1-Arbeit

Lifecycle, Viewport-Bewegung, Fensterlayout und die FIRST-ED-Terminaloberfläche werden inzwischen in eigenen Kompatibilitätsmodulen entwickelt und getestet. Dieses Dokument behandelt Such- und Datei-Semantik; offen bleibt der systematische Nachweis der Verhaltensabdeckung aller historischen Routinen statt einer pauschalen Behauptung, die genannten Meilensteine seien noch unimplementiert.


## Unterbrechung und Cancellation

Kompatibles Read/Write sowie modernes Save führen Codec-I/O jetzt über `EditorInterruptionService` aus. Während asynchrone I/O aussteht, gibt der Dienst dem Host regelmäßig eine EditBreathe-artige Gelegenheit, physische Eingabe einzusammeln.

Bei Read File werden dekodierte Zeilen erst eingefügt, nachdem der Codec erfolgreich beendet und ein abschließender Unterbrechungs-Checkpoint bestanden wurde. Ctrl-U während des Lesens bricht deshalb den Codec-Token ab und lässt aktuelles Dokument, Cursor, Dirty-Flag und Undo-Stack unverändert.

Write/Save reichen denselben verknüpften Token an den Codec weiter. Ein abgebrochener externer Dateischreibvorgang kann auf Dateisystem-/Provider-Ebene trotzdem teilweise sichtbar sein; Editor-Dokumentzustand wird erst nach erfolgreichem Abschluss aktualisiert. Save löscht bei Unterbrechung den Dirty-Zustand nicht.

Semantisches Find/Find Again verwendet den unterbrechbaren Suchpfad. Der Scan prüft einmal pro logischer Zeile und übernimmt Cursor sowie gemerkten Suchzustand erst nach erfolgreichem Abschluss. Direkte synchrone Service-Methoden bleiben für Anwendungen verfügbar, die bewusst einen nicht unterbrechbaren In-Process-Aufruf wünschen.

## Konsistenz beim Speichern während weiterer Bearbeitung

Ein Host kann während einer asynchronen Dateioperation weiter schreiben, auch in einem zweiten verknüpften Fenster. Vor dem Save werden deshalb die Dokumentversion und der genaue Zeilen-Snapshot erfasst. Nach erfolgreichem Schreiben entfernt `EditorDocument.MarkSnapshotSaved` die Dirty-Kennzeichnung **nur**, wenn aktuelle Version und Zeileninhalt weiterhin dem wirklich geschriebenen Snapshot entsprechen. Der zugeordnete Dateipfad darf nach einem erfolgreichen Schreibvorgang aktualisiert werden; neuere, nicht gespeicherte Änderungen bleiben als Dirty erkennbar.

Dasselbe Schutzverhalten gilt sowohl im FIRST-ED-Dateidienst als auch in der modernen UTF-8-Persistenz. `WriteFile` bleibt eine direkte Ausgabe ohne Änderung von Dateiname oder Dirty-Status. Diese zusätzliche Absicherung ist eine moderne Architekturentscheidung, keine Behauptung über den kooperativen DOS-Editor von 1985.

## Sichere Zuordnung des Dateiziels bei asynchronem Lesen

`ReadIntoCurrentWindowAsync` hält das ursprünglich gewählte Fenster, Dokument, die Einfügezeile und einen Dokument-Snapshot **vor** dem ersten asynchronen Warten fest. Wechselt der Host während des Dekodierens zu einem anderen Fenster, werden die Zeilen dennoch in den ursprünglichen Textstrom eingefügt; die aktuelle Fensterwahl bleibt bestehen. Bewegt der Benutzer inzwischen den Cursor im Zielfenster, wird seine neue Position nach dem üblichen Zeilen-Realignment erhalten statt durch den verspäteten Dateiimport zurückgesetzt.

Wurde das ursprüngliche Fenster geschlossen, mit einem anderen Dokument verknüpft oder sein Text/Metadatenzustand während des Lesens verändert, wird der Import ohne weitere Textmutation oder Undo-Eintrag abgelehnt. Über den Command Dispatcher entsteht der vorhandene typisierte Fehler `OperationFailed`. Das ist eine moderne Sicherheitsregel für asynchrone Hosts und keine Behauptung über den DOS-Eingabezyklus von 1985. Deterministische Tests verwenden einen gezielt wartenden Codec ohne Zeitannahmen.

## Portable Tests mit dekodierten Dateizeilen
In `conformance/v1/cases/file-read-topology.json` liefert `initial.fileReadLines` einen bereits dekodierten Zeilenbestand (Text und optionale Flags). Die portable Operation `read_file` wird über den normalen semantischen Dispatcher ausgeführt, während ein deterministischer Test-Codec die Zeilen liefert. So werden Einfügeposition, Realignment, Undo und Fenster-Isolation **ohne** Dateisystem- oder Byteformat-Abhängigkeit überprüfbar. Bei leerem Dateiinhalt bleiben Text, Dirty-Zustand und Undo unverändert. Andere Sprachimplementierungen dürfen eigene idiomatische Adapter verwenden.
