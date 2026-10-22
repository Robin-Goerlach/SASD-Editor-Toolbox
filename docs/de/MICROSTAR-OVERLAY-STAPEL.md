# MicroStar – Popup-Stapel

Der neue `ConsoleMicroStarOverlayStack` verwaltet temporäre Dialoge
als **LIFO-Stapel**. Jedes geöffnete Popup besitzt ein `IDisposable`-
Objekt; erst das innerste Popup darf geschlossen werden. Ein Verstoß
ändert die gespeicherten Fenster nicht, sodass korrektes Schließen
danach möglich bleibt.

Der Renderer baut zunächst den Editor aus seinem Datenmodell auf und
zeichnet danach die aktiven Popups von unten nach oben. Schließt ein
untergeordnetes Fenster, erscheint sein übergeordnetes Fenster wieder.
Plattformabhängiges Lesen von Terminal-Bildschirmzellen entfällt.
Der wiederverwendbare Editor-Core bleibt davon unabhängig.

Das ist eine Basis für das beobachtbare Verhalten von `MakeWindow` und
`RestoreWindow`, noch **keine vollständige historische API-Nachbildung**.
Tastaturfokus, komplexe Dialoginhalte und Resize-Verhalten sind offen.
Headless-Tests sichern Stapel- und Freigabeverhalten ab.
