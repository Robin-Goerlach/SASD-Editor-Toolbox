# .NET-Beispiele

**Sprachen:** [English](README.md) · Deutsch

Dieses Verzeichnis enthält ausführbare .NET-Hosts und Entwicklerbeispiele für
die [SASD Editor Toolbox](../../README.md). Die Beispiele zeigen, wie eine
Anwendung Benutzereingaben, Dialoge, Darstellung, Persistenz und
anwendungsspezifische Abläufe mit dem wiederverwendbaren Editor-Kern verbindet.

Die Beispiele sind bewusst **Hosts und keine alternativen
Editor-Implementierungen**. Wiederverwendbares Editorverhalten gehört nach
[`src/dotnet/Sasd.Editor.Core`](../../src/dotnet/Sasd.Editor.Core/);
Konsolen-, Desktop- und anderer oberflächenspezifischer Code gehört hierher
oder in ein anderes Host-Projekt.

## Verfügbares Beispiel

| Projekt | Typ | Zweck |
| --- | --- | --- |
| [`Sasd.Editor.FirstEd.Sample`](./Sasd.Editor.FirstEd.Sample/) | Interaktive Konsolenanwendung | Demonstriert die öffentliche .NET-Editor-API durch einen FIRST-ED-kompatiblen Terminal-Host, einschließlich Bearbeitung, Suche, Blöcken, Marken, gestapelten Fenstern, historischer Dateiverarbeitung, einem von MicroStar inspirierten Menü und kooperativem Hintergrunddruck. |

Das FIRST-ED-Beispiel ist derzeit der einzige ausführbare .NET-Host in diesem
Verzeichnis. Ein grafisches Beispiel kann später hinzukommen; ein
WinForms-, WPF-, MAUI- oder Web-Host ist hier noch nicht implementiert.

## Voraussetzungen

- [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0)
- ein interaktives Terminal für den eigentlichen Editor
- Windows, Linux oder eine andere Plattform mit einer kompatiblen .NET-Konsole

Alle folgenden Befehle werden im Wurzelverzeichnis des Repositorys ausgeführt.

## Schnellstart

Eine vorbereitete, bearbeitbare Demonstration starten:

```bash
dotnet run --project samples/dotnet/Sasd.Editor.FirstEd.Sample -- --demo
```

Mit einem leeren Dokument im Arbeitsspeicher starten:

```bash
dotnet run --project samples/dotnet/Sasd.Editor.FirstEd.Sample
```

Die unterstützten Startoptionen anzeigen:

```bash
dotnet run --project samples/dotnet/Sasd.Editor.FirstEd.Sample -- --help
```

Die Option `--help` funktioniert auch bei umgeleiteter Ein- oder Ausgabe. Die
Editor-Modi benötigen ein echtes interaktives Terminal.

## Empfohlene erste Sitzung

Der Modus `--demo` verwendet eine echte `EditorSession` und ein normales
bearbeitbares Dokument; er ist keine Attrappe. Er startet ohne Dateinamen, ohne
Datenträgerzugriff und mit einem leeren Undo-Verlauf.

Dieser kurze Rundgang zeigt die wichtigsten Funktionen:

1. Mit den Pfeiltasten sowie Pos1, Ende, Bild auf und Bild ab navigieren.
2. Text eingeben und **Escape** drücken, um die letzte
   Bearbeitungstransaktion rückgängig zu machen.
3. **Strg-Q**, danach **F** drücken, `WIDGET` eingeben und mit Enter suchen.
4. **Strg-K**, danach **B** drücken, um den Anfang eines ganzzeiligen Blocks zu
   markieren. Den Cursor bewegen, mit **Strg-K**, danach **K** das Blockende
   setzen und den Block mit **Strg-K**, danach **C** kopieren.
5. Mit **Strg-K**, danach **1** die Marke 1 setzen; mit **Strg-Q**, danach **1**
   zu ihr zurückkehren.
6. In einem ausreichend hohen Terminal mit **Strg-O**, danach **O** ein
   gestapeltes Fenster erzeugen.
7. Mit **F10** das von MicroStar inspirierte Beispielmenü öffnen. Mit den
   Pfeiltasten navigieren und mit Enter bestätigen oder direkt einen
   angezeigten Befehlsbuchstaben verwenden.
8. Mit **Strg-K**, danach **X** und der Eingabe von `YES` den Editor verlassen.

Die Demo enthält ein gespeichertes ESC-Steuerzeichen, auf das `[2J` folgt.
Der Renderer muss es ungefährlich in Caret-Schreibweise darstellen und darf es
niemals als Terminalbefehl zum Löschen des Bildschirms ausführen.

## Wichtige Befehle

FIRST-ED-Befehle verwenden nacheinander eingegebene Steuerpräfixe.
**Strg-K, X** bedeutet beispielsweise: Strg-K drücken, loslassen und danach X
drücken.

| Tasten | Aktion |
| --- | --- |
| Pfeiltasten, Pos1, Ende, Bild auf, Bild ab | Moderne Navigationsalternativen |
| Enter | Neue Zeile oder Bewegung im Überschreibmodus |
| Strg-N | Zeile einfügen |
| Escape | Rückgängig |
| Strg-Q, F | Suchen |
| Strg-L | Nächstes Vorkommen suchen |
| Strg-K, B / Strg-K, K | Blockanfang / Blockende setzen |
| Strg-K, C / Strg-K, V / Strg-K, Y | Block kopieren / verschieben / löschen |
| Strg-K, 1..9 / Strg-Q, 1..9 | Nummerierte Marke setzen / aufsuchen |
| Strg-O, O / Strg-O, Y | Fenster erzeugen / löschen |
| Strg-O, J | Zwei Fenster mit demselben Dokument verbinden |
| Strg-K, S | Aktuelles Dokument speichern |
| Strg-K, W / Strg-K, R | Datei schreiben / lesen |
| Strg-U | Abbruch oder Unterbrechung anfordern, sofern unterstützt |
| F10 | Den von MicroStar inspirierten Host-Menüdialog öffnen |
| Strg-K, X | Nach Bestätigung mit `YES` beenden |

Die vollständige Übersicht aller primären sowie der Strg-K-, Strg-O- und
Strg-Q-Bindungen steht in der
[FIRST-ED-Befehlsübersicht](../../docs/de/FIRST-ED-BEFEHLE.md).

## Warnung zum Dateiformat

Das Konsolenbeispiel verwendet absichtlich den historischen
FIRST-ED-8-Bit-Dateicodec. Insbesondere bewahrt es die
`0x8D`-Markierung für umgebrochene Zeilen. Dies unterscheidet sich von der
modernen UTF-8-Persistenz-API, die `FileTextStorage` bereitstellt.

Bei Experimenten mit **Speichern**, **Datei schreiben**, Blockexport oder den
F10-Dateibefehlen gilt:

- nur entbehrliche Testdateien verwenden;
- nicht davon ausgehen, dass beliebiger Unicode-Text durch den historischen
  Codec dargestellt werden kann;
- beachten, dass **Datei schreiben** exportiert, ohne das Dokument mit dem
  Zielpfad zu verbinden;
- beachten, dass **Speichern** ein unbenanntes Dokument mit dem gewählten Pfad
  verbindet und spätere Speichervorgänge direkt dorthin schreibt;
- jede Rückfrage zum Überschreiben sorgfältig prüfen, bevor sie bestätigt wird.

Das `--demo`-Dokument wird nicht automatisch geschrieben.

## Architektur

Das Beispiel folgt derselben Host-neutralen Verarbeitungskette, die auch von
anderen Terminal- oder grafischen Anwendungen erwartet wird:

```text
ConsoleKeyInfo
    -> ConsoleKeyTranslator
    -> EditorKeyStroke
    -> Editor-eigener Typeahead-Puffer
    -> FirstEdKeyMap
    -> semantischer EditorCommandRequest
    -> EditorCommandDispatcher
    -> EditorSession und Editor-Dienste
    -> Viewport-/Status-Projektion
    -> ConsoleFirstEdRenderer
```

Wichtige Grenzen:

- `Sasd.Editor.Core` ist für Dokumente, Fenster, Bearbeitungsdienste,
  semantische Befehle, Undo, Topologie, Ablaufsteuerung und
  Persistenzabstraktionen zuständig.
- Das Beispiel ist für `Console`-Zugriffe, Übersetzung von Terminaltasten,
  Eingabedialoge, Farben, Cursorpositionierung, sichere Zeilenprojektion und
  Anwendungsmenüs zuständig.
- Das Erfassen von Eingaben ist von der Befehlszuordnung getrennt. Die
  Tastenzuordnung beschreibt benötigte Argumente, zeigt aber niemals selbst
  einen Dialog an.
- Tastatureingaben und eingespeiste Makro-/Typeahead-Eingaben durchlaufen
  dieselbe normalisierte Editor-Verarbeitungskette.
- MicroStar-spezifische Menüs, Dateiauswahl, Blockdateibefehle und
  Druck-in-Datei-Abläufe bleiben Funktionen des Beispiel-Hosts; der
  wiederverwendbare Kern hängt nicht von ihnen ab.
- Mehrere Fenster dürfen dasselbe Dokument verwenden und behalten dabei
  unabhängige Cursor- und Viewport-Zustände.

Die ausführbare Beispieldatei darf nicht wie eine wiederverwendbare Bibliothek
referenziert werden. Anwendungen sollen
[`Sasd.Editor.Core`](../../src/dotnet/Sasd.Editor.Core/) referenzieren und
einen eigenen Host-Adapter implementieren.

## Orientierung im Projekt

Die wichtigsten Einstiegspunkte in
[`Sasd.Editor.FirstEd.Sample`](./Sasd.Editor.FirstEd.Sample/) sind:

| Datei oder Gruppe | Verantwortung |
| --- | --- |
| `Program.cs` | Wertet den Startmodus aus, erzeugt die öffentliche Editor-Sitzung und startet die Editor-Systemschleife. |
| `FirstEdSampleStartup.cs` | Definiert und verarbeitet den kleinen Kommandozeilenvertrag. |
| `FirstEdDemoScenario.cs` | Erzeugt die sichere, bearbeitbare Demonstration im Arbeitsspeicher. |
| `ConsoleFirstEdHost.cs` | Koordiniert Eingaben, Dialoge, semantische Befehlsausführung, Menüs, Dateiaktionen und Hintergrunddruck. |
| `ConsoleKeyTranslator.cs` | Übersetzt plattformspezifische Konsolenereignisse in Host-neutrale Editor-Tasteneingaben. |
| `ConsoleFirstEdPromptService.cs` und Dialogrichtlinien | Wandeln Terminalantworten in typisierte semantische Befehlsargumente um. |
| `ConsoleFirstEdRenderer.cs` | Stellt gestapelte Fenster, Statuszeilen, Dialoge und den aktiven Cursor dar. |
| `ConsoleFirstEdRowProjection.cs` | Projiziert Steuerzeichen und Unicode sicher auf physische Terminalzeilen. |
| `ConsoleMicroStar*.cs` und `MicroStar*.cs` | Implementieren ausschließlich für das Beispiel vorgesehene Menüs, Dateiauswahl, Blockdateioperationen und kooperativen Druck. |

Alle Beispieltypen bleiben intern, solange kein echter öffentlicher
Host-Vertrag erforderlich ist. Öffentliche wiederverwendbare Verträge gehören
in das Core-Projekt.

## Bauen und testen

Die vollständige .NET-Referenzimplementierung wiederherstellen, bauen und
testen:

```bash
dotnet restore Sasd.Editor.Toolbox.slnx
dotnet build Sasd.Editor.Toolbox.slnx --configuration Release --no-restore
dotnet test Sasd.Editor.Toolbox.slnx --configuration Release --no-build
```

Die Repository-weite, sprachneutrale Struktur und die Metadaten prüfen:

```bash
python3 tools/verify_repository_contract.py
```

Die automatisierten Tests decken die ohne Oberfläche prüfbaren Richtlinien des
Beispiels und seine Integration mit echten semantischen Editorbefehlen ab. Sie
können die physische Tastenübermittlung eines Terminals, Zeichenzellenbreiten,
Farben oder die interaktive Cursorpositionierung nicht bestätigen. Diese
Bereiche benötigen weiterhin manuelle Kurztests auf repräsentativen Terminals.

## Ein weiteres .NET-Beispiel ergänzen

Ein neues Beispiel soll:

1. `src/dotnet/Sasd.Editor.Core` statt eines anderen Beispiels referenzieren;
2. UI-Framework- und Plattform-APIs außerhalb des Kerns halten;
3. `EditorSession`, semantische Befehlsausführung und öffentliche
   Persistenzabstraktionen wiederverwenden, statt eine weitere Editor-Engine zu
   implementieren;
4. Dialoge, Rendering und anwendungsspezifische Abläufe im Host halten;
5. nach Möglichkeit deterministische, oberflächenlose Tests für
   Host-Richtlinien ergänzen;
6. dokumentieren, ob ein Verhalten historische Kompatibilität, eine moderne
   Host-Richtlinie oder anwendungsspezifisches Verhalten darstellt;
7. nur dann in `Sasd.Editor.Toolbox.slnx` aufgenommen werden, wenn es am
   normalen .NET-Build- und Test-Gate teilnehmen soll.

## Weiterführende Dokumentation

- [Interaktiver FIRST-ED-Terminal-Host](../../docs/de/INTERAKTIVER-FIRST-ED-HOST.md)
- [Entwicklerdemo und Abnahme der öffentlichen API](../../docs/de/DEMO-EDITOR-UND-PUBLIC-API-ABNAHME.md)
- [FIRST-ED-Befehlsübersicht](../../docs/de/FIRST-ED-BEFEHLE.md)
- [Architektur](../../docs/de/ARCHITECTURE.md)
- [Sprachneutraler V1-Vertrag](../../spec/editor-v1.md)
- [Arbeitsanweisungen des Repositorys](../../AGENTS.md)

## Status

Dieses Beispiel ist eine ausführbare Entwicklerreferenz und Abnahmehilfe für
die aktuelle .NET-Implementierung. Für sich allein belegt es weder vollständige
historische Gleichwertigkeit noch die abschließende manuelle Terminalabnahme
oder ein veröffentlichtes V1.0-Paket.
