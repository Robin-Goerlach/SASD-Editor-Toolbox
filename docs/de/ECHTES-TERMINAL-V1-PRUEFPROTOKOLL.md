# FIRST-ED und MicroStar – V1-Prüfprotokoll für echte Terminals

Status: **Manuelle Prüfung erforderlich; kein Nachweis vollständiger historischer Gleichwertigkeit**.

## Zweck und Sicherheit

FIRST-ED und das von MicroStar inspirierte F10-Menü laufen im **selben** Programm `Sasd.Editor.FirstEd.Sample`. MicroStar ist hier ein Sample-Host-Menü, keine eigene ausführbare Anwendung und keine exakte Rekonstruktion der Menügestaltung von 1985. Ausschließlich entbehrliche Testdateien verwenden. Das historische Dateiformat benutzt 8-Bit-Zeichen und 0x8D-Markierungen, nicht die allgemeine moderne UTF-8-Speicherung.

Je einmal unter Windows Terminal/PowerShell auf Windows und einem nativen Linux-Terminal (z. B. GNOME Terminal, Konsole oder xterm) ausführen. WSL kann zusätzlich geprüft werden, ersetzt aber den nativen Linux-Test nicht. Empfohlene Größe: mindestens 100 Spalten und 35 Zeilen; die tatsächliche Größe notieren.

## Bauen und starten (Repositorywurzel)

```sh
dotnet --info
dotnet restore Sasd.Editor.Toolbox.slnx
dotnet build Sasd.Editor.Toolbox.slnx -c Release
dotnet test Sasd.Editor.Toolbox.slnx -c Release --no-build
dotnet run --project samples/dotnet/Sasd.Editor.FirstEd.Sample -c Release --no-build -- --demo
```

Die Ein-/Ausgabe des interaktiven Editors nicht umleiten. `--help` funktioniert auch ohne interaktives Terminal. `Strg-K, X` bedeutet zuerst Strg-K, dann X; zum Beenden mit `YES` bestätigen.

## Pro Testumgebung erfassen

- Datum / prüfende Person:
- Betriebssystem und Version:
- Nativ oder WSL:
- Terminal und Version:
- Schriftart / Zeichencodierung / Tastaturbelegung:
- Spalten x Zeilen:
- `dotnet --version`:
- `git rev-parse HEAD`:
- Build-/Testergebnis:
- Screenshot-/Log-Nachweis (normalerweise außerhalb des Repositories):

## FIRST-ED – Testliste

Jeden Punkt als **BESTANDEN / FEHLER / BLOCKIERT** kennzeichnen. Bei Fehlern genaue Tastenfolge und Soll-/Ist-Verhalten festhalten.

1. Demo zeigt bearbeitbares NONAME-Dokument mit intakter Statuszeile und Cursor.
2. Pfeile, Pos1, Ende, Bild auf/ab: Navigation, Viewport und Status stimmen überein.
3. Text eingeben, Escape drücken: Undo stellt den vorherigen Zustand wieder her.
4. Strg-Q, dann F, `WIDGET` und Enter: Treffer; Strg-L wiederholt die Suche.
5. Strg-K/B und Strg-K/K markieren einen ganzzeiligen Block; Strg-K/C kopiert ihn; Undo korrigiert die Änderung.
6. Strg-K/1 setzt eine Marke; Strg-Q/1 springt zu ihr.
7. Bei ausreichender Terminalhöhe erzeugt Strg-O/O ein Fenster; Strg-O/J verknüpft Fenster desselben Dokuments. Cursor dürfen unabhängig sein, Text bleibt gemeinsam.
8. Gespeichertes ESC plus `[2J` aus der Demo erscheint ungefährlich als `^[[2J`; Bildschirm und Statuszeile werden nicht gelöscht. Optional Strg-P, dann `[`, dann druckbares `[2J` im Testtext eingeben.
9. Terminal mit mehreren Fenstern verkleinern/vergrößern; Überschreiben von Statuszeilen, falsche Cursorposition oder Scrollfehler protokollieren.
10. Strg-K/S speichert eine Wegwerfdatei; Strg-K/W exportiert, ohne den Dokumentdateinamen zu ändern. Überschreib- und Abbruchdialoge prüfen.
11. Strg-K/X beendet erst nach `YES`; abgebrochene Bestätigung beendet nicht.

Für Steuerzeichen, Unicode-Zeichenbreite und Darstellung außerdem `docs/de/TERMINALAUSGABE-V1-ABNAHME.md` verwenden.

## MicroStar – F10-Testliste

1. F10 öffnet das Menü, ohne Dokumenttext oder Undo zu verändern.
2. Pfeile ändern nur die Auswahl; Pos1/Ende, Enter und Buchstaben aktivieren die angezeigten Optionen.
3. Escape, Strg-U oder Menü Q brechen ab, ohne destruktive Befehle auszuführen.
4. F=Suchen, R=Ersetzen, G=Zeile anspringen: Dialog und Befehl prüfen; Abbruch ohne Änderung.
5. S=Speichern, W=Schreiben, I=Lesen: nur Testdateien; Überschreibabfrage und Abbruch prüfen.
6. B/E/C/M/D: Blockanfang/-ende, Kopieren, Verschieben und Löschen nur im Testdokument; Undo prüfen.
7. T=Block schreiben, L=Block lesen: Wegwerfdatei für den Rundlauf verwenden; Block- und Cursorzustand prüfen.
8. P=Save & Open, A=Abandon & Open: Abbruch und Bestätigung einschließlich geänderter Dokumente ausprobieren; niemals wichtige Dateien verwenden.
9. H=Drucken, J=Status, K=Druck stoppen: Druck in eine Testdatei, Status und Abbruch prüfen. Keine Behauptung physischer Drucker- oder exakter PrintNext-Parität.
10. Menüdarstellung, Fokus und Status müssen nach Aktionen, Abbrüchen und Fensterwechseln wieder korrekt sein.

## Ergebnisse und V1-Freigabe

Pro Test: **BESTANDEN / FEHLER / BLOCKIERT**, Soll, Ist und Umgebung. Screenshots nur bei Bedarf und ohne private Daten. Abweichungen unterscheiden nach Implementierungsfehler, terminalabhängiger Besonderheit, bewusst moderner Abweichung oder historisch nicht belegtem Verhalten.

Eine grüne CI oder diese kurze manuelle Testliste allein bestätigt **nicht** die vollständige Abnahme der 123 historischen Indexroutinen, der 20 Ergänzungen oder von MicroStar. V1.0-Tag/Release erst nach separater historischer Gesamtprüfung.
