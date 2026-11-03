# Entwicklungs-Roadmap — SASD Editor Toolbox

[English](../en/ROADMAP.md) | [Repository-Übersicht](../../README.md) | [V1-Release-Gap-Analyse](../en/V1-RELEASE-GAP-ANALYSIS.md)

**Stand: 3. November 2026.** Geprüfter Default-Branch: `main`, Commit `c9b60e7d1e42a3a3614b60e86dead007e646aac2` (2. November 2026). Die GitHub-Actions-Workflows `repository-contract` und `dotnet` waren für **genau diesen Commit** erfolgreich. **V1.0 ist weder freigegeben noch veröffentlicht.**

Diese Roadmap führt den bisher auf Architektur-, Kompatibilitäts-, Release-Gap-, Abnahme- und Release-Candidate-Dokumente verteilten Plan zusammen. Sie beschreibt **eine technische Richtung mit Nachweisen, keinen verbindlichen Veröffentlichungstermin**. M0–M5 ordnen bereits geleistete Arbeit *rückblickend* ein; es wird **nicht** behauptet, dass früher gleichnamige GitHub-Meilensteine existierten. Spätere Phasen sind Vorschläge und erweitern den eingefrorenen V1-Umfang nicht stillschweigend.

## Bedeutung der Statusangaben

- `[x]` bedeutet, dass **genau der jeweilige Punkt** durch Code, eingecheckte Tests/Dokumentation, einen erfolgreich beobachteten CI-Lauf oder ausdrücklich eingegrenzte Nutzerrückmeldung belegt ist. Daraus folgt **keine** pauschale historische Gleichwertigkeit.
- `[ ]` bedeutet, dass Implementierung, Prüfung, manuelle Durchführung, Dokumentation, Entscheidung, Abnahme oder Veröffentlichung noch offen ist.
- **IMPLEMENTIERT** = Code vorhanden; **AUTOMATISIERT** = Test/CI prüft; **GEMELDET** = praktische Beobachtung, eventuell an älterem Stand; **ABGENOMMEN** = verbindliche Prüfung mit Nachweis erfolgreich; **VERÖFFENTLICHT** = ausdrücklich genehmigt und freigegeben.
- Windows, Debian **unter WSL** und **natives Debian/Linux** sind unterschiedliche Prüfumgebungen. Grüne CI und ein WSL-Terminaltest ersetzen keine native Terminalabnahme. Ergebnisse älterer Commits gelten nicht automatisch für spätere Änderungen.

## Meilensteinübersicht

| Phase | Ziel | Stand | Bezug zur Freigabe |
| --- | --- | --- | --- |
| M0 | Repository, Clean-Room-Regeln, Architektur, gemeinsamer Vertrag | **Erledigt** | V1-Fundament |
| M1 | Oberflächenunabhängiges Editor-/Dokument-/Fenstermodell | **Implementiert und getestet** | V1 |
| M2 | Editierfunktionen, historische Adapter, sichere Speicherung | **Implementiert; historische Abnahme noch offen** | V1 |
| M3 | Interaktiver FIRST-ED-Terminalhost | **Implementiert; gemeldete Tests; Endabnahme offen** | V1 |
| M4 | MicroStar-Terminalabläufe | **Im Sample implementiert; manuelle Abnahme unvollständig** | V1 |
| M5 | Sprachübergreifender Vertrag, .NET-CI, Consumer, RC-Paket | **Automatisiertes Fundament erledigt** | V1 |
| M6 | Interaktive und historische V1-Abnahme | **In Arbeit / Freigabehindernis** | V1 |
| M7 | Unveränderlichen V1-Release-Candidate prüfen | **Offen** | V1 |
| M8 | Eigentümerfreigabe und V1.0-Veröffentlichung | **Offen** | V1 |
| F0+ | Migration zum SASD Editor Toolkit, Desktop-UI, Sprachportierungen | **Zukunft; kein V1-Blocker** | Nach V1 |

## M0 – Projektidentität, Architektur und Repository-Vertrag

**Ziel:** ein langfristig wartbares, eigenständiges Editor-Fundament statt einer Pascal-Quelltextkopie oder Übernahme von DOS-Implementierungsdetails.

- [x] Repository, MIT-Lizenz und verbindliche Arbeitsregeln in [`AGENTS.md`](../../AGENTS.md) festlegen.
- [x] Unabhängig entwickelte, verhaltensorientierte Modernisierung der Turbo Editor Toolbox von 1985 definieren; kein Borland-Quelltext und keine geschützten Handbuchkopien.
- [x] Wiederverwendbaren Editor-Core von Terminal- und zukünftigen Desktop-Hosts trennen.
- [x] Separate `src/`-, `tests/`- und `samples/`-Sprachbereiche für .NET, C++ und Java einrichten.
- [x] [`spec/editor-v1.md`](../../spec/editor-v1.md) als sprachneutralen Verhaltensvertrag und [`conformance/`](../../conformance/README.md) als gemeinsame ausführbare Testfallbasis etablieren.
- [x] Englisch als primäre und Deutsch als begleitende technische Dokumentationssprache vorsehen, einschließlich [Architektur](ARCHITECTURE.md) und [Mehrsprachigkeitsregeln](MEHRSPRACHIGE-ENTWICKLUNG.md).
- [x] Gewollte Kompatibilitätsgrenzen in der [V1-Kompatibilitätsmatrix](BORLAND-V1-KOMPATIBILITAET.md) dokumentieren.

**Abnahmekriterium:** Architektur, Repository-Vertrag und nachvollziehbares gemeinsames Verhalten dokumentiert. **Für das Fundament erfüllt.**

## M1 – Editor-Core, Dokument-, Text- und Fenstermodell

**Ziel:** tatsächliche Textbearbeitung über eine wiederverwendbare, UI-unabhängige API.

- [x] Zeilenorientierter Buffer-Vertrag (`ITextBuffer`), verkettete Zeilenspeicherung und `EditorDocument`.
- [x] Trennung von Dokumentidentität, `EditorWindow` und `EditorSession`; mehrere verknüpfte Fenster verwenden ein Dokument, aber eigenen Cursor-/Viewport-Zustand.
- [x] Fensterstapel, Zeilengeometrie und historische Create-/Delete-Window-Regeln.
- [x] Cursor- und Viewport-Navigation, virtuelle Spalten, Insert/Overtype, Tabulatoren, Autoindent, Ränder und Umbruch.
- [x] Zeilenattribute, ausgehende `Wrapped`-Grenzen, Benutzerfarben, Marker und unabhängige Blockgrenzen.
- [x] Zentraler `EditorLineTopology`-Abgleich von Fenstern, Markern und Blöcken bei strukturellen Änderungen.
- [x] Transaktionales Undo einschließlich dokumentübergreifender Blockoperationen und Wiederherstellung des Navigationszustands.
- [x] Unit-Tests der Core-Invarianten und eigenständig nutzbare öffentliche Core-Schnittstellen.

**Abnahmekriterium:** .NET-Core arbeitet ohne Console-, WinForms-, WPF- oder andere konkrete Oberflächenbibliotheken. **Implementiert; M6 prüft noch freigaberelevante Randfälle.**

## M2 – Befehle, historische Kompatibilität und Dateiverarbeitung

**Ziel:** spezielle FIRST-ED-Semantik abgrenzen und dennoch moderne Sicherheit und Erweiterbarkeit erhalten.

- [x] Editierbefehle: Einfügen/Löschen/Zeilen verbinden/Neue Zeile, Block kopieren/verschieben/löschen, Groß-/Kleinschreibung, Zentrieren und Absatzformatierung.
- [x] Eigene FIRST-ED-Kompatibilitätsprozessoren für Navigation, Primitive, Blöcke und Neuformatierung; semantischer Command-Dispatcher.
- [x] Suchen/Ersetzen/Find Again, typisierte Prompt-Daten, Befehlsgrenzen-Validierung und typisierte Fehlermeldungen.
- [x] FIRST-ED-Tasten-/Präfixbelegung, begrenzter Typeahead, Ctrl-U-Unterbrechung und kooperatives Scheduling/Hooks.
- [x] Moderne UTF-8-Speicherung getrennt vom optionalen historischen 8-Bit-Codec mit `0x8D`-Umbruchgrenze.
- [x] Vorbereitete/sichere Schreiboperationen, Überschreiben/Abbrechen, Revisions-/Konkurrenzschutz und gezielte Regressionstests.
- [x] Die **123 im gedruckten Handbuchindex genannten Routinen** und **20 zusätzlichen Namen** erfassen. Zum dokumentierten Prüfstand haben **103/123** Indexroutinen Verweise auf benannte portable Konformitätsfälle; für **20/123** ist anderer Nachweis zu prüfen. Ein Verweis oder eine Klassifikation beweist **keine** historische Vollständigkeit.
- [ ] Abnahme der tatsächlich relevanten historischen Verhaltensweisen und der gewollten Abweichungen in M6 abschließen; das Inventar nicht als Kompatibilitätszertifikat ausgeben.

**Nachweise:** [Kompatibilitätsmatrix](BORLAND-V1-KOMPATIBILITAET.md), [Handbuchindex-Audit](HANDBUCH-PROZEDURENINDEX-AUDIT.md), [englische Prüfliste der 20 unverketteten Einträge](../en/HANDBOOK-UNLINKED-INDEX-V1-REVIEW.md) und [historisches Entscheidungsregister](../en/V1-HISTORICAL-PARITY-DECISION-REGISTER.md).

## M3 – Interaktiver FIRST-ED-Terminalhost und Entwicklerdemo

**Ziel:** den Editor-Core in einem echten Editor demonstrieren, ohne Terminalabhängigkeiten im Core.

- [x] Interaktives `Sasd.Editor.FirstEd.Sample` mit FIRST-ED-/WordStar-Tasten, Dialogen, Status, Viewport und mehrfachen/verknüpften Fenstern.
- [x] Geführter Start mit `--demo` und eigenständiger öffentlicher Core-Consumer.
- [x] Sichere sichtbare Darstellung von Steuerzeichen ohne Ausführung von Escape-Sequenzen; Resize-/Redraw-Korrekturen und Headless-Regressionstests.
- [x] FE-01..FE-07 sowie FE-09..FE-11 unter Windows Terminal als PASS **gemeldet**; FE-08 als PASS für die vorgeladene Steuerzeichenanzeige mit genau dokumentierter Einschränkung.
- [x] FE-01..FE-11 unter Debian **WSL** nach Korrektur der FE-03-/FE-07-Testdurchführung als PASS **gemeldet**, bei dokumentiertem lokalem Commit `adc3d813e7620dd5c93d7d98a98fa66992332452`.
- [ ] Vollständige Endabnahme auf dem späteren Kandidaten-Commit dokumentieren und manuell unter **nativem Debian** prüfen; die bisherigen Rückmeldungen sind keine Gesamtfreigabe.

**Nachweise:** [Terminal-Prüfprotokoll](ECHTES-TERMINAL-V1-PRUEFPROTOKOLL.md), [Demo-/Public-API-Abnahme](DEMO-EDITOR-UND-PUBLIC-API-ABNAHME.md), [englisches Release-Abnahmeformular](../en/V1-ACCEPTANCE-WORKSHEET.md).

## M4 – MicroStar-Terminalbeispiel mit Datei- und Druckabläufen

**Ziel:** F10-/Menü- und Beispielanwendungsabläufe abdecken, ohne MicroStar-Oberflächenlogik in den Core einzubauen.

- [x] F10-Menü, Navigation, Popups/Overlay-Stapel, Dialogfokus und Grundlagen der Bildschirmwiederherstellung.
- [x] Suchen/Ersetzen/Goto, Dateibrowser, Save/Write/Read, Öffnen und Ersetzungsabläufe auf Sample-Ebene.
- [x] Blockdateien lesen/schreiben, Blockaktionen sowie Abbruch-/Überschreibepfade auf Sample-Ebene.
- [x] Portables Print-to-File mit Auftragsverwaltung, Seiteneinteilung, Status und Abbruch; **kein** physischer DOS-Drucker.
- [x] Automatisierte Tests für die MicroStar-Hostkomponenten.
- [x] MS-02..MS-05 für Windows und WSL als PASS **gemeldet**; MS-01 (F10-Menüöffnung) gemeldet, aber die Prüfung auf unveränderten Text und Undo im Repository weiterhin nur PARTIAL.
- [ ] Zusätzlich gemeldetes **MS-06: Blockoperationen 5/5 PASS unter Windows und Debian/WSL** mit genauem Commit, Prüfer und Einzelschritten ins zentrale Formular übernehmen; im geprüften Repository-Stand ist MS-06 noch als nicht berichtet eingetragen.
- [ ] MS-07..MS-10 durchführen und dokumentieren, MS-01 vollständig nachprüfen und den endgültigen Kandidaten auch unter nativem Debian testen.

**Abnahmekriterium:** Alle zum V1-Umfang gehörenden Sample-Abläufe sind auf den geforderten Plattformen mit PASS belegt oder ausdrücklich begründet und freigegeben. **Noch nicht erfüllt.** Siehe [Terminal-Prüfprotokoll](ECHTES-TERMINAL-V1-PRUEFPROTOKOLL.md).

## M5 – Konformität, CI und Vorabpaket

**Ziel:** den öffentlichen Core reproduzierbar bauen, prüfen und aus einem anderen Programm verwenden können.

- [x] Gemeinsame maschinenlesbare `conformance/v1`-Testfälle und .NET-Konformitätsadapter.
- [x] Workflow `repository-contract` zur Struktur-/Vertragsprüfung.
- [x] Workflow `dotnet` für .NET 10 auf Windows und Ubuntu, einschließlich Public-API- und FIRST-ED-Help-Tests.
- [x] Eigenständigen Core-Consumer mit Projekt-/Source-Referenz tatsächlich ausführen.
- [x] **Vorläufiges, unveröffentlichtes** `Sasd.Editor.Core.1.0.0-rc.1.nupkg` erzeugen und unabhängigen PackageReference-Consumer aus lokalem Feed mit isoliertem Cache ausführen.
- [x] Paketmetadaten/README/Assembly prüfen; SHA-256-Begleitdatei und Build-Herkunftsnachweis erzeugen.
- [x] Beide Workflows für `main` `c9b60e7d1e42a3a3614b60e86dead007e646aac2` erfolgreich: [Repository-Vertrag](https://github.com/Robin-Goerlach/SASD-Editor-Toolbox/actions/runs/41394415645) und [.NET](https://github.com/Robin-Goerlach/SASD-Editor-Toolbox/actions/runs/41394415519).
- [ ] Den **später eingefrorenen Kandidaten** und dessen tatsächlich herunterladbares Paket unter M7 separat nachweisen. Alte grüne CI-Läufe sind keine spätere Release-Freigabe.

**Abnahmekriterium:** Automatisierte Builds, Tests und Projekt-/Paket-Consumer unter beiden CI-Systemen. **Technisches Fundament erfüllt; Veröffentlichungs-Gate offen.**

## M6 – Interaktive und historische V1.0-Abnahme (aktuelle Priorität)

**Ziel:** die Freigabe abschließen, statt immer neue Funktionen hinzuzufügen. Seit 30. Oktober 2026 gilt Feature-Freeze: nur einen **reproduzierten V1-Blocker** beheben, mit Regressionstest und Nachprüfung.

### M6.1 – FIRST-ED und MicroStar in echten Terminals

- [x] Die **11 FE**-, **10 MS**-, **5 TAB**- und **5 sicheren Legacy-I/O**-Prüfungen im [Abnahmeformular](../en/V1-ACCEPTANCE-WORKSHEET.md) definieren.
- [x] Eingegrenzte Windows-/WSL-Ergebnisse für FIRST-ED mit Korrekturen und Einschränkungen festhalten.
- [x] Teilweise MicroStar-Nachweise (MS-01..MS-05) für Windows/WSL dokumentieren; neueren MS-06-5/5-Bericht getrennt bis zur Übernahme verfolgen.
- [ ] Noch offene Windows-Einzelschritte mit echtem SHA, SDK, Terminal und Prüfer abschließen.
- [ ] Die vorgegebenen Prüfungen auf einem **nativen Debian-/Linux-Terminal** wiederholen, nicht nur unter WSL.
- [ ] TAB-01..TAB-05 für fensterlokale Tabulatorbreiten (Zahl, verknüpfte Ansichten, Fokuswechsel, Abbruch/Fehleingabe, leere Eingabe) festhalten.
- [ ] Legacy IO-01..IO-05 ausschließlich mit **entbehrlichen Testdateien** prüfen: 8-Bit-Roundtrip, Kodierungsfehler, Abbruch, abgelehntes Überschreiben und Write-Dokumentidentität.
- [ ] FAIL/BLOCKED-Einträge bewerten, tatsächlich blockierende Fehler beheben und betroffene Tests am korrigierten Commit wiederholen.

### M6.2 – Historische Abweichungen ausdrücklich entscheiden

- [x] Produktziel festlegen: **modernes funktionales Editor-Fundament mit dokumentierten Abweichungen**, nicht exakte Borland-Pascal-API/ABI oder pauschale historische Vollparität (Eigentümerentscheidung vom 29. Oktober 2026).
- [x] Moderne HP-01..HP-10-Regeln im [Produktumfang](../en/V1-PRODUCT-SCOPE-PROPOSAL.md) und [Entscheidungsregister](../en/V1-HISTORICAL-PARITY-DECISION-REGISTER.md) dokumentieren.
- [ ] Für jeden HP-01..HP-10-Punkt konkrete Einzelabnahme, Nachweise und gegebenenfalls ausdrücklich genehmigte Ausnahmen erfassen.
- [ ] Unter den 20 Indexroutinen ohne direkten portablen Testlink die freigaberelevanten Fälle prüfen; reine Host-/DOS-Sachverhalte erfordern **nicht** automatisch neue portable Fälle.
- [ ] README, Lizenz-/Urheberhinweise, Paketmetadaten und Release Notes auf korrekte Drittanbieterzuordnung und **keine** unhaltbare historische Gleichwertigkeitsbehauptung überprüfen.

**Bewusst nicht V1:** originale `Optstr`-Grammatik, binäres `EDITERR.MSG`, globales `Tabsize` wie 1985, DOS-Bildschirmspeicher/Assembler, physischer Drucker, Lightning-Rechtschreibprüfung und originale Pascal-ABI. Die modernen Ersatzabläufe müssen ihre **tatsächlich zugesagten** Prüfungen dennoch bestehen.

**M6-Abnahmekriterium:** Alle für die Freigabe nötigen M6.1-Beobachtungen und M6.2-Entscheidungen liegen mit nachvollziehbarem Quellstand vor; freigaberelevante Fehler sind behoben oder ausdrücklich entschieden. **OFFEN.**

## M7 – Reproduzierbarer Release Candidate und abschließende Nachweise

**Ziel:** exakt den Build einfrieren, der freigegeben werden soll – nicht auf ältere CI-Ergebnisse vertrauen.

- [ ] Exakten `main`-SHA auswählen und einfrieren; sauberen Checkout und OS/.NET-SDK dokumentieren.
- [ ] Beide Workflows für **genau diesen SHA** erfolgreich prüfen.
- [ ] NuGet-Paket aus dem zugehörigen CI-Lauf herunterladen, SHA-256 gegen die Begleitdatei vergleichen; `lib/net10.0/Sasd.Editor.Core.dll`, Manifest und README prüfen.
- [ ] Unabhängige Source-/Projekt- und echte Paket-Consumer auf dem Kandidaten nachweisen.
- [ ] V1-Umfang, Nutzerdokumentation, Lizenz-/Attributionsprüfung und [Release Notes](../en/V1-RELEASE-NOTES-DRAFT.md) abschließen.
- [ ] Plattform-Abnahmen, HP-Entscheidungen, bekannte Einschränkungen und Nachweislinks im [RC-Gate](../en/V1-RELEASE-CANDIDATE-GATE.md) bündeln.
- [ ] Zielversion, Veröffentlichungsort und Prüferentscheidung dokumentieren. In dieser Phase nichts ohne Genehmigung produktiv veröffentlichen.

**M7-Abnahmekriterium:** Ein unveränderlicher Kandidat ist vollständig nachvollziehbar, technisch bewertet und entscheidungsreif. **OFFEN.**

## M8 – Eigentümerfreigabe und V1.0-Veröffentlichung

- [ ] Ausdrückliche, datierte Freigabe durch den Eigentümer für den akzeptierten SHA und dokumentierte Abweichungen einholen.
- [ ] V1.0-Tag/Release **erst nach** erfüllten oder ausdrücklich entschiedenen Pflicht-Gates erstellen.
- [ ] Nur am ausdrücklich vereinbarten Ort veröffentlichen; keine stillschweigende Veröffentlichung auf NuGet.org oder im separaten [SASDGMBH/Editor-Toolbox](https://github.com/SASDGMBH/Editor-Toolbox)-Release-Repository.
- [ ] Endgültige Release Notes, Paketidentität/Checksumme, Installation und bekannte Einschränkungen bereitstellen.
- [ ] Veröffentlichtes Artefakt durch einen unabhängigen Consumer prüfen und Abnahmebelege archivieren.

**M8-Abnahmekriterium:** Öffentliche, genehmigte Version mit reproduzierbarer Herkunft, korrekten Funktionsaussagen und erfolgreichem externen Konsum. **Noch nicht erreicht.**

## Nach V1 – Weiterentwicklung zum SASD Editor Toolkit (nicht Bestandteil von V1)

Das langfristige Migrationsziel ist das **[SASD Editor Toolkit](https://github.com/Robin-Goerlach/SASD-Editor-Toolkit)**, **nicht** das eigenständige [SASD UI Toolkit](https://github.com/Robin-Goerlach/SASD-UI-Toolkit). Nur weil die Toolbox inzwischen weit entwickelt ist, darf vorhandener Editor-Toolkit-Code nicht ungeprüft verworfen werden. Die Reihenfolge ist verbindlich: **zuerst Toolbox V1.0 abnehmen, danach die Migration planen/durchführen und neue Editor-Entwicklung im Editor Toolkit fortsetzen**.

### F0 – Migrationsarchitektur und Repository-Übergang

- [ ] Existierende Editor-Toolkit-APIs, Module, Tests, Kompatibilitätszusagen und Überschneidungen zum freigegebenen Toolbox-Core inventarisieren.
- [ ] Architekturentscheidungen (ADRs) für Übernahme/Refactoring, schrittweise Migration, Paketgrenzen, Namespaces, Git-Historie, Urheberzuordnung und Rückfallweg dokumentieren.
- [ ] Bewährte Tests, Konformitätsverträge, Editor-Invarianten und historische Adapter erhalten; keine unkontrollierte Komplettneuschreibung und kein pauschales Löschen.
- [ ] Paket-/Versionsnamen, Upgradepolitik, Clean-Room-Darstellung und Lizenzstrategie nach gesonderter Prüfung entscheiden.
- [ ] Zunächst einen schmalen, unabhängig testbaren Migrationsschnitt mit unverändertem beobachtbarem Verhalten abnehmen.

### F1 – Desktop-Editor und SASD UI Toolkit prüfen

- [ ] UI-unabhängigen Editor-View-/Host-Vertrag für Darstellung, Selektion/Caret, Eingabe/IME, Clipboard, Dateidialoge und Fokus definieren.
- [ ] Einen kleinen, tatsächlich bedienbaren Desktop-Editor auf Basis des freigegebenen Cores prototypisieren; zuerst schlanke Hostgrenzen statt großer Abstraktionsschichten.
- [ ] Das **separate C++ SASD UI Toolkit** als *optionales* UI-Backend evaluieren, nicht als Editor-Domänenmodell oder Pflichtlaufzeit für jede Sprache.
- [ ] Bei C++/.NET- oder C++/Java-Interop Ownership, Lebensdauer, Threads, UTF-8/UTF-16, Exceptions, Callbacks und Dispose explizit regeln. Native Hosts gegen eine kleine stabile C-kompatible Bridge abwägen; eine universelle FFI nicht voreilig festschreiben.
- [ ] Bedienbarkeit/Leistung an realen Anwendungen beurteilen; keine GUI-Freigabe ohne echte manuelle Desktop-Tests.

### F2 – Eigenständige Implementierungen in anderen Sprachen

- [x] C++-/Java-Verzeichnisgerüste und gemeinsame Vertragsregeln angelegt; .NET bleibt vorerst die aktiv entwickelte Referenz.
- [ ] Ersten C++-Vertikalschnitt (Buffer, Dokument/Ansicht, Cursor und portabler Konformitätstest-Runner) implementieren; danach Editieren/Speicherung/Undo ergänzen.
- [ ] Java als idiomatische eigenständige Implementierung mit eigenen Tests/Build/CI aufbauen; **keine** .NET-Laufzeitabhängigkeit und keine künstliche Nachbildung des .NET-Garbage-Collectors in C++.
- [ ] Python und weitere Sprachen bei konkretem Bedarf prüfen; eigene Implementierungsbereiche und Konformitätsadapter anlegen statt sofortige Vollparität zu versprechen.
- [ ] Sprachpakete dürfen unterschiedliche Versionen haben, während die Vertragskonformität ausdrücklich und überprüfbar bleibt.

### F3 – Weitere Produktfunktionen nach echtem Anwendungsbedarf

- [ ] Zusätzliche Editor-Host-Schnittstellen, Erweiterbarkeit, Rich-Text-Funktionen und weitere Backends nur mit konkretem Use Case und eigenem Meilenstein aufnehmen.
- [ ] Vollständige IDE, Syntax-/LSP-Dienste, Plugin-Marktplatz, breite Web-/Mobile-UIs und spekulative Speicheroptimierung nicht vorziehen.
- [ ] Rope/Piece Table erst anhand von Benchmarks statt Vermutungen auswählen; Kompatibilitäts- und Topologie-Tests erhalten.
- [ ] Lizenzstrategie vor jeder Änderung ausdrücklich entscheiden: die **aktuelle `LICENSE` ist MIT**. Eine diskutierte AGPL- oder kommerzielle Regelung gilt **nicht** automatisch und hebt bereits eingeräumte Rechte nicht rückwirkend auf.

## Nächster konkreter Schritt und Freigabedisziplin

**Nächster Arbeitsschritt:** die bereits gemeldeten MS-06-Ergebnisse (Umgebungen, Prüfer, SHA und fünf Teilschritte) in den Abnahmenachweisen zusammenführen; anschließend **MS-07: Blockdatei schreiben/lesen** nur mit entbehrlichen Daten prüfen. Danach MS-08..MS-10, TAB- und I/O-Prüfungen bearbeiten, mit **gesondertem nativem Debian-Lauf**. Keine neue Editor-Funktion beginnen, während diese Gates offen sind.

Ein Meilenstein ist erst durch **seine dokumentierten Abnahmekriterien** abgeschlossen, nicht durch eine gefühlte Prozentzahl. Bei Implementierungen/Fehlerkorrekturen Architekturgrenzen, sinnvolle Kommentare, Regressionstests und englische/deutsche Dokumentation bewahren. Ändert sich der akzeptierte Commit, müssen die betroffenen manuellen Nachweise und beide CI-Läufe neu bewertet werden.

**Verbindliche Detailnachweise zur V1-Freigabe:** [Release-Gap-Analyse](../en/V1-RELEASE-GAP-ANALYSIS.md) · [Abnahmeformular](../en/V1-ACCEPTANCE-WORKSHEET.md) · [historische Entscheidungen](../en/V1-HISTORICAL-PARITY-DECISION-REGISTER.md) · [Release-Candidate-Gate](../en/V1-RELEASE-CANDIDATE-GATE.md) · [Release-Notes-Entwurf](../en/V1-RELEASE-NOTES-DRAFT.md). Diese Roadmap führt zusammen, setzt aber keine dort dokumentierten Freigabegates außer Kraft.
