# Zusammengesetzte Undo-Transaktionen und Navigationszustand

## Zweck

Die erste .NET-Implementierung verwendet bewusst ein correctness-first Snapshot-Undo. Die historische Turbo Editor Toolbox besaß ein anderes, zeilenorientiertes Undo-Modell: `EditUndo` nimmt eine gespeicherte Textzeile vom Undo-Stack und fügt sie oberhalb der aktuellen Zeile ein; bei Block Delete beschreibt das Handbuch außerdem, dass die Low-Level-Löschung gelöschte Zeilen für Undo vorbereitet.

Dieses historische Speichermodell definiert **nicht** die Semantik einer modernen Befehlstransaktion über zwei Dokumente. SASD behandelt zusammengesetztes Command-Undo deshalb ausdrücklich als Modernisierungsgrenze.

## Entscheidung: eine logische Mutation, ein History-Eintrag

Ein erfolgreich abgeschlossener logischer SASD-Mutationsbefehl erzeugt höchstens einen Undo-History-Eintrag.

Besonders wichtig ist das bei Block Move zwischen zwei Dokumenten. Der Befehl entfernt Zeilen aus der Quelle und fügt sie in das Ziel ein. Würden beide Änderungen als zwei unabhängige benutzersichtbare Undo-Einträge gespeichert, könnte ein einzelnes Undo die Quelle wiederherstellen und gleichzeitig die Zielkopie stehen lassen. Der verschobene Text wäre vorübergehend doppelt vorhanden.

`EditorUndoManager.BeginTransaction` akzeptiert deshalb ein oder mehrere beteiligte Fenster, de-dupliziert deren Dokumente und erfasst jedes betroffene Dokument vor der Mutation. `EditorUndoTransaction.Commit` trägt die Transaktion erst ein, nachdem Textmutation, Topologie-Reparatur und endgültiger Navigationszustand des Befehls abgeschlossen sind.

Ein dokumentübergreifender Block Move erhöht `EditorUndoManager.Count` folglich genau um eins.

## Warum Begin/Commit explizit ist

Die frühere `Capture`-API legte sofort einen Snapshot auf den Stack. Für eine sehr kleine Ein-Dokument-Mutation genügt das, sie kann aber nicht unterscheiden, ob ein Befehl danach fehlschlägt oder erfolgreich endet, und sie kennt den endgültigen Cursor-/Ankerzustand des Befehls nicht.

Die Transaktions-API trennt deshalb beide Zeitpunkte:

```text
BeginTransaction
    -> Vorzustand von Dokumenten + Session erfassen
Mutation ausführen
Topologie reparieren / endgültigen Cursor setzen
Commit
    -> Nachzustand der Session erfassen
    -> genau einen History-Eintrag anlegen
```

Wirft der Befehl eine Exception oder verlässt den Pfad vor `Commit`, wird die vorbereitete Transaktion nicht in die History aufgenommen.

Die ältere Methode `Capture(EditorWindow, ...)` bleibt als Low-Level-Kompatibilitäts-API für bestehende Aufrufer verfügbar. Repository-eigene Mutationspfade verwenden jetzt jedoch explizite Transaktionen.

## Wiederhergestellter Zustand

Dokument-Snapshots stellen wieder her:

- logischen Zeilentext und Zeilenflags;
- Persistenzmetadaten;
- Dirty-Zustand;
- Dokumentversion.

Zusätzlich speichert die Transaktion Session-Zustand, den strukturelle Bearbeitung indirekt verändern kann:

- Cursor, TopLine und LeftColumn jedes angezeigten Fensters;
- Identität des aktuellen Fensters;
- nummerierte Marker;
- unabhängige Begin-/End-Grenzen des Blocks.

Fenster-Editieroptionen, gestapelte Fenstergeometrie sowie die Zuordnung Fenster/Dokument gehören nicht zu einer Texttransaktion, weil die auditierten Mutationsbefehle diese Bereiche nicht besitzen.

## Spätere ausdrückliche Benutzeraktionen erhalten

Undo muss durch den Befehl verursachte Topologieänderungen reparieren, darf aber Navigation *nach* der Bearbeitung nicht blind zurücksetzen.

Deshalb speichert die Transaktion sowohl den Vor- als auch den Nachzustand der Session. Bei Undo wird ein Fenster-, Marker-, Block- oder Current-Window-Zustand nur dann auf den Vorzustand zurückgesetzt, wenn sein aktueller Wert noch dem gespeicherten Nachzustand des Befehls entspricht. Eine spätere ausdrückliche Cursorbewegung, Markerzuweisung oder Blockneudefinition gewinnt damit.

Nach dem Wiederherstellen der Dokumente werden verbleibende Marker und Blockendpunkte gegen die wiederhergestellten Textströme normalisiert. Liegt ein später gesetzter Anker auf einer Zeile, die nach Undo nicht mehr existiert, wird er invalidiert, statt eine Referenz außerhalb des Dokuments zu hinterlassen.

Dieser Post-State-Vergleich ist eine correctness-first Policy; wir müssen dafür keine stabilen Objektidentitäten für jede einzelne Zeile einführen, nur um automatisches Topologie-Realignment von späterer Benutzernavigation zu unterscheiden.

## Destruktive Befehle und Compound History

`EditWindowDeleteText` bleibt bewusst destruktiv und nicht undo-fähig. Wird ein Dokument zerstört, entfernt `EditorUndoManager.DiscardDocument` jede Transaktion, die dieses Dokument referenziert.

Bei einer zusammengesetzten Transaktion geschieht das vollständig oder gar nicht. Referenziert ein dokumentübergreifender Move Quelle und Ziel und wird eine Seite absichtlich zerstört, bleibt nicht die Zielhälfte als separat undo-fähiges Fragment erhalten.

## Undo-Limit

`EditorUndoManager.Limit` zählt logische Transaktionen. Ein Zwei-Dokument-Move verbraucht einen Platz und nicht zwei.

Der FIRST-ED-Kompatibilitätsbefehl zum Setzen des Limits verlangt weiterhin mindestens eins. Der darunterliegende moderne Manager darf mit direkter Anwendungskonfiguration den Wert null verwenden, um Capture vollständig abzuschalten.

## Regressionstests

`CompoundUndoCompatibilityTests` prüft:

- ein dokumentübergreifender Block Move erzeugt genau einen Undo-Eintrag;
- ein einziges Undo stellt Quelle und Ziel wieder her;
- Cursor-/TopLine-/LeftColumn-Zustand verknüpfter Fenster wird restauriert;
- durch Topologie veränderte Blockgrenzen und Markerpositionen werden restauriert;
- spätere ausdrückliche Navigation und Markeränderungen bleiben erhalten, sofern sie gültig sind;
- das Zerstören eines Dokuments verwirft die gesamte Compound-Transaktion.

Das Snapshot-Design bevorzugt bewusst nachvollziehbare Korrektheit gegenüber Speichereffizienz. Ein späteres Delta-/Journal-Backend kann die Speicherstrategie ersetzen, ohne diese Transaktionssemantik zu verändern.


## Fensteridentität nach Link Window – Sicherheit des älteren Capture-Zugangs

Das bevorzugte transaktionsbasierte Undo speichert zu jedem Fenster sowohl `WindowId` als auch das zugeordnete `DocumentId`. Navigationszustände werden nur bei weiterhin passender Zuordnung wiederhergestellt. Der ältere öffentliche Aufruf `EditorUndoManager.Capture(EditorWindow, description)` bleibt für bestehende Anwendungen verfügbar. Bisher stellte er seine Cursorposition allein anhand der `WindowId` wieder her. Wenn der historische Befehl **Link Window** das Fenster zwischenzeitlich mit einem anderen Textstrom verbunden hatte, konnte Undo das falsche Fenster aktivieren und die alte Cursorposition in ein fremdes Dokument übertragen. Die Textwiederherstellung selbst erfolgte bereits korrekt anhand der `DocumentId`.

`RestoreLegacyCursor` prüft jetzt zusätzlich die Dokumentidentität. Ist das ursprüngliche Fenster **noch vorhanden, aber umgekoppelt**, bleiben dessen Navigation und die aktuelle Fensterauswahl unangetastet; der ursprüngliche Text wird dennoch über ein weiterhin verbundenes Geschwisterfenster restauriert. Wurde das ursprüngliche Fenster **geschlossen**, darf die bisherige Fallback-Regel ein überlebendes Geschwisterfenster des alten Dokuments auswählen. Ohne Umkopplung funktioniert die gewohnte Cursorwiederherstellung weiterhin. Der Fix führt weder Pascal-Zeigerlogik ein noch verändert er die atomare Undo-Buchführung.

Vier direkte Regressionstests in `UndoWindowIdentityTests` prüfen Umkopplung, unveränderten Capture-Zugang, Fallback nach Schließen und den modernen Transaktionsweg. Ein portabler Fall `windows.undo-relinked-view-preserves-unrelated-current-window` belegt außerdem das hostneutrale Navigationsverhalten. Das sind konkrete Regressionsnachweise und keine Behauptungen über den ursprünglichen DOS-Undo-Stack.


### Bewusste Marker-Neuzuordnung nach dokumentübergreifendem Block-Move

`UndoMarkerReassignmentTests` prüft zusätzlich eine unabhängige Regel der selektiven Wiederherstellung. Verschiebt Move Block einen Marker auf eine erhaltene Quellzeile oder entfernt eine markierte Zeile, macht Undo diese *befehlsbedingten* Änderungen rückgängig. Wird dagegen nach dem Verschieben ein anderer Marker vom Benutzer ausdrücklich neu gesetzt, bleibt diese spätere Entscheidung erhalten. Ein gemeinsamer Undo-Schritt restauriert beide Dokumente und Blockgrenzen, aber überschreibt nicht den unabhängig geänderten Marker. Dies ist eine moderne SASD-Verantwortlichkeitsregel und keine Nachbildung von Pascal-Zeigern.
