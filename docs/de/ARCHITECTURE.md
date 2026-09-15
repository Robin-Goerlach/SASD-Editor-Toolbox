# Architektur

## Ziel

Die SASD Editor Toolbox ist ein **einbettbarer Editor-Kern** und nicht nur ein einzelner Desktop-Editor. Deshalb werden Text, Ansichten/Fenster, Editieroperationen, Befehle, Persistenz, Darstellung, Erweiterungspunkte und Hintergrundaufgaben getrennt.

Damit kann derselbe Kern später aus WPF, WinForms, Terminal- und Web-Oberflächen genutzt werden, ohne dass die Kernbibliothek von einem UI-Framework abhängt.

## Textspeicher

Die historische Toolbox arbeitete mit verketteten Zeilendeskriptoren. Die erste SASD-Implementierung übernimmt die zeilenorientierte Semantik mit `LinkedLineTextBuffer`, kapselt sie aber hinter `ITextBuffer`. Dadurch können wir später für große Dateien auf Piece Table oder Rope wechseln, ohne Befehle, Fensterlogik oder Anwendungen neu entwerfen zu müssen.

## Dokumente und Fenster

`EditorDocument` besitzt Text, Dateiinformationen, Dirty-State und Version. `EditorWindow` ist eine Sicht auf ein Dokument und besitzt Cursor, Scrollposition und Modi. `EditorSession` verwaltet die offenen Fenster. Mehrere Fenster können dasselbe Dokument anzeigen; das entspricht funktional den historischen „linked windows“.

## Editierkern

`EditorEngine` kennt Editieroperationen, aber keine konkreten Tasten. WordStar-, Visual-Studio-, Vim- oder SASD-spezifische Tastenzuordnungen gehören in eine darüberliegende Schicht.

## Undo

Die erste Implementierung verwendet vollständige Snapshots. Das ist noch nicht die endgültige Performance-Lösung, schafft aber eine saubere Grenze für spätere Delta- oder Piece-Table-Varianten.

## Hooks

`IEditorHooks` überträgt die historischen Erweiterungspunkte in eine moderne, typisierte und async-fähige API: Befehlsfilter, Fehlerbehandlung, Statusanpassung, Ersetzen-Entscheidung und kooperative Hintergrundarbeit.

## Darstellung

Statt direkt in einen 80x25-Bildschirm beziehungsweise Videospeicher zu schreiben, liefert SASD ein `EditorViewport`-Modell. Die Oberfläche entscheidet über Schrift, Farben, Blockdarstellung, Cursor und Bildschirmkoordinaten.

## Mehrere Programmiersprachen

Spätere Implementierungen liegen als gleichberechtigte Verzeichnisse neben `src/dotnet`. Die sprachneutrale Semantik wird unter `spec/` gepflegt.

## Bewusst später

Als nächste V1-Schritte folgen insbesondere die vollständige historische WordStar-Tastenzuordnung mit Prefix-Dispatcher, ein interaktiver FIRST-ED-artiger Host, restliche Kompatibilitätsdetails, erweiterte Suche, MicroStar-artige Beispiele und später Performance-Backends wie Piece Table oder Rope.
