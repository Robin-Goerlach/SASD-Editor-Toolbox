# FIRST-ED-Kompatibilität für Zeileneinfügung und New Line

## Umfang

Dieses Dokument beschreibt die Clean-Room-Übertragung des historischen Verhaltens von `EditInsertLine` und `EditNewLine` in die .NET-Kompatibilitätsschicht. Die Unterscheidung ist wichtig, weil FIRST-ED **Ctrl-N** und **Return** unterschiedliche Bedeutungen gibt, obwohl beide Operationen eine logische Zeile erzeugen können.

Die Implementierung erhält bewusst das beobachtbare Editorverhalten und baut weder Turbo-Pascal-Zeilendeskriptor-Pointer noch Heap-Verwaltung oder DOS-Bildschirmmechanik künstlich nach.

## Zwei getrennte semantische Befehle

Der portable Befehlswortschatz unterscheidet jetzt:

- `EditorCommandId.InsertLine` — Gegenstück zu `EditInsertLine` und zum FIRST-ED-Befehl Ctrl-N;
- `EditorCommandId.NewLine` — Gegenstück zu `EditNewLine`, ausgelöst durch Return.

`FirstEdKeyMap` ordnet deshalb Ctrl-N `InsertLine` und Enter/Return `NewLine` zu. So geht das historische Insert-/Overtype-Verhalten von Return nicht in einer zu allgemeinen Zeilenoperation verloren.

## EditInsertLine

`FirstEdPrimitiveCompatibilityProcessor.InsertLine` bildet die drei dokumentierten Fälle um die aktuelle Cursorposition ab:

1. **Spalte null:** Die aktuelle Zeile wird leer; ihr kompletter Text wandert in die neu darunter eingefügte Zeile. Aus Benutzersicht wird damit eine leere Zeile oberhalb des bisherigen Textes eingefügt.
2. **An oder hinter dem letzten Nicht-Leerzeichen:** Unter der aktuellen Zeile wird eine leere logische Zeile eingefügt.
3. **Innerhalb des inhaltlichen Textes:** Die aktuelle Zeile wird am Cursor geteilt; der Text rechts vom Cursor wandert in die neue untere Zeile.

Das aktuelle Fenster bleibt auf der oberen Zeile. Eine erfolgreiche Operation erzeugt einen Undo-Snapshot, setzt den Dirty-State und meldet die neue Zeile an `EditorLineTopology`, damit verknüpfte Fenster, Marker und Blockgrenzen realigned werden.

Die historische Implementierung drückt diese Beziehungen über Identitäten verketteter Zeilendeskriptoren aus. Die .NET-Portierung verwendet Dokumentidentität plus logische Zeilennummern. Wo das Handbuch nicht festlegt, welcher fremde Anker nach einem Split an welchem Deskriptor verbleiben soll, gilt die dokumentierte V1-Regel von `EditorLineTopology`; wir erfinden dafür keine Pointer-Simulation.

## EditNewLine / Return

`EditNewLine` ist vom Modus abhängig.

### Insert-Modus

Return führt denselben strukturellen Split wie `EditInsertLine` aus, der aktive Cursor folgt dem abgetrennten Text jedoch auf die neue untere Zeile. Autoindent verändert nur die resultierende Cursorspalte: Ist Autoindent aktiv, steht der Cursor unter dem ersten Nicht-Leerzeichen der zuvor aktuellen Zeile. Ist diese Zeile leer oder Autoindent deaktiviert, steht der Cursor in Spalte null.

Das `Wrapped`-Flag der zuvor aktuellen Zeile wird gelöscht. Ein explizites Return markiert damit für die historische Reformat-Logik eine Absatzgrenze.

### Overtype-Modus

Return ist normalerweise nur Cursorbewegung: Der Cursor geht auf die folgende logische Zeile, ohne die aktuelle Zeile zu teilen. Befand er sich bereits auf der letzten Zeile des Textstroms, wird eine neue leere Zeile angehängt und der Cursor dorthin bewegt.

Für Autoindent gilt dieselbe Spaltenregel wie im Insert-Modus. Das Löschen eines zuvor gesetzten `Wrapped`-Flags ist trotzdem eine Textzustandsänderung; deshalb erzeugt die .NET-Implementierung Undo und Dirty-State, wenn sich dieses Flag tatsächlich ändert, auch wenn keine Zeile eingefügt wird.

## Strukturelles Realignment

Jede logische Zeileneinfügung dieser Kompatibilitätsbefehle ruft `EditorLineTopology.LinesInserted` auf. Dieselbe Koordination verwendet jetzt auch die allgemeine Engine, wenn ihr generischer Newline- oder automatischer Wordwrap-Pfad eine Zeile einfügt.

Damit wird eine wichtige Klasse veralteter Referenzen verhindert: Ein verknüpftes Fenster unterhalb der Einfügestelle, ein Marker auf einer späteren Zeile oder eine spätere Blockgrenze wandert mit dem Dokument, statt auf der alten numerischen Position stehen zu bleiben.

Das historische `EditRealign` aktualisiert zeilenbezogenen Fensterzustand nach strukturellen Änderungen; es definiert keine Regel, die die Cursorspalte auf die physische Textlänge verkürzt. `EditorWindow.ClampCursor` normalisiert deshalb nun Zeile und Viewport, erhält aber nichtnegative virtuelle Spalten.

## Wrapped-Fortsetzungszeilen

Der allgemeine Wordwrap-Pfad erzeugt die neue untere Zeile mit `EditorLineFlags.Wrapped` und meldet die Einfügung ebenfalls an den Topologie-Koordinator. Das passt dazu, dass die Reformat-Helfer anhand des `Wrapped`-Bits der Fortsetzungszeile entscheiden, ob der Absatz weitergeht.

Der exakte Algorithmus von `EditLongLine`, `EditShortLine`, `EditShiftLine` und `EditReformat` ist ein eigener Kompatibilitäts-Meilenstein. Dieser Schritt schafft die strukturellen Invarianten dafür, statt den vorhandenen Absatzformatierer vorschnell zu ersetzen.

## Undo- und Optimierungspolitik

V1 bevorzugt Korrektheit und Prüfbarkeit. Eine einzelne kompatible Zeileneinfügung bzw. New-Line-Operation erzeugt genau einen Snapshot-Undo-Eintrag, wenn Text, Zeilenstruktur oder Flags verändert werden. Die Implementierung wird absichtlich noch nicht auf Descriptor-Deltas optimiert; die vorhandene Undo-Grenze erlaubt später ein kompakteres Journal, ohne die Befehlsemantik zu ändern.

## Regressionstests

`InsertionCompatibilityTests` prüft:

- Return und Ctrl-N als getrennte semantische Befehle;
- alle drei Fälle von `EditInsertLine`;
- Cursorverbleib bei Insert Line;
- Insert-Mode-New-Line mit Cursorbewegung und Autoindent;
- Overtype-New-Line ohne unnötige Textmutation;
- das Anhängen einer letzten Leerzeile im Overtype-Modus;
- das Löschen des `Wrapped`-Flags der vorherigen Zeile;
- Realignment von verknüpftem Fenster und Marker beim automatischen Wordwrap.

Die Join-Seite der Zeilenlöschung ist separat durch `DeletionCompatibilityTests` abgesichert, sodass Einfüge- und Lösch-Topologie aus beiden Richtungen getestet werden.
