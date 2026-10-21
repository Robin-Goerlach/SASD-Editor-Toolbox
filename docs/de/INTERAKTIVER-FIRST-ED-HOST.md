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

**Ctrl-K S (Datei speichern)** besitzt im Terminalhost jetzt eine zustandsabhängige Abfrage. Hat ein neu angelegtes Dokument `NONAME` noch keinen zugeordneten Dateipfad, fragt der Host nach **Save as file path**. Escape/Ctrl-U sowie leere oder nur aus Leerzeichen bestehende Antworten brechen ohne Dispatch und Datei-I/O ab. Nach erfolgreichem Speichern benutzt dieselbe Tastenkombination den zugeordneten Dateipfad ohne erneute Abfrage. Diese Entscheidung ist **dokumentbezogen**, nicht ein global gemerkter Dateiname. Die kleine `ConsoleFirstEdSavePromptPolicy` verwendet die gemeinsame hostneutrale Dateinamen-Auflösung und besitzt Headless-Regressionstests mit tatsächlichem semantischen Save-Dispatch und einem protokollierenden Test-Codec. Das ist eine bewusst dokumentierte **moderne Host-Regel**, keine Behauptung über den genauen DOS-Dialog `EditCpFileSave`. Editor-Kern, direktes Write File und Unicode-Persistenz bleiben unverändert.

Die interaktive Zeilenabfrage besitzt nun ein kleines, separat testbares **Eingabemodell im Beispiel-Host**. Escape und physisches Ctrl-U brechen ab, ohne einen Editorbefehl zu erzeugen; Enter bestätigt auch eine bewusst leere Eingabe. Passt die gesamte Abfrage nicht in die Terminalzeile, zeigt der Host die **zuletzt eingegebenen Zeichen**, übergibt aber weiterhin den vollständigen unveränderten Wert an die hostneutrale Befehlsauflösung. Das ist eine moderne Terminal- und Abbruchregel, keine behauptete DOS-Bildschirmkompatibilität. Automatisierte Eingabe-/Projektionsprüfungen ersetzen keinen manuellen Terminal-Smoke-Test.

## Status und Rendering

Beim Start erzeugt das Beispiel ein leeres unbenanntes Dokument. Die Statuszeile beginnt daher mit Fenster 1 / `NONAME`. Angezeigt werden einbasierte Zeilen-/Spaltenwerte sowie Insert-, Word-Wrap-, Auto-Indent- und Dirty-Zustand.

Moderne Pfeiltasten sowie Home, End, Page Up und Page Down bleiben zusätzliche Aliase. Die historischen Ctrl-K-/Ctrl-O-/Ctrl-Q-Präfixfolgen bleiben weiterhin verfügbar.

## Terminalgrenzen

Console-/Terminal-APIs melden nicht auf jedem Betriebssystem und Terminalemulator alle Steuerkombinationen identisch. Der Adapter normalisiert deshalb nur das, was der Host tatsächlich liefert; der Core bleibt davon unabhängig. Wo die Plattform es zulässt, wird Ctrl-C als normale Eingabe behandelt, damit auch die historische Belegung erreichbar ist.

## Aktuelle Darstellung gestapelter Fenster

Das Beispiel rendert inzwischen alle übereinander angeordneten `EditorWindowFrame`-Bereiche mit eigenen Status- und Textzeilen. Das aktuelle Fenster besitzt den physischen Terminal-Cursor; `EditorSession.EnsureAllWindowCursorsHorizontallyVisible(width)` stellt **in jeder View** die horizontale Sichtbarkeit sicher, bevor gerendert wird.

Damit bleiben die historische All-Fenster-Regel und die fensterspezifischen absoluten Zeilennummern Teil des wiederverwendbaren Editor-Modells. Dynamisches Terminal-Resize und plattformspezifische Tasten bleiben Host-Policy; MicroStar-Pulldown-Menüs und Pop-ups sind spätere Host-Beispiele.

## Überschreibbestätigung bei neu ausgewählten Ausgabedateien

Wenn Ctrl-K S ein neues `NONAME`-Dokument erstmals unter einem **vom Benutzer gewählten Dateinamen** speichert oder Ctrl-K W Text in eine **neu benannte Ausgabedatei** schreibt, prüft der Terminalhost jetzt zunächst, ob das Ziel existiert. Ist das der Fall, muss zusätzlich ausdrücklich `YES` bestätigt werden, bevor überhaupt ein Dateischreibbefehl ausgeführt wird. Escape, Ctrl-U, leere Eingabe und jede andere Antwort brechen ohne Dispatch und Datei-I/O ab. Die bereits vorhandene hostneutrale Auswertung prüft `YES`; die kleine, ausschließlich zum Konsolenbeispiel gehörende `ConsoleFirstEdOverwritePolicy` erhält nach Bestätigung den ursprünglichen Dateinamen und die sonstigen Befehlsargumente. Headless-Tests prüfen dies anhand echter temporärer Dateien und des semantischen Dispatchers.

Ein wiederholtes Save auf einen dem Dokument **bereits zugeordneten Dateipfad** bleibt direkt ausführbar; diesen Pfad hat der Nutzer zuvor ausgewählt. Direktes Write File verändert weiterhin weder Dateizuordnung noch Dirty-Flag des Dokuments. Read File benötigt keine Überschreibbestätigung. Der wiederverwendbare Editor-Kern und die historischen Unterschiede der Dateibefehle bleiben unverändert.

Das ist eine **moderne Schutzabfrage des Terminalhosts**, keine Behauptung über den originalen DOS-Dialog von 1985 und keine atomare Dateisperre: Nach `File.Exists` kann ein anderer Prozess die Datei weiterhin ändern oder neu anlegen. Andere Oberflächen müssen eine eigene Überschreib-Policy vorsehen.

## Historische leere Tabulator-Abfrage (Ctrl-K T)

Der Handbuchbefehl `EditCptabdef` hat gegenüber normalen Zahlenabfragen eine Besonderheit: Eine **bewusst leere** Antwort setzt den Tabulatorabstand auf die aktuelle angezeigte Cursor-Spalte minus eins. Der FIRST-ED-Terminalhost bildet dies jetzt in der separat testbaren `ConsoleFirstEdTabWidthPromptPolicy` ab. Die Cursor-Spalte wird im .NET-Modell bereits nullbasiert gespeichert und ist daher genau der benötigte Wert. Null/Escape bleibt ein **Abbruch**; eine ausdrücklich eingegebene Zahl verwendet weiterhin die gemeinsame numerische Befehlsauflösung. In der ersten Spalte ergäbe die historische Formel null; der Host verwirft den ungültigen Wert, statt die bestehende Tabulatorbreite zu beschädigen.

Das ist historisches Host-Prompt-Verhalten mit einer dokumentierten modernen Grenze. Während der ursprüngliche Tabulatorabstand global war, verwendet SASD bewusst `TabSize` je Fenster. Der Core-Dispatcher und alle anderen numerischen Prompts bleiben unverändert. Die Headless-Tests bestätigen keine physische DOS-Tastatur- oder Bildschirmdarstellung.


## Ctrl-P / historische Steuerzeichenabfrage

Das Handbuch von 1985 (EditInsertCtrlChar, gedruckte S. 182) beschreibt **ein nachfolgendes Zeichen, das vor dem Einfügen in einen ASCII-Steuercode umgewandelt wird**. Der Terminalhost übergab zuvor ein eingegebenes `A` als Buchstaben `A` statt als ASCII 0x01. Die neue, unabhängig testbare `ConsoleFirstEdControlCharacterPromptPolicy` übersetzt ASCII A-Z/a-z zu 0x01..0x1A sowie `@ [ \\ ] ^ _` zu 0x00 bzw. 0x1B..0x1F. Von der Konsole bereits als Steuerzeichen gemeldete Ctrl+Buchstabe-Eingaben bleiben unverändert. Ein gewöhnliches U fordert 0x15 an; physisches Ctrl-U und Escape brechen ab. Andere Tasten, Nicht-ASCII-Eingaben, Enter und Backspace führen ebenfalls nicht zum Einfügen gewöhnlichen Texts.

Der Renderer liest genau ein ConsoleKeyInfo; die reine Host-Policy wandelt es um; die allgemeine Befehlsauflösung erhält nur den fertig übersetzten Code. Der Prompt erklärt jetzt die unterstützten Zeichen. Headless-Tests sichern Zuordnung, Abbruch, unzulässige Eingaben, tatsächliches Einfügen und Undo ab. Der bisher verknüpfte portable Kernel-Test prüft ein **bereits aufgelöstes** Steuerzeichen, nicht die physische Konsole. Eine DOS-Scancode- oder Typeahead-Parität wird nicht behauptet; reale Windows-/Linux-Terminaltests bleiben offen.
