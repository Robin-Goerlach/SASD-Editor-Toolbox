# Zeilentopologie und Referenz-Realignment

## Zweck

Die Turbo Editor Toolbox speichert Text in verketteten Zeilendeskriptoren. Mehrere Editorzustände zeigen direkt auf diese Deskriptoren: aktuelle Zeile und Top-Zeile jedes Fensters, Textmarker sowie die beiden Blockgrenzen. Ein Einfügen oder Löschen einer Zeile verändert deshalb mehr als nur den Textstrom.

Die SASD Editor Toolbox bildet weder Pascal-Pointer noch Deskriptor-Freelists nach. Die .NET-Implementierung verwendet stattdessen eine stabile `DocumentId` und nullbasierte logische Zeilennummern. `EditorLineTopology` ist die zentrale Koordinationsschicht, die diese Referenzen nach strukturellen Änderungen konsistent hält.

Es handelt sich um eine Clean-Room-Übertragung des beobachtbaren Verhaltens. Das Handbuch dient als Anforderungsquelle; der historische Quellcode wird nicht kopiert.

## Übernommenes historisches Verhalten

Der aktuelle V1-Audit überträgt folgende beobachtbare Regeln:

- `EditDeleteLine` löscht die aktuelle logische Zeile. Ist sie die einzige verbleibende Zeile, bleibt der Zeilendeskriptor bestehen und die Zeile wird geleert, statt die letzte Zeile aus dem Textstrom zu entfernen.
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

Der historische Editor kann `Blockfrom` und `Blockto` unabhängig voneinander darstellen und einen einzelnen Pointer auf `nil` setzen. Das gegenwärtige .NET-V1-Modell verwendet dagegen ein vollständiges `EditorBlock`-Paar. Wird eine der beiden Blockgrenzen gelöscht, entfernt die .NET-Implementierung deshalb den vollständigen aktiven Block einschließlich Hervorhebung, statt künstlich ein halb definiertes Blockobjekt zu erzeugen.

Das ist eine bewusst konservative Repräsentationsentscheidung und keine Behauptung, dass die Implementierung von 1985 beide Pointer gelöscht hätte. Ein späteres Block-Ankermodell kann unabhängig definierte Start-/Endgrenzen darstellen, falls diese Low-Level-Parität sinnvoll wird.

Wird dagegen eine Zeile *innerhalb* eines Blocks gelöscht, bleibt der Block bestehen. Die spätere Grenze wird verschoben, sodass der Block weiterhin dieselben überlebenden logischen Zeilen umfasst.

## Undo und Dirty-State

Das kompatible Zeilenlöschen erzeugt vor der Mutation einen Snapshot, markiert das Dokument als geändert und hinterlässt einen Undo-Eintrag. Das ist bewusst einfacher als die historische zeilenweise Undo-Repräsentation. Das Snapshot-Backend ist ein korrektheitsorientiertes Implementierungsdetail hinter der vorhandenen Undo-Grenze und kann später optimiert werden.

## Aktuelle Integrationsgrenze

Die Topologieschicht wird jetzt verwendet durch:

- den semantischen `DeleteLine`-Kompatibilitätspfad;
- das kompatible Einfügen eingelesener Dateien über `EditorFileService`.

Sie ist noch nicht das universelle Mutation-Gateway für jede strukturelle Operation. Neue Zeile, automatisches Word-Wrap, Zeilen-Join, Block Copy/Move/Delete und Absatz-Reformatierung benötigen weiterhin einen Prozedur-für-Prozedur-Audit. Bis dieser abgeschlossen ist, bezeichnet die Kompatibilitätsmatrix `EditDelline`/`EditRealign` bewusst als wachsende Grundlage und nicht als vollständige Parität.

## Tests

`LineTopologyCompatibilityTests` prüft:

- Ungültigmachen eines Markers auf einer gelöschten Zeile;
- Verschieben eines späteren Markers;
- Cursor- und TopLine-Realignment in mehreren verknüpften Fenstern;
- Löschen einer Blockgrenze und Bereinigung der Hervorhebung;
- Löschen innerhalb eines bestehenden Blocks;
- die Ein-Zeilen-Regel „leeren, aber nicht entfernen“;
- Datei-Insert mit Realignment von verknüpften Fenstern, Markern und Blockgrenzen bei unverändertem auslösenden Cursor.

Die Tests validieren bewusst beobachtbares Editorverhalten und keine Pascal-Pointerstruktur.

## Performance-Policy

Die aktuelle Implementierung durchläuft bei gemeldeten Strukturänderungen die geöffneten Fenster und die Markertabelle. V1 priorisiert Korrektheit, Nachvollziehbarkeit und einfache Invarianten. Falls Profiling später zeigt, dass sehr große Dokumente oder viele Anker eine schnellere Repräsentation benötigen, können Intervallstrukturen, persistente Anker oder buffer-native Anchor-Mechanismen intern eingesetzt werden, ohne den Kompatibilitätsvertrag zu verändern.
