# MicroStar F10: Dokumentwechsel

Im F10-Menü gibt es jetzt **P Save/Open** und **A Abandon/Open**.
Die Dateiauswahl ist zunächst rein lesend.

Save/Open verwendet die vorhandenen Speichern-Dialoge und deren
Schutz gegen ungewolltes Überschreiben. Fehlgeschlagenes oder
abgebrochenes Speichern verhindert den Dateiwechsel.

Abandon/Open verlangt bei ungespeicherten Änderungen die explizite
Eingabe `DISCARD`. Die Auswahl des Menübefehls allein reicht
nicht als Einwilligung zum Verwerfen.

Der Core lädt und prüft das neue Dokument vor dem eigentlichen
Fensterwechsel. Verknüpfte andere Fenster behalten das vorherige
Dokument. Abweichende Zwischenzustände führen zum Abbruch.

Die Tastenkürzel sind moderne SASD-Zuordnungen. Ein manueller
interaktiver Abnahmetest am Terminal steht noch aus.
