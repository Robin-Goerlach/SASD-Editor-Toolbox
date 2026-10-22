# Vorbereitetes Öffnen – wiederverwendbarer Editor-Core

`EditorFileService.OpenReplacingCurrentWindowAsync` führt das
**Lesen–Prüfen–Vorbereiten–Umschalten** eines Dokuments durch. Anders
als `Read File` ersetzt diese Funktion die Dokumentzuordnung des
aufrufenden Fensters, statt Zeilen einzufügen.

Die bisherige Dokumentidentität, Änderungsnummer, Schmutzkennzeichnung
und der gesamte Text werden vor dem asynchronen Einlesen festgehalten.
Erst nach erfolgreicher Dekodierung, Validierung und erneuter Prüfung
wird das Fenster auf das neue Dokument umgehängt. Bei Fehlern bleibt
das bisherige Dokument unverändert. Weitere Fenster mit derselben
alten Datei behalten ihre Zuordnung.

Ein geändertes Dokument darf standardmäßig nicht verworfen werden.
Der Parameter `allowDiscardDirty` setzt eine **ausdrückliche
Genehmigung des Aufrufers** voraus. Die Bestätigung und das Speichern
gehören weiterhin in den Anwendungs-Workflow.

Diese Änderung implementiert die sichere Core-Grundlage. Die komplette
MicroStar-Menüanbindung folgt separat. Tests behandeln verknüpfte
Fenster, schmutzige Dokumente, fehlerhafte Dateien und Leereinträge.
