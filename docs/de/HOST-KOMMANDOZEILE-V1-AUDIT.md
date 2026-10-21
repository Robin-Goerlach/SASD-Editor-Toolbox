# FIRST-ED-Kommandozeile – historische V1-Einzelabnahme

**Stand:** Gezielte Prüfung der Host-Verantwortlichkeiten, 21. Oktober 2026;
**keine** vollständige Abnahme von historischer Terminal-, Tastatur- oder
Typeahead-Funktionalität. Originalquelle: Borland *Turbo Editor Toolbox
Owner's Handbook* (1985), gedruckte Seiten 120–122 und 237.

Die historische Kommandozeile nutzt DOS-Bildschirmzellen, die globale Spalte
`Cmdcol` und einen Pascal-`Varstring`. SASD lässt die Bildschirmzeile beim
Terminalhost. Der wiederverwendbare Core verarbeitet semantische Befehle und
besitzt einen separat begrenzten Typeahead-Puffer. Diese Trennung ist bewusst
gewählt: DOS-Zeiger und Bildschirmspalten dürfen keine Abhängigkeiten späterer
WinForms-/WPF-/Web-Oberflächen werden.

| Historischer Eintrag | Handbuch | SASD-Referenzhost | Abnahmegrenze |
| --- | --- | --- | --- |
| `EditAppchar` (S. 120) | Hängt ein Zeichen an einen Varstring; bei Länge ab `Defnocols` droht ein Überlauf. | `ConsolePromptEditor` hält die vollständige Antwort in einem verwalteten `StringBuilder`. | **Moderne Ersetzung:** kein fester Pascal-Puffer. Der Text wird nicht auf die Fensterbreite gekürzt. |
| `EditAppcmdnam` (S. 121) | Hängt einen Kommandozeilentext an, erhöht `Cmdcol` und könnte bei Überlänge die obere Statuszeile überschreiben. | `ReadPrompt` zeichnet in einer reservierten letzten Zeile und positioniert dort den Cursor. | **Bewusste Sicherheitsabweichung:** weder globale `Cmdcol`-Variable noch absichtliches Überschreiben einer Fenster-Statuszeile. |
| `EditAskfor` (S. 122) | Liest bis Return; sofortiges Return ergibt eine leere Zeichenfolge. Bei `Interactive=false` wird Typeahead, sonst direkt die Tastatur verwendet. Steuerzeichen erscheinen als Caret-Notation. Ist die Kommandozeile voll, wird keine weitere Eingabe angenommen. Ctrl-U kann den früheren Varstring-Inhalt wiederherstellen. | `ConsolePromptEditor` unterscheidet Enter (auch leer gültig) von Escape/Ctrl-U (Abbruch). Nicht druckbare Steuerzeichen gelangen nicht ungeprüft in Dateinamen-/Suchdialoge. Lange Antworten bleiben vollständig erhalten, die Anzeige scrollt nach rechts. Der interaktive Host verwendet `Console.ReadKey`; der Core verfügt separat über Typeahead. | **Teilentsprechung mit expliziten Abweichungen:** Ctrl-U bricht ab statt vorherigen Inhalt abzurufen; keine Caret-Notation, keine harte Bildschirmbreitenbegrenzung und keine behauptete Integration der Host-Prompts in den Core-Typeahead-Puffer. |
| `EditZapcmdnam` (S. 237) | Löscht die Kommandozeile und setzt `Cmdcol` auf die erste Spalte. | Jede neue `ReadPrompt`-Abfrage zeichnet eine frische letzte Zeile; vorherige Kommandozeilenpositionen bleiben nicht erhalten. | **Host-Verantwortlichkeit**, keine DOS-Speicher- oder Bildschirmparität. |

## In dieser Entwicklungsrunde korrigiert

1. **Terminalübergreifender Abbruch:** `ConsolePromptKeyPolicy` erkennt
   Escape und Ctrl-U sowohl als benannte Taste/Tastenkombination als auch als
   rohe ASCII-Bytes `0x1B` und `0x15`. Unix-Pseudoterminals können
   beispielsweise `ConsoleKey.NoName` liefern. `ConsolePromptEditor` und
   die Ctrl-P-Steuerzeichenabfrage verwenden dieselbe Host-Regel. Druckbare
   AltGr-Zeichen funktionieren weiterhin. Dies ist eine **tatsächlich
   geschlossene Host-Lücke**, keine neue semantische Editor-Operation.
2. **Unicode-sichere Eingabebearbeitung:** Backspace entfernt ein vollständiges
   Unicode-Zeichenelement, etwa ein Emoji aus zwei UTF-16-Codeeinheiten oder
   einen Buchstaben samt kombinierendem Akzent. Die rechtsseitige
   Kommandozeilenprojektion beginnt nicht mehr mitten in einem solchen Element.
   **Die vollständige Antwort bleibt bei der Anzeige unverändert.** Genaue
   Terminalzellenbreiten von Emoji und CJK-Zeichen erfordern weiterhin
   manuelle Plattformtests.
3. **Ausführbare Belege:** Die vorhandenen Prompt-Tests wurden um rohe
   Ctrl-U-/Escape-Bytes ohne benannten ConsoleKey, AltGr, Unicode-Backspace
   und schmale Unicode-Anzeigen ergänzt. Die Ctrl-P-Tests prüfen dieselbe
   zentrale Abbruchregel. Bisherige Fälle zu leerem Enter versus Abbruch,
   langen Eingaben, Dateinamen, Ersetzen und ASCII-Steuerzeicheneingabe
   bleiben bestehen.

## Für die V1.0-Freigabe noch offen

- Die vier Einträge bleiben im historischen Index **`host-sample`** und
  erhalten nicht künstlich einen portablen Routinen-Nachweis nur wegen
  zusätzlicher .NET-Host-Tests.
- Echte Windows-Console-/Terminal- und Linux-PTY-Tests für Ctrl-U, Escape,
  AltGr, schmale Fenster, Unicode und Bildschirmaktualisierung fehlen.
- Ein optionaler **klassischer Askfor-Modus** mit vorherigem Varstring-Inhalt
  und strenger Zeilenbreite kann später separat diskutiert werden. Er darf
  den aktuellen sicheren Ctrl-U-Abbruch für Speichern/Ersetzen nicht
  stillschweigend verändern.
- BIOS-/Videospeicher-Konventionen und globale `Cmdcol`-Werte bleiben aus
  dem Editor-Kern ausgeschlossen.
