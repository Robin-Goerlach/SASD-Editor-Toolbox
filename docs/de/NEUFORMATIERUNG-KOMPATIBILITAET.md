# FIRST-ED-Kompatibilität der Absatz-Neuformatierung

## Umfang

Dieses Dokument beschreibt die Clean-Room-Übertragung der historischen `EditReformat`-Familie in die .NET-Implementierung. Das Handbuch dient als Verhaltensspezifikation; die Pascal-Implementierung mit Pointer-Splicing wird nicht kopiert.

Zur historischen Familie gehören `EditCompressLine`, `EditShiftLine`, `EditLongLine`, `EditShortLine` und `EditReformat`. In der .NET-Version werden ihre beobachtbaren Aufgaben durch `FirstEdReformatCompatibilityProcessor` koordiniert, anstatt eine pointerförmige API nachzubauen.

## Übernommenes Verhalten

`EditCompressLine` reduziert mehrfache Leerzeichen, damit die anschließende Zeilenverarbeitung mit normalisierten Worttrennern arbeiten kann. `EditShiftLine` sorgt dafür, dass das erste Nicht-Leerzeichen mindestens am aktuellen linken Rand steht. `EditLongLine` verschiebt Wörter nach unten, wenn die aktuelle Zeile den rechten Rand überschreitet; `EditShortLine` holt Wörter aus einer gewrappten Folgezeile nach oben, solange sie noch passen. `EditReformat` wiederholt diesen Vorgang bis zum Absatzende und arbeitet unabhängig vom aktuellen Wordwrap-Modus.

Die Implementierung bildet diese Regeln als Planungsphase ab:

1. Absatz ab der aktuellen logischen Zeile bestimmen;
2. Wörter normalisieren;
3. prüfen, dass jedes einzelne Wort zwischen die aktuellen Ränder passt;
4. vollständige Zielzeilen ohne Dokumentmutation planen;
5. Ersetzungen/Einfügungen/Löschungen als zusammenhängende Änderung anwenden;
6. strukturelle Zeilenänderungen an `EditorLineTopology` melden.

Das ist bewusst besser review- und testbar als eine Nachbildung der historischen Deskriptor-Splicing-Sequenz.

## Bedeutung von `Wrapped`

V1 behandelt `EditorLineFlags.Wrapped` jetzt als **weiche ausgehende Zeilengrenze**: Ist das Bit auf Zeile *N* gesetzt, entstand die Grenze von Zeile *N* zu Zeile *N+1* durch Word-Wrap und nicht durch einen expliziten Absatz-/Newline-Abschluss.

Diese Interpretation ist wichtig, weil das Handbuch beschreibt, dass `EditNewLine` das `Wrapped`-Bit der zuvor aktuellen Zeile löscht und damit das Absatzende markiert. Das historische Dateiformat stellt dieselbe Beziehung durch das High-Bit-Carriage-Return hinter einer gewrappten Zeile dar.

Auch der automatische Word-Wrap in `EditorEngine` ist an dieses Modell angepasst: Die obere Zeile erhält `Wrapped`; eine bereits bestehende nachfolgende weiche Grenze wird auf das neu eingefügte untere Fragment übertragen. Gewöhnliche Zeicheneingabe löscht das Grenzflag nicht mehr.

## Ränder und Wortverschiebung

Die Neuformatierung verwendet den inklusiven Bereich von `LeftMargin` bis `RightMargin`. Text wird mit Einrückung bis zum linken Rand ausgegeben; nach der Komprimierung werden Wörter durch genau ein Leerzeichen getrennt. Wörter werden solange zur aktuellen Zeile hinzugefügt, wie das nächste Wort noch hineinpasst. Damit wird das beobachtbare Push-down-/Pull-up-Verhalten von Long Line und Short Line erhalten, ohne diese historischen Helfer als öffentliche APIs zu exponieren.

Das Handbuch beschreibt einen Fehler für ein nicht teilbares, zu langes Wort. Die .NET-Implementierung validiert zuerst den vollständigen Plan. Ist ein Wort breiter als der verfügbare Randbereich, schlägt der Befehl fehl, ohne Text, Dirty-State, Cursor oder Undo-Historie zu verändern.

Für modernen Text nutzt die Normalisierung die Unicode-fähige Whitespace-Erkennung von .NET. ASCII verhält sich wie im historischen Modell; zusätzlicher Unicode-Whitespace ist eine dokumentierte Erweiterung.

## Cursor und Wordwrap-Modus

Die technische Referenz beschreibt den Startpunkt und den Wortfluss der Neuformatierung, legt aber keine abschließende Cursorverschiebung fest. Die .NET-V1-Policy erhält deshalb die ursprünglich aktuelle logische Zeile und Spalte – einschließlich einer gültigen virtuellen Spalte.

`EditReformat` ist unabhängig von `EditorWindowOptions.WordWrap`. Wordwrap steuert den automatischen Umbruch bei der Texteingabe; die explizite Absatz-Neuformatierung richtet sich immer nach den konfigurierten Rändern.

## Topologie und Zeilenidentität

Der Plan kann mehr oder weniger logische Zeilen erzeugen als der Quellabsatz. Zunächst werden überlebende Zeilenpositionen ersetzt; zusätzliche Zielzeilen werden nach dem bisherigen Absatz-Footprint eingefügt, überschüssige Quellzeilen am Ende dieses Footprints gelöscht. Die Insert-/Delete-Anzahlen werden anschließend über `EditorLineTopology` gemeldet.

Dadurch werden Fenster, Top-Line-Anker, Marker und Blockgrenzen, die auf spätere überlebende logische Zeilen zeigen, realigned. Marker auf Absatzzeilen, die bei einer Verkürzung physisch entfernt werden, werden entsprechend dem normalen Topologievertrag undefiniert.

`UserColored` bleibt auf überlebenden Deskriptorpositionen erhalten. `InBlock` wird nicht blind kopiert, sondern nach dem strukturellen Realignment erneut aus dem logischen Block projiziert.

## Undo und atomare Fehlerbehandlung

Eine erfolgreiche Neuformatierung erzeugt vor der Mutation genau einen Undo-Snapshot und markiert das Dokument als geändert. Eine wirkungslose Neuformatierung erzeugt keinen Undo-Eintrag und verändert den Dirty-State nicht. Validierungsfehler treten vor Snapshot und Pufferänderung auf.

Das ist bewusst korrektheitsorientiert. V1 optimiert weder Absatzplanung noch Snapshots oder Ankersuche vorzeitig.

## Tests

`ReformatCompatibilityTests` prüft Randumbruch bei ein- und ausgeschaltetem Wordwrap, Leerraumkomprimierung, Hochholen von Wörtern, harte Absatzgrenzen, Vergrößerung und Verkleinerung der Zeilenzahl, Marker-/Linked-Window-Realignment, Cursorerhalt, Erhalt von `UserColored`, No-op-Verhalten und die atomare Ablehnung eines zu breiten Wortes.

`InsertionCompatibilityTests` prüft zusätzlich die ausgehende `Wrapped`-Semantik beim automatischen Word-Wrap und bei gewöhnlicher Zeicheneingabe in einer bereits gewrappten Zeile.

## Verbleibende verwandte Arbeit

Die Reformat-Familie ist nun verhaltensorientiert abgebildet. Für V1 bleiben insbesondere der abschließende Bulk-Topologie-Audit für Block Copy/Move/Delete, Command-Grenzvalidierung, vollständige Abort-Unterstützung bei Langläufern und typisierte historische Fehlerressourcen. Diese Themen bleiben bewusst vom Reformat-Algorithmus getrennt, damit dessen interne Strategie später optimiert werden kann, ohne den Kompatibilitätsvertrag zu verändern.
