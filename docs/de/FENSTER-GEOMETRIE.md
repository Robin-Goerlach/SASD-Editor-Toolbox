# FIRST-ED-Fenstergeometrie

## Umfang

Dieses Dokument beschreibt die Clean-Room-Übertragung der sichtbaren Fenstergeometrie aus Turbo Editor Toolbox V1 in die SASD Editor Toolbox. Das historische Handbuch dient als Verhaltensspezifikation; Pascal-Quellcode wird nicht in die Implementierung kopiert.

Die wichtige Trennung lautet:

- `EditorWindow` verwaltet Dokument-/View-Zustand wie Cursor, oberste Textzeile und horizontalen Offset.
- `EditorWindowLayout` verwaltet die vertikale Verteilung der Bildschirmzeilen auf sichtbare Fenster.
- `EditorWindowFrame` ist die unveränderliche Projektion aus oberster Bildschirmzeile und Höhe eines Fensters.
- Renderer verwenden diese Frames, entscheiden aber nicht selbst über die Semantik des Fenster-Splittings.

Dadurch bleibt der wiederverwendbare Core unabhängig von Console-, WinForms-, WPF-, Avalonia- oder Browser-Geometrie-APIs.

## Historisches Zeilenmodell

Für die V1-Kompatibilität enthält die Höhe eines sichtbaren Fensters seine Statuszeile. Ein gültiges Fenster benötigt damit mindestens drei Host-Zeilen:

- eine Statuszeile;
- mindestens zwei Textzeilen.

`EditorWindowFrame.MinimumHeight` ist deshalb `3`; `TextRows` entspricht `Height - 1`.

## Create Window

Der semantische Befehl `CreateWindow` wird auf `FirstEdCompatibilityProcessor.CreateWindow(size, donorWindowNumber)` abgebildet.

Die Operation folgt dem dokumentierten Verhalten von `EditWindowCreate(Size, Win)`:

1. das sichtbare Spenderfenster ermitteln;
2. eine neue Fensterhöhe unter drei Zeilen ablehnen;
3. ein Split ablehnen, der das Spenderfenster auf weniger als drei Zeilen verkleinern würde;
4. die angeforderten Zeilen aus dem unteren Bereich des Spenderfensters übernehmen;
5. ein neues leeres `NONAME`-Dokument/Fenster direkt nach dem Spender in die sichtbare Fensterreihenfolge einfügen;
6. falls der Cursor des komprimierten Spenders unterhalb seines neuen sichtbaren Textbereichs läge, ihn auf die letzte sichtbare Textzeile des Spenders zurücksetzen und dabei die Spalte erhalten.

Die Gesamtzahl der Layout-Zeilen ändert sich durch den Split nicht.

## Delete Window

Das einzige sichtbare Fenster darf nicht gelöscht werden. Andernfalls wird das Ziel aus der sichtbaren Fensterreihenfolge entfernt und seine Bildschirmfläche nach der historischen Regel weitergegeben:

- wird Fenster 1 gelöscht, erhält das bisherige Fenster 2 die frei werdenden Zeilen;
- sonst erhält das direkt oberhalb liegende sichtbare Fenster die frei werdenden Zeilen.

Enthält das gelöschte Fenster den aktiven Block, wird der Block aufgehoben. Dies ist bewusst keine Undo-Blockoperation, weil die historische Fensterlöschung diesen Fensterkontext unmittelbar verwirft.

### Verknüpfte Dokumente

Die Pascal-Version musste entscheiden, ob ein Text Stream freigegeben werden durfte, und Links sowie Speicher explizit verwalten. SASD bildet einen gemeinsamen Text Stream durch mehrere `EditorWindow`-Objekte ab, die auf dasselbe `EditorDocument` zeigen. Wird eine View gelöscht, bleibt das Dokument automatisch erhalten, solange eine andere View darauf verweist. Managed Lifetime ersetzt hier die historische Pointer-/Free-List-Verwaltung.

## Policy bei Größenänderung des Hosts

Das Handbuch beschreibt eine feste Bildschirmgeometrie, nicht moderne veränderbare Terminalfenster. `EditorWindowLayout.ResizeWorkspace` verwendet deshalb ausdrücklich eine **moderne SASD-Regel** und behauptet hierfür keine historische Semantik:

- zusätzliche Zeilen erhält das unterste sichtbare Fenster;
- beim Verkleinern werden freie Zeilen von unten nach oben entzogen;
- kein sichtbares Fenster wird unter drei Zeilen verkleinert;
- kann der Host nicht drei Zeilen pro Fenster bereitstellen, wird die Größenänderung abgelehnt und der Terminal-Referenzhost fordert eine Vergrößerung des Terminals an.

Diese Regel kann später ausgetauscht oder konfigurierbar gemacht werden, ohne die historische Create-/Delete-Semantik zu verändern.

## Terminal-Referenzhost

`ConsoleFirstEdRenderer` stellt jetzt alle `EditorWindowFrame` gleichzeitig dar. Jedes Fenster besitzt eine eigene Statuszeile und einen eigenen Text-Viewport; das aktuelle Fenster wird mit `>` markiert und erhält den sichtbaren Terminal-Cursor. Die unterste Terminalzeile bleibt für Prompts und kurzlebige Befehlsmeldungen reserviert.

Für jedes sichtbare Fenster verwendet der Renderer `EditorViewportBuilder.Build(window, textRows, width)`. Damit nutzt auch der Multi-Window-Host weiterhin dieselbe UI-neutrale Rendering-Projektion wie ein späterer GUI-Host.

## Tests

`WindowLayoutCompatibilityTests` prüft:

- Zeilen-Splitting und Einfügereihenfolge;
- das Drei-Zeilen-Minimum für neues und Spenderfenster;
- die Cursor-Korrektur nach Komprimierung;
- die Übernahme frei werdender Zeilen beim Löschen des ersten bzw. eines späteren Fensters;
- das Aufheben eines aktiven Blocks;
- Host-Größenänderungen ohne Verletzung der Mindesthöhe.
