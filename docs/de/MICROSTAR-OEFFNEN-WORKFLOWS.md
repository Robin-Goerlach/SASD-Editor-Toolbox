# MicroStar – Vorprüfung für Save & Open / Abandon & Open

Der neue `MicroStarOpenPolicy` definiert die Sicherheitsregeln für
den späteren Wechsel zwischen Dokumenten.

`SaveThenOpen` darf erst nach einem **erfolgreichen Speichern** das
Dokument wechseln. Ein fehlgeschlagenes Speichern ist niemals eine
stillschweigende Zustimmung zum Verwerfen.

`AbandonThenOpen` verlangt bei ungespeicherten Änderungen eine
**gesonderte ausdrückliche Bestätigung**. Ein Klick auf den Menüpunkt
allein reicht nicht. `Cancel` erlaubt grundsätzlich keinen Wechsel.

Vor dem eigentlichen Umschalten muss außerdem erneut geprüft werden,
ob sich das Ausgangsdokument während asynchroner Dateioperationen
verändert hat. Die Entscheidungsschicht enthält weder Terminalcode
noch eigentliche Dateioperationen oder Änderungen an Editorfenstern.

**Stand:** Die überprüfbare Sicherheitslogik und ihre Regressionstests
sind implementiert. Die vollständige Dateiwechsel-Funktion ist noch
nicht an das F10-Menü angebunden. Dafür folgt ein gesonderter Schritt
mit vorbereitetem Einlesen, sicherem Fensterwechsel und Undo-Tests.
