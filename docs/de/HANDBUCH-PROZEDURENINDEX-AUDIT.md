# V1-Audit des historischen Prozeduren- und Funktionsindexes

## Quelle und Einordnung

Das *Turbo Editor Toolbox Owner's Handbook* (1985) enthält auf den gedruckten Seiten **243–244** einen Prozeduren-/Funktionsindex mit **123** Einträgen. Die Einzelbeschreibungen stehen auf den gedruckten Seiten **118–240**. Eigenständig erfasste Namen und Seitenverweise liegen in [der maschinenlesbaren Inventarliste](../handbook-procedure-index-v1.json). Weder das historische Handbuch noch Pascal-Quellcode werden ins Repository übernommen.

Die Klassifikation beschreibt **beobachtbare Aufgaben**, keine 1:1-Abbildung historischer Pascal-APIs. Jeder Datensatz verweist auf die aktuelle SASD-Dokumentation. `implemented` bedeutet eine zunächst plausible konzeptionelle .NET-Zuordnung, **keinen** Nachweis vollständiger historischer Randfallparität. Dies ist **noch keine V1-Freigabe**.

## Ergebnis der ersten Zuordnung

| Klassifikation | Anzahl | Bedeutung |
|---|---:|---|
| Implemented | 83 | Dokumentierte Aufgabe in der .NET-Referenz abgebildet. |
| Modern replacement | 7 | Moderne Invariante/Abstraktion ersetzt historische interne Mechanik. |
| Host/sample concern | 27 | Darstellung, Prompts und Wrapper gehören nicht zum portablen Core. |
| Not applicable | 3 | DOS-/BIOS-/Videospeichermechanik ist nicht portabel. |
| Documented divergence | 1 | Bewusst dokumentierte moderne Abweichung. |
| Needs review | 2 | Vollständiger Verhaltensnachweis steht noch aus. |
| **Gesamt** | **123** | Ein Eintrag je gedruckter Funktionsbeschreibung. |

## Offene Prüfungen

- `EditGenlineno` (Handbuch S. 176): Verhalten und Regressionstests gegen die Originalbeschreibung prüfen.
- `EditHscroll` (Handbuch S. 179): Verhalten und Regressionstests gegen die Originalbeschreibung prüfen.

`EditDefineTab` ist eine dokumentierte Abweichung: historische globale Tabulatorbreite gegenüber SASD-Fensteroptionen. Für interaktive Kommandoprozessoren und Bildschirmfunktionen müssen die Host-Grenzen gesondert bewertet werden; dies ist keine Behauptung einer vollständigen DOS-Oberflächenkopie.

## Nächste Schritte und Abschlusskriterien

Jeden offenen Eintrag anhand seiner eigenen Handbuchbeschreibung prüfen, die bereits vorhandenen Tests abgleichen und nur bei echten Verhaltenslücken unabhängigen Code samt Regressionstests ergänzen. Auch die als implementiert eingestuften Zuordnungen brauchen einen Randfall-/Testabgleich; künstliche Pascal-APIs sind ausdrücklich unerwünscht. Inventarliste, deutsche/englische Audit-Dokumente, Matrix und nötigenfalls sprachneutralen Vertrag/Conformance-Fälle synchron halten.

Die Python-Vertragsprüfung sichert **123 eindeutige Namen**, die fortlaufenden gedruckten Seiten **118–240**, zulässige Statuswerte und existente Belegpfade. Das beweist die **Inventarvollständigkeit**, nicht die historische Verhaltensgleichheit. V1 bleibt offen, bis die weiteren fachlichen Prüfungen abgeschlossen sind.

