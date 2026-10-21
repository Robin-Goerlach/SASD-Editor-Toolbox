# V1-Terminalausgabe – Sicherheit und manuelle Abnahme

**Geltungsbereich:** FIRST-ED-Terminalbeispiel für .NET, Oktober 2026.
Diese Anleitung beschreibt überprüfbare Ergebnisse und noch **ausstehende**
manuelle Freigabeschritte. Sie behauptet ausdrücklich nicht, dass Windows-
oder Linux-Terminals bereits interaktiv getestet wurden.

## Warum diese Änderung für V1.0 wichtig ist

Der Editor darf absichtlich ASCII-Steuerzeichen als echten Text speichern,
beispielsweise über `EditInsertCtrlChar` (Ctrl-P). Zuvor schrieb der
Terminalrenderer Dokumenttext nach einem einfachen Abschneiden anhand
von `string.Length` **ungefiltert** mittels `Console.Write` aus. Eine
Zeichenfolge aus ASCII-ESC und `[2J` konnte daher vom Terminal als
Bildschirm-Löschbefehl interpretiert werden, statt lediglich im Dokument
angezeigt zu werden. CR, LF, TAB und Backspace konnten ebenfalls
Bildschirmpositionen und Statuszeilen beeinflussen. Betroffen war der
gemeinsame Ausgabeweg für Dokumenttext, Dateinamen, Status und Meldungen.

Die neue reine Hostklasse `ConsoleFirstEdRowProjection` ist jetzt die
**einzige** Projektion vor jeder Konsolenzeile. C0-Steuerzeichen erscheinen
als sichtbare Caret-Notation: `ESC` als `^[`, `CR` als `^M`, `LF` als
`^J` und `NUL` als `^@`. `DEL` wird zu `^?`, C1-Steuerzeichen
einschließlich `0x9B` (8-Bit-CSI) zu ungefährlichen ASCII-Hex-Markierungen
wie `[9B]`. Ungültige UTF-16-Surrogate werden als `?` angezeigt.
**Gespeicherte Zeichen und Dateibytes werden dadurch nicht verändert.**

Das Abschneiden geschieht an vollständigen Unicode-*Zeichenelementen*
statt mitten in einem Surrogatpaar oder einer kombinierenden Sequenz.
Eine vorsichtige, hostlokale Breitenabschätzung behandelt gebräuchliche
CJK- und Emoji-Zeichen als zweispaltig, Grundbuchstaben mit Akzent als
einspaltig. Die Cursorprojektion berücksichtigt auch die verbreiterte
Caret-Notation für Steuerzeichen und virtuelle Spalten. Ausgegeben wird
die **vollständige gespeicherte logische Zeile**, nicht der bereits nach
UTF-16-Länge beschnittene neutrale Viewport-Text.

Damit ist noch **keine exakte Terminalzellenbreite für alle Schriftzeichen**
nachgewiesen. Unterschiedliche `wcwidth`-Versionen, mehrdeutig breite
Zeichen, zusammengesetzte Emoji-/ZWJ-Sequenzen und Rechts-nach-links-
Darstellungen können je nach Terminal und Schriftart abweichen.
Die logischen UTF-16-Positionen `Cursor.Column` und `LeftColumn` im
Editor-Kern bleiben deshalb bewusst unverändert.

## Automatisierte Nachweise

`ConsoleFirstEdRowProjectionTests` prüft:

- C0/DEL/C1/ESC und gespeicherte ANSI-Folgen, die als ungefährliche
  sichtbare Zeichen erscheinen statt Terminalbefehle auszuführen;
- echte semantische ESC-Einfügung mit anschließendem Anzeigen und Undo,
  ohne Veränderung des gespeicherten Steuerzeichens;
- schmale Zeilen ohne halbe Caret-Notation, Surrogatpaare oder Akzente;
- gebräuchliche CJK-/Emoji-Breiten, kombinierende Akzente sowie
  Cursorpositionen nach sichtbarer Steuerzeichenübersetzung;
- horizontale Ausschnitte mitten in einem Unicode-Zeichenelement,
  virtuelle Spalten, fehlerhafte Surrogate und ungültige Geometrie.

Diese Tests laufen ohne grafische oder reale Terminaloberfläche. Sie ersetzen
keine physische Tastatureingabe oder Prüfung verschiedener Terminalemulatoren.

## Manuelle Windows-/Linux-Prüfliste (noch nicht durchgeführt)

Für jeden Punkt Betriebssystem, Terminalemulator, Schriftart,
.NET-Runtime-Version, Fenstergröße, Eingabemethode, Beobachtung und Ergebnis
notieren. Unter Windows beispielsweise Windows Terminal/PowerShell mit
Windows 11, unter Linux ein echtes PTY wie GNOME Terminal oder xterm unter
Debian/Ubuntu verwenden.

1. **ESC bleibt sichtbar:** Ctrl-P und dann `[` eingeben; anschließend
   die druckbaren Zeichen `[2J`. Erwartete Anzeige: `^[[2J` ohne
   Bildschirm-Löschung und ohne Beschädigung der Statuszeilen. Undo testen.
2. **Weitere Steuerzeichen:** Ctrl-P gefolgt von `M` (CR), `J` (LF) und
   `I` (TAB), jeweils einzeln. Erwartet: `^M`, `^J`, `^I` innerhalb
   derselben logischen Textzeile ohne physische Cursor-Sprünge.
3. **Schmale Fenster und Größenänderung:** Bei angezeigten Steuerzeichen
   die Terminalbreite verändern, auch bis auf ein oder zwei verfügbare
   Textspalten, soweit der Emulator das erlaubt. Kein automatischer
   Zeilenumbruch in benachbarte Statuszeilen.
4. **Unicode:** `a界b`, `a😀b`, `é` und zusammengesetzte Emoji
   eingeben bzw. laden. Abschneiden am rechten Rand, tatsächliche
   Cursorposition und Zellenbreiten prüfen. Abweichungen festhalten.
5. **Eingabeaufforderungen:** Ctrl-U und Escape bei Datei-, Such- und
   Ersetzen-Prompts prüfen; AltGr darf nicht fälschlich abbrechen.
   Lange Unicode-Dateinamen müssen vollständig erhalten bleiben;
   Backspace soll komplette Zeichenelemente entfernen.
6. **Mehrere verknüpfte Fenster:** Text mit Steuerzeichen in einem Fenster
   anzeigen und ein anderes bedienen. Statuszeilen und physischer Cursor
   dürfen nicht beschädigt werden.
7. **Dateiverarbeitung:** Dokument mit einem echten Steuerzeichen über den
   vorgesehenen Codec speichern und wieder laden. Die Anzeige muss das
   Steuerzeichen weiter maskieren; der gespeicherte Code muss erhalten bleiben.

Bei Fehlern genaue Reproduktionsschritte und Terminaldetails festhalten.
Ein grüner GitHub-CI-Lauf allein ist **keine** vollständige V1-Terminalabnahme.

## Architektur

Die Projektion befindet sich **ausschließlich im Konsolenbeispiel**.
Unicode-Dateiverarbeitung, Kernbefehle, Zeilenflags, Undo und andere
Oberflächen bleiben unverändert. Die Implementierung legt zunächst Wert
auf Lesbarkeit und überprüfbare Korrektheit, nicht auf Rendergeschwindigkeit.
Eine künftig benötigte exakt plattformabhängige Spaltenberechnung sollte
als explizite Host-Fähigkeit realisiert werden und darf nicht die
Zeichenpositionen des Editor-Dokuments umdefinieren.
