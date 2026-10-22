# MicroStar F10: Navigation mit der Tastatur

Das F10-Popup kann jetzt mit den **Pfeiltasten** bedient werden.
`Home`/`End` springen zum ersten beziehungsweise letzten Eintrag,
`Enter` bestätigt; `Escape` und `Strg+U` brechen ab.
Die bisherigen Buchstabenkürzel bleiben erhalten.

Die Klasse `ConsoleMicroStarMenuNavigation` bildet eine unabhängige
Zustandsmaschine. Pfeiltasten ändern nur die Auswahl, nicht das
Editor-Dokument. Erst eine ausdrückliche Bestätigung gibt einen Befehl
an die bereits existierende Befehlsverarbeitung weiter.

Das Zeichen `>` markiert den aktuellen Eintrag; die ursprünglichen
Menütexte und Fenstermaße werden nicht verändert. Navigation und
Darstellung liegen vollständig außerhalb des plattformneutralen
Editor-Cores.

Die Reihenfolge der modernen SASD-Bedienung ist **keine behauptete
historische MicroStar-Tastenbelegung**. Automatisierte Headless-Tests
prüfen Navigation, Umlauf, Abbruch und Auswahl.
