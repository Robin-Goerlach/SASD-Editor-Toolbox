# Bibliothek-zuerst-Abnahme V1.0 – der öffentliche Core ist das Produkt

[English](../en/LIBRARY-FIRST-V1-ACCEPTANCE.md) |
[Roadmap](ROADMAP.md) |
[Public-API- und Demo-Abnahme](DEMO-EDITOR-UND-PUBLIC-API-ABNAHME.md) |
[Release-Candidate-Gates](../en/V1-RELEASE-CANDIDATE-GATE.md)

**Produktentscheidung am 16.11.2026 bestätigt. Status: Abnahmeplan,
KEIN bestandenes Release-Zertifikat.** Die historische Turbo Editor
Toolbox war eine Bibliothek aus Quelltext-Modulen und Bausteinen für
Entwickler, kein fertiger Endanwender-Editor. Die SASD Editor Toolbox V1
verfolgt dieses Ziel als eigenständige moderne .NET-10-Bibliothek.
Andere Sprachen sollen später unabhängig folgen.

## Produkt- und Nachweispriorität

1. **Primäres Produkt:** eine verständliche, dokumentierte,
   UI-unabhängige Bibliothek `Sasd.Editor.Core` mit öffentlicher
   Schnittstelle und tatsächlich nutzbarem Paket.
2. **Korrektheit:** reproduzierbare Tests für Bearbeitung,
   gemeinsame Dokumente und unabhängige Ansichten, Suche, Blöcke,
   Undo, Speicherung, Fehler/Abbruch und strukturelle Invarianten.
3. **Referenzhosts:** FIRST-ED und MicroStar beweisen die Integration
   in echte Anwendungen. Terminal-Menüs, Tastenbelegung, Statuszeilen,
   Pop-ups und Drucken sind Sample-/Host-Aufgaben, nicht die allgemeine
   Bibliotheks-API. Bisherige relevante Terminal-Nachweise bleiben gültig.
4. **Historische Vorlage:** Das Handbuch von 1985 beschreibt
   Verhaltensanforderungen, nicht eine zu kopierende Pascal-API/ABI,
   DOS-Laufzeit oder vollständige Endanwender-Anwendung.

**Stoppregel:** Keine zusätzlichen Editor-Features nur zur Verbesserung
der Referenzoberfläche. Nur reproduzierte freigaberelevante Fehler oder
ausdrücklich genehmigte Umfangsänderungen rechtfertigen neue Arbeit.
Ein funktionierender Demo-Editor ersetzt keine unabhängige API-Abnahme;
ein funktionierender Paket-Consumer hebt alte verbindliche Terminaltests
nicht eigenmächtig auf.

## Prüfmatrix für unabhängige Bibliotheksnutzer

Für den **eingefrorenen Kandidaten-SHA** ausfüllen. Historische
Bestandslisten und CI-Ergebnisse älterer Commits genügen nicht.
Je Zeile **PASS / FAIL / BLOCKED / APPROVED LIMITATION** sowie
SHA, konkreten Prüfnachweis, Prüfer und Datum dokumentieren.

| ID | Abnahmefrage aus Entwicklersicht | Benötigter Nachweis | Stand |
| --- | --- | --- | --- |
| LIB-01 | Funktioniert Core ohne Console, FIRST-ED, MicroStar, WinForms, WPF oder sonstige Host-Abhängigkeit? | Projekt-/Abhängigkeitsprüfung, Build und Tests | OFFEN |
| LIB-02 | Kann ein fremdes Projekt mit ausschließlich öffentlichen APIs Dokumente erstellen, bearbeiten, navigieren, suchen, Blöcke bilden und Undo ausführen? | Separat kompilierter Consumer, API-Tests, erkannte Lücken | OFFEN |
| LIB-03 | Sind Lebenszyklen von Dokumenten und verknüpften Ansichten sowie Cursor-/Viewport-Zustände verständlich und sicher? | API-Review, Tests, dokumentierte Grenzen | OFFEN |
| LIB-04 | Sind UTF-8, optionaler Legacy-8-Bit-Codec, Fehler, ungültige Eingaben, Abbruch und Dateischutz getrennt geregelt? | Regressionen und entbehrliche Testdateien | OFFEN |
| LIB-05 | Lassen sich Befehle, Eingabezuordnung, Rendering und Hooks ohne Kopieren des Hosts integrieren? | Überprüfung der Host-Grenzen und unabhängiges Integrationsbeispiel | OFFEN |
| LIB-06 | Kann ein unabhängiges Projekt das **tatsächlich gepackte NuGet-Paket** wiederherstellen und nutzen? | PackageReference-Consumer unter Windows und Ubuntu am **Kandidaten-SHA** | OFFEN |
| LIB-07 | Sind öffentliche Einstiegspunkte, API-Beispiele, Fehlermodell, Nullability, Kompatibilität und Ausschlüsse verständlich dokumentiert? | README, XML-Doku, samples/dotnet, API-Review und Release Notes | OFFEN |
| LIB-08 | Stimmen Spezifikation, Konformitätstests, Paketmetadaten, Lizenz und Herkunft zum verteilten Produkt? | Manifest, Tests, Paketinhalt, SHA-256 und Rechteprüfung | OFFEN |

Die aktuelle CI führt bereits eigenständige Projekt- und Paket-Consumer
auf Windows/Ubuntu aus und prüft Paketinhalt und SHA-256. Das sind
**wertvolle vorhandene automatische Belege**, aber die individuellen
LIB-01..08-Urteile und der endgültige Release-SHA sind hier noch nicht
freigegeben. Keine stillschweigende Umwertung auf PASS.

## Zu prüfende Bibliotheksgrenzen

- Core verwaltet Dokumente, Textpuffer und Bearbeitung; die Oberfläche
  verwaltet native Eingaben, Dialoge, Tastenbelegung und Rendering.
- Externe Programme sollen normal editieren können, ohne Ctrl-K/Ctrl-Q
  zu simulieren oder FIRST-ED-Interna zu übernehmen.
- Verknüpfte Fenster teilen Dokumentinhalt, nicht Cursor, Viewport
  und fensterlokale Einstellungen; strukturelle Änderungen und Undo
  müssen konsistent bleiben.
- Speicheranbieter sind austauschbar. Unicode-Dateien und der optionale
  historische 8-Bit-Import/-Export sind strikt getrennt. Ein gescheiterter
  Schreibvorgang darf bestehende Daten nicht stillschweigend zerstören.
- Review von öffentlichen APIs: unnötig freigelegte Interna, unklare
  Namen, nicht geprüfte Fehlerpfade und inkompatible Änderungen erkennen.
  Keine neue Abstraktionsschicht ohne konkretes Consumer-Problem erzwingen.
- `spec/` und `conformance/` tragen die sprachunabhängigen Regeln.
  C++ und Java sind spätere eigenständige Implementierungen, keine
  Laufzeitvoraussetzung für die .NET-V1.

## Referenzhosts: nachgeordnet, nicht bedeutungslos

Das [Terminal-Prüfprotokoll](ECHTES-TERMINAL-V1-PRUEFPROTOKOLL.md)
enthält gemeldete Windows-/WSL-Tests FE-01..11, MS-01..10,
TAB-01..05 und IO-01..05 einschließlich des Windows-Nachweises
zum Schutz einer bestehenden Datei bei Unicode-Ablehnung.
Die Beobachtungen und ihre Grenzen erhalten. WSL ist **kein
natives Debian**; der exakte Session-Commit fehlt zum Teil.
Bisherige Pflichtprüfungen sind durchzuführen oder ausdrücklich
durch den Produkteigentümer einzugrenzen, nicht einfach als
bestanden zu erklären. HP-01..HP-10 dienen der Umfangs- und
Nachweisprüfung, nicht einem vollständigen DOS-Editor-Nachbau.

## Kandidatennachweis (erst nach tatsächlicher Prüfung ausfüllen)

| Feld | Wert |
| --- | --- |
| Exakter Release-Kandidaten-SHA | NICHT EINGEFROREN |
| .NET-SDK, Betriebssystem und Runner | FÜR FINALEN KANDIDATEN NICHT ERFASST |
| Unabhängiger ProjectReference-Consumer | FINALER SHA-NACHWEIS OFFEN |
| Unabhängiger NuGet-PackageReference-Consumer | FINALER SHA-NACHWEIS OFFEN |
| NuGet-Paketname, SHA-256 und Herkunft | FINALES VERTEILUNGSPAKET OFFEN |
| LIB-01..08-Prüfurteile | OFFEN |
| Verbindliche Host-Ausnahmen/Umfangsanpassungen | OFFEN |
| Freigabe mit Prüfer/Datum | NICHT ERTEILT |

Das Entwicklungsrepository verwendet derzeit **MIT**. Für die
Veröffentlichung unter der SASD-Organisation ist **AGPL-3.0-only**
vorgesehen, vorbehaltlich Rechte-/Herkunftsprüfung und konsistenter
Release-Paket-Metadaten. Frühere MIT-Pakete dürfen nicht rückwirkend
als AGPL ausgewiesen werden. Kommerzielle Lizenzierung ist vertagt.
Keine Tags, Uploads, Repository-Übertragung oder Veröffentlichung
ohne ausdrückliche Freigabe.
