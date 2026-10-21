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

Die Inventarliste unterstützt pro historischer Routine ein optionales Feld `conformanceCases`. Die Repository-Vertragsprüfung kontrolliert, ob jede referenzierte Fall-ID in einer durch das portable Manifest eingebundenen Datei vorkommt, weist doppelte oder unbekannte IDs zurück und meldet die Zahl verknüpfter Routinen. **Derzeit sind 103 von 123 Zeilen** mit konkreten portablen Fällen verknüpft. Für alle anderen bestehen zunächst nur konzeptionelle beziehungsweise dokumentierte Zuordnungen, deren Regressionstest-Abdeckung weiterhin einzeln geprüft werden muss.

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

Fünf weitere gemeinsame Fälle verknüpfen jetzt `EditTab`, `EditDeleteLeftChar`, `EditDeleteRightChar` und `EditDeleteRightWord` mit den semantischen Befehlen. Unterschieden werden Tab im Einfüge-/Überschreibmodus, Backspace im virtuellen Bereich ohne Textänderung sowie das Löschen von Interpunktions- und Leerzeichenläufen. Jetzt besitzen **50 von 123** Routinen direkte portable Verweise.

Die Suite `windows-and-block-lifecycle.json` verknüpft jetzt alle 10 bislang nicht portabel belegten historischen Fenster- und Blocknavigations-/Lebenszyklusroutinen mit tatsächlich ausgeführten Fällen. Insgesamt besitzen **60 von 123** Einträgen direkte Testverweise. Die Fenster-/Dokumentzuordnung und hostneutrale Zeilenrahmen ergänzen bisherige .NET-spezifische Geometrie- und Blocktests; weitere nicht verknüpfte Routinen bleiben gesondert zu prüfen.

Sieben weitere negative und Grenzfälle prüfen abgelehnte Fensterverknüpfungen, die Einbasierung von Fensternummern, das Drei-Zeilen-Minimum beim Teilen, die Übernahme freigewordener Zeilen und die Bereinigung von Markern/Blockgrenzen beim letzten Dokumentfenster. Damit werden die zehn bereits verknüpften historischen Fenster-/Blockroutinen besser abgesichert; die Zahl der zugeordneten Routinen steigt dadurch nicht.

Acht portable Reformat-Fälle wurden mit den fünf historischen Aufgaben von `EditCompressLine`, `EditShiftLine`, `EditLongLine`, `EditShortLine` und `EditReformat` verknüpft. Damit besitzen **65 von 123** Einträgen direkte portable Testbelege. Geprüft wird beobachtbares Verhalten, nicht die identische historische Implementierung interner Hilfsroutinen.

Acht Typeahead-Fälle verknüpfen Pokechr, EditPushtbf, EditUserpush und EditAbort mit konkreten portablen Eingabe-/Abbruchtests. Vom Host stammendes Ctrl-U ist bewusst nicht mit Makroeingaben identisch; normalisierte Tasten bleiben von plattformspezifischen Scancodes getrennt. Der historische Index besitzt jetzt **69 von 123** Einträgen mit direkten portablen Testbelegen.

Die neuen Fälle in `firsted-keymap.json` verknüpfen fünf weitere historische Routinen für Ctrl-K/O/Q, Befehlsklassifikation sowie Text-/Befehlsverarbeitung mit ausführbaren Tests an der normalisierten Eingabegrenze. Damit besitzen **74 von 123** Indexeinträgen direkte portable Belege; konkrete Host-Dialoge bleiben von der wiederverwendbaren Editorbibliothek getrennt.

22 neue portable Host-Prompt-Fälle prüfen die Abbruchgrenze zwischen FIRST-ED-Tastenbelegung, gelieferten Eingabewerten und ausgeführten Befehlen. Sieben interaktive historische Cp-Wrapper erhalten konkrete ausführbare Verweise auf ihre Teilaufgaben. Damit wächst die Host-Abdeckung vor V1, ohne eine vollständige Gleichwertigkeit der DOS-Terminaloberfläche zu behaupten. **81 von 123** Indexeinträge besitzen nun portable Testverknüpfungen.

Zehn neue portable Fälle belegen jetzt sechs bisher noch nicht zugeordnete, bereits implementierte FIRST-ED-Kernelroutinen (Zentrieren, Linksbewegung, Zeilen verbinden, Steuerzeichen einfügen sowie Blockmarkierungen entfernen und wiederherstellen). Der Index besitzt jetzt für **87 von 123** Routinen konkrete Testverweise. Die übrigen Einträge erfordern eine gesonderte Funktions- oder Host-Abnahme und gelten nicht automatisch als fehlend.

Zehn weitere portable Fälle zum kooperativen Lebenszyklus verknüpfen die fünf bisher unbelegten, bereits implementierten Scheduler-/Unterbrechungs-/System-/Exit-Routinen mit tatsächlichen Tests. Geprüft werden Vorrang der Eingabe, begrenzte Idle-Abschnitte, geregeltes Beenden und das einmalige Verbrauchen eines physischen Ctrl-U-Abbruchs. Zusammen mit den Kernel-Fällen besitzen nun **92 von 123** Handbucheinträgen direkte portable Verweise. Historische DOS-Scheduler-Interna bleiben ausgeklammert.

Zwölf weitere hostneutrale Cp-Wrapper-Fälle verknüpfen neun bislang unbelegte historische interaktive Prozeduren für Fenster, Marker, Ränder, Tabulatoren und Undo-Grenzen. Geprüft werden bestätigte beziehungsweise abgebrochene Dialoge sowie ungültige zustandsabhängige Werte über echte Ctrl-K/O/Q-Befehle. Damit besitzen **101 von 123** historische Routinen direkte portable Testbelege, jedoch keine pauschale Zertifizierung der DOS-Oberfläche.

## Nächste Schritte und Abschlusskriterien

Jeden offenen Eintrag anhand seiner eigenen Handbuchbeschreibung prüfen, die bereits vorhandenen Tests abgleichen und nur bei echten Verhaltenslücken unabhängigen Code samt Regressionstests ergänzen. Auch die als implementiert eingestuften Zuordnungen brauchen einen Randfall-/Testabgleich; künstliche Pascal-APIs sind ausdrücklich unerwünscht. Inventarliste, deutsche/englische Audit-Dokumente, Matrix und nötigenfalls sprachneutralen Vertrag/Conformance-Fälle synchron halten.

Die Python-Vertragsprüfung sichert **123 eindeutige Namen**, die fortlaufenden gedruckten Seiten **118–240**, zulässige Statuswerte und existente Belegpfade. Das beweist die **Inventarvollständigkeit**, nicht die historische Verhaltensgleichheit. V1 bleibt bis zum Abschluss des übergreifenden Verhaltens- und Regressionstest-Audits offen.


## Ergänzende .NET-Host-Belege: erstes Speichern

Der FIRST-ED-Terminalhost fragt bei Ctrl-K S für ein neues Dokument ohne zugeordneten Dateipfad nun nach einem Zielnamen. Headless-Tests trennen die Entscheidung vom Console-Rendering, prüfen Abbruch durch `null`/leere Antwort und führen erstes sowie wiederholtes Speichern über den echten semantischen Dispatcher mit einem protokollierenden Codec aus. Ein Dokument mit bekanntem Pfad wird ohne erneute Abfrage gespeichert; die Pfadentscheidung bleibt dokumentbezogen. Dies ist eine **moderne Host-Bedienregel** mit .NET-Testbelegen, jedoch weder ein sprachneutraler Conformance-Fall noch eine bestätigte Nachbildung des ursprünglichen `EditCpFileSave`-Dialogs. Der historische Indexeintrag erhält deshalb noch **keinen** direkten portablen Testverweis.

## .NET-Terminalbeispiel: Bestätigung vor dem Überschreiben

Der Terminalhost verlangt jetzt ein ausdrückliches YES, bevor bei einem ersten Ctrl-K S Save As oder bei Ctrl-K W Write File eine vorhandene, neu ausgewählte Datei überschrieben wird. Ein Abbruch erzeugt keinen semantischen Schreibauftrag; ein bestätigtes direktes Write File lässt Dateizuordnung, Dirty und Undo weiterhin unverändert. Tests prüfen die reine Host-Entscheidung, echte temporäre Dateien und den semantischen Dispatcher. Normales Save auf einen bereits zugeordneten Pfad bleibt ohne neue Bestätigung. Diese Regel ist **moderne Host-Beleglage**, kein direkter sprachneutraler historischer Kompatibilitätsnachweis für `EditCpFileSave` oder originale DOS-Dialoge.

## V1-Abnahme – gezielte Belegprüfung (Oktober 2026)

Für diesen Freigabeschritt wurden alle 123 Inventarzeilen gegen die 14 Dateien des portablen Conformance-Manifests und den .NET-Testadapter abgeglichen. Vor den neuen Fällen enthielt das Repository **228 portable Fälle, 101 verknüpfte Routinen und 172 Verweise** (eine Routine kann mehrere Fälle referenzieren); sämtliche verknüpften Fälle hatten ein nichtleeres Erwartungsobjekt. Vier bisherige Verweise (`EditK` zweimal, `EditO`, `EditQ`) prüfen bewusst **nur die Tastenübersetzung**, nicht die Ausführung einer Host-Abfrage. Das ist für Keymap-Aufgaben sinnvoll, aber kein Beweis für DOS-Dialogparität.

Für zwei bisher unverknüpfte Verantwortlichkeiten gibt es nun neue ausführbare portable Testfolgen:

- **`EditCprfw` (gedruckte S. 151):** Ctrl-K R fordert einen Read-File-Pfad an. Eine abgebrochene Dateinamensabfrage löst weder einen Dateiimport noch eine Textänderung aus. Ein mit Leerzeichen umgebener Name wird bereinigt und durch den echten semantischen Dispatcher an einen Test-Decoder geleitet. Geprüft werden Einfügung nach der Ausgangszeile, unveränderter Cursor, Dirty und genau ein Undo-Schritt. **Nicht** geprüft: DOS-Darstellung und physische Dateidekodierung.
- **`EditCvts2i` (gedruckte S. 158):** Die moderne Grenze für numerische Host-Eingaben akzeptiert Leerzeichen um eine gültige einbasierte Go-To-Line-Zahl; 32-Bit-Überlauf beendet die Abfrage ohne Mutation. Die **internen Pascal-Konvertierungsregeln** werden damit nicht behauptet.

Die Repository-Verifikation verbietet nun Fälle mit leerem `expect`-Objekt. Damit können reine Ausführungen ohne Ergebnisprüfung die Belegzahl nicht erhöhen. Die historische Richtigkeit und Vollständigkeit der erwarteten Ergebnisse wird dadurch noch nicht bewiesen.

**Neuer prüfbarer Stand:** 232 portable Fälle, 103/123 Routinen mit konkreten Verweisen und 20 ohne portablen Direktbeleg (7 moderne Ersetzungen, 9 Host-/Beispielaufgaben, 1 dokumentierte Abweichung, 3 obsolet gewordene DOS-Routinen). **`EditCpFileSave` bleibt bewusst unverknüpft**, weil das moderne erstmalige Speichern und die Überschreibabfrage des Terminalhosts noch nicht auf exakte historische DOS-Dialogtreue geprüft wurden.

### Noch offene Freigabeprüfungen

- Handbuchbeschreibung, .NET-Verhalten und wichtige Randfälle aller **84 als `implemented` eingestuften Routinen** individuell prüfen. Ein konkreter Testverweis ist Regressionsschutz, kein umfassendes historisches Gleichwertigkeitszertifikat.
- Die **20 unverknüpften** Routinen sachlich bewerten. Obsolete Bildschirm-Hardware und reine UI-Aufgaben sollen keine künstlichen Core-APIs erzeugen.
- Echte Windows- und Linux-Terminal-Smoke-Tests durchführen: Steuerkombinationen, Prompt-Abbruch, Speichern/Lesen/Schreiben, Ctrl-U, verknüpfte Fenster und Größenänderungen. Headless-Tests beweisen keine reale Tastenausgabe.
- Öffentliche APIs, Persistenzverträge, Undo-/Anker-Invarianten und Release-Unterlagen final prüfen. Tag, Paket und Release nur nach ausdrücklicher Zustimmung.

**Dies ist ein Teil der V1-Abnahme und noch keine Freigabe.**

## Konkreter Handbuchbefund: EditCptabdef (gedruckte S. 155)

Bei der direkten Prüfung der Handbuchbeschreibung wurde eine tatsächliche Host-Lücke sichtbar, die reine Indexzahlen nicht zeigen: Eine bewusst leer bestätigte Tabulatorbreite ist **nicht** einfach ein abgebrochener Befehl. Der historische Wrapper leitet den Tabulatorabstand aus der aktuellen Cursor-Spalte minus eins ab. Bisher verwarf der Terminalhost eine leere Eingabe. Eine neue reine Host-Policy samt Headless-Tests prüft jetzt leeres Enter, tatsächlichen Abbruch, positive Eingaben, Überlauf, die Null-Grenze in der ersten Spalte und die Isolation der Fensteroptionen. Das einbasierte historische Verhalten wird auf die nullbasierte Cursorposition übertragen, ohne die allgemeine Zahlenauflösung anderer Befehle zu ändern. Die bewusste Abweichung zwischen historisch globalem und modern fensterbezogenem Tabulatorabstand bleibt dokumentiert. Diese gezielte Korrektur verbessert die echte historische Abnahme, ersetzt aber nicht den vollständigen Einzelvergleich.
