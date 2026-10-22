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

## Form-Feed in der Druckdatei

Der Dateiausgabeadapter schreibt einen Seitenwechsel als einzelnes
Byte `0x0C` ohne zusätzliches Zeilenende. Normale Textzeilen behalten
das plattformübliche Zeilenende. Ein Byte-genauer Test kontrolliert
diesen Unterschied unter Windows und Ubuntu. Es handelt sich um eine
moderne SASD-Konvention, nicht um nachgewiesene historische
Druckersteuerung. Eine Dokumentzeile, die nur aus Form-Feed besteht,
ist im derzeitigen String-basierten Ausgabeformat nicht von einem
Seitenwechsel zu unterscheiden; typisierte Druckdatensätze sind daher
als mögliche spätere Verbesserung vorgesehen.
