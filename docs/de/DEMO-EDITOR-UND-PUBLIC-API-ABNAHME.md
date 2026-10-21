# V1-Demo-Editor und Abnahme der öffentlichen APIs

**Stand (Oktober 2026):** Erste ausführbare Abnahmestufe. Noch keine V1.0-Freigabe und keine vollständige interaktive Terminalabnahme.

## Architekturentscheidung: keinen zweiten Editor-Kern entwickeln

Das bereits vorhandene FIRST-ED-Terminalprogramm unter `samples/dotnet/Sasd.Editor.FirstEd.Sample` ist unsere erste **echte Demo und Vorlage für andere Entwickler**. Es nutzt die öffentlichen Bausteine `EditorSession`, `EditorCommandDispatcher`, Dokument/Fenster, Datei-Codec und Undo. Wir bauen kein getrenntes Textmodell, keinen künstlichen Editor und keinen zusätzlichen Persistenzweg für die Demo.

Eine bewusst kleine **WinForms-Demo** empfehle ich als nächste grafische Referenzanwendung, sobald die wiederverwendbare V1-Basis ausreichend abgenommen ist: Textfenster, Datei-/Bearbeiten-/Suche-Befehle, verknüpfte Ansichten und ein Diagnostikbereich. Sie soll ausschließlich auf den Editor-Kern zugreifen. WPF, MAUI und Web werden dafür nicht zur Abhängigkeit. Die WinForms-Demo ist noch nicht implementiert.

## Demo starten (Windows PowerShell oder Linux-Terminal)

Voraussetzung: .NET 10 SDK. Im Hauptverzeichnis des Repositories ausführen:

```powershell
dotnet restore Sasd.Editor.Toolbox.slnx
dotnet build Sasd.Editor.Toolbox.slnx -c Release
dotnet run --project samples/dotnet/Sasd.Editor.FirstEd.Sample -c Release --no-build -- --help
dotnet run --project samples/dotnet/Sasd.Editor.FirstEd.Sample -c Release --no-build -- --demo
```

Ohne Option startet ein leeres Dokument. `--demo` erstellt einen **echten, bearbeitbaren Beispieldatensatz im Arbeitsspeicher**. Es wird dabei keine Datei angelegt, keine Dateizuordnung festgelegt und keine Undo-Historie erzeugt. `--help` funktioniert auch ohne interaktives Terminal. Unbekannte oder kombinierte Optionen werden mit einem Fehler quittiert.

**Achtung:** Die historische Terminaldemo speichert über den 8-Bit-Latin-1-/0x8D-Codec. Für moderne Unicode-Dateien dient das gesonderte öffentliche `FileTextStorage`-API. Der Demo-Text ist bewusst ASCII-kompatibel und enthält lediglich ein echtes ESC-Steuerzeichen als Test der sicheren Anzeige. Beim Speichern nur Wegwerf-Dateinamen verwenden.

## Manuelle Demo-Prüfung (noch offen)

1. `--demo` starten. Das NONAME-Dokument muss vollständig bearbeitbarer Text sein, kein Mockup und kein Screenshot.
2. Mit Pfeilen/Home/End bewegen, Zeichen einfügen, danach Escape für Undo verwenden.
3. Ctrl-Q gefolgt von F öffnet die Suche. Nach `WIDGET` suchen; das Wort kommt im Beispiel vor.
4. Ctrl-K B setzt den Blockanfang, Ctrl-K K das Blockende und Ctrl-K C kopiert den Block. Danach Undo testen.
5. Ctrl-O O fragt zwei Zahlen für ein zusätzliches Fenster ab. Ctrl-O J verbindet Fenster mit demselben Dokument. Ihre Cursorpositionen müssen unabhängig, ihre Texte gemeinsam sein. Das Terminal muss hoch genug für mehrere Fenster sein.
6. Ctrl-K 1 setzt Marker 1, Ctrl-Q 1 springt dorthin. Ctrl-P plus ASCII-Buchstabe fügt ein echtes Steuerzeichen ein.
7. Die Zeile `SAFE CONTROL DISPLAY:` enthält tatsächlich ESC mit druckbarem `[2J`. Sichtbar sein muss `^[[2J` – **ohne Bildschirm-Löschung**.
8. Optional Ctrl-K S zum Speichern nach Pfadabfrage oder Ctrl-K W zum direkten Schreiben ohne Umbenennung des Dokuments. Nur eigens angelegte Testdateien verwenden.
9. Ctrl-K X beendet nach ausdrücklicher Bestätigung `YES`.

Die weitergehende Prüfung realer Tastaturen, Terminalfonts und Unicode-Breiten steht in `docs/de/TERMINALAUSGABE-V1-ABNAHME.md`. Erfolgreiche Headless-CI allein ist keine interaktive Freigabe.

## Matrix der öffentlichen V1-APIs

| Bereich | Nachzuweisendes Verhalten | Stand |
| --- | --- | --- |
| Dokumente/Fenster | Öffentliches Erzeugen/Laden/Anzeigen; gemeinsamer Text und unabhängige Cursor | Headless-Tests vorhanden; interaktiv offen |
| Semantische Befehle/Undo | `EditorCommandRequest` über Dispatcher; keine internen Helfer erforderlich | Neue externe API-Tests; historische Einzelabnahme bleibt offen |
| Moderne UTF-8-Ablage | Strict UTF-8, Load/Edit/Save/Reopen, Zeilentrenner und korrekter Dirty-Status | Automatisiert unter Linux/Windows |
| Historischer Codec | 0x8D, unterschiedliche Write-/Save-Semantik, exakte Bytes und Fehlerbehandlung | Automatisiert; weitere Historienabnahme offen |
| Fehler/Abbruch | Keine Veränderung von Text, vorhandenen Dateien oder Undo bei ungültigem Speichern, fehlender Datei und Abbruch | Öffentliche API-Tests vorhanden |
| Demo-Host/CLI | `--help`, `--demo`, echte Bearbeitung, Suche, Undo, verknüpfte Fenster | Headless abgesichert, echte Terminaltests fehlen |
| Öffentliche API-Freigabe | Nullable-Verträge, Ausnahmen, Host-Grenzen, Versionierung und Kompatibilitätsregeln | Vor V1 noch vollständig zu prüfen |

Die neue `PublicEditorApiAcceptanceTests`-Klasse liegt **in einer separat kompilierten .NET-Testassembly**. Sie verwendet nur öffentlich verfügbare Editor- und Datei-Schnittstellen: UTF-8-Laden/Bearbeiten/Speichern, historische direkte Write- und Save-Befehle, fehlgeschlagene Latin-1-Speicherung mit unveränderten Dateibytes, fehlgeschlagener Dateiimport mit verknüpften Fenstern und vorher abgebrochene Speicherung. `FirstEdDeveloperDemoTests` prüft die Startoptionen, das saubere unbenannte Demo-Dokument sowie Suche, Änderung, Undo und geteilte Dokumente mit eigenen Cursorpositionen. Alle Tests arbeiten ohne realen Konsolenemulator.

## Was ist von wem zu erledigen?

**ChatGPT/Entwicklung:** Weitere API-Grenzfälle prüfen, Nullability-/Fehler-/Cancellation-Verträge dokumentieren, Tests erweitern, Code sauber halten und später eine kleine WinForms-Demo vorbereiten.

**Manuelle Freigabe:** Bei Gelegenheit Demo im Windows Terminal und unter einem Linux-PTY ausführen, Terminalgröße/Tastatureingaben und Ergebnis notieren. Nur bei Fehlern sind genaue Reproduktionsschritte oder ein Screenshot nötig. Für den aktuellen automatisierten Entwicklungsschritt muss der Nutzer nichts tun.

Das separate `SASD-Editor-Toolkit` bleibt unverändert. Keine Veröffentlichung, kein Paket und kein V1.0-Tag ohne spätere ausdrückliche Freigabe.
