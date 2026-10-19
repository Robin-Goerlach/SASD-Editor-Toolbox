# FIRST-ED-Kernel-Kompatibilitätsaudit

## Umfang dieses Audit-Schritts

Dieses Dokument hält den Clean-Room-Abgleich mehrerer kleiner, aber wichtiger Routinen der Turbo Editor Toolbox V1 fest, deren sichtbares Verhalten von üblichen modernen Editor-Standards abweicht.

Ziel ist keine Quellcode-Übersetzung. Das Handbuch dient als Verhaltensspezifikation; C#-Datenstrukturen, Speicherverwaltung und Benennungen bleiben eigenständig.

## Geprüfte Routinen

| Historische Routine / Konzept | .NET-Implementierung | Erhaltenes Verhalten |
|---|---|---|
| `EditLeftChar` | `FirstEdPrimitiveCompatibilityProcessor.MoveLeftChar` | Von Spalte 1 geht der Cursor in die vorherige Zeile direkt hinter deren letztes Nicht-Leerzeichen. Am ersten Zeichen des Textstroms erfolgt keine Bewegung. |
| `EditRightChar` | `FirstEdPrimitiveCompatibilityProcessor.MoveRightChar` | Der Befehl erhöht nur die Spalte und überschreitet keine logische Zeilengrenze. Damit sind virtuelle Spalten hinter dem Zeilenende möglich. |
| `EditLeftWord` | `FirstEdPrimitiveCompatibilityProcessor.MoveLeftWord` | Führende Einrückung wird zunächst auf Spalte 1 reduziert; von Spalte 1 geht es hinter das letzte Nicht-Leerzeichen der vorherigen Zeile. Sonst wird der Beginn der vorherigen Wort-/Zeichenklasse derselben Zeile gewählt. |
| `EditRightWord` | `FirstEdPrimitiveCompatibilityProcessor.MoveRightWord` | Alphanumerische Zeichen, Interpunktion und Leerraum bilden getrennte Klassen. Der Cursor überquert die aktuelle Klasse und folgende Leerzeichen; hinter dem letzten Nicht-Leerzeichen geht es an den Anfang der nächsten Zeile. |
| `EditTab` | `FirstEdPrimitiveCompatibilityProcessor.Tab` | Der Cursor geht zum nächsten Tabulatorstopp. Im Insert-Modus werden die nötigen Leerzeichen eingefügt; im Overtype-Modus ist Tab nur Cursorbewegung. |
| `EditSetMarker` / `EditJumpMarker` | `EditorMarker`, `EditorSession.SetMarker`, `JumpToMarker` | Ein Marker bezeichnet eine logische Zeile. Beim Anspringen kann das Fenster wechseln; die Spalte des gewählten Zielfensters bleibt jedoch erhalten. |
| `EditOffblock` | `EditorSession.ClearBlockHighlights` | Löscht das `InBlock`-Flag in allen offenen Textströmen, ohne die logischen Blockgrenzen zu verändern. |
| `EditMarkblock` | `EditorSession.MarkBlockHighlights` | Projiziert den aktiven Ganzzeilenblock erneut in die `InBlock`-Flags, ohne die Blockgrenzen zu ändern. |

## Warum diese Regeln nicht in `EditorEngine` liegen

`EditorEngine` ist die wiederverwendbare Editing-Schicht für SASD-Anwendungen. Einige historische FIRST-ED-Regeln sind nach heutigen Maßstäben bewusst ungewöhnlich. So kann ein Rechts-Befehl den Cursor in einer virtuellen Spalte belassen, statt in die nächste logische Zeile zu wechseln; beim Links-Befehl von Spalte eins wird dagegen die Position hinter dem letzten *Nicht-Leerzeichen* der vorherigen Zeile verwendet.

Auch die historischen Wortbefehle verwenden ein Drei-Klassen-Modell statt eines modernen Identifier-Token-Begriffs: alphanumerische Zeichen, Interpunktion und Leerraum sind getrennte Läufe. Diese Regeln liegen deshalb in `FirstEdPrimitiveCompatibilityProcessor`. Der moderne Kern bleibt dadurch eigenständig nutzbar, während das Kompatibilitätsprofil explizit, testbar und später auf C++, Java und JavaScript übertragbar bleibt.

## Zeichenklassen bei Wortbewegungen

Für ASCII-Text entspricht die Implementierung direkt den drei Klassen des Handbuchs. Die .NET-Implementierung verwendet bewusst die Unicode-fähigen Prüfungen `char.IsLetterOrDigit` und `char.IsWhiteSpace`; alle übrigen Zeichen gelten als Interpunktion. Für ASCII bleibt das historische Ergebnis erhalten, ohne die wiederverwendbare Bibliothek künstlich auf ASCII zu begrenzen.

`EditRightWord` überquert die aktuelle Nicht-Leerraum-Klasse und danach vorhandenen Leerraum. Steht der Cursor bereits hinter dem letzten Nicht-Leerzeichen, wechselt der nächste Aufruf auf Spalte null der folgenden logischen Zeile. `EditLeftWord` behandelt führende Einrückung gesondert: Ein Cursor innerhalb oder direkt hinter dem führenden Leerraum geht zuerst auf Spalte null; erst ein weiterer Aufruf wechselt in die vorherige logische Zeile.

## Marker-Modell

Der historische Marker ist zeilenorientiert. Die erste .NET-Implementierung speichert nun Dokumentidentität plus logischen Zeilenindex statt einer vollständigen Cursorposition. Beim Sprung wird die Zeile geändert, aber nicht die vorhandene Spalte des Zielfensters.

Die historische Implementierung verwendete Zeilendeskriptor-Zeiger. Die .NET-Implementierung speichert dagegen einen logischen Index und korrigiert ihn für die auditierten Einfüge-, Lösch-, Join-, Reformat- und Block-Bulk-Pfade zentral über `EditorLineTopology`. Das bezieht sich auf geprüfte Befehlspfade, nicht automatisch auf jede zukünftige interne Mutation. Siehe [Zeilentopologie](ZEILENTOPOLOGIE-UND-REALIGNMENT.md).

## Block-Markierungsmodell

Logische Blockdefinition und sichtbare Blockflags sind getrennte Konzepte. `ClearBlockHighlights` entfernt jedes `InBlock`-Bit aus jedem eindeutigen offenen Dokument, lässt `EditorSession.Block` aber bestehen. `MarkBlockHighlights` kann anschließend die Flags des aktiven Blocks wiederherstellen.

Diese Trennung ist für die Reparatur veralteter oder inkonsistenter Blockmarkierungen wichtig und entspricht der im Handbuch beschriebenen Rolle von `EditOffblock`.

## Bewusste moderne Ersetzungen

- Die Pascal-`Maxint`-Grenze von `EditRightChar` wird durch einen normalen CLR-`int.MaxValue`-Überlaufschutz ersetzt. Eine künstliche 16-Bit-Grenze wird modernen Dokumenten nicht auferlegt.
- Pointer-Identitäten, Deskriptor-Freelists und manuelles Freigeben von Speicher werden nicht nachgebaut. Stattdessen werden ihre sichtbaren Editor-Invarianten geprüft.
- Das historische `EditMarkblock` konnte das Markieren abbrechen, sobald Tastatureingabe vorlag. Der heutige In-Memory-Flag-Durchlauf ist klein und synchron; Eingabepriorisierung und Cancellation bleiben getrennte Aufgaben. Diese alte Paint-Optimierung wird nicht als implementierte Semantik ausgegeben.
- Die historische Toolbox besitzt eine globale Tabulatorbreite. Das heutige SASD-Modell hält `TabSize` in den Fensteroptionen, damit unabhängige Views unterschiedliche Einstellungen haben können. Das ist eine dokumentierte moderne Designentscheidung und bleibt Teil des finalen V1-Abweichungsaudits.

## Neue Tests

`CompatibilityKernelAuditTests` prüft inzwischen:

- Links-Bewegung über eine Zeilengrenze bei nachgestellten Leerzeichen;
- Rechts-Bewegung in eine virtuelle Spalte ohne Zeilenwechsel;
- Links-Wort-Bewegung bei führender Einrückung und Übergang in die vorherige Zeile;
- Rechts-/Links-Wort-Bewegung über alphanumerische, Interpunktions- und Leerraum-Klassen;
- Rechts-Wort-Übergang vom Ende einer logischen Zeile in die nächste;
- Tab im Insert-Modus inklusive Mutation, Dirty-State und Undo;
- Tab im Overtype-Modus ohne Mutation, Dirty-State oder Undo;
- zeilenorientierte Marker-Sprünge mit erhaltener Zielspalte;
- globales Löschen der Blockflags nach Art von `EditOffblock` und anschließendes erneutes Markieren.

## Nächster Low-Level-Audit

Zeilentopologie, Reformatierung, Befehlsgrenzen sowie das typisierte Fehler-/Ressourcenmodell sind inzwischen implementiert und getestet. Maßgeblich sind die neueren Fachtexte:

- [Zeilentopologie](ZEILENTOPOLOGIE-UND-REALIGNMENT.md)
- [Reformat-Kompatibilität](NEUFORMATIERUNG-KOMPATIBILITAET.md)
- [Befehlsgrenzen](BEFEHLSGRENZEN-VALIDIERUNG.md)
- [Fehlermodell](FEHLERMODELL.md)

Der verbleibende V1-Audit nutzt das [vollständige historische Prozedureninventar](HANDBUCH-PROZEDURENINDEX-AUDIT.md). `EditDestxtdes` bleibt eine Speicherfreigaberoutine, keine nachzubauende C#-Pointer-API.

## Zeile zentrieren: Wiederholungsbefehle und virtuelle Ränder

`EditorEngine.CenterLine` berechnet die Einrückung nun mit breiten Ganzzahlen, um Überläufe bei großen virtuellen Randwerten zu vermeiden. Eine bereits zentrierte Zeile verändert weder Dokumentversion und Dirty-State noch die Undo-Historie, selbst wenn der aktive Cursor vom virtuellen Bereich ans physische Zeilenende zurückkehrt. Cursor verknüpfter Ansichten bleiben unabhängig. Wirkliche Textänderungen bleiben ein einzelner Undo-fähiger Vorgang; eine physisch nicht darstellbare Ergebnislänge wird vorab abgelehnt. Dies ist eine moderne Leerlauf- und Größen-Sicherheitsregel.

Die Zentrierung vergleicht den gewünschten Text jetzt bereits **vor** der Anlage einer zweiten Zeichenkette mit dem vorhandenen Puffer. Ein moderner Ressourcenschutz lehnt die automatische Erzeugung von mehr als **1.000.000 führenden Leerzeichen** aus extrem großen virtuellen Randwerten vor Undo/Textänderung ab. Diese Grenze betrifft nur neu generierte Einrückung, nicht das Beibehalten oder Prüfen vorhandener längerer Zeilen. Damit darf eine formal gültige, aber extreme Randeingabe keine unbeabsichtigt sehr große Speicherbelegung auslösen.

## Direkte EditorEngine-API: virtuelle Spalten und wirkungslose Befehle

Die öffentliche `EditorEngine` ist auch ohne FIRST-ED-Dispatcher verwendbar. Direkte Eingaben mit ausschließlich CR sowie Zeichen-/Wortlöschungen hinter dem physischen Ende der letzten Zeile erzeugen jetzt keine künstlichen Dirty-/Versions-/Undo-Transaktionen. Backspace im virtuellen Bereich bewegt nur den Cursor des aufrufenden Fensters nach links und löst keine ungültige Zeichenentfernung nach Beginn einer Undo-Transaktion mehr aus. Diese modernen Sicherheitsgrenzen ergänzen die bereits geprüften historischen Kommando-Prozessoren und ersetzen sie nicht.


## Ressourcenschutz beim Tabulator und virtuelle Spalten

Die direkte Methode `EditorEngine.Tab` und der historische Tab im Einfügemodus nutzen jetzt denselben begrenzten Einfügepfad. Eine nicht darstellbare nächste Spalte, mehr als **4.096 automatisch einzufügende Tab-Leerzeichen** oder mehr als **4.096 fehlende Leerzeichen zwischen physischem Zeilenende und virtuellem Cursor** führen vor jeder Undo-Transaktion beziehungsweise Textänderung zu einem typisierten Fehler. Diese vorsichtige moderne Grenze schützt den zeichenweisen Einfügealgorithmus; sie ist keine historische Beschränkung von 1985. Der historische Tab im Überschreibmodus bleibt eine reine Cursorbewegung und darf auch entfernte virtuelle Spalten erreichen, solange die Zielspalte darstellbar ist.
