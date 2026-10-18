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

Write/Save reichen denselben verknüpften Token an den Codec weiter. Der eingebaute historische Codec schreibt jetzt zuerst eine vollständige temporäre Datei im Zielverzeichnis und veröffentlicht sie erst nach erfolgreich abgeschlossenem, nicht abgebrochenem Schreiben. Abbruch oder Fehler **vor der Veröffentlichung** lassen die bisherigen Zieldatei-Bytes unverändert. Benutzerdefinierte externe Codecs können weiterhin teilweise Dateien schreiben und müssen ihre eigenen Garantien dokumentieren. Save löscht bei Unterbrechung den Dirty-Zustand nicht.

Semantisches Find/Find Again verwendet den unterbrechbaren Suchpfad. Der Scan prüft einmal pro logischer Zeile und übernimmt Cursor sowie gemerkten Suchzustand erst nach erfolgreichem Abschluss. Direkte synchrone Service-Methoden bleiben für Anwendungen verfügbar, die bewusst einen nicht unterbrechbaren In-Process-Aufruf wünschen.

## Konsistenz beim Speichern während weiterer Bearbeitung

Ein Host kann während einer asynchronen Dateioperation weiter schreiben, auch in einem zweiten verknüpften Fenster. Vor dem Save werden deshalb die Dokumentversion und der genaue Zeilen-Snapshot erfasst. Nach erfolgreichem Schreiben entfernt `EditorDocument.MarkSnapshotSaved` die Dirty-Kennzeichnung **nur**, wenn aktuelle Version und Zeileninhalt weiterhin dem wirklich geschriebenen Snapshot entsprechen. Der zugeordnete Dateipfad darf nach einem erfolgreichen Schreibvorgang aktualisiert werden; neuere, nicht gespeicherte Änderungen bleiben als Dirty erkennbar.

Dasselbe Schutzverhalten gilt sowohl im FIRST-ED-Dateidienst als auch in der modernen UTF-8-Persistenz. `WriteFile` bleibt eine direkte Ausgabe ohne Änderung von Dateiname oder Dirty-Status. Diese zusätzliche Absicherung ist eine moderne Architekturentscheidung, keine Behauptung über den kooperativen DOS-Editor von 1985.

## Sichere Zuordnung des Dateiziels bei asynchronem Lesen

`ReadIntoCurrentWindowAsync` hält das ursprünglich gewählte Fenster, Dokument, die Einfügezeile und einen Dokument-Snapshot **vor** dem ersten asynchronen Warten fest. Wechselt der Host während des Dekodierens zu einem anderen Fenster, werden die Zeilen dennoch in den ursprünglichen Textstrom eingefügt; die aktuelle Fensterwahl bleibt bestehen. Bewegt der Benutzer inzwischen den Cursor im Zielfenster, wird seine neue Position nach dem üblichen Zeilen-Realignment erhalten statt durch den verspäteten Dateiimport zurückgesetzt.

Wurde das ursprüngliche Fenster geschlossen, mit einem anderen Dokument verknüpft oder sein Text/Metadatenzustand während des Lesens verändert, wird der Import ohne weitere Textmutation oder Undo-Eintrag abgelehnt. Über den Command Dispatcher entsteht der vorhandene typisierte Fehler `OperationFailed`. Das ist eine moderne Sicherheitsregel für asynchrone Hosts und keine Behauptung über den DOS-Eingabezyklus von 1985. Deterministische Tests verwenden einen gezielt wartenden Codec ohne Zeitannahmen.

## Portable Tests mit dekodierten Dateizeilen
In `conformance/v1/cases/file-read-topology.json` liefert `initial.fileReadLines` einen bereits dekodierten Zeilenbestand (Text und optionale Flags). Die portable Operation `read_file` wird über den normalen semantischen Dispatcher ausgeführt, während ein deterministischer Test-Codec die Zeilen liefert. So werden Einfügeposition, Realignment, Undo und Fenster-Isolation **ohne** Dateisystem- oder Byteformat-Abhängigkeit überprüfbar. Bei leerem Dateiinhalt bleiben Text, Dirty-Zustand und Undo unverändert. Andere Sprachimplementierungen dürfen eigene idiomatische Adapter verwenden.

## Sicherheit bei asynchroner Suche und Ersetzung

Während einer unterbrechbaren Suche können Host-Callbacks weitere Eingaben verarbeiten. Der .NET-Referenzkern durchsucht deshalb einen **festen Snapshot der logischen Zeilen des Startdokuments**, statt nach jedem Unterbrechungspunkt erneut `CurrentWindow` auszuwerten. Ein reiner Fensterwechsel leitet die Suche nicht um: Der Treffer gehört weiterhin zur ursprünglichen Ansicht, während das inzwischen ausgewählte Fenster aktiv bleibt. Vor der Übernahme prüft der Editor, ob Fenster und Dokument noch existieren bzw. verbunden sind, Cursor, Version und tatsächlicher Zeileninhalt unverändert blieben und keine neuere Suche das Ergebnis überholt hat. Bei einem Konflikt meldet der Dispatcher den typisierten Fehler `OperationFailed`, ohne die neuere Navigation oder Suche zu überschreiben.

`ReplaceNextAsync` sichert zusätzlich die asynchrone Host-Bestätigung `BeforeReplaceAsync` ab. Nur wenn Treffer, Text, Fenster und aktives Ersetzungsziel weiterhin gültig sind, darf die Ersetzung durchgeführt werden. Änderungen, neue Suchaufträge oder ein Wechsel der aktiven Ansicht verhindern eine veraltete Ersetzung. `Cancel` bleibt ein wirkungsloser Abbruch; `Skip` verschiebt den Cursor nur bei weiterhin gültigem Suchkontext. Das sind bewusste Sicherheitsregeln für moderne asynchrone Hosts, keine Behauptungen über das Laufzeitverhalten des DOS-Editors von 1985.

## Find Again nach Änderungen und Undo

Das gespeicherte Suchmuster und die Suchoptionen gehören zum Suchdienst und nicht zum Text-Undo-Protokoll. Die alte Trefferposition darf jedoch nach einer Textänderung **nicht** blind übersprungen werden. Der `EditorSearchService` merkt sich deshalb den ursprünglichen Zeilentext. Nur wenn an der gemerkten Cursorposition der Zeilentext und die Dokumentversion unverändert sind, darf Find Again hinter dem vorigen Treffer fortsetzen. Nach strukturellen Änderungen kann an derselben Zeilennummer ein anderer Treffer mit identischem Text stehen. Nach dem Löschen eines Treffers durch Ersetzen mit leerem Text kann damit der neu freigelegte nächste Treffer gefunden werden.

Stellt Undo ein Dokument wieder her, wird dessen alter Trefferanker verworfen, **nicht** aber das Suchmuster oder die Suchoptionen. Find Again startet anschließend an der wiederhergestellten Cursorposition. Gleichzeitig entwertet Undo noch laufende asynchrone Suchoperationen, damit verspätete Ergebnisse nicht die restaurierte Dokument-, Marker- oder Blocklage überschreiben. Dies sind ausdrücklich moderne Zustandsregeln und keine behaupteten Eigenschaften der historischen Undo-Zeilenstapel.

## Validierung logischer Zeilengrenzen

Die wörtliche V1-Suche betrachtet jeweils eine logische Textzeile. Suchmuster mit eingebettetem CR oder LF werden deshalb jetzt als ungültiges Argument abgelehnt und nicht als unerfüllbares Find-Again-Muster gespeichert. Auch `ReplaceNextAsync` und `ReplaceAll` lehnen CR/LF im Ersatztext **vor** der Suche beziehungsweise Textänderung ab. Ihr bisheriger einzeiliger Puffereingriff darf nicht heimlich weitere logische Zeilen erzeugen, sonst würden Marker, Blöcke, Darstellung und Dateispeicherung verschiedene Zeilenstrukturen sehen. Der leere Ersatztext bleibt weiterhin erlaubt. Der Dispatcher meldet einen typisierten Argumentfehler über den UI-neutralen Fehler-Hook; direkte Serviceaufrufe erhalten `ArgumentException`.

Das ist eine dokumentierte V1-Grenze und kein Ausschluss zukünftiger mehrzeiliger Ersetzungen. Eine spätere Erweiterung benötigt eine eigene strukturelle Bearbeitungsoperation einschließlich korrekter Topologie-Behandlung.

## Suchgrenzen bei Wrap-around

`SearchOptions(WrapAround: true)` ist eine ausdrücklich aktivierte moderne Host-Option. Die Vorwärtssuche prüft zunächst Treffer-**Startpositionen** ab dem Cursor und nach dem Umbruch die davorliegenden Startpositionen. Beginnt ein Treffer vor dem ursprünglichen Cursor und reicht mit seinem Text darüber hinaus, ist er dennoch ein gültiger Treffer nach dem Umbruch. Die Cursorposition trennt also mögliche Trefferanfänge, **nicht** die Textbereiche. Synchrone und unterbrechbare Suche verwenden jetzt dieselbe kommentierte Hilfsfunktion, damit Groß-/Kleinschreibung, Wortgrenzen und Wrap-around übereinstimmen. Für FIRST-ED bleibt der Umbruch standardmäßig deaktiviert.

Der gemeinsame Conformance-Adapter kennt nun `find_next_with_options`, um diese modernen Host-Optionen getrennt vom FIRST-ED-Standardbefehl zu testen. So bleibt die historische Tastaturbelegung unangetastet, während optionales Wrap-around, Wortgrenzen und Groß-/Kleinschreibung für spätere C++-/Java-Implementierungen sprachneutral prüfbar werden.

## Ersetzungsbestätigung zeigt den tatsächlich gefundenen Text

Bei einer Suche ohne Unterscheidung von Groß-/Kleinschreibung kann das Muster `alpha` im Dokument den Text `Alpha` finden. `IEditorHooks.BeforeReplaceAsync` erhält nun über `ReplaceCandidate.ExistingText` den **tatsächlich gefundenen Dokumenttext** (`Alpha`), nicht nur das eingegebene Suchmuster. Der Text stammt aus demselben Dokument-Snapshot, der nach der asynchronen Bestätigung nochmals geprüft wird. Damit beziehen sich Ersetzen, Überspringen und Abbrechen auf den tatsächlich sichtbaren Treffer, ohne eine UI-Abhängigkeit im Kern zu erzeugen.

## Sicheres Schreiben über temporäre Dateien

Beide eingebauten Dateischreiber verwenden jetzt intern `StagedFileWriter`. Der historische Codec erstellt zuerst den vollständigen 8-Bit-Datenstrom einschließlich der `0x8D`-Wrapped-Grenzen. Der moderne UTF-8-Speicher erzeugt den Inhalt aus dem gesicherten Dokument-Snapshot mit dessen Zeilenende-Konvention. Anschließend wird jeweils eine eindeutig benannte temporäre Datei **im Zielverzeichnis** vollständig geschrieben, auf Abbruch geprüft und per Überschreiben/Umbenennen veröffentlicht. Ein Fehler oder Abbruch während dieses ersten Schritts lässt die alte Zieldatei unverändert; Aufräumfehler verdecken nicht den ursprünglichen Schreibfehler.

Diese Absicherung ist eine **moderne Best-Effort-Regel**, kein Nachweis für Absturzsicherheit oder ursprüngliches DOS-Verhalten. Garantien für atomare Umbenennung, Zugriffsrechte/Metadaten, konkurrierende Schreibaufträge und persistentes fsync hängen vom Dateisystem und späteren Architekturentscheidungen ab. Fremde `IEditorFileCodec`-Implementierungen dürfen eigene Schreibregeln haben. Deterministische Tests steuern Schreibabbrüche und -fehler direkt am temporären Dateischritt.

## Vollständige Prüfung dekodierter Dateiimporte

Ein benutzerdefinierter `IEditorFileCodec` kann trotz erfolgreichem asynchronem Lesen ungültige Daten liefern. `ReadIntoCurrentWindowAsync` kopiert deshalb zunächst die zurückgegebenen Zeilen und überprüft **jede** davon vor dem Anlegen einer Undo-Transaktion oder einer Textänderung. CR, LF und `0x8D` dürfen nicht als Klartext innerhalb einer logischen Zeile auftreten; unbekannte Bit-Kombinationen in den Zeilenattributen werden ebenfalls abgelehnt. Das zulässige `Wrapped`-Attribut bleibt die ausgehende weiche Zeilengrenze der vorhergehenden Zeile. `InBlock` wird bei ansonsten gültigen importierten Zeilen weiterhin entfernt. Große Datenbestände werden bei der Prüfung kooperativ auf Abbruch kontrolliert.

Bei fehlerhaften Codec-Daten meldet der semantische Dispatcher den typisierten Dateifehler `FileIoFailed`. Dokumenttext, Cursor, Marker, Block und Undo bleiben unverändert. Diese moderne Integritätsregel für Dateiadapter ist vom historischen 8-Bit-Format getrennt und kann in anderen Sprachimplementierungen nachgebildet werden.

## Regressionstests für historische 0x8D-Zeilengrenzen

Die Byteformat-Testreihe für .NET prüft jetzt ausdrücklich ein abschließendes `Wrapped`-Kennzeichen, die defensive Kombination `0x8D/LF`, aufeinanderfolgende weiche und harte Zeilentrenner mit absichtlich leeren Zeilen, einzelne CR-/LF-Grenzen neben CRLF, abschließende gewöhnliche Trennzeichen ohne künstliche zusätzliche Leerzeile, leere physische Dateien sowie Latin-1-Steuerzeichen und High-Bytes (außer dem reservierten `0x8D`). Diese Tests beschreiben die **dokumentierte Behandlung der Referenzimplementierung**, nicht eine pauschale Behauptung, jede defensive Eingabevariante sei im Handbuch von 1985 definiert.

## Gleichzeitige Schreibvorgänge auf dieselbe Datei

Der gemeinsame Dateischreiber reserviert jetzt **vor dem ersten asynchronen Warten** eine Sperre für den normalisierten Zielpfad. Schreiben zwei eingebaute UTF-8- bzw. historische Codec-Aufrufe auf dieselbe Datei, kann der zweite Vorgang erst nach Abschluss des ersten seinen temporären Inhalt schreiben und veröffentlichen. Dadurch kann ein älterer, langsamer eingebauter Schreibvorgang nicht mehr hinter einem bereits bestätigten neueren Schreibvorgang die alten Bytes veröffentlichen. Unterschiedliche Zieldateien bleiben parallel beschreibbar. Abgebrochene Wartende legen keine temporäre Datei an; Fehler geben die Sperre frei, nicht mehr benötigte Sperren werden verworfen.

Das gilt innerhalb **eines Prozesses** für die eingebauten Dateischreiber. Fremde `IEditorFileCodec`-Implementierungen, weitere Prozesse, Symlink-/Hardlink-Aliase, plattformspezifische Groß-/Kleinschreibung und fremde Dateizugriffe sind nicht durch diese Sperre geschützt. Der Dirty-State wird unabhängig davon weiterhin anhand des tatsächlich gespeicherten Dokument-Snapshots bestätigt. Deterministische Tests sichern die Sperr- und Abbruchregeln ab.

## Validierung logischer Zeilen bei modernem UTF-8

Die moderne UTF-8-Dokumentablage unterstützt Unicode, aber jeder Puffereintrag steht weiterhin für **genau eine logische Zeile**. `FileTextStorage` prüft vor dem Schreiben, ob Zeilentexte eingebettetes CR/LF enthalten und ob die eingestellte Zeilenendekonvention genau CRLF, LF oder CR ist. Andernfalls könnte sich beim erneuten Laden unbemerkt die Anzahl logischer Zeilen ändern. Fehlerhafte Inhalte werden abgelehnt, ohne eine temporäre Datei zu erzeugen oder das Ziel zu ersetzen; Dirty-State und Undo bleiben unverändert. Anders als beim historischen 8-Bit-Codec ist `0x8D` in modernem UTF-8 kein reserviertes Zeilentrennzeichen.

Die verwendete Zeilenendekonvention wird zusammen mit Dokumentversion und Zeilen-Snapshot festgehalten. Ändert der Host diese Metadaten während der asynchronen Ausgabe, bestätigt `MarkSnapshotSaved` nicht fälschlich die alte Darstellung als aktuellen Speicherstand. Das historische `Wrapped`-Dateiformat bleibt hiervon unabhängig.

## Speicherbestätigungen bei verschiedenen Zieldateien

Eine Sperre pro Zielpfad schützt nicht, wenn **dasselbe Dokument gleichzeitig unter zwei verschiedenen Dateinamen** gespeichert wird. Diese Dateiausgaben können in beliebiger Reihenfolge fertig werden. Deshalb erhält jeder Save-/Save-As-Auftrag vor dem asynchronen Schreiben eine monoton steigende Nummer. Nur die **neueste erfolgreich abgeschlossene Speicheranforderung** darf die aktuelle Dateizuordnung bestätigen. Ein älterer, verspäteter Auftrag darf weder `FilePath` zurücksetzen noch den Dirty-State nach einer neueren erfolgreichen Ausgabe löschen. Fehlerhafte oder abgebrochene neuere Anforderungen blockieren eine ältere erfolgreiche Speicherung nicht. Die bisherigen Prüfungen von Text, Version und verwendeter Zeilenendekonvention bleiben erhalten.

Die Anforderungsnummer gehört zum Dokument und nicht zu einer konkreten Oberfläche oder einem Codec. Das historische direkte Write File bleibt unverändert, weil es die Dokumentidentität grundsätzlich nicht verändert. Deterministische Tests beenden Schreibaufträge über einen steuerbaren Test-Codec absichtlich in unterschiedlicher Reihenfolge.
