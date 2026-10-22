# MicroStar – Bestätigungsdialog beim Überschreiben

Beim Export einer Blockdatei auf einen bereits vorhandenen Dateinamen
öffnet der Konsolenhost jetzt einen echten **Ja/Nein-Popupdialog**.
Er verwendet die neue `MicroStarDialogFocus`-Klasse, die vorhandene
Popup-Geometrie und den verschachtelbaren Overlay-Stapel.

Standardmäßig ist **Nein** ausgewählt. Ein versehentliches `Enter`
führt deshalb nicht zum Überschreiben. `Tab`, `Shift+Tab` und die
Pfeiltasten wechseln die Auswahl; `Enter` bestätigt. Die ausdrückliche
Eingabe von `Y` oder `N` ist ebenfalls möglich. `Esc` und
`Strg+U` brechen ab. Wenn der Terminalbildschirm zu klein ist,
bleibt die sichere Texteingabe `YES` erforderlich.

Das Dialogfenster enthält keine Dateischreib- oder Editorlogik. Die
Bedienung ist eine moderne SASD-Entscheidung und wird nicht als genaue
Rekonstruktion des historischen MicroStar behauptet.
