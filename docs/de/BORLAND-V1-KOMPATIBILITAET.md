# Borland Turbo Editor Toolbox - V1-Kompatibilitätsmatrix

Status: **Implementiert**, **Grundlage**, **Geplant**.

Ziel ist funktionale Abdeckung, nicht eine identische Quellcode- oder API-Struktur.

| Historischer Bereich | SASD-V1-Abbildung | Status |
|---|---|---|
| Textzeilen / Text Streams | `ITextBuffer`, `EditorDocument` | Implementiert |
| Verkettete Zeilen | `LinkedLineTextBuffer` | Implementiert |
| Zeilenflags Block / Wrapped / Sonderfarbe | `EditorLineFlags` | Implementiert |
| Mehrere Fenster | `EditorSession`, `EditorWindow` | Implementiert |
| Verknüpfte Fenster auf denselben Text | mehrere Fenster auf einem `EditorDocument` | Implementiert |
| Insert / Overtype | `EditorWindowOptions.InsertMode` | Implementiert |
| Word-Wrap | `EditorWindowOptions.WordWrap` | Implementiert |
| Auto-Indent | `EditorWindowOptions.AutoIndent` | Implementiert |
| Ränder / Tabulatorbreite | `EditorWindowOptions` | Implementiert |
| Cursorbewegung | `EditorEngine` plus Kompatibilitäts-Navigation | Implementiert |
| Historische Begin/End/Goto-Details | `FirstEdCompatibilityProcessor` | Implementiert |
| Zeichen-/Wort-/Zeilenlöschen | `EditorEngine` | Implementiert |
| Groß-/Kleinschreibung, Zentrieren, Absatzformatierung | `EditorEngine` | Implementiert |
| Ganze-Zeilen-Blöcke | `EditorBlock`, `EditorSession` | Implementiert |
| Block kopieren/verschieben/löschen/verbergen | Engine/Session | Implementiert |
| Blockanfang/-ende anspringen | Compatibility Processor | Implementiert |
| Marker | Marker 1..20 | Implementiert |
| Undo inkl. Limit | `EditorUndoManager` + Command Binding | Implementiert (Snapshot-Backend) |
| Suchen/Ersetzen | `EditorSearchService` | Grundlage |
| Datei lesen/schreiben | `ITextStorage`, `FileTextStorage` | Grundlage |
| Dirty-/Change-Flag | `EditorDocument.IsDirty` | Implementiert |
| Allgemeiner Command Dispatcher | `EditorCommandDispatcher` | Grundlage / wird erweitert |
| UI-unabhängige normalisierte Tasten | `EditorKeyStroke` | Implementiert |
| Ctrl-K/Ctrl-O/Ctrl-Q Prefix-Dispatcher | Prefix-Zustandsautomat in `FirstEdKeyMap` | Implementiert (Zuordnung) |
| FIRST-ED-/WordStar-kompatible Tasten | `FirstEdKeyMap`, `EditorCommandBinding` | Implementiert (Zuordnung) |
| Abfrage-Metadaten für parametrisierte Befehle | `EditorCommandArgumentKind` | Implementiert |
| Fenster hoch/runter/goto und Stream-Linking | Compatibility Processor + `EditorWindow.AttachDocument` | Implementiert |
| UserCommand-Idee | `IEditorHooks.FilterCommand` | Implementiert |
| UserError-Idee | `IEditorHooks.OnErrorAsync` | Implementiert |
| UserStatusLine-Idee | `IEditorHooks.TransformStatus` | Implementiert |
| UserReplace-Idee | `IEditorHooks.BeforeReplaceAsync` | Implementiert |
| UserTask-Idee | Hooks + `EditorScheduler` | Implementiert |
| Bildschirm-Update | `EditorViewportBuilder`, Renderer im Host | Grundlage |
| FIRST-ED-Demo | zunächst Konsolenbeispiel | Grundlage |
| MicroStar-Menüs/Pop-ups | Host-Beispiele | Geplant |
| Hintergrunddruck | Scheduler-Beispiel | Geplant |
| Historischer Fehlertext-Katalog | typisierte Fehlercodes/Ressourcen | Geplant |
| DOS-/Videospeicher-Routinen | bewusst durch Host-Rendering ersetzt | Ersetzt |
| Overlays | auf modernen Plattformen nicht erforderlich | Entfällt |

Eingabemapping und ein großer Teil der historischen Command-Prozessoren sind jetzt getrennt testbar. V1 ist erst fertig, wenn insbesondere die verbleibenden Search-/File-/Lifecycle-Prozessoren, ein interaktiver FIRST-ED-artiger Host, exakte bildschirmabhängige Scroll-Semantik, weitere Kompatibilitätstests und Dokumentation abgeschlossen sind.
