# MicroStar-Dateibrowser: F5 Kopieren und F6 Umbenennen

Im vorhandenen Dateibrowser unter **F10 → L** stehen jetzt `F5`
zum Kopieren und `F6` zum Umbenennen einzelner Dateien bereit.

Die Aktion ist nur für eine ausgewählte Datei, nicht für ein
Verzeichnis zulässig. Nach der Tastenauswahl muss der Nutzer den
neuen Zielpfad ausdrücklich eingeben. Leere Eingaben brechen ab.
Relative Pfade beziehen sich auf das aktuell angezeigte Verzeichnis.

Der getrennte Service `MicroStarFileOperations` schützt bestehende
Zieldateien mittels `overwrite: false`. Nach erfolgreicher
Dateioperation lädt der Browser das Verzeichnis neu. Fehlgeschlagene
Operationen werden sichtbar gemeldet; kein stilles Überschreiben.

Die Kürzel sind moderne SASD-Entscheidungen, keine zugesicherte
Rekonstruktion der historischen MicroStar-Tasten. Tests sichern
Aktionsauswahl, Zielauflösung und Aktualisierung ab. Der
plattformneutrale Editor-Core bleibt unverändert.
