# MicroStar – markierten Block als Datei schreiben

Das Handbuch beschreibt das Schreiben eines markierten Blocks als eigene
MicroStar-Funktion, zusätzlich zum Schreiben des gesamten Dokuments.
Das Beispielmenü bietet hierfür das moderne Kürzel `T` an (keine
Behauptung historischer Tastaturidentität).

Ein vollständiger Zeilenblock ist erforderlich. Der Export erzeugt eine
Momentaufnahme der ausgewählten Zeilen im **Quelltext**, auch wenn ein
anderes Editorfenster aktiv ist. Das reine Darstellungsflag `InBlock`
wird nicht auf die Datei übertragen, die echte `Wrapped`-Zeilenstruktur
bleibt erhalten. Vor dem Überschreiben einer vorhandenen Datei muss
ausdrücklich `YES` eingegeben werden.

Weder Dateizuweisung noch Dirty, Text, Cursor oder Undo werden durch den
Export verändert. Tests prüfen die Abgrenzung, fehlende Blockdefinition
und einen vollständigen Dateicodec-Roundtrip.

Offen: **Blockdatei einlesen**, echte MicroStar-Popups, Dateiverzeichnis,
Hintergrunddruck und gegebenenfalls Lightning-Anbindung.
