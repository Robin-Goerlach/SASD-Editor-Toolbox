# MicroStar – erste Popup-Fensterdarstellung

Das Konsolenbeispiel zeigt das F10-Menü jetzt als **zentriertes,
umrahmtes Popup**, das über den bestehenden Editortext gezeichnet wird.
Die Fenstermaße berechnet `ConsoleMicroStarPopupLayout` unabhängig
von Konsolen-APIs; die Darstellung bleibt in der Host-Schicht.

Escape und Ctrl-U brechen ab. Gültige Buchstaben lösen weiter die
bestehenden Editorbefehle aus. Nach dem Schließen rekonstruiert der
Renderer die Anzeige vollständig aus dem Editor-Modell. Anders als bei
historischer direkter Bildschirmzellen-Sicherung ist somit kein
plattformabhängiges Zurücklesen des Terminal-Videospeichers nötig.

Das Popup ist **kein** Editor-Dokumentfenster. Auch bei Abbruch werden
Text, Undo und Fenstermodell nicht verändert. Auf zu kleinen Terminals
bleibt die bisherige Eingabezeilen-Variante erhalten.

Diese Implementierung bildet das beobachtbare Ein-/Ausblenden
funktional nach; sie behauptet noch **keine vollständige historische
`MakeWindow`/`RestoreWindow`-Kompatibilität** oder Pfeiltasten-Menüführung.
Neue Headless-Tests prüfen Geometrie, Rahmen und Tasten.
