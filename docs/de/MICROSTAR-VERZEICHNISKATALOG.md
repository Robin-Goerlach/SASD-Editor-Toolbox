# MicroStar – Verzeichnisübersicht (nur lesen)

`MicroStarDirectoryCatalog` ermittelt die direkten Einträge eines
Verzeichnisses und liefert eine Momentaufnahme für die spätere
Dateiauswahl. Unterverzeichnisse erscheinen zuerst, anschließend
werden Dateien nach Namen sortiert. Es erfolgt **keine rekursive Suche**.

Jeder Eintrag besitzt den vollständigen Pfad als stabile ID sowie
einen Anzeigenamen und die Information „Verzeichnis oder Datei“.
Die Umsetzung verwendet das bereits vorhandene Modell
`MicroStarSelectionList`, vermischt aber keinen Dateisystemzugriff
mit der Tastaturnavigation.

Fehlende und unzugängliche Verzeichnisse führen zu einer Fehlermeldung
statt zu einer irreführenden leeren Liste. Die Komponente führt
**keine schreibenden Dateioperationen** aus. Neue Tests mit temporären
Verzeichnissen prüfen Ordnung, Tiefe, Identität und Fehlerfälle.

Ein vollständiger interaktiver Dateimanager ist damit noch nicht
fertiggestellt; die Datenversorgung ist nun jedoch vorbereitet.
