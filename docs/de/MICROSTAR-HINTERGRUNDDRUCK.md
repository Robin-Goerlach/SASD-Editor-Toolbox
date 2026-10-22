# MicroStar – kooperativer Hintergrunddruck

Die Beispielanwendung erhält mit `MicroStarPrintJob` und
`MicroStarPrintQueue` eine eigene, von Druckerhardware unabhängige
Druckauftragsverwaltung.

Ein Druckauftrag übernimmt beim Start eine Momentaufnahme der
Dokumentzeilen. Pro Leerlaufzyklus des vorhandenen `EditorScheduler`
wird **höchstens eine Zeile** an die austauschbare Schnittstelle
`IMicroStarPrintSink` weitergereicht. Die Tastatur behält Vorrang.

Ein laufender Auftrag wird nicht unbemerkt durch einen zweiten ersetzt.
Abbruch verhindert weitere Ausgaben. Druckerfehler werden gespeichert;
unklare Teilausgaben werden nicht automatisch wiederholt, um doppelte
Ausdrucke zu vermeiden.

Tests sichern die Anzahl der Schritte, Momentaufnahmen, Abbruch,
Fehlerbehandlung und konkurrierende Aufträge ab.

**Noch offen:** Anbindung an das F10-Menü, realer Drucker-/Spooleradapter,
Seitenformatierung und die historische `PrintNext`-Detailtreue.
