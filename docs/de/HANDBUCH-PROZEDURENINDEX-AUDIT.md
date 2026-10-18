# V1-Audit des historischen Prozeduren- und Funktionsindexes

## Quelle und Einordnung

Das *Turbo Editor Toolbox Owner's Handbook* (1985) enthält auf den gedruckten Seiten **243–244** einen Prozeduren-/Funktionsindex mit **123** Einträgen. Die Einzelbeschreibungen stehen auf den gedruckten Seiten **118–240**. Eigenständig erfasste Namen und Seitenverweise liegen in [der maschinenlesbaren Inventarliste](../handbook-procedure-index-v1.json). Weder das historische Handbuch noch Pascal-Quellcode werden ins Repository übernommen.

Die Klassifikation beschreibt **beobachtbare Aufgaben**, keine 1:1-Abbildung historischer Pascal-APIs. Jeder Datensatz verweist auf die aktuelle SASD-Dokumentation. `implemented` bedeutet eine zunächst plausible konzeptionelle .NET-Zuordnung, **keinen** Nachweis vollständiger historischer Randfallparität. Dies ist **noch keine V1-Freigabe**.

## Ergebnis der ersten Zuordnung

| Klassifikation | Anzahl | Bedeutung |
|---|---:|---|
| Implemented | 84 | Dokumentierte Aufgabe in der .NET-Referenz abgebildet. |
| Modern replacement | 8 | Moderne Invariante/Abstraktion ersetzt historische interne Mechanik. |
| Host/sample concern | 27 | Darstellung, Prompts und Wrapper gehören nicht zum portablen Core. |
| Not applicable | 3 | DOS-/BIOS-/Videospeichermechanik ist nicht portabel. |
| Documented divergence | 1 | Bewusst dokumentierte moderne Abweichung. |
| Needs review | 0 | Vollständiger Verhaltensnachweis steht noch aus. |
| **Gesamt** | **123** | Ein Eintrag je gedruckter Funktionsbeschreibung. |

## Zuordnung der letzten zwei Routinen

- `EditGenlineno` (Handbuch S. 176): Der historische Rückwärtslauf wird durch `Cursor.Line + 1` im fensterspezifischen Status ersetzt; die Berechnung ist O(1) und hat keine Darstellungs-Nebeneffekte.
- `EditHscroll` (Handbuch S. 179): Der UI-unabhängige Kern korrigiert die horizontale Sichtbarkeit **aller** Fenster anhand einer vom Host gelieferten Breite; der Terminalhost ruft den Dienst vor dem Rendern aller Fenster auf.

Siehe [Kompatibilität von Zeilennummern und horizontaler Sichtbarkeit](BILDSCHIRMPOSITION-KOMPATIBILITAET.md) einschließlich der bewusst abweichenden historischen Abbruchprüfung.

In der **ersten Indexzuordnung** bleiben keine `needs-review`-Einträge übrig. Das ist ausdrücklich **keine abschließende V1-Freigabe**: Testtiefe, Host-Wrapper und alle als implementiert eingeordneten Routinen benötigen weiterhin einen gezielten Release-Audit.

`EditDefineTab` ist eine dokumentierte Abweichung: historische globale Tabulatorbreite gegenüber SASD-Fensteroptionen. Für interaktive Kommandoprozessoren und Bildschirmfunktionen müssen die Host-Grenzen gesondert bewertet werden; dies ist keine Behauptung einer vollständigen DOS-Oberflächenkopie.

## Zusätzliche ausführbare Belege: portable Tests

Die .NET-Tests laden inzwischen jede Datei aus `conformance/v1/manifest.json`, nicht mehr stillschweigend nur `core-smoke.json`. Die neuen Fälle in `screen-and-annotations.json` prüfen das beobachtbare Verhalten zu `EditGenlineno`, `EditHscroll`, `EditColorFile` und `EditColorLine`, einschließlich verknüpfter Fenster, Flags sowie Text-/Undo-Invarianten. Das ist **gezielte ausführbare Evidenz für diese Zuordnungen**, kein Nachweis für sämtliche 123 historischen Routinen.

## Maschinengeprüfte Verweise auf ausführbare Belege

Die Inventarliste unterstützt pro historischer Routine ein optionales Feld `conformanceCases`. Die Repository-Vertragsprüfung kontrolliert, ob jede referenzierte Fall-ID in einer durch das portable Manifest eingebundenen Datei vorkommt, weist doppelte oder unbekannte IDs zurück und meldet die Zahl verknüpfter Routinen. **Derzeit sind 46 von 123 Zeilen** mit konkreten portablen Fällen verknüpft. Für alle anderen bestehen zunächst nur konzeptionelle beziehungsweise dokumentierte Zuordnungen, deren Regressionstest-Abdeckung weiterhin einzeln geprüft werden muss.

Diese Verweise sind **gezielte Testbeispiele**, keine vollständigen historischen Verhaltensbeweise. Die Prüfung erzeugt bewusst keine erfundenen Tests für Einträge, die lediglich als `implemented` oder `modern-replacement` eingeordnet wurden.

Neun neue portable Befehlsfolgen liefern Nachweise für zehn weitere Routinen zu unabhängigen Blockgrenzen, Zeilen-Realignment, Markeridentität und transaktionalem Undo. Erwartete Befehlsablehnungen werden explizit statt als Editierung behandelt.

Die neuen Fälle in `viewport-navigation.json` liefern portable Regressionstest-Belege für acht weitere historische Navigationsroutinen: vertikale Cursorbewegungen, Scroll Up/Down, Page Up/Down und Dateigrenzen. Berücksichtigt werden verknüpfte Fenster, virtuelle Spalten, einzeilige Viewports und sehr große vom Host gelieferte Zeilenzahlen. Ihre Referenz-IDs werden anhand des Conformance-Manifests maschinell geprüft.

Neue portable Befehlsfolgen in `search-and-replace.json` liefern zusätzliche Nachweise für die historischen Routinen `EditFind` und `EditReplace`. Sie unterscheiden eine ausdrücklich leere Sucheingabe von einem fehlenden Parameter und prüfen Find Again, erfolglose Suchen, Ersetzen durch leeren Text sowie Undo. Die Fälle verwenden den semantischen Dispatcher ohne Bindung an ein konkretes Tastaturlayout.

Die portablen Fälle in `file-read-topology.json` verknüpfen `EditFileRead` und `EditReatxtfil` mit einem eingespeisten **bereits dekodierten Zeilenbestand** und dem öffentlichen Read-File-Befehl. Geprüft werden ursprüngliches Fenster/Cursor, verknüpfte Ansichten, Block-/Marker-Realignment, Wrapped-Flags, leere Eingaben und ein einzelnes Undo. Da das Byteformat separat getestet wird, beweisen diese Links nicht automatisch jedes historische Detail des 0x8D-Dateiformats.

Acht weitere portable Such-/Ersetzungstests sichern die standardmäßig groß-/kleinschreibungsunabhängige Suche, Find Again ohne automatischen Umbruch, unabhängige verknüpfte Ansichten, Dokumentwechsel zwischen normalen Befehlen, sehr große virtuelle Spalten und Find Again nach einer Ersetzung ab. Die separaten asynchronen Host-Regressionstests des .NET-Kerns bleiben dafür zusätzlich notwendig.

Vier neue portable Ablehnungsfälle sichern die einzeilige Such-/Ersetzungsgrenze ab. CR und LF in Mustern beziehungsweise Ersatztexten müssen ohne Veränderung von Cursor, Text oder Undo abgelehnt werden.

Portable Befehlsfolgen prüfen jetzt Find Again nach Undo und leerer Ersetzung, Undo-Isolation zwischen Dokumenten sowie Ersetzung innerhalb markierter Blöcke mit verknüpften Fenstern. Diese gemeinsamen Tests beschreiben nur beobachtbares Verhalten; asynchrone Host-Zwischenfälle bleiben zusätzlich durch .NET-Tests abgesichert.

Sieben weitere portable Testfälle in `search-and-replace.json` prüfen das optionale Wrap-around des modernen Suchdienstes über `find_next_with_options`. Die historische FIRST-ED-Suche bleibt standardmäßig ohne Umbruch. Getestet werden Trefferstartgrenzen, ganze Wörter, die Suchreihenfolge über mehrere Zeilen und die für Find Again gespeicherten Optionen.

Drei neue negative portable Fälle in `file-read-topology.json` weisen nach, dass ungültige dekodierte Zeilen (eingebettetes CR, LF oder 0x8D) den *gesamten* Import mit `file_io_failed` ablehnen, ohne Dokument oder Undo zu verändern. Sie ergänzen separate .NET-Tests für Byteformat und asynchrone Lesevorgänge, ersetzen diese jedoch nicht.

Neue Regressionstests für konkurrierende Dateiausgaben prüfen die pfadbezogene Serialisierung des eingebauten Staged-Writers innerhalb eines Prozesses, abbrechbare Wartende, Freigabe nach Fehlern und unabhängige Ausgaben an verschiedene Ziele. Das erweitert die Persistenz-Evidenz, ersetzt aber nicht den offenen Einzelnachweis für historische Routinen.

Fünf neue portable Fälle zur Dateiausgabe testen WriteFile und SaveFile mit einem protokollierenden Codec für logische Zeilen, ohne von echten Dateien oder Bytes abzuhängen. Vier Fälle liefern erstmals ausdrücklich verknüpfte Tests für die historische Routine `EditFileWrite`. Damit besitzen jetzt **33 von 123** historischen Routinen direkte Verweise auf portable Testbelege; die übrigen Zuordnungen benötigen weiterhin den vollständigen Verhaltens-Audit.

Die neue `compatibility-primitives.json`-Suite verknüpft 13 bisher unbelegte historische Prozeduren mit tatsächlich ausgeführten Tests. Sie umfasst unabhängige Optionen verknüpfter Fenster, virtuelle Spalten, einbasierte Befehle, historische Wortklassen, Undo und die moderne Behandlung wirkungsloser Textbefehle. **46 von 123** Indexeinträgen besitzen damit ausdrücklich portable Belege – keine vollständigen historischen Freigabezertifikate.

## Nächste Schritte und Abschlusskriterien

Jeden offenen Eintrag anhand seiner eigenen Handbuchbeschreibung prüfen, die bereits vorhandenen Tests abgleichen und nur bei echten Verhaltenslücken unabhängigen Code samt Regressionstests ergänzen. Auch die als implementiert eingestuften Zuordnungen brauchen einen Randfall-/Testabgleich; künstliche Pascal-APIs sind ausdrücklich unerwünscht. Inventarliste, deutsche/englische Audit-Dokumente, Matrix und nötigenfalls sprachneutralen Vertrag/Conformance-Fälle synchron halten.

Die Python-Vertragsprüfung sichert **123 eindeutige Namen**, die fortlaufenden gedruckten Seiten **118–240**, zulässige Statuswerte und existente Belegpfade. Das beweist die **Inventarvollständigkeit**, nicht die historische Verhaltensgleichheit. V1 bleibt bis zum Abschluss des übergreifenden Verhaltens- und Regressionstest-Audits offen.

