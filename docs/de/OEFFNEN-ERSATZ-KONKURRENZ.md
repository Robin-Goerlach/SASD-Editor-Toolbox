# Dokumentwechsel: Tests für gleichzeitige Änderungen

Der Core-Dateiwechsel wird jetzt auch bei kontrolliert verzögertem
asynchronem Einlesen getestet. Die Tests halten den Decoder ohne
Zeitverzögerungs-Tricks an und verändern zwischenzeitlich die
Editorumgebung.

Abgedeckt sind eine neue Textänderung, das Schließen des aufrufenden
Fensters, ein Wechsel des aktiven Fensters, eine direkte Buffer-Änderung
ohne Versionsänderung sowie ein Abbruch der Operation.

Unmittelbar vor dem Umschalten wird nun auch das CancellationToken
erneut geprüft. Ein verspätet fertig gewordener Decoder darf deshalb
einen bereits abgebrochenen Dateiwechsel nicht nachträglich durchführen.

Die Prüfungen sichern den bestehenden Vertrag ab: Bei einem ungültigen
oder veralteten Ausgangszustand bleibt das ursprüngliche Dokument
erhalten. Die Interaktion am echten Terminal bleibt eine separate
Abnahmeaufgabe.
