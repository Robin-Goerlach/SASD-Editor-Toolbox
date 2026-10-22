# MicroStar – sicheres Kopieren und Umbenennen

`MicroStarFileOperations` trennt schreibende Dateisystemoperationen
von der Konsolendarstellung. Zwei Methoden stehen bereit:
`Copy(Quelle, Ziel)` und `Rename(Quelle, Ziel)`.

**Bestehende Zieldateien werden niemals überschrieben.** Die Prüfung
erfolgt unmittelbar durch `File.Copy` beziehungsweise `File.Move`
mit `overwrite: false` und nicht ausschließlich durch eine vorherige
Bildschirmabfrage. Fehlende Quelldateien, gleiche Quell-/Zielpfade
und Verzeichnisquellen werden zurückgewiesen.

Der Service arbeitet nur mit einzelnen Dateien. Rekursive
Verzeichnisoperationen und automatische nicht-atomare Verschiebungen
über Laufwerksgrenzen sind bewusst noch nicht vorgesehen. Fehler
werden an den Host weitergegeben und nicht verschwiegen.

Automatisierte Tests mit temporären Dateien sichern die wichtigsten
Fehler- und Erfolgsfälle ab. Die Bedienoberfläche folgt separat.
