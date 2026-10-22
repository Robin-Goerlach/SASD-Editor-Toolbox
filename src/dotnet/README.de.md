# .NET-Implementierung

[English](README.md) | **Deutsch**

Dieses Verzeichnis enthält die aktive **C#/.NET-Referenzimplementierung** der SASD Editor Toolbox.

> **Status:** aktive Referenzimplementierung  
> **Zielframework:** .NET 10 (`net10.0`)  
> **Wiederverwendbare Bibliothek:** `Sasd.Editor.Core`  
> **Sprachübergreifender Verhaltensvertrag:** [`spec/editor-v1.md`](../../spec/editor-v1.md)

Die .NET-Implementierung ist derzeit die am weitesten entwickelte ausführbare Implementierung im Repository. Sie dient zur Entwicklung, Erprobung und Überprüfung der Editorarchitektur, ist aber **nicht selbst die sprachneutrale Spezifikation**. Plattformübergreifend beobachtbares Verhalten wird unter `spec/` festgelegt; gemeinsam nutzbare Konformitätsfälle befinden sich unter `conformance/`.

## Zweck dieses Verzeichnisses

`src/dotnet` enthält ausschließlich **produktiven Quellcode** für die .NET-Implementierung.

Der wiederverwendbare Editor-Kern ist bewusst unabhängig von einer konkreten Benutzeroberfläche. Er kann in unterschiedlichen Anwendungen eingesetzt werden, ohne Abhängigkeiten von Terminal-, WinForms-, WPF-, ASP.NET-, Browser- oder anderen Oberflächen-APIs einzugehen.

Die .NET-Implementierung ist im Repository auf drei Bereiche verteilt:

```text
src/dotnet/       wiederverwendbarer Produktivcode
tests/dotnet/     .NET-Regressions- und Konformitätstests
samples/dotnet/   ausführbare Beispiele und Referenzoberflächen
```

Diese Trennung ist wesentlich: Eine Oberfläche darf vom Core abhängen; **der Core darf niemals von einer Oberfläche abhängen**.

## Aktuelle Verzeichnisstruktur

```text
src/dotnet/
└── Sasd.Editor.Core/
    ├── Abstractions/
    ├── Commands/
    ├── Document/
    ├── Editing/
    ├── Hooks/
    ├── IO/
    ├── Input/
    ├── Model/
    ├── Properties/
    ├── Rendering/
    ├── Scheduling/
    ├── Search/
    ├── Undo/
    ├── Windows/
    └── Sasd.Editor.Core.csproj
```

### Verantwortlichkeiten im Core

| Bereich | Aufgabe |
| --- | --- |
| `Abstractions` | Kleine wiederverwendbare Schnittstellen, beispielsweise für den Textpuffer. |
| `Commands` | Semantische Editorbefehle, Validierung, Befehlsverteilung und FIRST-ED-Kompatibilitätsprozessoren. |
| `Document` | Dokumentzustand, Schnappschüsse und die derzeitige verkettete Zeilenpuffer-Implementierung. |
| `Editing` | Allgemeine Bearbeitungsoperationen, Editorsitzungen, Zeilentopologie und historisches Bearbeitungsverhalten. |
| `Hooks` | Von der Oberfläche unabhängige Erweiterungs-, Fehler- und Statusschnittstellen nach dem Vorbild der historischen Toolbox. |
| `IO` | Moderne Speicherabstraktionen, Dateidienste und der isolierte historische FIRST-ED-Dateicodec. |
| `Input` | Normalisierte Tasteneingaben, Typeahead-Puffer und FIRST-ED-Tastenbelegung. |
| `Model` | Oberflächenunabhängige Zustands- und Wertmodelle, etwa Positionen, Marken, Blöcke und Zeilenattribute. |
| `Rendering` | Oberflächenunabhängige Berechnung des sichtbaren Ausschnitts; die eigentliche Darstellung ist Aufgabe des Hosts. |
| `Scheduling` | Kooperative Ablaufsteuerung, Eingabeverarbeitung und Unterbrechungs-/Abbruchbehandlung. |
| `Search` | Suchen, Ersetzen und Suchoptionen. |
| `Undo` | Transaktionale Rückgängig-Verwaltung und Wiederherstellung des Editorzustands. |
| `Windows` | Editoransichten, ansichtsbezogene Optionen und oberflächenunabhängiges Layout gestapelter Fenster. |

Die Verzeichnisse orientieren sich an fachlichen Verantwortlichkeiten und nicht an bestimmten UI-Technologien. Neuer Produktivcode sollte zunächst einem dieser Bereiche zugeordnet werden, bevor ein weiterer oberster Core-Bereich entsteht.

## Architekturgrenzen

### Der Core bleibt unabhängig von der Benutzeroberfläche

`Sasd.Editor.Core` darf nicht von konkreten UI-Frameworks abhängen und keine direkte Benutzerinteraktion ausführen.

Insbesondere gehören folgende Dinge **nicht** in den Core:

- Dialoge oder Darstellung über `Console`;
- WinForms-Formulare und -Controls;
- WPF-Fenster und -Steuerelemente;
- ASP.NET- oder browserspezifische APIs;
- plattformspezifische Eingabeereignisse;
- Dialogfenster, Message-Boxen und andere UI-Aufgaben des Hosts;
- beispielspezifisches Verhalten ohne allgemein nutzbare Editorfunktion.

Ein Terminal-, WinForms-, WPF-, Web- oder späterer MAUI-Host übersetzt seine Eingaben in Editorbegriffe, ruft die öffentliche Core-API auf und stellt den resultierenden Editorzustand dar.

### Dokumente und Fenster sind unterschiedliche Konzepte

Der Text gehört zu einem `EditorDocument`.

Ein `EditorWindow` ist eine **Ansicht auf ein Dokument**. Es verwaltet beispielsweise die Cursorposition, den sichtbaren Bereich und ansichtsbezogene Bearbeitungsoptionen. Mehrere Fenster können daher dasselbe Dokument anzeigen.

Dokument- und Fensteridentität dürfen nicht zusammengeführt werden, nur weil eine einfache Anwendung zunächst ein Dokument in einem einzigen Fenster darstellt.

### Allgemeine Bearbeitung und historische Kompatibilität bleiben getrennt

Das Projekt verwendet das Verhalten der historischen **Borland Turbo Editor Toolbox 1.0 / FIRST-ED** als Referenz für die V1-Kompatibilität.

Allgemein wiederverwendbares modernes Editorverhalten gehört in Dienste wie `EditorEngine`. Ungewöhnliche historische Regeln werden dagegen in ausdrücklich gekennzeichneten Kompatibilitätskomponenten isoliert, beispielsweise FIRST-ED-Prozessoren, Tastenbelegung und Legacy-Dateicodec.

So werden DOS-spezifische oder historische Konventionen nicht unbeabsichtigt zu Anforderungen an moderne Editoranwendungen.

### Strukturänderungen berücksichtigen die Zeilentopologie

Das Einfügen, Entfernen, Verbinden oder Umstrukturieren logischer Textzeilen kann mehr verändern als nur den Text. Betroffen sein können:

- Cursorpositionen in verbundenen Fenstern;
- Anker des sichtbaren Textausschnitts;
- Zeilenmarken;
- Blockgrenzen;
- Rückgängig- und Navigationszustände.

`EditorLineTopology` ist die zentrale Stelle, um diese Auswirkungen koordiniert zu behandeln. Neue strukturelle Operationen müssen die Zusammenhänge prüfen, statt Zeilennummern an verschiedenen Stellen unabhängig voneinander zu korrigieren.

### Moderne Dateiverarbeitung und Legacy-Kompatibilität bleiben getrennt

Die moderne Dokumentpersistenz verwendet die Speicherabstraktion um `ITextStorage` und `FileTextStorage`.

Die historische bytegenaue FIRST-ED-Kompatibilität ist hinter `IEditorFileCodec` und `FirstEdLegacyFileCodec` isoliert.

Das historische Dateiformat darf nicht zum allgemeinen Speicherformat moderner Anwendungen werden.

### Eingaben werden vor der Befehlsverarbeitung normalisiert

Der beabsichtigte, vom Host unabhängige Eingabeweg sieht vereinfacht so aus:

```text
Eingabe des Hosts
    ↓
EditorKeyStroke
    ↓
EditorTypeaheadBuffer
    ↓
Tastenzuordnung / FirstEdKeyMap
    ↓
EditorCommandRequest
    ↓
EditorCommandDispatcher
    ↓
Editor-Dienste
```

Der Host übersetzt seine plattformspezifischen Eingabeereignisse in normalisierte Editorereignisse. Der Core muss nicht wissen, ob die ursprüngliche Eingabe aus einem Terminal, aus WinForms oder WPF, aus einem Testskript oder einer anderen Quelle stammt.

### Kooperative Ablaufsteuerung

Die Scheduling-Schicht erhält die historische Idee kurzer kooperativer Hintergrundarbeiten, modernisiert aber die Anbindung an den Host.

Länger laufende Operationen müssen unterbrechbar bleiben und dürfen die Editorhauptschleife nicht unbemerkt blockieren. Plattformseitiges Cancellation und der historische Editor-Abbruch werden bewusst als unterschiedliche Konzepte behandelt.

## Einordnung im mehrsprachigen Projekt

SASD Editor Toolbox soll einen gemeinsamen Verhaltensvertrag mit **unabhängigen Implementierungen in unterschiedlichen Programmiersprachen** besitzen.

Die derzeitigen Implementierungsbereiche:

```text
src/dotnet/   aktive C#/.NET-Referenzimplementierung
src/cpp/      Gerüst für die C++-Implementierung
src/java/     Gerüst für die Java-Implementierung
```

Die .NET-Version darf als ausführbare Referenz für andere Implementierungen dienen. Diese sollen aber **nicht** mechanisch die C#-Klassenhierarchie übersetzen und dürfen zur Laufzeit nicht von .NET-Binärdateien abhängig sein.

Das sprachübergreifende Prinzip lautet:

> **Gemeinsam ist das Verhalten, nicht die Implementierung.**

Portables Verhalten wird in [`spec/editor-v1.md`](../../spec/editor-v1.md) definiert und nach Möglichkeit durch gemeinsame [Konformitätstests](../../conformance/README.md) überprüfbar gemacht.

## Umfang der V1-Kompatibilität

Das V1-Ziel orientiert sich funktional an der historischen Dokumentation zur Turbo Editor Toolbox 1.0.

Es handelt sich um eine **eigenständige Clean-Room-Neuimplementierung**:

- Die historische Dokumentation dient dazu, beobachtbares Verhalten zu bestimmen.
- Dieses Verhalten wird unabhängig beschrieben und neu implementiert.
- Historischer Quellcode wird weder kopiert noch zeilenweise übertragen.
- DOS-interne Implementierungsmechanismen werden durch moderne Abstraktionen ersetzt, sofern sie nicht zum beobachtbaren Verhalten gehören.
- Historische Bezeichnungen dienen bei Bedarf der nachvollziehbaren Kompatibilitätsdokumentation.

Die .NET-Implementierung besitzt derzeit den größten Funktions- und Testumfang. Der Status „aktive Referenzimplementierung“ bedeutet jedoch **nicht**, dass sämtliche historischen Funktionen bereits die abschließende V1-Freigabeprüfung bestanden haben.

Der jeweils aktuelle Stand ist in der Kompatibilitätsdokumentation vermerkt.

## Bauen und Testen

Der maßgebliche .NET-Build erfolgt über die Solution im **Repository-Hauptverzeichnis**.

Voraussetzung:

- .NET 10 SDK

Befehle:

```bash
dotnet restore Sasd.Editor.Toolbox.slnx

dotnet build Sasd.Editor.Toolbox.slnx \
  --configuration Release \
  --no-restore

dotnet test Sasd.Editor.Toolbox.slnx \
  --configuration Release \
  --no-build
```

Die Solution umfasst derzeit:

- `src/dotnet/Sasd.Editor.Core/Sasd.Editor.Core.csproj`
- `tests/dotnet/Sasd.Editor.Core.Tests/Sasd.Editor.Core.Tests.csproj`
- `samples/dotnet/Sasd.Editor.FirstEd.Sample/Sasd.Editor.FirstEd.Sample.csproj`

Die Repository-Struktur und die gemeinsamen Konformitätsmetadaten werden unabhängig davon geprüft:

```bash
python3 tools/verify_repository_contract.py
```

## FIRST-ED-Referenzbeispiel starten

Das aktuelle .NET-Beispiel ist eine terminalbasierte Anwendung, die die öffentliche Core-API verwendet.

Aus dem Repository-Hauptverzeichnis:

```bash
dotnet run --project samples/dotnet/Sasd.Editor.FirstEd.Sample/Sasd.Editor.FirstEd.Sample.csproj
```

Für die nicht destruktive Entwicklerdemo:

```bash
dotnet run --project samples/dotnet/Sasd.Editor.FirstEd.Sample -- --demo
```

Die verfügbaren Startoptionen:

```bash
dotnet run --project samples/dotnet/Sasd.Editor.FirstEd.Sample -- --help
```

Das Beispiel liegt bewusst **außerhalb** von `src/dotnet`. Es zeigt, wie eine Anwendung die Core-Bibliothek verwendet, ohne terminalbezogenes Verhalten zu einer Abhängigkeit des Core zu machen.

## Wohin gehören neue Änderungen?

| Art der Änderung | Verzeichnis |
| --- | --- |
| Wiederverwendbares Editorverhalten | `src/dotnet/Sasd.Editor.Core/` |
| .NET-Regressionstest | `tests/dotnet/Sasd.Editor.Core.Tests/` |
| Sprachübergreifend verbindliches Verhalten | `spec/` |
| Gemeinsamer maschinenlesbarer Konformitätsfall | `conformance/` |
| Terminal-, Demo- oder Host-Funktion | `samples/dotnet/` |
| Architektur- oder Kompatibilitätserläuterung | `docs/en/` und gegebenenfalls das passende Dokument unter `docs/de/` |
| Übergreifende Entwicklungsregeln | `AGENTS.md` |

Bei sprachübergreifend relevantem Verhalten empfiehlt sich diese Reihenfolge:

1. Das gewünschte beobachtbare Verhalten präzisieren.
2. Die portable Spezifikation aktualisieren, soweit mehrere Sprachen betroffen sind.
3. Nach Möglichkeit einen gemeinsamen Konformitätsfall ergänzen oder anpassen.
4. Deterministische .NET-Regressionstests erstellen.
5. Das Verhalten im Core implementieren.
6. Die zugehörige englische und deutsche Dokumentation aktualisieren.
7. Repository-Vertragsprüfung und vollständigen .NET-Release-Build mit Tests ausführen.

## Entwicklungsgrundsätze

Für die Arbeit an dieser Implementierung gelten insbesondere:

- Produktivcode, APIs und Bezeichner werden auf Englisch geschrieben.
- Nullable Reference Types und zeitgemäße C#-Sicherheitsregeln bleiben erhalten.
- Öffentliche APIs erhalten sinnvolle XML-Dokumentationskommentare, wenn ihr Vertrag nicht offensichtlich ist.
- Kommentare erklären vor allem **warum** etwas so umgesetzt wurde, welche Invarianten gelten und welche Kompatibilitätsgrenzen oder Abwägungen bestehen.
- Korrektheit und Verständlichkeit haben zunächst Vorrang vor spekulativer Optimierung.
- Benutzeroberflächen- und Plattformcode bleiben außerhalb des wiederverwendbaren Core.
- Dokument- und Fensterzustände bleiben getrennt.
- Bei strukturellen Textänderungen werden Topologie, Marken, Blöcke und Undo mitgeprüft.
- Gültige virtuelle Cursorspalten werden nicht ohne ausdrücklichen Grund begrenzt.
- Moderne Persistenz bleibt unabhängig vom historischen Dateicodec.
- Verhaltensänderungen erhalten deterministische, ohne Benutzeroberfläche ausführbare Regressionstests.
- Tests werden nicht abgeschwächt, nur damit sie zu einem zufälligen Implementierungsdetail passen.

## Weiterführende Dokumentation

- [Repository-Übersicht](../../README.md)
- [Sprachneutraler V1-Editorvertrag](../../spec/editor-v1.md)
- [Gemeinsame Konformitätsfälle](../../conformance/README.md)
- [.NET-Tests](../../tests/dotnet/Sasd.Editor.Core.Tests)
- [.NET-FIRST-ED-Beispiel](../../samples/dotnet/Sasd.Editor.FirstEd.Sample)
- [Architekturdokumentation (EN)](../../docs/en/ARCHITECTURE.md)
- [Mehrsprachige Entwicklung (EN)](../../docs/en/MULTI-LANGUAGE-DEVELOPMENT.md)
- [Borland-V1-Kompatibilitätsstatus (EN)](../../docs/en/BORLAND-V1-COMPATIBILITY.md)
- [Entwicklungsregeln des Repositories](../../AGENTS.md)

## Zusammenfassung

`src/dotnet` ist die Heimat des produktiven Quellcodes für die .NET-Implementierung der SASD Editor Toolbox.

Die Aufgabe dieses Bereichs besteht **nicht** darin, ein bestimmtes Editorfenster bereitzustellen. Stattdessen stellt er die wiederverwendbare, testbare Editortechnik bereit, auf der ganz unterschiedliche Fenster und Anwendungen aufbauen können.

Das ist der entscheidende Architekturgrundsatz: **Die Editorlogik gehört hierher; die Benutzeroberfläche wird darum herum aufgebaut.**
