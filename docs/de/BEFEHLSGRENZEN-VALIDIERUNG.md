# Validierung an der Befehlsgrenze

## Zweck

Semantische Befehle bilden eine Vertrauensgrenze zwischen Hosts/Automation und dem wiederverwendbaren Editor-Kern. Tastaturabfragen, Menüs, Tests sowie spätere C++-/Java-Adapter können Requests erzeugen. Ungültige Argumente müssen deshalb einheitlich abgewiesen werden, bevor Text verändert oder Datei-I/O gestartet wird.

Die .NET-Referenzimplementierung bündelt die Request-Validierung in `EditorCommandValidator`. Zustandsabhängige Kompatibilitätsregeln bleiben bei dem Dienst, der den betreffenden Zustand besitzt.

## Stabile Request-Regeln

| Befehlsfamilie | Regel an der Grenze |
|---|---|
| Goto Zeile / Spalte / Fenster | einsbasierter Zahlenwert >= 1 |
| Delete Window mit expliziter Nummer | Fensternummer >= 1; ohne Nummer gilt das aktuelle Fenster |
| Create Window | Größe >= 3 und Spenderfenster >= 1 |
| Link Window | Ziel- und Quellfenster >= 1 |
| linker/rechter Rand | Zahlenwert >= 1 vor der zustandsabhängigen Reihenfolgeprüfung |
| Tabulatorbreite | >= 1 |
| historischer Undo-Limit-Befehl | >= 1 |
| Marker setzen/anspringen | Marker 1..20 |
| darstellungsabhängige Befehle | übergebene Page Size >= 1 |
| Steuerzeichen einfügen | in .NET genau ein UTF-16-Zeichen |
| Find | nichtleeres Suchmuster |
| Replace | nichtleeres Suchmuster plus Ersatzwert; der Ersatz darf leer sein |
| Read/Write File | nichtleerer aufgelöster Pfad |

Moderne Low-Level-Dienste dürfen breitere Fähigkeiten anbieten, wenn der Kompatibilitätsbefehl dadurch nicht verändert wird. Insbesondere darf der allgemeine Undo-Manager mit Limit 0 das Capture abschalten, obwohl der historische Kompatibilitätsbefehl Werte unter 1 verwirft.

## Modernisierung der Replace-Argumente

Der erste .NET-Dispatcher packte Such- und Ersatztext mit einem NUL-Trenner in einen String. Diese Transportdarstellung eignet sich nicht als sprachübergreifender semantischer Vertrag.

`EditorCommandRequest` besitzt jetzt `Text2`. Bei `ReplaceNext` ist `Text` das Suchmuster und `Text2` der Ersatztext. Ein leerer Ersatz ist gültig und löscht den Treffer.

Der Dispatcher akzeptiert das alte NUL-Packing noch als .NET-Übergangspfad, damit bestehende Aufrufer nicht brechen. Der Terminalhost erzeugt es jedoch nicht mehr und gemeinsame Conformance-Fälle hängen nicht davon ab.

## Grenzvalidierung und zustandsabhängige Validierung

Der zentrale Validator prüft nur Tatsachen, die aus dem Request selbst feststehen. Veränderlichen Editorzustand dupliziert er nicht. Randreihenfolge, Kapazität des Spenderfensters, tatsächliche Fensterauflösung und die Frage, ob eine syntaktisch gültige Markernummer aktuell definiert ist, bleiben bei den zuständigen Diensten.

## Fehlerverhalten

Bei einem Validierungsfehler liefert der Dispatcher `false`, führt keine Befehlsmutation beziehungsweise Datei-I/O aus und meldet über `IEditorHooks.OnErrorAsync` den hostneutralen Code `EDITOR_COMMAND_INVALID_ARGUMENT`.

`EditorCommandValidationFailure` enthält zusätzlich einen typisierten `EditorCommandValidationCode`, Parameternamen und einen englischen Fallback-Diagnosetext. Die inzwischen implementierte Fehler-/Ressourcenschicht bildet dies auf `EditorErrorId.InvalidCommandArgument`, den stabilen Code `EDITOR_COMMAND_INVALID_ARGUMENT` und den Ressourcenschlüssel `Editor.Errors.Command.InvalidArgument` ab; siehe [FEHLERMODELL.md](FEHLERMODELL.md).

## Tests und sprachübergreifende Conformance

`CommandBoundaryValidationTests` deckt fehlende/ungültige Zahlenwerte, Page Size, Create-/Link-Argumente, Markergrenzen, leere Dateipfade, Replace-Argumente, Undo Limit 0 sowie die Form des Steuerzeichen-Requests ab.

Gemeinsame V1-Conformance-Fälle prüfen zusätzlich ungültiges Goto Line und Undo Limit, damit spätere C++- und Java-Adapter dasselbe Grenzverhalten ohne Abhängigkeit von .NET-Exception-Typen reproduzieren können.
