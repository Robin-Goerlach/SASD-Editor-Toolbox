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

## Prüfung historischer Routinen: Change Case und Delete Text Right (21. Oktober 2026)

In diesem Prüfschritt wurden die gedruckten Einzelbeschreibungen **direkt** mit dem .NET-Dispatcher, dem Editor-Kern und ausführbaren Tests verglichen, statt lediglich vorhandene Verweise zu zählen.

| Handbucheintrag | Gedruckte Seite | Geprüftes Verhalten und Tests | Grenze |
|---|---:|---|---|
| `EditChangeCase` | 135 | Der Buchstabe unter dem Cursor wird von Groß- zu Kleinschreibung oder umgekehrt geändert; die Cursorposition bleibt gleich. Nicht umschaltbare Zeichen erzeugen keine Änderung und kein Undo. Vier zusätzliche portable Fälle prüfen Unicode-Titelbuchstaben, ein CJK-Zeichen ohne Groß-/Kleinschreibung, ä → Ä und Ä → ä; zwei direkte .NET-Theoriefälle prüfen den No-op. | Das Handbuch von 1985 definiert keine Unicode-Regeln. Invariante Umwandlung je UTF-16-Codeeinheit ist eine **moderne Erweiterung**, keine behauptete DOS-Unicode-Semantik. |
| `EditDeleteTextRight` | 165 | Das Zeichen unter dem Cursor und der restliche Text **derselben Zeile** werden gelöscht. Neue portable Fälle belegen, dass die Folgezeile erhalten bleibt, `UserColored`-Flags bestehen bleiben und Undo den ursprünglichen Text samt Flags wiederherstellt. | Originale Videospeicherdarstellung, DOS-Speicherstruktur und noch ungeprüfte Host-Zusammenspiele sind mit diesen Fällen nicht abgenommen. |

**Tatsächlich korrigierter Fehler:** Zuvor behandelte die .NET-Fallumschaltung jedes *nicht großgeschriebene* Zeichen als potenziell klein geschrieben. Unicode-Titelbuchstaben gehören weder zur Kategorie Groß- noch Kleinbuchstaben. Der Code prüft nun ausdrücklich `char.IsLower`, bevor `ToUpperInvariant` aufgerufen wird. Nicht umschaltbare Zeichen bleiben unverändert. Ausführliche Kommentare begründen die Handbuchregel und die moderne Unicode-Grenze. Die Implementierung von Delete To End Of Line musste nicht verändert werden, erhielt aber aussagekräftigere portable Tests.

In diesem Einzelschritt wurden **zwei der 84 als implementiert eingestuften Verantwortlichkeiten** gezielt anhand des Handbuchs geprüft. Eine Freigabe der übrigen 82 wird dadurch nicht behauptet. Unverändert sind **103 von 123** Routinen mit portablen Tests verknüpft; sechs neue Fälle erhöhen die portable Sammlung von 232 auf **238 Fälle**. Reale Windows-/Linux-Terminaltests und die abschließende V1-Freigabeprüfung bleiben offen.

## V1-Handbuchabnahme: Wrapped beim Zeilenverbinden

Der direkte Handbuchvergleich der Routinen `EditDeleteLeftChar` (gedruckte S. 161), `EditDeleteRightChar` (S. 163), `EditDeleteRightWord` (S. 164) und `EditJoinLine` (S. 184) bestätigt: Eine Verbindung entfernt einen logischen Zeilendeskriptor samt dazwischenliegendem Übergang; Fenster-, Marker- und Undo-Konsistenz bleiben wichtig. Beim Vergleich mit dem **normativen V1-Modell für ausgehende Zeilenübergänge** zeigte sich ein konkreter Fehler: Der C#-Code behielt bisher das `Wrapped`-Flag der *oberen* statt der ehemaligen *unteren* Zeile. Dies konnte die vom historischen 8-Bit-Codec geschriebenen Trennzeichen verändern, obwohl der sichtbare Text korrekt verbunden war.

Die korrigierte Kompatibilitätshilfe übernimmt nun ausschließlich `Wrapped` von der unteren Zeile und behält die sonstigen Darstellungsflags der oberen Zeile bei. Sechs **neue** portable V1-Fälle prüfen Wechsel zwischen weichem und hartem Übergang, Rückwärts-/Vorwärts-/Wort-Verbindungen und Undo. Direkte .NET-Tests prüfen zusätzlich die tatsächlichen Legacy-Dateibytes und die Zustände verknüpfter Fenster. Damit liegt für das konkrete Persistenzrisiko ausführbare Evidenz vor.

Diese vier **bereits verknüpften** Handbucheinträge enthalten jetzt die zusätzlichen Test-IDs. Die Anzahl direkt verknüpfter Routinen bleibt somit **103/123**, während die portablen Fälle von **238 auf 244** steigen. Die vollständige Prüfung aller 84 implementierten Routinen, alle historischen DOS-Randfälle und die manuellen Windows-/Linux-Terminaltests werden dadurch nicht als erledigt ausgegeben.


## V1-Einzelprüfung: InsertLine, NewLine, DeleteLine

Das historische Handbuch beschreibt `EditInsertLine` (S. 183), `EditNewLine` (S. 191) und `EditDeleteLine` (S. 162). Dieser Abnahmeschritt hat die tatsächliche .NET-Kompatibilitätsimplementierung und ihre Tests mit diesen Beschreibungen sowie der normativen V1-`Wrapped`-Regel verglichen.

- **InsertLine:** Die drei dokumentierten Teilungspositionen (Spalte eins, innerhalb des Texts, nach dem letzten Nicht-Leerzeichen) arbeiten strukturell korrekt. Ein tatsächlicher Fehler bei der Flag-Zuordnung wurde gefunden: Das ursprüngliche ausgehende `Wrapped` blieb bisher nach der Teilung an der oberen statt hinter der neuen unteren Zeile. Die gemeinsame Split-Hilfe erzeugt jetzt einen harten oberen Übergang, erhält den bisherigen ausgehenden Übergang hinter der unteren Zeile und bewahrt das obere `UserColored`-Flag. Die zentrale Topologie aktualisiert weiterhin die Anker.
- **NewLine:** Im Insert-Modus wird dieselbe korrigierte Split-Funktion verwendet; der Cursor folgt der neuen unteren Zeile. Autoindent richtet sich weiterhin nach der Einrückung der bisherigen Zeile. Overtype bewegt sich ohne Split nach unten (außer am Dateiende) und entfernt weiterhin das bisherige ausgehende `Wrapped` als rückgängig machbare Zustandsänderung. Der allgemeine Editor-Kern bekommt keine zweite Flag-Logik.
- **DeleteLine:** Der historische Sonderfall bei nur einer vorhandenen Zeile erhält den Deskriptor und die Länge des bisherigen Zeichenbereichs und füllt diesen mit Leerzeichen. Zusätzliche direkte Tests prüfen das Entfernen des bisherigen `Wrapped`, den Erhalt von `UserColored`, ungültig werdende Marker, verknüpfte Fenster und Undo. Die bereits zentrale Mehrzeilen-Topologie bleibt unverändert.

Sieben neue portable Fälle in `core-smoke.json` sowie direkte .NET-Regressionsprüfungen für die Legacy-Dateibytes, verknüpfte Fenster und Undo sichern die konkreten Ergebnisse ab. Die 123 historischen Routinen bleiben klassifiziert, die Anzahl **bereits** direkt verknüpfter Routinen bleibt bei 103. Die portable Sammlung wächst durch sieben Fälle von **244 auf 251**. Die vollständige historische Pascal-Implementationsgleichheit wird damit nicht behauptet. Reale Windows-/Linux-Terminaltests und die weiteren Einzelprüfungen bleiben offen.


## Gezielte V1-Blockabnahme: Copy, Delete, Move (21. Oktober 2026)

Die Handbuchstellen S. 127 (`EditBlockCopy`), S. 128 (`EditBlockDelete`) und S. 131 (`EditBlockMove`) wurden mit der tatsächlichen Kompatibilitätsschicht, Topologie und Undo geprüft. Das historische Verbot, einen Block **in sich selbst** zu verschieben, bleibt vor jeder Mutation bestehen. Das Handbuch macht keine Aussage über Dirty/Version/Undo beim Verschieben **direkt vor die unmittelbar folgende Zeile**. SASD behandelt dies jetzt ausdrücklich als **erfolgreichen modernen No-op**, da der Block dabei in seiner ursprünglichen Position verbleibt. So entstehen keine künstlichen Änderungen oder Undo-Einträge.

Neue Headless-Tests prüfen sichtbare/verborgene benachbarte Moves, eine echte Verschiebung nach oben samt Neuverankerung einer zuvor entfernten TopLine, vollständige dokumentübergreifende Verschiebung (eine leere Quellzeile bleibt, Undo stellt beide Texte gemeinsam wieder her) und Copy innerhalb der eigenen Auswahl (die Einfügung gehört danach zum zusammenhängenden hervorgehobenen Block). Diese SASD-Regeln werden nicht als unbelegte Pascal-Interna ausgegeben.

Zwei portable Fälle in `block-topology-and-undo.json` unterscheiden No-op und tatsächliche Verschiebung und sind mit `EditBlockMove` verknüpft. **253 portable V1-Fälle**, **103/123 historische Routinen mit direkten Verweisen**. Echte Windows-/Linux-Terminaltests und die übrigen historischen Einzelprüfungen bleiben offen.


## V1-Prüfschritt: EditUndo und EditWindowLink nach Umkopplung

Der Vergleich von `EditUndo` und `EditWindowLink` aus dem Handbuch von 1985 mit der ausdrücklich modernen Snapshot-Undo-Policy von SASD deckte eine Lücke im erhaltenen **älteren Capture-API** auf: Die Cursorwiederherstellung wählte ein Fenster allein anhand der `WindowId` – selbst wenn dieses Fenster zwischenzeitlich einem anderen `DocumentId` zugeordnet worden war. Der Text wurde schon zuvor im richtigen Dokument restauriert, jedoch konnte ein unbeteiligtes Fenster die alte Cursorposition erhalten und unerwartet den Fokus übernehmen.

Der Fix setzt voraus, dass das ursprüngliche Fenster noch dasselbe Dokument zeigt. Ist es **umgekoppelt**, bleiben Cursor und spätere Fensterauswahl unangetastet; ist es **geschlossen**, greift weiterhin die bisherige Fallback-Regel für ein überlebendes verknüpftes Fenster. Moderne zusammengesetzte Undo-Transaktionen prüfen beide Identitäten bereits. Vier direkte Regressionstests prüfen diese Grenzen. Ein neuer portabler Fall `windows.undo-relinked-view-preserves-unrelated-current-window` bestätigt das moderne semantische Undo-/Fensterverhalten und ist beiden historischen Routinen zugeordnet.

Das portable Manifest umfasst jetzt **254 Fälle**; **103/123 Handbuchroutinen** haben weiterhin direkte Verweise (die neuen Fälle vertiefen bestehende Belege). Pascal-interne Undo-Stacks/Zeiger und echte Terminaltasten sind damit nicht freigegeben; die V1-Abnahme ist noch offen.


## V1-Abnahme: EditInsertCtrlChar (gedruckte S. 182)

Das Originalhandbuch legt zwei Schritte fest: ein Zeichen einlesen, es zum ASCII-Steuerzeichen übersetzen und dann diesen Code einfügen. Die direkte Codeprüfung zeigte eine tatsächliche Lücke **im FIRST-ED-Konsolenbeispiel**: Nach Ctrl-P wurde ein gewöhnliches A an den Kern weitergereicht; dadurch konnte der Buchstabe A statt 0x01 eingefügt werden. Die neue reine Host-Policy übersetzt die ASCII-Tasteneingabe vor dem semantischen Dispatch. Sie erkennt A-Z (beide Schreibweisen), die sechs ASCII-Steuer-Satzzeichen und bereits durch die Konsole übersetzte physische Steuerzeichen. Escape/Ctrl-U und nicht unterstützte Eingaben erzeugen keinen Befehl. Kommentare und Headless-Tests unterscheiden ein gewöhnliches U (0x15) vom echten Ctrl-U (Abbruch).

Der portable Fall `kernel.insert-control-character-writes-literal-control-into-line` belegt weiterhin die **semantische Einfügung eines fertigen Steuerzeichens** und ist bereits mit `EditInsertCtrlChar` verknüpft. Neue xUnit-Host-Tests prüfen ergänzend physische Tasteneingaben, ungültige Zeichen, Dispatcher und Undo. **Das sind Host-Tests, keine zusätzlichen portablen Conformance-Fälle.** Der Index verbleibt bei **103/123 direkt verknüpften Routinen** und **254 portablen Fällen**. Eine historische DOS-Scancode-/Typeahead-Prüfung oder vollständige Abnahme der Textabfrage EditAskfor ist damit nicht erfolgt.


## V1-Prüfung der Kommandozeilen-Hilfsroutinen: EditAppchar, EditAppcmdnam, EditAskfor, EditZapcmdnam

Der direkte Handbuchvergleich (gedruckte S. 120–122 und 237) zeigt eine **bewusste Host-Abweichung**: Das ursprüngliche `EditAskfor` konnte mit Ctrl-U frühere Varstring-Inhalte wiederherstellen und nahm bei voller Kommandozeile keine weitere Eingabe an. Das moderne Terminalbeispiel behandelt Ctrl-U dagegen als **Abbruch** und behält lange Antworten vollständig, während die Anzeige nach rechts scrollt. `EditAppcmdnam` erhöhte die globale Spalte Cmdcol und konnte die obere Statuszeile überschreiben; moderne Prompts verwenden eine reservierte letzte Zeile. `EditZapcmdnam` setzte Cmdcol zurück; jede neue Host-Abfrage erhält heute eine eigene Zeile. Statt des Pascal-`Defnocols`-Überlaufs in `EditAppchar` wird ein verwalteter String verwendet.

Ein tatsächlicher Fehler innerhalb dieser modernen Regeln wurde behoben: Manche Terminals melden Ctrl-U/Escape nur als rohe Bytes 0x15/0x1B ohne benannten ConsoleKey. Eine gemeinsame reine Host-Policy erkennt nun beides. Backspace und die schmale Anzeige trennen außerdem keine Unicode-Surrogatpaare oder kombinierenden Zeichenfolgen mehr. Gezielt ergänzte .NET-Tests prüfen diese Regeln ohne neue Terminal-Abhängigkeiten des Kerns.

Die Einzelheiten und offenen Punkte stehen in `docs/de/HOST-KOMMANDOZEILE-V1-AUDIT.md`. Alle vier Routinen bleiben **host-sample** ohne künstliche portable Belege; der Stand von **103/123 verknüpften Routinen und 254 portablen V1-Fällen** bleibt unverändert. Reale Terminaltests und ein möglicher separater klassischer Abrufmodus sind noch offen. DOS-Typeahead- und Bildschirmparität sind damit nicht abgenommen.


## V1-Sicherheit der Bildschirmausgabe: EditUpdphyscr / EditUpdwindow

Bei der gezielten Terminal-Abnahme wurde ein tatsächliches Sicherheitsproblem im .NET-FIRST-ED-Renderer entdeckt: Der gespeicherte Dokumenttext wurde einschließlich der historischen `EditInsertCtrlChar`-Steuerbytes direkt an `Console.Write` übergeben. Ein echtes ESC mit nachfolgendem `[2J` konnte deshalb als Terminalbefehl wirken. Die neue reine Hostklasse `ConsoleFirstEdRowProjection` zeigt C0-/DEL-/C1-Codes als ungefährliche Zeichen an, berücksichtigt beim Abschneiden ganze Unicode-Zeichenelemente, schätzt verbreitete breite Glyphen und berechnet die physische Cursorposition nach expandierten Steuerzeichen. **Gespeicherter Text, Core-Viewport-Vertrag und historische Dateicodecs bleiben unverändert.**

Die ursprünglichen Bildschirmspeicher-Routinen werden bewusst durch den modernen Renderer ersetzt, nicht durch neue BIOS-/Assembler-Schnittstellen nachgebaut. `EditUpdphyscr` bleibt `modern-replacement` (gedruckte S. 221), `EditUpdwindow` bleibt `host-sample` (S. 223). Aus Host-xUnit-Tests werden keine künstlichen portablen Routinenverweise abgeleitet. Der Index bleibt daher bei **103/123 verknüpften Routinen** und **254 portablen Fällen**. Reale Windows-/Linux-Prüfungen und Unsicherheiten zu Terminalzellenbreiten stehen in `docs/de/TERMINALAUSGABE-V1-ABNAHME.md` und sind **noch nicht freigegeben**.
