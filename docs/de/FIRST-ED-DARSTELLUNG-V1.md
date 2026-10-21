# FIRST-ED: Physische Darstellung vervollständigt (21.10.2026)

## Belegte Handbuchanforderungen

Kapitel 11 des Turbo-Editor-Toolbox-Handbuchs (gedruckte S. 69-72)
unterscheidet fünf Attribute/Farbrollen: Normaltext (Txtcolor), Statuszeile
(Bordcolor), Blockmarkierung (Blockcolor), Kommandozeile (Cmdcolor) und
besondere Zeilen (Usercolor). Für EditColorLine/EditColorFile
(S. 137-138) gilt: Colored hat Vorrang vor Blockcolor und Txtcolor.
FIRST-ED kann rechts Zeilenenden anzeigen: `<` für hartes Zeilenende,
`.` für Bildschirmzeilen hinter dem Dateiende und Leerzeichen für
Wrapped-Zeilen. MicroStar blendet diese Kennzeichen laut Handbuch aus.

## Umsetzung

Der Core lieferte die Flags `InBlock`, `Wrapped`, `UserColored` bereits.
`ConsoleFirstEdPresentationPolicy` bildet sie jetzt auf Darstellungsrollen
und optionale Randkennzeichen ab. Der Terminalhost nutzt diese Informationen
beim Zeichnen; der portable Editor-Core bleibt ohne Console-Abhängigkeit.
Die Kennzeichenspalte reduziert ausschließlich die verfügbare physische
Textbreite, nicht Dokumenttext, logische Spalten oder Dateiformat. Die
letzte Terminalspalte bleibt als Schutz vor implizitem Zeilenumbruch frei.

Die konkreten `ConsoleColor`-Werte sind eine **moderne Host-Entscheidung**,
keine behauptete historische Farbpalette. Andere Oberflächen können andere
Farben verwenden. `ShowRightEdgeMarkers` gehört ausschließlich zum Sample.
Die bestehende sichere Terminalprojektion wandelt gespeicherte Steuerzeichen
weiterhin in inerte Anzeigen um.

## Tests und Grenzen

`FirstEdPresentationPolicyTests` prüft Priorität, Zeilenenden, EOF,
Abschaltung, verknüpfte Fenster und unveränderte Dokument-/Undo-Daten.
Reale Terminaltests auf Windows/Linux (kleine Fenster, Theme,
Größenänderung, Cursor) bleiben eine manuelle V1-Abnahmeschranke.
DOS-Videospeicher, MicroStar-Menüs/Popups und Druckspooler werden damit
nicht nachgebildet.
