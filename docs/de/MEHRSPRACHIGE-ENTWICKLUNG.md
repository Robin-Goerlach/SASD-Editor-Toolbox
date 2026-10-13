# Architektur für mehrsprachige Entwicklung

## Ziel

Die SASD Editor Toolbox ist ein gemeinsamer Produktvertrag mit mehreren unabhängigen nativen Implementierungen. Das Repository soll C#/.NET, C++, Java und spätere Sprachen unterstützen, ohne eine Sprache zu einem Wrapper um eine andere zu machen.

Die zentrale Regel lautet:

> **Verhalten teilen, nicht Implementierung.**

Die .NET-Implementierung ist derzeit die vollständigste ausführbare Referenz. Sie hilft beim Verständnis von Absicht und Randfällen; normativ bleibt jedoch der sprachneutrale Vertrag unter `spec/`.

## Implementierungsbereiche

Jede Sprache verwendet dieselben drei gleichberechtigten Wurzeln:

```text
src/<sprache>/      wiederverwendbarer Produktionscode
tests/<sprache>/    native Unit-/Integrations-/Conformance-Adapter
samples/<sprache>/  Host- und Referenzanwendungen
```

Die aktuellen Sprachkennungen sind `dotnet`, `cpp` und `java`. Weitere Sprachen werden ergänzt, indem alle drei passenden Wurzeln angelegt und die Implementierung in `implementations.json` registriert wird.

Jede Sprachspur besitzt ihre eigene Paket-/Modulstruktur und ihr eigenes Build-System. Bewusst gibt es kein einziges universelles Build-Werkzeug für .NET, C++ und Java.

## Gemeinsam genutzte Artefakte

Für alle Implementierungen gemeinsam sind `spec/editor-v1.md`, die Fälle unter `conformance/`, die Kompatibilitäts- und Architekturdokumentation unter `docs/`, die Clean-Room-/Rechtsregeln in `AGENTS.md` sowie die gemeinsamen Fachbegriffe Dokument, Fenster/View, Cursor, Marker, Block, Wrapped-Grenze, Typeahead, Command und Viewport.

Diese gemeinsamen Artefakte beschreiben Semantik und Invarianten, nicht die Klassenhierarchie einer Sprache.

## Sprachspezifisch bleibt

Jede Implementierung darf idiomatisch über Namespace-/Package-Konventionen, Ownership und Lebensdauer, Value-/Reference-Typen, Collections und String-Typen, Async-/Cancellation-Mechanismen, Fehler-/Result-Darstellung, Build-/Testwerkzeuge, öffentliche API-Namen und interne Buffer-/Indexierungsstrukturen entscheiden.

C++ muss weder .NET-Garbage-Collection noch Records nachbilden. Java muss keine C#-Events, Structs oder Nullable-Syntax kopieren. C# muss keine native Pointer-Ownership imitieren. Entscheidend ist das beobachtbare Editor-Ergebnis.

## Synchronisationsablauf

Bei einer portablen semantischen Änderung:

1. Verhalten in `spec/` festlegen oder präzisieren;
2. wenn portabel darstellbar, einen gemeinsamen Conformance-Fall hinzufügen oder anpassen;
3. englische und deutsche Kompatibilitäts-/Architekturdokumentation aktualisieren;
4. alle aktiv gepflegten Implementierungsbereiche anpassen;
5. Repository-Vertragsprüfung und native Tests jeder betroffenen Sprache ausführen;
6. bewusste temporäre Lücken dokumentieren statt den gemeinsamen Vertrag abzuschwächen.

So wird verhindert, dass die am weitesten entwickelte Sprache stillschweigend zur Spezifikation wird.

## Conformance-Adapter

Ein sprachspezifischer Conformance-Adapter gehört unter `tests/<sprache>/`. Er darf die gemeinsamen JSON-Fälle in native Testobjekte übersetzen, aber deren Bedeutung nicht verändern.

Erwartete Zustände sind bewusst partiell. Ein Runner prüft nur die unter `expect` angegebenen Felder und darf zusätzliche native Diagnose- oder interne Zustände ignorieren.

Das .NET-Testprojekt enthält den ersten Adapter und führt die gemeinsamen V1-Smoke-Fälle in der normalen CI aus. Der Adapter ist bewusst klein und bildet die konzeptionellen Operation-IDs auf den vorhandenen semantischen `EditorCommandDispatcher` ab; er ist ein Muster für weitere Sprachen und keine vorgeschriebene API-Struktur.

## CI-Strategie

Repository-Verifikation und Sprach-Builds sind getrennte Aufgaben:

- `repository-contract.yml` prüft Struktur und gemeinsame JSON-Metadaten.
- `dotnet.yml` baut und testet die .NET-Spur.
- C++ und Java erhalten eigene Workflows, sobald die jeweilige Implementierung beginnt.

Ein Fehler in einem nativen Build darf keine andere Sprache als Build-Abhängigkeit erzwingen. Sprachübergreifende Parität entsteht durch Vertrag und Conformance-Fälle, nicht durch binäres Cross-Linking.

## Versionierung und Releases

Die V1-**Vertragsversion** ist gemeinsam. Paket-/Bibliotheksversionen dürfen zunächst unabhängig fortschreiten, während Ports aufholen. Eine Sprache sollte V1-Konformität erst behaupten, wenn sie den erforderlichen V1-Conformance-Satz und ihre nativen Regressionstests besteht.

Führt ein späterer V2-Vertrag inkompatible Semantik ein, bleiben die V1-Fälle erhalten, damit ältere Paketlinien ihren Vertrag weiter prüfen können.

## Reihenfolge beim Portieren

Eine neue Sprache startet mit dem kleinsten Architekturskelett, das gemeinsame Conformance-Fälle ausführen kann: Textmodell, Dokument/View-Trennung, Cursor-Grundoperationen und Topologie. Danach folgen Kompatibilitätsprozessoren, Storage, Undo, Suche, Scheduling und Hosts.

Nicht damit beginnen, jede C#-Datei mechanisch zu übersetzen. Die Architektur soll mit den Stärken der Zielsprache neu umgesetzt werden, während der gemeinsame Vertrag erhalten bleibt.
