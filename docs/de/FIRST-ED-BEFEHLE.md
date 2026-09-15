# FIRST-ED-Befehlsbelegung

## Zweck

Dieses Dokument beschreibt die erste Kompatibilitäts-Eingabeschicht der SASD Editor Toolbox. Grundlage sind die FIRST-ED-Kurzreferenz und die Beschreibung der Command Dispatcher im Handbuch der Turbo Editor Toolbox 1.0.

Das historische Design trennt normalen Texteingaben von Befehlen und leitet Ctrl-K, Ctrl-O und Ctrl-Q an eigene Prefix-Dispatcher weiter. SASD erhält dieses Verhalten in `FirstEdKeyMap`, ersetzt aber DOS-Scancodes durch das UI-neutrale Modell `EditorKeyStroke`.

## Architekturgrenze

`FirstEdKeyMap` verändert **keinen** Text und fragt den Benutzer **nicht** selbst nach Eingaben. Die Übersetzung liefert stattdessen:

- normalen einzufügenden Text;
- einen semantischen Editorbefehl;
- einen noch offenen Befehls-Prefix;
- oder eine nicht zugeordnete Eingabe.

Befehle, die historisch nach Dateiname, Zahl, Suchtext oder Bestätigung gefragt haben, tragen einen `EditorCommandArgumentKind`. Der jeweilige Host sammelt den Wert ein und erzeugt danach einen `EditorCommandRequest`. Dadurch bleibt die Tastaturkompatibilität unabhängig von Konsole, WPF, WinForms und Web-Oberflächen.

## Direkte Befehle

| Taste | Semantischer Befehl |
|---|---|
| Ctrl-A | Wort nach links |
| Ctrl-S | Zeichen nach links |
| Ctrl-D | Zeichen nach rechts |
| Ctrl-F | Wort nach rechts |
| Ctrl-E | Zeile nach oben |
| Ctrl-X | Zeile nach unten |
| Ctrl-C | Seite nach unten |
| Ctrl-W | Nach oben scrollen |
| Ctrl-Z | Nach unten scrollen |
| Ctrl-P | Steuerzeichen einfügen (Host-Abfrage) |
| Ctrl-J | Zeilenanfang/-ende umschalten |
| Enter / Ctrl-N | Zeile einfügen |
| Ctrl-G / Entf | Zeichen rechts löschen |
| Rücktaste / Ctrl-H | Zeichen links löschen |
| Ctrl-R | Seite nach oben |
| Ctrl-T | Wort rechts löschen |
| Ctrl-Y | Zeile löschen |
| Ctrl-B | Absatz neu formatieren |
| Ctrl-V | Insert/Overwrite umschalten |
| Ctrl-L | Nächstes Vorkommen suchen |
| Escape | Undo |

Pfeiltasten, Pos1/Ende und Bild-auf/Bild-ab werden zusätzlich als moderne Aliase akzeptiert. Die historischen Control-Tasten bleiben erhalten.

## Ctrl-K-Prefix

| Sequenz | Semantischer Befehl |
|---|---|
| Ctrl-K B | Blockanfang |
| Ctrl-K K | Blockende |
| Ctrl-K C | Block kopieren |
| Ctrl-K V | Block verschieben |
| Ctrl-K Y | Block löschen |
| Ctrl-K H | Block anzeigen/verbergen |
| Ctrl-K R | Datei lesen (Dateiname abfragen) |
| Ctrl-K W | Datei schreiben (Dateiname abfragen) |
| Ctrl-K S | Datei speichern |
| Ctrl-K T | Tabulatorbreite setzen |
| Ctrl-K X | Beenden (Bestätigung) |
| Ctrl-K M | Marker setzen (Nummer abfragen) |
| Ctrl-K 1..9 | Nummerierten Marker setzen |

## Ctrl-O-Prefix

| Sequenz | Semantischer Befehl |
|---|---|
| Ctrl-O X | Fenster nach unten / nächstes Fenster |
| Ctrl-O E | Fenster nach oben / vorheriges Fenster |
| Ctrl-O G | Zu Fenster springen |
| Ctrl-O J | Fenster verknüpfen (zwei Zahlen) |
| Ctrl-O Y | Fenster löschen |
| Ctrl-O O | Fenster erzeugen (zwei Werte der historischen Abfrage) |
| Ctrl-O W | Word-Wrap umschalten |
| Ctrl-O C | Zeile zentrieren |
| Ctrl-O I | Zu Spalte springen |
| Ctrl-O N | Zu Zeile springen |
| Ctrl-O K | Groß-/Kleinschreibung wechseln |
| Ctrl-O L | Linken Rand setzen |
| Ctrl-O R | Rechten Rand setzen |
| Ctrl-O S | Undo-Limit setzen |
| Ctrl-O 1..9 | Zu nummeriertem Fenster springen |

Der historische Create-Window-Befehl fragte sowohl nach einer Größe als auch nach einem Quellfenster. SASD modelliert deshalb bereits die Zwei-Werte-Anforderung; die konkrete Bildschirmaufteilung folgt später in der Host-/Rendering-Schicht.

## Ctrl-Q-Prefix

| Sequenz | Semantischer Befehl |
|---|---|
| Ctrl-Q C | Dateiende |
| Ctrl-Q R | Dateianfang |
| Ctrl-Q I | Auto-Indent umschalten |
| Ctrl-Q B | Blockanfang anspringen |
| Ctrl-Q K | Blockende anspringen |
| Ctrl-Q J | Marker anspringen |
| Ctrl-Q A | Suchen/Ersetzen |
| Ctrl-Q F | Suchmuster eingeben |
| Ctrl-Q D | Zeilenende |
| Ctrl-Q S | Zeilenanfang |
| Ctrl-Q Y | Bis Zeilenende löschen |
| Ctrl-Q 1..9 | Nummerierten Marker anspringen |

## Stand der Übertragung

Dieser Schritt implementiert die vollständige FIRST-ED-**Eingabezuordnung** und testet den Zustand der Prefix-Kommandos. Ein Teil der semantischen Befehle besitzt bereits Prozessoren im `EditorCommandDispatcher`; Abfragen und die noch fehlenden Datei-/Fenster-/Suchdetails werden absichtlich in weiteren, getrennt testbaren Schritten übertragen. So gelangen weder UI-Abfragen noch historische Tastaturdetails in den Editor-Kern.
