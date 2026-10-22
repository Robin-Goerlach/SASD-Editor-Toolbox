# MicroStar: erstes funktionales F10-Menü

Das Turbo-Editor-Toolbox-Handbuch beschreibt ein MicroStar-Pulldown-Menü
auf **F10** (gedruckte S. 42-53). FIRST-ED-Kernel und MicroStar-Anwendung
dürfen nicht verwechselt werden.

Im .NET-Konsolenbeispiel öffnet F10 nun eine **bedienbare Menüauswahl**.
Das Menü nutzt vorhandene semantische Editorbefehle und deren bestehende
Dialog-/Argumentverarbeitung. Die Auswahl kann unter anderem Suchen,
Ersetzen, Zeilensprung, Dateioperationen, Undo, Blockbearbeitung und
Fensterwechsel auslösen. Abbruch löst keine Änderung aus. Ein offener
Ctrl-K/O/Q-Präfix wird vor dem Menü zurückgesetzt.

Die erste Oberfläche verwendet bewusst die sichere **Kommandozeile**
statt eines echten überlappenden Popup-Fensters. Vollständige
`MakeWindow`/`RestoreWindow`-Popups und Menüführung gehören zu einem
folgenden Schritt.

**Historische Abgrenzung:** F10 ist im Handbuch belegt; die gewählten
SASD-Buchstaben und ihre Reihenfolge sind moderne Beispielhost-Entscheidungen,
nicht nachweislich Borlands originale Menübelegung.
`ConsoleMicroStarMenuTests` sichert Zuordnung und Abbruch ab.
