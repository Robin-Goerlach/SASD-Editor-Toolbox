# MicroStar F10: Hintergrunddruck in Datei

Das F10-Menü unterstützt nun `H` zum Start eines Hintergrunddrucks
in eine neue UTF-8-Datei und `K` zum Abbrechen.

Die Ausgabedatei wird mit `FileMode.CreateNew` angelegt: vorhandene
Dateien werden nicht überschrieben. Der Druckauftrag übernimmt einen
Text-Snapshot. Pro Leerlaufzyklus wird höchstens eine Zeile ausgegeben,
wobei die Eingabe Vorrang hat.

`MicroStarPrintFileSink` schließt die Datei bei Abschluss, Abbruch
oder behandeltem Druckfehler. Ein bereits teilweise erzeugtes Dokument
bleibt sichtbar und wird ausdrücklich nicht als vollständiger Ausdruck
ausgegeben.

Die Tastenkürzel sind moderne SASD-Entscheidungen. Reale Druckertreiber,
Seitenformatierung und genaue historische `PrintNext`-Semantik fehlen
weiterhin. Tests prüfen erfolgreiche Ausgabe, Überschreibschutz und
Abbruch.
