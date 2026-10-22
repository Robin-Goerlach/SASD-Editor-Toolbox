# MicroStar – wiederverwendbare Dialog-Fokusverwaltung

Die neue Klasse `MicroStarDialogFocus` verwaltet den Tastaturfokus für
spätere Dateiauswahl-, Eingabe- und Bestätigungsdialoge. Sie liegt nur in
der Konsolen-Beispielanwendung; der wiederverwendbare Editor-Core wird
nicht von Terminalfunktionen abhängig.

Steuerelemente besitzen eine eindeutige ID, einen Typ (Eingabefeld,
Auswahlfeld oder Aktion) und einen Aktivierungszustand. `Tab` und
Pfeil-ab bewegen den Fokus vorwärts; `Shift+Tab` und Pfeil-auf
rückwärts. Deaktivierte Elemente werden übersprungen. `Enter`
liefert eine Aktivierungsentscheidung, `Escape` beziehungsweise
`Strg+U` einen Abbruch, ohne Editorbefehle auszuführen.

Verschachtelte Dialoge besitzen voneinander unabhängige Fokusobjekte.
Die Implementierung ist zunächst eine **Architekturgrundlage**, noch
kein fertiger interaktiver Dateidialog. Umfangreiche englische
Codekommentare und plattformunabhängige Tests dokumentieren die Regeln.
