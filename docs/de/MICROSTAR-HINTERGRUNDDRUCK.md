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


## Abbruch während einer bereits begonnenen Ausgabe

Ein Abbruch kann eintreten, nachdem der Ausgabeadapter bereits mit
`WriteLineAsync` begonnen hat. Der Druckauftrag beendet sich jetzt
auch in diesem Fall kontrolliert, schließt einen eigenen Ausgabekanal
und gibt die `OperationCanceledException` an den Aufrufer weiter.

Der Fortschrittszähler wird für einen abgebrochenen Datensatz nicht
erhöht. Es erfolgt kein automatischer Wiederholungsversuch, denn der
Drucker oder das Dateisystem könnte bereits einen Teil übernommen
haben. Ein deterministischer Regressionstest prüft den Abbruch
innerhalb der Ausgabe, die Freigabe der Ressource und das Ausbleiben
weiterer Ausgabeschritte.

Dies ist eine moderne Sicherheitsgarantie, keine Behauptung über die
exakte historische Borland-`PrintNext`-Semantik.
