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

Das aktuelle .NET-Modell `EditorBlock` speichert ein vollständiges Start-/End-Paar. Historisch konnten `Blockfrom` und `Blockto` unabhängig voneinander undefiniert sein. Die exakte Parität der Begin-/End-Endpunktzustände bleibt deshalb ein eigener Audit-Punkt.

Das Handbuch legt nicht separat fest, ob ein Marker, der auf eine Zeile **innerhalb eines verschobenen Blocks** zeigt, dieser physischen Zeile an den neuen Ort folgen soll. Die .NET-Implementierung verwendet hier konservative Löschsemantik für Quellanker: Marker auf wegbewegten Quellzeilen werden ungültig; nur der logische Block wird am Ziel explizit neu aufgebaut. Das ist dokumentierte Implementierungspolitik und keine Behauptung über nicht dokumentiertes historisches Verhalten.

Das correctness-first Snapshot-Undo speichert bei einer dokumentübergreifenden Verschiebung Quelle und Ziel derzeit in zwei Snapshots. Beide Textströme sind wiederherstellbar, aber eine einzige zusammengesetzte Cross-Document-Undo-Transaktion ist noch nicht modelliert. Ein späteres Undo-Journal kann das verbessern, ohne Block- oder Topologiesemantik zu ändern.

Pascal-Pointer-Splicing, Deskriptor-Freelists und manuelle Speicherfreigabe werden nicht in die moderne API übernommen.

## Regressionstests

Die Block-Kompatibilitätstests decken Copy in ein anderes Dokument, Erhalt der Nicht-Block-Zeilenmetadaten, Realignment von verknüpften Fenstern und Markern beim Löschen, vollständiges Löschen eines Textstroms, Verschiebungen innerhalb desselben Dokuments ober- und unterhalb des Quellbereichs, Ablehnung eines Ziels im Quellblock, dokumentübergreifende Moves, die Verlagerung der Blockdefinition sowie identisches Verhalten von Command-Dispatcher und direktem Engine-Aufruf ab.
