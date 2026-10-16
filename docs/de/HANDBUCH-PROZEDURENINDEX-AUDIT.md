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

Die Inventarliste unterstützt pro historischer Routine ein optionales Feld `conformanceCases`. Die Repository-Vertragsprüfung kontrolliert, ob jede referenzierte Fall-ID in einer durch das portable Manifest eingebundenen Datei vorkommt, weist doppelte oder unbekannte IDs zurück und meldet die Zahl verknüpfter Routinen. **Derzeit sind 28 von 123 Zeilen** mit konkreten portablen Fällen verknüpft. Für alle anderen bestehen zunächst nur konzeptionelle beziehungsweise dokumentierte Zuordnungen, deren Regressionstest-Abdeckung weiterhin einzeln geprüft werden muss.

Diese Verweise sind **gezielte Testbeispiele**, keine vollständigen historischen Verhaltensbeweise. Die Prüfung erzeugt bewusst keine erfundenen Tests für Einträge, die lediglich als `implemented` oder `modern-replacement` eingeordnet wurden.

Neun neue portable Befehlsfolgen liefern Nachweise für zehn weitere Routinen zu unabhängigen Blockgrenzen, Zeilen-Realignment, Markeridentität und transaktionalem Undo. Erwartete Befehlsablehnungen werden explizit statt als Editierung behandelt.

Die neuen Fälle in `viewport-navigation.json` liefern portable Regressionstest-Belege für acht weitere historische Navigationsroutinen: vertikale Cursorbewegungen, Scroll Up/Down, Page Up/Down und Dateigrenzen. Berücksichtigt werden verknüpfte Fenster, virtuelle Spalten, einzeilige Viewports und sehr große vom Host gelieferte Zeilenzahlen. Ihre Referenz-IDs werden anhand des Conformance-Manifests maschinell geprüft.

## Nächste Schritte und Abschlusskriterien

Jeden offenen Eintrag anhand seiner eigenen Handbuchbeschreibung prüfen, die bereits vorhandenen Tests abgleichen und nur bei echten Verhaltenslücken unabhängigen Code samt Regressionstests ergänzen. Auch die als implementiert eingestuften Zuordnungen brauchen einen Randfall-/Testabgleich; künstliche Pascal-APIs sind ausdrücklich unerwünscht. Inventarliste, deutsche/englische Audit-Dokumente, Matrix und nötigenfalls sprachneutralen Vertrag/Conformance-Fälle synchron halten.

Die Python-Vertragsprüfung sichert **123 eindeutige Namen**, die fortlaufenden gedruckten Seiten **118–240**, zulässige Statuswerte und existente Belegpfade. Das beweist die **Inventarvollständigkeit**, nicht die historische Verhaltensgleichheit. V1 bleibt bis zum Abschluss des übergreifenden Verhaltens- und Regressionstest-Audits offen.

