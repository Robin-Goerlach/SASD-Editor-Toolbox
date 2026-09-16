# FIRST-ED-Kompatibilität der Löschbefehle

## Umfang

Dieses Dokument beschreibt die Clean-Room-Übertragung der Löschregeln, die derzeit über die FIRST-ED-Kompatibilitätsschicht laufen. Es ergänzt die Dokumentation zur Zeilentopologie: Löschen verändert Text und kann zugleich die Topologie verändern, sobald eine vollständige logische Zeile verschwindet.

## Delete Right Character

`EditDeleteRightChar` entfernt das Zeichen an der Cursorposition, solange der Cursor noch innerhalb des inhaltlichen Textes steht. Das Handbuch beschreibt eine eigene Regel am Zeilenende: Befindet sich der Cursor hinter dem letzten Nicht-Leerzeichen, wird – falls vorhanden – die darunterliegende Zeile an die aktuelle Zeile angehängt.

`FirstEdPrimitiveCompatibilityProcessor.DeleteRightCharacter` hält diese Unterscheidung aufrecht. Nach Erreichen der Position hinter dem letzten Nicht-Leerzeichen werden nachfolgende Leerzeichen nicht mehr wie gewöhnliche Zeichen behandelt. Beim Join verschwindet die folgende logische Zeile; diese Löschung wird `EditorLineTopology` gemeldet, damit verknüpfte Fenster, Marker und Blockgrenzen konsistent repariert werden.

## Delete Right Word

Die historische Wortdefinition entspricht dem Wortbewegungs-Audit: Zeichen gehören zu drei Klassen – alphanumerische Zeichen, Interpunktion und Leerraum. `EditDeleteRightWord` entfernt den Lauf der Klasse unter dem Cursor und anschließend unmittelbar folgende Leerzeichen. Interpunktion wird deshalb nicht stillschweigend mit dem vorhergehenden alphanumerischen Wort zusammengefasst.

Beispiel:

```text
abc!!  def
   ^
```

Delete Right Word am ersten `!` entfernt `!!  `; übrig bleibt `abcdef`.

Steht der Cursor an oder hinter der ersten Position nach dem letzten Nicht-Leerzeichen, verbindet Delete Right Word stattdessen die folgende Zeile mit der aktuellen. Existiert keine folgende Zeile, findet keine Mutation statt.

Die .NET-Implementierung verwendet Unicode-fähige `char.IsLetterOrDigit`- und `char.IsWhiteSpace`-Prüfungen. Für ASCII bleibt das historische Drei-Klassen-Verhalten erhalten, ohne den wiederverwendbaren Kern künstlich auf ASCII zu begrenzen.

## Delete Line

Auch der semantische Befehl `DeleteLine` läuft durch den Compatibility Processor. Seine Sonderregeln stehen in `ZEILENTOPOLOGIE-UND-REALIGNMENT.md`: Der einzige verbleibende logische Zeilencontainer wird erhalten und geleert, Marker auf der gelöschten Zeile werden undefiniert, das Löschen einer Blockgrenze deaktiviert die aktive Blockrepräsentation und alle überlebenden Referenzen werden realigned.

## Undo und No-op-Verhalten

Eine erfolgreiche Löschung erzeugt vor der Mutation genau einen Snapshot und markiert das Dokument als geändert. Ein Grenzfall, bei dem weder Zeichen noch Folgezeile gelöscht werden können, liefert `false`, ohne Undo-Eintrag und ohne Dirty-State zu erzeugen.

Das ist bewusst korrektheitsorientiert. In V1 werden weder Snapshot-Speicher noch Anchor-Suche vorzeitig optimiert.

## Tests

`DeletionCompatibilityTests` prüft:

- Interpunktion als eigenständige Wortklasse;
- das Mitlöschen unmittelbar folgender Leerzeichen;
- Zeilen-Join von Delete Right Word am hinteren Zeilenrand;
- Marker-Ungültigkeit und Marker-Verschiebung durch diesen Join;
- gewöhnliches Löschen eines einzelnen Zeichens;
- Zeilen-Join von Delete Right Character und Realignment eines verknüpften Fensters.

Zusammen mit `LineTopologyCompatibilityTests` ist die Lösch-/Topologie-Grenze damit ausführbar abgesichert und nicht nur dokumentiert.

## Verbleibender Lösch-Audit

`DeleteLeftCharacter` sowie Low-Level-Details einiger Block- und Reformat-Mutationen verwenden weiterhin allgemeine Engine-Pfade und benötigen einen eigenen Handbuch-Audit, bevor exakte V1-Parität behauptet wird. Die Kompatibilitätsmatrix unterscheidet deshalb zwischen den implementierten Rechts-/Zeilen-Löschregeln und den noch offenen Löschpfaden.
