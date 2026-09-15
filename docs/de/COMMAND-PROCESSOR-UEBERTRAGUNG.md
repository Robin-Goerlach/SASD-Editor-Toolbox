# Übertragung der FIRST-ED-Command-Prozessoren

## Warum eine eigene Schicht?

Das Handbuch der Turbo Editor Toolbox trennt die Tastatur-Dispatcher von den Prozeduren, die eine Editoroperation tatsächlich ausführen. SASD übernimmt diese Grenze und hält historische Eingabekonventionen aus der allgemeinen Editing Engine heraus.

`FirstEdKeyMap` beantwortet, **welcher** semantische Befehl zu einer Tastensequenz gehört. `EditorCommandDispatcher` entscheidet, **welcher Prozessor** ausgeführt wird. `FirstEdCompatibilityProcessor` enthält historisch bedingte Übersetzungsregeln; `EditorSearchService`, `EditorFileService` sowie Scheduling/Lifecycle kapseln ihre zustandsbehafteten Aufgaben.

Das ist für spätere Implementierungen in C++, Java und JavaScript wichtig: Das sprachneutrale Verhalten lässt sich übernehmen, ohne DOS-Tastatur- oder Bildschirmannahmen mitzuschleppen.

## Übertragene Command-Semantik

| Historisches Konzept | C#-Implementierung | Kompatibilitätsdetail |
|---|---|---|
| `EditBeginningEndLine` | `MoveBeginningOrEndOfLine` | nicht erste Spalte -> erste Spalte; erste Spalte -> hinter letztes Nicht-Leerzeichen |
| `EditEndLine` | `MoveEndOfLine` | nachgestellte Leerzeichen zählen nicht zum Zeilenende |
| `EditGotoLine` | `GoToLine` | einbasierter Wert; hinter EOF letzte Zeile; Spalte bleibt erhalten |
| `EditGotoColumn` | `GoToColumn` | einbasierter Wert; virtuelle Spalten erlaubt |
| `EditTopBlock` / `EditBottomBlock` | `GoToBlockBoundary` | kann zum Fenster des Block-Dokuments wechseln |
| `EditWindowUp` / `EditWindowDown` | Compatibility-/Session-Navigation | umlaufend über erstes/letztes Fenster |
| `EditWindowGoto` | `GoToWindow` | einbasierte Fensternummer modulo Fensterzahl |
| `EditWindowLink` | `LinkWindows` | vorhandenes Zielfenster wird an das Quelldokument gehängt |
| `EditWindowTopFile` / `EditWindowBottomFile` | Compatibility Processor | dokumentierte Cursor-Spalte und TopLine werden berücksichtigt |
| `EditUpLine` / `EditDownLine` | Compatibility Processor | Viewport folgt dem Cursor am sichtbaren Rand |
| `EditScrollUp` / `EditScrollDown` | Compatibility Processor | einzeiliges Scrollen plus dokumentierte Cursor-Randkorrektur |
| `EditUpPage` / `EditDownPage` | Compatibility Processor | Viewport um sichtbare Zeilen minus eins |
| Ränder/Tab/Undo-Limit | Compatibility Processor | Benutzereingaben werden in modernen internen Zustand übersetzt |
| `EditFind`-Wiederholung | `EditorSearchService.FindAgain` | vorheriges Muster wird gemerkt, Suche läuft hinter dem letzten Treffer weiter |
| `EditReatxtfil`-Einfügen | `EditorFileService.ReadIntoCurrentWindowAsync` | Einfügen hinter aktueller Zeile, Cursor bleibt stehen |
| `EditFileWrite` | `EditorFileService.WriteCurrentWindowAsync` | aktueller Stream wird an expliziten Pfad geschrieben |
| Wrapped-Zeilen in Dateien | `FirstEdLegacyFileCodec` | High-Bit-CR (`0x8D`) wird `EditorLineFlags.Wrapped` |
| `EditExit` / `Rundown` | `RequestRundown`, `RundownRequested` | Exit fordert Schleifenende an und speichert nicht |
| `EditSchedule` | `EditorScheduler.RunCycleAsync` | vorhandene Eingabe hat Vorrang vor Hintergrundarbeit |
| `EditSystem` | `EditorSystemLoop.RunAsync` | Scheduler-Zyklen bis Rundown |

## Moderne Umsetzung von Window Linking

Die Pascal-Version musste Zeiger umhängen und aufgegebene Text-Streams explizit freigeben. In C# wird ein `EditorWindow` sicher an ein anderes `EditorDocument` gehängt. Nicht mehr referenzierte Dokumente fallen unter die Garbage Collection. Verknüpfte Fenster teilen Text, behalten aber eigene Cursor- und Scrollzustände.

## Kompatibilitäts-I/O versus moderne Persistenz

`FileTextStorage` bleibt der moderne UTF-8-Provider für vollständige Dokumente. Die historische Byte-Konvention ist in `IEditorFileCodec` / `FirstEdLegacyFileCodec` isoliert. Dadurch wird das alte Dateiformat nicht unbeabsichtigt zum Speicherformat aller zukünftigen SASD-Anwendungen.

## Lifecycle- und Page-Hinweis

Der Host besitzt die Bestätigungs-UI für Ctrl-K X; der Core besitzt die direkte Exit-/Rundown-Aktion. Das Handbuch nennt die Page-Verschiebung, aber keine separate Bildschirmzeile des Cursors danach. SASD erhält deshalb die relative sichtbare Cursorzeile als ausdrücklich moderne Regel. Details stehen in `LIFECYCLE-UND-SCROLLEN.md`.

## Bewusst noch offen

- physische Fenstergröße/-komprimierung bei Create Window als Aufgabe der Host-/Layout-Schicht;
- der interaktive FIRST-ED-Host;
- der verbleibende V1-Kompatibilitätsaudit und historische Fehlerressourcen;
- MicroStar-Menüs, Pop-ups und Hintergrunddruck als spätere Beispiele.

Diese Trennung ist wichtiger als ein schneller monolithischer Port.
