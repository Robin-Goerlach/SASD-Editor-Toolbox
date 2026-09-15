# Interaktiver FIRST-ED-Terminal-Host

## Zweck

Das ursprüngliche FIRST-ED-Programm ist bewusst sehr klein: Es initialisiert die Toolbox und übergibt dann an die Editor-Schleife. Das Handbuch beschreibt beim Start ein Dokument `NONAME` in Fenster 1, eine Statuszeile mit Dateiname sowie Zeilen-/Spaltennummer, WordStar-artige Steuerbefehle, Escape als Undo und Ctrl-K X zum Beenden.

Das .NET-Beispiel folgt jetzt derselben architektonischen Idee, ohne DOS-Terminalannahmen in `Sasd.Editor.Core` einzubauen.

## Host-Pipeline

```text
ConsoleKeyInfo
    -> ConsoleKeyTranslator
    -> EditorKeyStroke
    -> FirstEdKeyMap
    -> EditorInputAction / EditorCommandBinding
    -> ConsoleFirstEdPromptService (falls erforderlich)
    -> EditorCommandDispatcher
    -> EditorSession / Services
    -> EditorViewportBuilder
    -> ConsoleFirstEdRenderer
```

`ConsoleFirstEdHost` implementiert `IEditorInputPump`; dadurch bleibt `EditorSystemLoop` Eigentümer der Hauptschleife. Der Terminaladapter liefert jeweils nur eine Eingabeeinheit. Wartet keine Taste, gibt er die Kontrolle an den kooperativen Scheduler zurück.

## Zuständigkeit für Abfragen

Abfragen bleiben bewusst Aufgabe des Hosts. Die Core-Keymap deklariert `Number`, `TwoNumbers`, `Text`, `FindReplace`, `Character`, `FilePath` oder `Confirmation`; der Terminal-Host setzt diese Metadaten mit `Console.ReadLine` beziehungsweise `Console.ReadKey` um.

Bei Ctrl-K X verlangt der Host das Wort `YES`, bevor `EditorCommandId.Exit` ausgeführt wird. Damit bleibt die im Handbuch beschriebene Trennung zwischen `EditCpExit` und `EditExit` erhalten, ohne UI-Code in den Core zu ziehen.

Eine leere Find-Eingabe wird auf `FindAgain` umgesetzt und nutzt damit den bereits im Core implementierten gemerkten Suchzustand.

## Status und Rendering

Beim Start erzeugt das Beispiel ein leeres unbenanntes Dokument. Die Statuszeile beginnt daher mit Fenster 1 / `NONAME`. Angezeigt werden einbasierte Zeilen-/Spaltenwerte sowie Insert-, Word-Wrap-, Auto-Indent- und Dirty-Zustand.

Moderne Pfeiltasten sowie Home, End, Page Up und Page Down bleiben zusätzliche Aliase. Die historischen Ctrl-K-/Ctrl-O-/Ctrl-Q-Präfixfolgen bleiben weiterhin verfügbar.

## Terminalgrenzen

Console-/Terminal-APIs melden nicht auf jedem Betriebssystem und Terminalemulator alle Steuerkombinationen identisch. Der Adapter normalisiert deshalb nur das, was der Host tatsächlich liefert; der Core bleibt davon unabhängig. Wo die Plattform es zulässt, wird Ctrl-C als normale Eingabe behandelt, damit auch die historische Belegung erreichbar ist.

## Noch offen

Das Beispiel ist jetzt tatsächlich interaktiv, stellt aber jeweils das **aktuelle Fenster** dar und noch nicht die historische vertikal gestapelte physische Fensteraufteilung. `CreateWindow` erzeugt bereits ein logisches Editorfenster; exaktes Aufteilen/Komprimieren von Bildschirmzeilen und gleichzeitiges Multi-Window-Rendering ist der nächste Host-/Layout-Kompatibilitätsschritt.
