# Zeilentopologie und Referenz-Realignment

## Zweck

Die Turbo Editor Toolbox speichert Text in verketteten Zeilendeskriptoren. Mehrere Editorzustände zeigen direkt auf diese Deskriptoren: aktuelle Zeile und Top-Zeile jedes Fensters, Textmarker sowie die beiden Blockgrenzen. Ein Einfügen oder Löschen einer Zeile verändert deshalb mehr als nur den Textstrom.

Die SASD Editor Toolbox bildet weder Pascal-Pointer noch Deskriptor-Freelists nach. Die .NET-Implementierung verwendet stattdessen eine stabile `DocumentId` und nullbasierte logische Zeilennummern. `EditorLineTopology` ist die zentrale Koordinationsschicht, die diese Referenzen nach strukturellen Änderungen konsistent hält.

Es handelt sich um eine Clean-Room-Übertragung des beobachtbaren Verhaltens. Das Handbuch dient als Anforderungsquelle; der historische Quellcode wird nicht kopiert.

## Übernommenes historisches Verhalten

Der aktuelle V1-Audit überträgt folgende beobachtbare Regeln:

- `EditDeleteLine` löscht die aktuelle logische Zeile. Ist sie die einzige verbleibende Zeile, bleibt der Zeilendeskriptor bestehen und die bisher gespeicherte Zeichenlänge wird mit Leerzeichen gefüllt, statt die letzte Zeile aus dem Textstrom zu entfernen.
- Ein Marker auf einer gelöschten Zeile wird undefiniert.
- Ist die gelöschte Zeile eine Blockgrenze, wird diese Grenze undefiniert und die Blockhervorhebung abgeschaltet.
- Fenster, deren aktuelle Zeile oder Top-Zeile gelöscht wurde, müssen anschließend weiterhin auf eine gültige Zeile zeigen.
- `EditRealign` korrigiert die Fensterbezüge nach Zeilen-Inserts und -Deletes.
- Das Einlesen einer Datei fügt Zeilen hinter der aktuellen Zeile ein und lässt die Cursorposition unverändert; Referenzen auf bereits vorhandene logische Zeilen sollen weiterhin genau diese logischen Zeilen bezeichnen.

## Moderne Abbildung

`EditorLineTopology` stellt drei fokussierte Operationen bereit:

- `LinesInserted(document, firstInsertedLine, count)` verschiebt Fenster-Cursor, TopLine-Anker, Marker und Blockgrenzen, die auf logische Zeilen ab der Einfügestelle zeigen.
- `LinesDeleted(document, firstDeletedLine, count)` macht Marker im entfernten Bereich ungültig, verschiebt spätere Anker und hängt Fenster-Cursor/TopLine an eine gültige verbleibende Zeile, wenn ihr bisheriges Ziel gelöscht wurde.
- `InvalidateLineReferences(document, lineIndex)` behandelt den Sonderfall von `EditDeleteLine` bei nur einer Zeile: Der letzte Deskriptor bleibt erhalten, aber marker- und blockartige Referenzen auf den historisch gelöschten Inhalt werden ungültig.

Der semantische Befehl `DeleteLine` läuft nun über `FirstEdPrimitiveCompatibilityProcessor.DeleteLine`. Damit bleiben die historischen Löschregeln außerhalb der allgemeinen modernen Standardlogik des `EditorEngine`.

`EditorFileService.ReadIntoCurrentWindowAsync` ruft nach dem Einfügen der eingelesenen Zeilen `LinesInserted` auf. Danach wird der Cursor des auslösenden Fensters wiederhergestellt, während verknüpfte Fenster, Marker und Blockgrenzen an den logischen Zeilen bleiben, auf die sie vor dem Einfügen gezeigt haben.

## Policy für Blockgrenzen

Das aktuelle `EditorBlockState` hält unabhängig nullable `Start`- und `End`-Endpunkte. Das Löschen einer Grenzzeile macht **nur den betroffenen Endpunkt** ungültig; der andere bleibt für einen späteren Begin-/End-Befehl erhalten. Ein vollständiger `EditorBlock` entsteht nur bei zwei gültigen, geordneten Grenzen desselben Dokuments; unvollständige Blöcke werden nicht hervorgehoben. Einfügungen und Löschungen realignen vorhandene Grenzen getrennt.

Wird eine Zeile *innerhalb* eines Blocks gelöscht, bleiben die überlebenden Grenzen bestehen und spätere Anker werden entsprechend verschoben. Siehe [Block-Kompatibilität](BLOCK-KOMPATIBILITAET.md) und `BlockEndpointStateTests`.

## Undo und Dirty-State

Kompatibles Zeilenlöschen ist ein logischer Befehl im transaktionalen Snapshot-Undo. `BeginTransaction` erfasst den Zustand vorher; `Commit` speichert nach Textänderung, Topologiekorrektur und befehlsbedingter Navigation einen History-Eintrag. Undo stellt passende Dokument- und Ankerzustände wieder her, ohne unabhängige spätere Benutzernavigation zu überschreiben. Siehe [Undo-Transaktionen](UNDO-TRANSAKTIONEN.md).

## Aktuelle Integrationsgrenze

`EditorLineTopology` ist in folgende auditierte Strukturpfade integriert:

- kompatibles `DeleteLine` einschließlich Ein-Zeilen-Sonderfall;
- Datei-Einfügen über `EditorFileService`;
- `InsertLine`, `NewLine` und generisches Newline/automatisches Wrapping;
- zeilenentfernende Lösch- und Join-Befehle;
- Expansion/Kontraktion bei der Absatz-Reformatierung;
- mehrzeiliges Block Copy/Move/Delete einschließlich dokumentübergreifender Änderungen.

Das ist eine **explizite Koordinationsgrenze**, kein automatischer Abfangmechanismus für jede zukünftige `ITextBuffer`-Mutation. Neue Strukturoperationen müssen den Dienst aufrufen und Regressionstests ergänzen. Die [Kompatibilitätsmatrix](BORLAND-V1-KOMPATIBILITAET.md) beschreibt auditierte Pfade, nicht Pascal-Pointergleichheit.

## Tests

`LineTopologyCompatibilityTests` prüft:

- Ungültigmachen eines Markers auf einer gelöschten Zeile;
- Verschieben eines späteren Markers;
- Cursor- und TopLine-Realignment in mehreren verknüpften Fenstern;
- unabhängige Ungültigmachung einer Blockgrenze und Bereinigung der Hervorhebung;
- Löschen innerhalb eines bestehenden Blocks;
- die Ein-Zeilen-Regel „leeren, aber nicht entfernen“;
- Datei-Insert mit Realignment von verknüpften Fenstern, Markern und Blockgrenzen bei unverändertem auslösenden Cursor.

Ergänzende Tests in `BlockEndpointStateTests`, `BlockCompatibilityTests`, `ReformatCompatibilityTests` und `CompoundUndoCompatibilityTests` prüfen Bulk-Realignment und Undo. Die Tests validieren beobachtbares Editorverhalten statt Pascal-Pointerstrukturen.

## Performance-Policy

Die aktuelle Implementierung durchläuft bei gemeldeten Strukturänderungen die geöffneten Fenster und die Markertabelle. V1 priorisiert Korrektheit, Nachvollziehbarkeit und einfache Invarianten. Falls Profiling später zeigt, dass sehr große Dokumente oder viele Anker eine schnellere Repräsentation benötigen, können Intervallstrukturen, persistente Anker oder buffer-native Anchor-Mechanismen intern eingesetzt werden, ohne den Kompatibilitätsvertrag zu verändern.
