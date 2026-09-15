# Typeahead, Makro-Eingabe und destruktives Löschen des Fenstertexts

## Umfang dieses Transfers

Dieser Milestone überträgt zwei leicht zu übersehende Low-Level-Verhaltensweisen der Turbo Editor Toolbox:

1. den editor-eigenen Typeahead-Puffer zwischen physischer Tastatureingabe und Command-Klassifizierung;
2. `EditWindowDeleteText`, das den im aktuellen Fenster angezeigten Text-Stream bewusst zerstört, ohne den gelöschten Text auf den Undo-Stack zu legen.

Die Umsetzung bleibt eine Clean-Room-Übertragung des dokumentierten Verhaltens. Pascal-Ringpuffer, DOS-Scan-Codes, Pointervariablen und historische Speicherverwaltung werden **nicht** nachgebaut.

## Historisches Eingabeverhalten in moderner Form

Das Handbuch beschreibt eine Standardkapazität von 500 Typeahead-Einträgen und zwei Einfügerichtungen:

- physische Tastatureingabe wird wie in einer Queue hinten angefügt (`Pokechr`-Prinzip);
- `EditPushtbf` legt ein Zeichen vorne ab, sodass es als Nächstes gelesen wird;
- `EditUserpush` legt eine komplette Zeichenfolge in umgekehrter Einfügereihenfolge vorne ab, damit sie anschließend wieder in ihrer natürlichen Reihenfolge gelesen wird.

`EditorTypeaheadBuffer` erhält diese beobachtbaren Regeln, speichert jedoch `EditorKeyStroke` statt roher Bytes. Dadurch bleibt der Core unabhängig von Terminal, WPF, WinForms, Browser oder einem späteren Script-Host.

Die API trennt die Fälle bewusst deutlich:

- `EnqueueFromHost` - einen normalisierten Host-/Tastendruck hinten anfügen;
- `PushNext` - einen normalisierten Tastendruck vorne einfügen;
- `PushSequence` - eine Sequenz vorne einfügen und ihre natürliche Wiedergabereihenfolge erhalten;
- `PushText` - Komfortfunktion für Makro-Strings einschließlich klassischer ASCII-Control-Zeichen;
- `TryRead` - die nächste logische Eingabeeinheit entnehmen;
- `ConsumeAbortRequest` - den historischen Abort-Zustand auslesen und zurücksetzen.

Die Standardkapazität beträgt 500. Würde eine Eingabe den Puffer überlaufen lassen, wird der wartende Inhalt gelöscht. `PushSequence` prüft die Kapazität vor dem Einfügen, statt erst mitten in der Schleife den Überlauf zu entdecken. Der beobachtbare Endzustand entspricht dem historischen Verhalten: Der wartende Typeahead-Puffer ist leer.

## Ctrl-U / EditAbort

Das Handbuch behandelt Ctrl-U auf dem physischen Eingabepfad (`Pokechr`) besonders: Abort wird sofort ausgelöst und nicht hinter bereits gepufferten Kommandos eingereiht. Deshalb fängt `EnqueueFromHost` ein normalisiertes Ctrl-U unmittelbar ab, leert den Typeahead-Puffer, setzt `AbortRequested` und legt Ctrl-U nicht in die Queue.

Ein über `PushNext` oder `PushSequence` eingefügtes Ctrl-U löst dagegen **keinen** sofortigen Abort aus. Diese Unterscheidung ist beabsichtigt: Laut Handbuch gehört das direkte Abort-Verhalten zu `Pokechr`, während `EditPushtbf` ein eigener Front-Einfügepfad ist.

Der Terminalhost setzt bei einem unmittelbaren Abort zusätzlich einen eventuell wartenden Prefix-Zustand zurück und zeigt eine Meldung an. `AbortRequested` bleibt zugleich als Core-Zustand für später vollständig übertragene, unterbrechbare Langläufer verfügbar. Die Verdrahtung dieses Flags mit jeder historischen Langlauf-Routine ist ein eigener Punkt des abschließenden Prozedur-Audits; moderne asynchrone Routinen besitzen zusätzlich `CancellationToken`.

## Eingabefluss des Terminalhosts

Der interaktive FIRST-ED-Host arbeitet jetzt logisch so:

```text
Console-Taste
   -> ConsoleKeyTranslator
   -> EditorTypeaheadBuffer.EnqueueFromHost
   -> EditorTypeaheadBuffer.TryRead
   -> FirstEdKeyMap
   -> EditorCommandDispatcher
   -> Editor-Services / Engine
```

Makro-Eingaben beginnen bereits auf der Typeahead-Stufe und laufen danach durch dieselbe Keymap und denselben Dispatcher wie echte Tastatureingaben. Das ist auch für spätere Makros, Skripte und reproduzierbare Editor-Demos nützlich.

## EditWindowDeleteText

`EditorCommandId.DeleteWindowText` und `EditorSession.DeleteCurrentWindowText()` bilden die historische destruktive Operation ab.

Beobachtbares Kompatibilitätsverhalten:

- der komplette aktuelle Text-Stream geht verloren;
- die Operation erzeugt keinen Undo-Eintrag;
- vorhandene Undo-Snapshots des zerstörten Dokuments werden verworfen;
- das betroffene Fenster erhält ein leeres `NONAME`-Dokument mit genau einer leeren logischen Zeile;
- waren mehrere Fenster mit demselben Dokument verknüpft, verlieren alle diese Views den alten Stream und die Verknüpfung wird aufgehoben;
- ein zu diesem Stream gehörender aktiver Block wird entfernt;
- Cursor- und Scrollpositionen der betroffenen Views werden auf den Anfang ihrer neuen leeren Dokumente gesetzt.

Historisch wurde die Verknüpfung über Pointer auf Text-Streams verwaltet. Die .NET-Umsetzung hängt stattdessen jedes betroffene Fenster an ein **eigenes neues leeres `EditorDocument`**. Damit ist der alte Text wirklich verschwunden und die Fenster sind nicht mehr miteinander verknüpft, ohne unsichere Pointer-Lebenszyklen nachzubauen.

### Marker-Bereinigung als moderne Sicherheitsregel

Die Beschreibung von `EditWindowDeleteText` nennt ausdrücklich das Entfernen des aktiven Blocks, legt in der von uns ausgewerteten Referenz aber kein Marker-Verhalten fest. SASD entfernt Marker, die auf die zerstörte Dokumentidentität zeigen, damit keine toten Referenzen erhalten bleiben. Das ist eine dokumentierte moderne Konsistenzregel und keine Behauptung über das historische Borland-Verhalten.

## Hinweis zur Tastenzuordnung

Die semantische Operation `DeleteWindowText` steht Hosts, Menüs, Skripten und späteren Kompatibilitätsprofilen bereits zur Verfügung. In diesem Schritt erfinden wir bewusst keine neue FIRST-ED-Tastenkombination. Eine Zuordnung kommt erst hinzu, wenn der entsprechende Default-Editor-Eintrag im Command-Audit eindeutig bestätigt ist. MicroStar-spezifische Bestätigungsdialoge gehören entsprechend in das spätere MicroStar-Beispiel/Profil und nicht in den wiederverwendbaren semantischen Core.

## Tests

Die Regressionstests prüfen unter anderem FIFO-Hosteingabe, Front-Priorität, Sequenzreihenfolge, Control-Zeichen in Makros, unmittelbares Ctrl-U vom Host, bewusst nicht sofort abgebrochenes Makro-Ctrl-U, Overflow-Clearing sowie das destruktive und nicht rückgängig machbare Löschen inklusive verknüpfter Fenster, Block-/Marker-Bereinigung und unveränderter unabhängiger Dokumente.
