# Typisierte MicroStar-Druckdatensätze

Der F10-Druck-in-Datei-Ablauf unterscheidet nun ausdrücklich zwischen
`Text` und `PageBreak`. Eine Dokumentzeile, die ausschließlich ein
Form-Feed-Zeichen enthält, bleibt eine **Textzeile** mit Zeilenende.
Ein durch die Seitenaufteilung eingefügter Seitenwechsel wird als
einzelnes Form-Feed-Byte ohne nachfolgendes Zeilenende ausgegeben.

`MicroStarTypedPrintPagination` übernimmt einen Snapshot des Dokuments.
`MicroStarPrintJob` verarbeitet je Scheduler-Schritt höchstens einen
Datensatz und `MicroStarPrintFileSink` übernimmt die Kodierung.

Die alte String-Schnittstelle bleibt vorerst für vorhandene Aufrufer
erhalten. Der produktive F10-Dateidruck nutzt den typisierten Pfad.
Echte Druckerhardware, Seiteneinstellungen und die genaue historische
Borland-`PrintNext`-Semantik sind weiterhin nicht abgenommen.

## Prüfung der vollständigen Druckanweisungen

Vor der Scheduler-Anmeldung werden alle typisierten Datensätze geprüft:
Textdatensätze benötigen Text, Seitenwechsel dürfen keinen Text tragen
und unbekannte Datensatzarten werden abgewiesen. Ein ungültiger späterer
Datensatz kann dadurch nicht erst nach teilweiser Ausgabe entdeckt werden.
Scheitert die Prüfung, verbleibt die Verantwortung für die Freigabe des
Ausgabeadapters beim Aufrufer. Ein Regressionstest prüft, dass weder
Scheduler-Arbeit noch Ausgabebytes entstehen.
