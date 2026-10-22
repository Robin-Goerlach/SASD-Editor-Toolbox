# MicroStar – Seitenaufteilung beim Hintergrunddruck

`MicroStarPrintPagination.Prepare` erzeugt vor dem Druckauftrag eine
unveränderliche Folge von Ausgabezeilen. Als **moderne SASD-Voreinstellung**
gelten 60 Textzeilen pro Seite. Zwischen Seiten wird ein Form-Feed-
Datensatz eingefügt, niemals nach der letzten vollständigen Seite.
Leerzeilen zählen zur Seitenlänge.

Der vorhandene kooperative Scheduler gibt weiterhin maximal einen
Datensatz je Leerlaufzyklus aus. Die konkrete historische
`PrintNext`-Formatierung ist dadurch **nicht** nachgewiesen.
Zeilenumbruch, Kopf-/Fußzeilen, Druckerränder und echte Druckertreiber
sind weiterhin offene Punkte.

Zusätzliche Tests prüfen Seitenwechsel, leere Eingaben und die
Unveränderlichkeit vorbereiteter Ausdrucke.
