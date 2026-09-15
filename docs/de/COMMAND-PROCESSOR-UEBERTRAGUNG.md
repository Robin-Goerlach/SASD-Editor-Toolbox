# Übertragung der FIRST-ED-Command-Prozessoren

## Warum eine eigene Schicht?

Das Handbuch der Turbo Editor Toolbox trennt die Tastatur-Dispatcher von den Prozeduren, die eine Editoroperation tatsächlich ausführen. SASD übernimmt diese Grenze, hält aber historische Eingabekonventionen aus der allgemeinen Editing Engine heraus.

`FirstEdKeyMap` beantwortet, **welcher** semantische Befehl zu einer Tastensequenz gehört. `EditorCommandDispatcher` entscheidet, **welcher Prozessor** ausgeführt wird. `FirstEdCompatibilityProcessor` enthält nur die kleinen Übersetzungsregeln, die wirklich historisch bedingt sind, etwa einbasierte Zeilen-/Spaltennummern und die Modulo-Nummerierung der Fenster.

Das ist auch für die späteren Implementierungen in C++, Java und JavaScript wichtig: Das sprachneutrale Verhalten lässt sich übernehmen, ohne DOS-Tastatur- oder Bildschirmannahmen mitzuschleppen.

## In diesem Meilenstein übertragen

| Historisches Konzept | C#-Implementierung | Kompatibilitätsdetail |
|---|---|---|
| `EditBeginningEndLine` | `MoveBeginningOrEndOfLine` | nicht erste Spalte -> erste Spalte; erste Spalte -> hinter letztes Nicht-Leerzeichen |
| `EditEndLine` | `MoveEndOfLine` | nachgestellte Leerzeichen zählen nicht als Zeilenende |
| `EditGotoLine` | `GoToLine` | Benutzernummer ist einbasiert; hinter EOF wird auf letzte Zeile begrenzt; Spalte bleibt erhalten |
| `EditGotoColumn` | `GoToColumn` | Benutzernummer ist einbasiert; virtuelle Spalten sind erlaubt |
| `EditTopBlock` / `EditBottomBlock` | `GoToBlockBoundary` | kann in ein Fenster mit dem Block-Dokument wechseln |
| `EditWindowUp` | `PreviousWindow` | vom ersten zum letzten Fenster umlaufend |
| `EditWindowDown` | vorhandenes `NextWindow` | vom letzten zum ersten Fenster umlaufend |
| `EditWindowGoto` | `GoToWindow` | einbasierte Fensternummer wird modulo Fensteranzahl interpretiert |
| `EditWindowLink` | `LinkWindows` | vorhandenes Zielfenster wird an das Quelldokument gehängt; View-Zustand bleibt unabhängig |
| linker/rechter Rand | Compatibility Processor | einbasierte Benutzerspalten werden auf nullbasierte interne Werte umgesetzt |
| Tabulatorbreite | Compatibility Processor | positive Breite wird an die Window Options weitergegeben |
| Undo-Limit | Compatibility Processor | nichtnegatives Limit wird an `EditorUndoManager` weitergegeben |

## Moderne Umsetzung von Window Linking

Die Pascal-Version musste Zeiger umhängen und einen aufgegebenen Text-Stream explizit freigeben. In C# kann ein `EditorWindow` sicher an ein anderes `EditorDocument` angehängt werden. Gibt es auf das alte Dokument danach keine Referenz mehr, ist seine Lebensdauer Aufgabe der Garbage Collection. Das sichtbare Verhalten bleibt erhalten: verknüpfte Fenster teilen denselben Text, besitzen aber weiterhin eigene Cursor- und Scrollzustände.

`EditorWindow.AttachDocument` ist deshalb `internal`. Beliebiger UI-Code kann das Dokument eines Fensters nicht unbemerkt austauschen; Linking bleibt eine Editor-/Session-Operation.

## Virtuelle Spalten

Das historische `EditGotoLine` lässt die aktuelle Spalte unverändert, auch wenn die Zielzeile kürzer ist. `EditGotoColumn` kann außerdem hinter den vorhandenen Text springen. SASD erhält dieses Verhalten an der Kompatibilitätsgrenze. Mutierende Operationen dürfen einen virtuellen Cursor später normalisieren, wenn die konkrete Operation dies benötigt.

## Bewusst noch offen

Dieser Schritt behauptet ausdrücklich **nicht**, dass bereits jeder Eintrag der Keymap ausführbar ist. Getrennte nächste Meilensteine bleiben insbesondere:

- Zustand für `FindAgain` und die restlichen Suchen/Ersetzen-Kompatibilitätsdetails;
- Read/Write/Save mit Host-Abfragen auf Basis von `ITextStorage`;
- interaktive Exit-Bestätigung und Rundown-Zustand des Editor-Loops;
- physische Fenstergröße bei Create Window als Aufgabe der Host-/Layout-Schicht;
- exaktes Scroll-/Page-Verhalten, weil das historische Verhalten an dargestellte Bildschirmzeilen gekoppelt ist;
- MicroStar-Menüs, Pop-ups und Hintergrunddruck als spätere Beispiele.

Diese Trennung ist wichtiger als ein schneller monolithischer Port.
