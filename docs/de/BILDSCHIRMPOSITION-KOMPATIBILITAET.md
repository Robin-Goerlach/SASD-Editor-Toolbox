# Absolute Zeilennummern und horizontale Viewport-Kompatibilität

## Historische Quelle und Umfang

Das **Turbo Editor Toolbox 1.0 Owner's Handbook** beschreibt `EditGenlineno` auf der gedruckten S. 176 und `EditHscroll` auf S. 179. Das Handbuch dient ausschließlich als externe **Verhaltensreferenz**; historischer Pascal-Code, Seiten und Bildmaterial gelangen nicht ins Repository.

- `EditGenlineno` berechnet absolute Zeilennummern für alle sichtbaren Fenster durch Rückwärtslaufen über verkettete Zeilendeskriptoren; die Statuszeile wird durch diese Routine noch nicht gezeichnet.
- `EditHscroll` prüft **alle** Fenster und korrigiert die linke Textkante, wenn der jeweilige Cursor außerhalb des horizontal sichtbaren Textbereichs liegt.

Beide historischen Routinen erwähnen das Abbrechen beim Erkennen von Tastatureingaben, da Deskriptor- und Bildschirmläufe lange dauern konnten. Direkte Indizes und ein begrenzter Fensterdurchlauf vermeiden diese langsamen historischen Durchläufe. Die eingabepriorisierte Ablaufsteuerung bleibt bei `EditorSystemLoop` und dem Host. Das ist eine dokumentierte Modernisierung, keine Behauptung identischer DOS-Zwischenzustände.

## SASD-Zuordnung

`EditorViewportBuilder.Build(window, height, width)` verwendet `EditorWindow.Cursor.Line` und liefert die **einbasierte** absolute Zeilennummer in `EditorStatus.Line`. Bei verknüpften Fenstern besitzt jede View ihre eigene Cursorposition. Damit ersetzt eine O(1)-Berechnung `EditGenlineno`. Die Status-Metadaten selbst zu berechnen ist nicht gleichbedeutend mit dem Zeichnen; das macht der Host.

`EditorWindow.EnsureCursorHorizontallyVisible(visibleColumns)` sorgt für minimale horizontale Bewegung einer View; `EditorSession.EnsureAllWindowCursorsHorizontallyVisible(visibleColumns)` führt dies für alle offenen Fenster mit gemeinsamer Textbreite aus. Hosts mit verschieden breiten Textfenstern können die View-Methode für jedes Fenster separat aufrufen.

Für die sichtbare Breite `w >= 1` gilt nach Normalisierung negativer Offsetwerte auf null:

- Liegt `cursor.Column < LeftColumn`, wird `LeftColumn = cursor.Column` gesetzt.
- Gilt `cursor.Column - LeftColumn >= w`, wird `LeftColumn = cursor.Column - w + 1` gesetzt.
- Andernfalls bleibt der linke Offset unverändert.

Die Prüfung durch Subtraktion vermeidet Overflow nahe `int.MaxValue`. Virtuelle Spalten hinter dem gespeicherten Text bleiben gültig. Text, Dirty-State, Undo, Cursorposition, aktuelles Fenster und vertikaler Offset werden nicht verändert. Ungültige Breiten werden vor jeglicher Änderung eines Session-Fensters abgewiesen.

## Terminalintegration und Regressionstests

`ConsoleFirstEdRenderer` übergibt die Textbreite einmal vor der Projektion der einzelnen Fenster. Die horizontale Korrektur liegt damit nicht länger versteckt im Terminalrenderer; die vertikale Cursor-Nachführung bleibt getrennte Host-Aufgabe.

`ScreenPositionCompatibilityTests` prüft linke/rechte Sichtgrenze, verknüpfte und unabhängige Fenster, Idempotenz, fehlerhafte Breiten, große virtuelle Spalten, gekürzten Viewporttext und fensterbezogene absolute Zeilennummern.

