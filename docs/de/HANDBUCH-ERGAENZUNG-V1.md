# Handbuchergänzung – Namen außerhalb des gedruckten 123er-Index

Der gedruckte *Procedures and Functions Index* des Handbuchs (S. 243-244)
führt 123 Seiten mit Routinenbeschreibungen (S. 118-240) auf. Er erfasst
**nicht** sämtliche im Fließtext, in Beispielen, Hooks oder in MicroStar
erwähnten Namen.

`docs/handbook-supplemental-symbols-v1.json` enthält jetzt **20
zusätzlich belegte Symbole**: Toolbox-Helfer, fünf Erweiterungs-Hooks,
ein zusammengesetztes Beispiel und drei MicroStar-Beispielhelfer.
Zu jedem Namen werden die gedruckte Handbuchseite, die Ebene, die
Umsetzungsklassifikation, der Repository-Beleg und die Begründung
aufgeführt. Der Contract-Validator verhindert Dopplungen zwischen
beiden Inventaren.

Dies ist **ausdrücklich ein nicht vollständiger, fortsetzbarer Audit**.
Die bloße Erwähnung eines Helfernamens liefert keine belastbare
historische Signatur. Diese wird daher auch nicht erfunden.

## Noch offene, im Handbuch belegte Funktionen

| Handbuchbereich | Status |
| --- | --- |
| FIRST-ED-Farben, Blockmarkierung und Randkennzeichen (S. 69-72, 137-138) | Konsolenhost umgesetzt, reale Terminalabnahme noch offen |
| Such-/Ersetzoptionen mit `Optstr` (Rückseite, Variablen S. 113) | Modernes C/W/R-Eingabeformat im Host; Original-Buchstabencode weiterhin unbelegt |
| `EDITERR.MSG` (S. 54) | Bewusst durch moderne typisierte Fehler ersetzt |
| MicroStar-Pulldown-Menüs, F10 (S. 42, 44-52) | Noch nicht umgesetzt |
| MicroStar `MakeWindow`/`RestoreWindow` (S. 53) | Noch nicht umgesetzt |
| MicroStar `PrintNext` (S. 53) | Scheduler vorhanden, Druckfunktion fehlt |
| MicroStar-Dateiverzeichnis und Rename/Copy/Delete (S. 42) | Noch nicht umgesetzt |
| MicroStar Read Block / Write Block (S. 45) | Als blockbasierte Dateioperationen offen |
| MicroStar Save & Open / Abandon & Open (S. 45) | Zusammengesetzte Abläufe offen |
| MicroStar/Lightning-Rechtschreibprüfung (S. 45, 98) | Optionale Integration offen |

MicroStar-Funktionen gehören zu einem **separaten Beispielhost** und
werden nicht künstlich zur verpflichtenden Editor-Core-API.
