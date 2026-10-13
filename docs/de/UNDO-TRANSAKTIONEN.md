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
