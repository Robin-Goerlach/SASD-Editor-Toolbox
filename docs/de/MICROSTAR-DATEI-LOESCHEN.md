# MicroStar: Löschen einzelner Dateien

Der Dateibrowser unterstützt jetzt **F8 – Datei löschen**. F8 führt noch
keine Löschung aus: Der Nutzer muss anschließend exakt `DELETE` eingeben.
Ein bloßes Enter, Escape oder jede andere Eingabe bricht ab.

`MicroStarFileOperations.Delete` befindet sich in der Anwendungsschicht
und kann nur einzelne reguläre Dateien löschen. Verzeichnisse und
symbolische Links/Reparse Points werden zurückgewiesen. Nach erfolgreicher
Löschung lädt der Browser die Liste neu; Fehler werden angezeigt.

**Wichtig:** Das Löschen ist dauerhaft und nicht über die Editor-Undo-
Funktion rückgängig zu machen. Zwischen Dateiprüfung und Löschung bleiben
bei parallelen Dateisystemänderungen mögliche Race Conditions. Ein späterer
Härtungsschritt kann dafür betriebssystemspezifische Handle-basierte
Operationen prüfen.

Automatisierte Tests mit temporären Dateien decken Erfolg und Schutzfälle
ab. Die bestehenden F5/F6-Funktionen bleiben unverändert.
