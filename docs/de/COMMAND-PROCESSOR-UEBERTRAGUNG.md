# Übertragung der FIRST-ED-Command-Prozessoren

## Warum eine eigene Schicht?

Das Handbuch der Turbo Editor Toolbox trennt die Tastatur-Dispatcher von den Prozeduren, die eine Editoroperation tatsächlich ausführen. SASD übernimmt diese Grenze und hält historische Eingabekonventionen aus der allgemeinen Editing Engine heraus.

`FirstEdKeyMap` beantwortet, **welcher** semantische Befehl zu einer Tastensequenz gehört. `EditorCommandDispatcher` entscheidet, **welcher Prozessor** ausgeführt wird. `FirstEdCompatibilityProcessor` enthält historisch bedingte Übersetzungsregeln; `EditorSearchService` und `EditorFileService` kapseln die zustandsbehafteten Such- bzw. Dateioperationen.

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
| Ränder/Tab/Undo-Limit | Compatibility Processor | Benutzereingaben werden in modernen internen Zustand übersetzt |
| `EditFind`-Wiederholung | `EditorSearchService.FindAgain` | vorheriges Muster wird gemerkt, Suche läuft hinter dem letzten Treffer weiter |
| `EditReatxtfil`-Einfügen | `EditorFileService.ReadIntoCurrentWindowAsync` | Einfügen hinter aktueller Zeile, Cursor bleibt stehen |
| `EditFileWrite` | `EditorFileService.WriteCurrentWindowAsync` | aktueller Stream wird an expliziten Pfad geschrieben |
| Wrapped-Zeilen in Dateien | `FirstEdLegacyFileCodec` | High-Bit-CR (`0x8D`) wird `EditorLineFlags.Wrapped` |

## Moderne Umsetzung von Window Linking

Die Pascal-Version musste Zeiger umhängen und aufgegebene Text-Streams explizit freigeben. In C# wird ein `EditorWindow` sicher an ein anderes `EditorDocument` gehängt. Nicht mehr referenzierte Dokumente fallen unter die Garbage Collection. Verknüpfte Fenster teilen Text, behalten aber eigene Cursor- und Scrollzustände.

## Kompatibilitäts-I/O versus moderne Persistenz

`FileTextStorage` bleibt der moderne UTF-8-Provider für vollständige Dokumente. Die historische Byte-Konvention ist in `IEditorFileCodec` / `FirstEdLegacyFileCodec` isoliert. Dadurch wird das alte Dateiformat nicht unbeabsichtigt zum Speicherformat aller zukünftigen SASD-Anwendungen.

Details stehen in `SUCHEN-UND-DATEI-BEFEHLE.md`.

## Bewusst noch offen

- interaktive Exit-Bestätigung und Rundown-Zustand des Editor-Loops;
- physische Fenstergröße bei Create Window als Aufgabe der Host-/Layout-Schicht;
- exaktes Scroll-/Page-Verhalten, weil die historische Semantik an dargestellte Bildschirmzeilen gekoppelt ist;
- der interaktive FIRST-ED-Host;
- MicroStar-Menüs, Pop-ups und Hintergrunddruck als spätere Beispiele.

Diese Trennung ist wichtiger als ein schneller monolithischer Port.
