# FIRST-ED-Kernel-Kompatibilitätsaudit

## Umfang dieses Audit-Schritts

Dieses Dokument hält den Clean-Room-Abgleich mehrerer kleiner, aber wichtiger Routinen der Turbo Editor Toolbox V1 fest, deren sichtbares Verhalten von üblichen modernen Editor-Standards abweicht.

Ziel ist keine Quellcode-Übersetzung. Das Handbuch dient als Verhaltensspezifikation; C#-Datenstrukturen, Speicherverwaltung und Benennungen bleiben eigenständig.

## Geprüfte Routinen

| Historische Routine / Konzept | .NET-Implementierung | Erhaltenes Verhalten |
|---|---|---|
| `EditLeftChar` | `FirstEdPrimitiveCompatibilityProcessor.MoveLeftChar` | Von Spalte 1 geht der Cursor in die vorherige Zeile direkt hinter deren letztes Nicht-Leerzeichen. Am ersten Zeichen des Textstroms erfolgt keine Bewegung. |
| `EditRightChar` | `FirstEdPrimitiveCompatibilityProcessor.MoveRightChar` | Der Befehl erhöht nur die Spalte und überschreitet keine logische Zeilengrenze. Damit sind virtuelle Spalten hinter dem Zeilenende möglich. |
| `EditTab` | `FirstEdPrimitiveCompatibilityProcessor.Tab` | Der Cursor geht zum nächsten Tabulatorstopp. Im Insert-Modus werden die nötigen Leerzeichen eingefügt; im Overtype-Modus ist Tab nur Cursorbewegung. |
| `EditSetMarker` / `EditJumpMarker` | `EditorMarker`, `EditorSession.SetMarker`, `JumpToMarker` | Ein Marker bezeichnet eine logische Zeile. Beim Anspringen kann das Fenster wechseln; die Spalte des gewählten Zielfensters bleibt jedoch erhalten. |
| `EditOffblock` | `EditorSession.ClearBlockHighlights` | Löscht das `InBlock`-Flag in allen offenen Textströmen, ohne die logischen Blockgrenzen zu verändern. |
| `EditMarkblock` | `EditorSession.MarkBlockHighlights` | Projiziert den aktiven Ganzzeilenblock erneut in die `InBlock`-Flags, ohne die Blockgrenzen zu ändern. |

## Warum diese Regeln nicht in `EditorEngine` liegen

`EditorEngine` ist die wiederverwendbare Editing-Schicht für SASD-Anwendungen. Einige historische FIRST-ED-Regeln sind nach heutigen Maßstäben bewusst ungewöhnlich. So kann ein Rechts-Befehl den Cursor in einer virtuellen Spalte belassen, statt in die nächste logische Zeile zu wechseln; beim Links-Befehl von Spalte eins wird dagegen die Position hinter dem letzten *Nicht-Leerzeichen* der vorherigen Zeile verwendet.

Diese Regeln liegen deshalb in `FirstEdPrimitiveCompatibilityProcessor`. Der moderne Kern bleibt dadurch eigenständig nutzbar, während das Kompatibilitätsprofil explizit, testbar und später auf C++, Java und JavaScript übertragbar bleibt.

## Marker-Modell

Der historische Marker ist zeilenorientiert. Die erste .NET-Implementierung speichert nun Dokumentidentität plus logischen Zeilenindex statt einer vollständigen Cursorposition. Beim Sprung wird die Zeile geändert, aber nicht die vorhandene Spalte des Zielfensters.

Die historische Implementierung verwendete Zeilendeskriptor-Zeiger. Dadurch folgte ein Marker seinem Deskriptor automatisch, wenn davor oder dahinter andere Zeilen eingefügt wurden. Die .NET-Implementierung speichert derzeit einen Index. Die stabile Markeridentität bei allen Low-Level-Änderungen der Zeilenstruktur gehört in den kommenden `EditDelline`-/`EditRealign`-Audit und wird **noch nicht** als vollständig behauptet.

## Block-Markierungsmodell

Logische Blockdefinition und sichtbare Blockflags sind getrennte Konzepte. `ClearBlockHighlights` entfernt jedes `InBlock`-Bit aus jedem eindeutigen offenen Dokument, lässt `EditorSession.Block` aber bestehen. `MarkBlockHighlights` kann anschließend die Flags des aktiven Blocks wiederherstellen.

Diese Trennung ist für die Reparatur veralteter oder inkonsistenter Blockmarkierungen wichtig und entspricht der im Handbuch beschriebenen Rolle von `EditOffblock`.

## Bewusste moderne Ersetzungen

- Die Pascal-`Maxint`-Grenze von `EditRightChar` wird durch einen normalen CLR-`int.MaxValue`-Überlaufschutz ersetzt. Eine künstliche 16-Bit-Grenze wird modernen Dokumenten nicht auferlegt.
- Pointer-Identitäten, Deskriptor-Freelists und manuelles Freigeben von Speicher werden nicht nachgebaut. Stattdessen werden ihre sichtbaren Editor-Invarianten geprüft.
- Das historische `EditMarkblock` konnte das Markieren abbrechen, sobald Tastatureingabe vorlag. Der heutige In-Memory-Flag-Durchlauf ist klein und synchron; Eingabepriorisierung und Cancellation bleiben getrennte Aufgaben. Diese alte Paint-Optimierung wird nicht als implementierte Semantik ausgegeben.
- Die historische Toolbox besitzt eine globale Tabulatorbreite. Das heutige SASD-Modell hält `TabSize` in den Fensteroptionen, damit unabhängige Views unterschiedliche Einstellungen haben können. Das ist eine dokumentierte moderne Designentscheidung und bleibt Teil des finalen V1-Abweichungsaudits.

## Neue Tests

`CompatibilityKernelAuditTests` prüft:

- Links-Bewegung über eine Zeilengrenze bei nachgestellten Leerzeichen;
- Rechts-Bewegung in eine virtuelle Spalte ohne Zeilenwechsel;
- Tab im Insert-Modus inklusive Mutation, Dirty-State und Undo;
- Tab im Overtype-Modus ohne Mutation, Dirty-State oder Undo;
- zeilenorientierte Marker-Sprünge mit erhaltener Zielspalte;
- globales Löschen der Blockflags nach Art von `EditOffblock` und anschließendes erneutes Markieren.

## Nächster Low-Level-Audit

Als nächste Gruppe sollten wir uns auf die Zeilenstruktur statt auf Pascal-Speicherverwaltung konzentrieren:

1. `EditDelline` und die sichtbaren Regeln für Fenster-Cursor/TopLine, Blockgrenzen und Marker;
2. `EditRealign` und die Viewport-Reparatur nach dem Entfernen einer Zeile;
3. `EditLeftWord` / `EditRightWord` mit den exakten FIRST-ED-Whitespace- und Randregeln;
4. die Reformat-Helfer `EditLongLine`, `EditShortLine` und `EditShiftLine`;
5. historische Fehlercodes/-ressourcen, soweit sie das Befehlsverhalten beeinflussen.

`EditDestxtdes` selbst ist eine Speicherfreigabe-Routine und sollte nicht als öffentliche C#-Pointer-/Freelist-API nachgebaut werden.
