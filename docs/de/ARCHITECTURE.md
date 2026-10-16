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

## Mehrsprachige Architektur

Sprachspezifische Implementierungen liegen gleichberechtigt unter `src/<sprache>` und besitzen passende Bereiche unter `tests/<sprache>` und `samples/<sprache>`. Keine Implementierung ruft zur Laufzeit eine andere Sprachimplementierung auf. Der gemeinsame Vertrag unter `spec/` definiert das beobachtbare Verhalten; `conformance/` enthält sprachunabhängige Testfälle, die von sprachspezifischen Testadaptern ausgeführt werden können.

Die .NET-Implementierung ist derzeit die ausführbare Referenz, weil sie am weitesten entwickelt ist. Ihre Klassenstruktur und API-Namen sind deshalb aber nicht normativ. C++, Java und spätere Ports dürfen idiomatische Ownership-, Collection-, Fehler- und Build-Modelle verwenden, solange die Editor-Invarianten erhalten bleiben.

Weitere Regeln zu Source of Truth, Synchronisierung, CI und Portierung stehen in [MEHRSPRACHIGE-ENTWICKLUNG.md](MEHRSPRACHIGE-ENTWICKLUNG.md).

## Aktueller V1-Abschluss und bewusste Verschiebungen

Die FIRST-ED-/WordStar-Tastenzuordnung, der Prefix-Dispatcher und ein interaktiver Terminal-Referenzhost sind bereits implementiert. Als nächstes V1-Release-Gate folgt der [historische Prozeduren-/Funktionsindex-Audit](HANDBUCH-PROZEDURENINDEX-AUDIT.md), der echte beobachtbare Lücken von überholter Pascal-/DOS-Technik trennt.

MicroStar-Menüs, Pop-ups und Hintergrunddruck bleiben Host-/Sample-Aufgaben. Piece Table, Rope und spekulative Optimierung folgen erst bei nachgewiesenem Bedarf.
