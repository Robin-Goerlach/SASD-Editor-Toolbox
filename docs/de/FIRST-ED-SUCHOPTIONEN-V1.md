# FIRST-ED: Such-/Ersetzoptionen im Konsolenhost (21.10.2026)

## Handbuchbeleg und Grenze

Die Rückseite des Handbuchs nennt ausdrücklich **Find and Find/Replace
with options**; die Variablenreferenz beschreibt `Optstr` als Zeichenfolge
für Such-/Ersetzoptionen (gedruckte S. 113). Die genaue historische
Buchstabensyntax wird daraus **nicht** ersichtlich.

Der Core unterstützt bereits `SearchOptions(CaseSensitive, WholeWord,
WrapAround)`. Der FIRST-ED-Konsolenhost bietet jetzt ausdrücklich
**moderne SASD-Kürzel** an, wenn ein neues Such- oder Ersetzkommando
mit nichtleerem Suchmuster eingegeben wird:

- `C`: Groß-/Kleinschreibung beachten;
- `W`: nur ganze Wörter;
- `R`: am Dateiende wieder vorne suchen;
- Leere Eingabe (Enter): bisherige FIRST-ED-Standardeinstellung.

Abbruch und unbekannte/doppelte Kürzel führen ohne Seiteneffekte zum
Abbruch. `FindAgain` übernimmt die zuvor gemerkten Optionen; eine
leere Suchmuster-Eingabe fragt daher nicht erneut danach.

`EditorCommandRequest.Options` überträgt die gewählten Optionen
über den bestehenden semantischen Dispatcher. Keine Abhängigkeit von
`Console` im Core und keine Veränderung der bisherigen sechs
Positionsparameter des Command-Records.

`ConsoleFirstEdSearchOptionsTests` sichert Eingabeprüfung, FindAgain,
Whole-Word-Replace und Undo ab. Die Kürzel sind **keine Behauptung
historischer Optstr-Byte-/Tastaturkompatibilität**.
