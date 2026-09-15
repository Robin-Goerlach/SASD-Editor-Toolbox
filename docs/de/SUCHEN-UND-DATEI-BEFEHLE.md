# FIRST-ED-Such- und Datei-Befehle

## Umfang

Dieser Meilenstein überträgt die nächste Gruppe von Verhaltensweisen der Turbo Editor Toolbox nach C#, ohne Host-Abfragen, Editorzustand und das historische Byte-Dateiformat in einer monolithischen Klasse zu vermischen.

Das Handbuch bleibt die Verhaltensreferenz; der Code wird unabhängig neu implementiert.

## Suchzustand und Find Again

Das historische `EditFind` fragt nach einem Suchmuster. Wird dabei eine leere Zeichenfolge eingegeben, wird das vorherige Suchmuster erneut verwendet. FIRST-ED besitzt außerdem einen Befehl zum Wiederholen der Suche. SASD bildet das explizit im `EditorSearchService` ab:

- `FindNext(pattern, options)` merkt sich Muster und Optionen;
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

`FileTextStorage` wird absichtlich **nicht** verändert. Es bleibt der moderne UTF-8-Provider für vollständige Dokumente. Historische Kompatibilität und moderne Persistenz können sich so unabhängig entwickeln.

## Write- und Save-Semantik

`WriteFile` schreibt den aktuellen Text-Stream an einen expliziten Pfad, benennt das Dokument aber nicht um und löscht das Dirty-Flag nicht. Das entspricht der direkten historischen Write-Operation mit Dateinamen.

`SaveFile` schreibt an den dem Dokument zugeordneten Pfad und löscht anschließend das Dirty-Flag. Ein moderner Host darf auch explizit einen Pfad mitgeben. Das ist bewusst als moderne Integrationsentscheidung dokumentiert, weil die Dokumentidentität in SASD eine moderne Abstraktion und kein Pascal-Globalzustand ist.

## Noch offen

Die wesentlichen V1-Lücken liegen jetzt vor allem bei Lifecycle und Darstellung: Rundown/Exit-Bestätigung, exakte bildschirmabhängige Scroll-/Page-Semantik, physische Create-Window-Anordnung, der interaktive FIRST-ED-Host und später die MicroStar-Demofunktionen.
