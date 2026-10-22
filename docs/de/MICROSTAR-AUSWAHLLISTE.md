# MicroStar – Modell für Auswahllisten

Die neue Klasse `MicroStarSelectionList` verwaltet Auswahl und
Scrollposition unabhängig von der grafischen Konsolendarstellung.
Sie dient als Grundlage für den späteren MicroStar-Dateimanager.

Jeder Eintrag besitzt eine eindeutige ID, eine Beschriftung und einen
Aktivierungszustand. Mit Pfeil-auf/ab, Bild-auf/ab und Pos1/Ende wird
navigiert. Deaktivierte Einträge bleiben sichtbar, werden aber beim
Auswählen übersprungen. `Enter` akzeptiert nur einen aktiven Eintrag;
`Escape` beziehungsweise `Strg+U` brechen ab.

Der sichtbare Ausschnitt folgt der Auswahl. Die Eingabeliste wird beim
Erzeugen kopiert, sodass spätere Änderungen der Originalsammlung eine
Auswahl nicht heimlich auf eine andere Datei umlenken können.

Noch handelt es sich **nicht um einen fertigen Dateidialog**. Die
Modellschicht enthält keinen Dateisystemzugriff und keine Konsolen-APIs.
Headless-Tests sichern Navigation, Scrollen und Fehlerfälle.
