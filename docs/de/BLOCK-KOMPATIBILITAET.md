# FIRST-ED-Block-Kompatibilität

## Umfang

Dieses Dokument beschreibt die Clean-Room-Übertragung der Ganzzeilen-Blockoperationen der Turbo Editor Toolbox in die C#/.NET-Implementierung.

Das Handbuch dient ausschließlich als Quelle für beobachtbares Verhalten und Anforderungen. Die Pascal-Algorithmen für verkettete Deskriptoren, Pointer, Heap-Verwaltung und Quellcodestruktur werden nicht nachgebaut.

## Historisches Verhalten als Vertrag

Das Handbuch definiert jeweils einen zusammenhängenden Block aus vollständigen Zeilen. Dieser Block kann auch aus einem anderen Fenster oder einer anderen Datei/einem anderen Dokument bearbeitet werden. Die drei grundlegenden Operationen sind:

- **Copy**: Kopien der Blockzeilen an der aktuellen Cursorposition einfügen; der Quelltext bleibt unverändert.
- **Move**: Blockzeilen aus dem ursprünglichen Textstrom entfernen und an der aktuellen Cursorposition einfügen. Liegt der Cursor innerhalb des zu verschiebenden Blocks, muss die Operation abgelehnt werden.
- **Delete**: alle Zeilen des Blocks entfernen. Die technische Referenz verlangt ausdrücklich, dass das Löschen über die Low-Level-Lösch-/Realignment-Logik läuft, damit mehrere Fenster und Undo berücksichtigt werden.

Das Handbuch beschreibt diese Vorgänge mit `Blockfrom`, `Blockto`, Zeilendeskriptor-Pointern und `EditDelline`. Diese Darstellungsdetails gehören nicht zum portablen SASD-Vertrag.

## Moderne C#-Architektur

### Unabhängiger Begin-/End-Endpunktzustand

Die historischen Anfangs- und Endgrenzen werden jetzt durch `EditorBlockState` und zwei unabhängig nullable `EditorBlockEndpoint`-Werte dargestellt. `EditorSession.Block` bleibt die bequeme operative Projektion, ist aber nur dann ungleich null, wenn beide Endpunkte einen geordneten Bereich im selben Dokument bilden.

`BeginBlock` setzt den Anfangsendpunkt neu. Das Handbuch zeigt ausdrücklich, dass ein vorhandener Endendpunkt nur erhalten bleibt, wenn er im selben Textstrom weiterhin nach dem neuen Anfang erreichbar ist; andernfalls wird dieser Endendpunkt undefiniert und der Zustand bleibt verborgen. Die Prozedurbeschreibung von `EditBlockEnd` ist knapper und sagt nur, dass die Endgrenze auf die aktuelle Zeile gesetzt wird. Für den umgekehrten ungültigen Fall verwendet die .NET-Implementierung deshalb eine **dokumentierte Clean-Room-Policy**: Ein Ende vor dem vorhandenen Anfang oder in einem anderen Dokument lässt nur den neu gesetzten Endendpunkt bestehen. Damit entsteht kein umgekehrter oder dokumentübergreifender operativer Block, ohne diese abgeleitete Symmetrie als wörtlich historisch belegt darzustellen.

Diese Trennung ist auch für die Topologie wichtig. Zeileneinfügungen realignen jeden definierten Endpunkt unabhängig. Wird eine Grenzzeile gelöscht, wird nur der auf diese Zeile zeigende Endpunkt undefiniert; der andere bleibt erhalten und kann später durch einen neuen Begin-/End-Befehl ergänzt werden. Das bildet die historische Zeilendeskriptor-Wirkung modern ab, ohne Pointeridentität offenzulegen.

Ein `EditOffblock`-artiges Löschen der Hervorhebung verändert den Endpunktzustand nicht. Hide/Display kann die Hervorhebung nur wieder einschalten, wenn ein vollständiges gültiges Paar existiert.

### Copy, Move und Delete

`FirstEdBlockCompatibilityProcessor` enthält die Kompatibilitätslogik für Copy, Move und Delete. Sowohl `EditorCommandDispatcher` als auch die direkten Blockmethoden von `EditorEngine` führen durch diesen Prozessor, damit sich beide Aufrufwege nicht auseinanderentwickeln.

Strukturelle Änderungen werden an `EditorLineTopology` gemeldet. Dieser Dienst richtet aktuelle Zeilen und `TopLine` verknüpfter Fenster, Marker und den logischen Blockbereich neu aus. Der Zielcursor wird damit als Anker an seiner bisherigen logischen Zeile behandelt: Wird ein kopierter oder verschobener Block direkt davor eingefügt, ändert sich die numerische Zeilennummer, der Cursor springt aber nicht stillschweigend auf eine der eingefügten Zeilen.

### Copy

Vor dem Einfügen wird der Block als Snapshot erfasst. `InBlock` wird nicht wie gewöhnliche Zeileninformation kopiert, weil dieses Flag nur die Projektion des aktuellen logischen Blocks ist. `Wrapped` und `UserColored` bleiben erhalten. Der aktive Block bleibt der Quellblock; ändern Einfügungen im selben Dokument seine numerischen Indizes, hält das Topologie-Realignment die Grenzen an den ursprünglichen Quellzeilen.

### Delete

Der vollständige Blockbereich wird entfernt und anschließend als eine logische Löschung an die Topologieschicht gemeldet. Umfasst der Block den gesamten Textstrom, bleibt wegen der Buffer-Invariante eine einzelne leere logische Zeile bestehen. Referenzen auf gelöschte logische Zeilen werden gemäß dem normalen Löschvertrag ungültig oder neu ausgerichtet.

### Move

Die Quellzeilen werden zunächst gesichert, dann über die Topologiegrenze entfernt und direkt vor der Zielcursorzeile eingefügt. Bei einer Verschiebung innerhalb desselben Dokuments muss der Cursor außerhalb des Quellblocks liegen. Nach der Löschung liefert der bereits realignte Zielcursor den korrekten Einfügeindex; fehleranfällige manuelle Indexarithmetik ist damit nicht erforderlich.

Nach dem Einfügen wird der logische Block um die verschobenen Zeilen neu aufgebaut; sein Hidden-Zustand bleibt erhalten. Dokumentübergreifende Verschiebungen markieren beide Dokumente als geändert und realignen Referenzen in beiden Textströmen.

## Explizite Modernisierungsgrenzen

`EditorBlockState` speichert den Endpunkt-Lebenszyklus bewusst getrennt von der vollständigen `EditorBlock`-Projektion. Dadurch bleiben unabhängig undefinierte Grenzen erhalten, ohne Pointer- oder Freelist-Mechanik in das öffentliche Modell zu übernehmen.

Das Handbuch legt nicht separat fest, ob ein Marker, der auf eine Zeile **innerhalb eines verschobenen Blocks** zeigt, dieser physischen Zeile an den neuen Ort folgen soll. Die .NET-Implementierung verwendet hier konservative Löschsemantik für Quellanker: Marker auf wegbewegten Quellzeilen werden ungültig; nur der logische Block wird am Ziel explizit neu aufgebaut. Das ist dokumentierte Implementierungspolitik und keine Behauptung über nicht dokumentiertes historisches Verhalten.

Das correctness-first Snapshot-Undo speichert bei einer dokumentübergreifenden Verschiebung Quelle und Ziel derzeit in zwei Snapshots. Beide Textströme sind wiederherstellbar, aber eine einzige zusammengesetzte Cross-Document-Undo-Transaktion ist noch nicht modelliert. Ein späteres Undo-Journal kann das verbessern, ohne Block- oder Topologiesemantik zu ändern.

Pascal-Pointer-Splicing, Deskriptor-Freelists und manuelle Speicherfreigabe werden nicht in die moderne API übernommen.

## Regressionstests

Die Blocktests decken unabhängige Begin-/End-Zustände, Reihenfolge- und Dokumentgrenzen, Realignment beziehungsweise Invalidierung einzelner Endpunkte bei strukturellen Änderungen und Hide/Display bei unvollständigen Definitionen ab. Zusätzlich bleiben Copy in ein anderes Dokument, Erhalt der Nicht-Block-Zeilenmetadaten, Realignment von verknüpften Fenstern und Markern beim Löschen, vollständiges Löschen eines Textstroms, Verschiebungen innerhalb desselben Dokuments ober- und unterhalb des Quellbereichs, Ablehnung eines Ziels im Quellblock, dokumentübergreifende Moves, die Verlagerung der Blockdefinition sowie identisches Verhalten von Command-Dispatcher und direktem Engine-Aufruf abgesichert.
