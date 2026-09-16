# FIRST-ED-Kompatibilität der Löschbefehle

## Umfang

Dieses Dokument beschreibt die Clean-Room-Übertragung der Löschregeln, die über die FIRST-ED-Kompatibilitätsschicht laufen. Es ergänzt die Dokumentation zur Zeilentopologie: Löschen verändert Text und kann zugleich die Topologie verändern, sobald eine vollständige logische Zeile verschwindet.

## Delete Left Character

`EditDeleteLeftChar` entfernt das Zeichen unmittelbar links vom Cursor und bewegt den Cursor eine Spalte nach links. In Spalte null wird die aktuelle logische Zeile – falls vorhanden – mit der vorherigen Zeile verbunden. In der ersten Spalte der ersten logischen Zeile findet keine Operation statt.

`FirstEdPrimitiveCompatibilityProcessor.DeleteLeftCharacter` behandelt den Zeilenrand als strukturelle Operation. Der aktuelle Zeileneintrag verschwindet, sein Text wird am Ende des letzten Nicht-Leerzeichens der vorherigen Zeile angefügt und `EditorLineTopology` repariert verknüpfte Fenster, Marker und Blockgrenzen. Der aktive Cursor steht anschließend am Join-Punkt der überlebenden vorherigen Zeile.

FIRST-ED kann den Cursor auch in einer virtuellen Spalte hinter der physischen Textlänge halten. Das Handbuch dokumentiert diese Möglichkeit für die Rechtsbewegung, definiert Backspace in diesem nicht gespeicherten Bereich aber nicht separat. Die .NET-V1-Regel wird deshalb ausdrücklich als moderne Policy dokumentiert: Backspace bewegt sich dort nur nach links und erzeugt weder Dirty-State noch Undo, bis ein tatsächlich gespeichertes Zeichen erreicht wird.

## Delete Right Character

`EditDeleteRightChar` entfernt das Zeichen an der Cursorposition, solange der Cursor noch innerhalb des inhaltlichen Textes steht. Das Handbuch beschreibt eine eigene Regel am Zeilenende: Befindet sich der Cursor hinter dem letzten Nicht-Leerzeichen, wird – falls vorhanden – die darunterliegende Zeile an die aktuelle Zeile angehängt.

Der Join erfolgt an der Cursorposition und entspricht damit dem Vertrag des historischen Helfers `EditJoinline`: Das erste Zeichen der folgenden Zeile wird dort platziert, wo der Cursor steht. Physisch gespeicherte Leerzeichen rechts von diesem logischen Join-Punkt werden deshalb nicht als künstliche Lücke erhalten. Das Entfernen der folgenden logischen Zeile wird `EditorLineTopology` gemeldet, damit verknüpfte Fenster, Marker und Blockgrenzen konsistent repariert werden.

## Delete Right Word

Die historische Wortdefinition entspricht dem Wortbewegungs-Audit: Zeichen gehören zu drei Klassen – alphanumerische Zeichen, Interpunktion und Leerraum. `EditDeleteRightWord` entfernt den Lauf der Klasse unter dem Cursor und anschließend unmittelbar folgende Leerzeichen. Interpunktion wird deshalb nicht stillschweigend mit dem vorhergehenden alphanumerischen Wort zusammengefasst.

Beispiel:

```text
abc!!  def
   ^
```

Delete Right Word am ersten `!` entfernt `!!  `; übrig bleibt `abcdef`.

Steht der Cursor an oder hinter der ersten Position nach dem letzten Nicht-Leerzeichen, verbindet Delete Right Word stattdessen die folgende Zeile mit der aktuellen. Wie bei Delete Right Character ist die Join-Position die Cursorposition. Existiert keine folgende Zeile, findet keine Mutation statt.

Die .NET-Implementierung verwendet Unicode-fähige `char.IsLetterOrDigit`- und `char.IsWhiteSpace`-Prüfungen. Für ASCII bleibt das historische Drei-Klassen-Verhalten erhalten, ohne den wiederverwendbaren Kern künstlich auf ASCII zu begrenzen.

## Delete Line

Auch der semantische Befehl `DeleteLine` läuft durch den Compatibility Processor. Seine Sonderregeln stehen in `ZEILENTOPOLOGIE-UND-REALIGNMENT.md`: Der einzige verbleibende logische Zeilencontainer wird erhalten und geleert, Marker auf der gelöschten Zeile werden undefiniert, das Löschen einer Blockgrenze deaktiviert die aktive Blockrepräsentation und alle überlebenden Referenzen werden realigned.

## Virtuelle Spalten und strukturelle Normalisierung

Nichtnegative virtuelle Cursorspalten sind gültiger Kompatibilitätszustand. `EditorWindow.ClampCursor` normalisiert deshalb nach strukturellen Änderungen Zeile und Viewport, ohne die Spalte auf `Text.Length` zu verkürzen. Das ist besonders für verknüpfte Fenster wichtig: `EditRealign` repariert zeilenbezogenen Fensterzustand, soll aber nicht nebenbei die unabhängige horizontale Cursorposition einer anderen Ansicht verändern.

## Undo und No-op-Verhalten

Eine erfolgreiche Textlöschung erzeugt vor der Mutation genau einen Snapshot und markiert das Dokument als geändert. Ein Grenzfall, bei dem weder Zeichen noch Folgezeile gelöscht werden können, liefert `false`, ohne Undo-Eintrag und ohne Dirty-State. Reine Cursorbewegung durch virtuellen Leerraum verändert ebenfalls weder Undo noch Dirty-State.

Das ist bewusst korrektheitsorientiert. In V1 werden weder Snapshot-Speicher noch Anchor-Suche vorzeitig optimiert.

## Tests

`DeletionCompatibilityTests` prüft:

- gewöhnliches Delete Left Character;
- Join mit der vorherigen Zeile in Spalte null einschließlich Realignment von verknüpften Fenstern und Markern;
- No-op am Anfang des Textstroms;
- die dokumentierte .NET-Regel für Backspace in virtuellen Spalten;
- Interpunktion als eigenständige Rechts-Wortklasse;
- das Mitlöschen unmittelbar folgender Leerzeichen;
- Zeilen-Join von Delete Right Word am hinteren Zeilenrand;
- Marker-Ungültigkeit und Marker-Verschiebung durch Joins;
- gewöhnliches Delete Right Character;
- Zeilen-Join von Delete Right Character und Realignment eines verknüpften Fensters.

Zusammen mit `LineTopologyCompatibilityTests` und `InsertionCompatibilityTests` wird die Einfüge-/Lösch-Topologie damit aus beiden Richtungen ausführbar geprüft.

## Verbleibender löschbezogener Audit

Die wesentliche Zeichen-/Wort-/Zeilen-Löschsemantik ist nun übertragen. Das verbleibende strukturelle Risiko konzentriert sich auf Block-Copy/Move/Delete und den vorhandenen `ReplaceAll`-Pfad der Absatz-Reformatierung. Diese Pfade müssen noch gegen `EditorLineTopology` auditiert werden, bevor für solche Operationen vollständige strukturelle V1-Parität behauptet wird.
