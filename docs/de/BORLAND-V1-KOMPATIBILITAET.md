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
| Physische gestapelte Fensterzeilen | `EditorWindowLayout`, `EditorWindowFrame` | Implementiert |
| `EditWindowCreate(Size, Win)` Split-/Komprimierungsregeln | Compatibility Processor + Layout | Implementiert |
| `EditWindowDelete(Wno)` Übernahme freier Zeilen | Compatibility Processor + Layout | Implementiert |
| Insert / Overtype | `EditorWindowOptions.InsertMode` | Implementiert |
| Word-Wrap | `EditorWindowOptions.WordWrap` | Implementiert |
| Auto-Indent | `EditorWindowOptions.AutoIndent` | Implementiert |
| Ränder / Tabulatorbreite | `EditorWindowOptions` | Implementiert |
| Cursorbewegung | `EditorEngine` plus Kompatibilitäts-Navigation | Implementiert |
| Historische Begin/End/Goto-Details | `FirstEdCompatibilityProcessor` | Implementiert |
| Up/Down-Line mit Viewport-Nachführung | Compatibility Processor + sichtbare Zeilenzahl | Implementiert |
| Scroll Up/Down mit Cursor-Randverhalten | `FirstEdCompatibilityProcessor` | Implementiert |
| Page Up/Down, dokumentierte Viewport-Verschiebung | Compatibility Processor, `visibleLines - 1` | Implementiert |
| Top/Bottom File inklusive Viewport-Platzierung | Compatibility Processor | Implementiert |
| Zeichen-/Wort-/Zeilenlöschen | `EditorEngine` | Implementiert |
| Groß-/Kleinschreibung, Zentrieren, Absatzformatierung | `EditorEngine` | Implementiert |
| Ganze-Zeilen-Blöcke | `EditorBlock`, `EditorSession` | Implementiert |
| Block kopieren/verschieben/löschen/verbergen | Engine/Session | Implementiert |
| Blockanfang/-ende anspringen | Compatibility Processor | Implementiert |
| Marker | Marker 1..20 | Implementiert |
| Undo inkl. Limit | `EditorUndoManager` + Command Binding | Implementiert (Snapshot-Backend) |
| Vorwärtssuche / gemerktes Find Again | `EditorSearchService` | Implementiert |
| Replace Next + Replace-Hook | `EditorSearchService`, `IEditorHooks` | Implementiert |
| Moderne UTF-8-Dokumentpersistenz | `ITextStorage`, `FileTextStorage` | Implementierte Grundlage |
| Historische Read-/Write-Command-Prozessoren | `EditorFileService` | Implementiert |
| High-Bit-CR-Konvention für Wrapped-Zeilen | `FirstEdLegacyFileCodec` | Implementiert |
| Host-Abfragevertrag | `EditorCommandArgumentKind` | Implementiert |
| Terminal-Abfragen | `ConsoleFirstEdPromptService` | Implementiertes Beispiel |
| Dirty-/Change-Flag | `EditorDocument.IsDirty` | Implementiert |
| Allgemeiner Command Dispatcher | `EditorCommandDispatcher` | Grundlage / wird erweitert |
| UI-unabhängige normalisierte Tasten | `EditorKeyStroke` | Implementiert |
| Console-Tastennormalisierung | `ConsoleKeyTranslator` | Implementiertes Beispiel |
| Ctrl-K/Ctrl-O/Ctrl-Q Prefix-Dispatcher | Prefix-Zustandsautomat in `FirstEdKeyMap` | Implementiert (Zuordnung) |
| FIRST-ED-/WordStar-kompatible Tasten | `FirstEdKeyMap`, `EditorCommandBinding` | Implementiert (Zuordnung) |
| Fenster hoch/runter/goto und Stream-Linking | Compatibility Processor + `EditorWindow.AttachDocument` | Implementiert |
| `EditExit` / `Rundown` | `EditorCommandId.Exit`, `EditorSession.RundownRequested` | Implementiert |
| Ctrl-K X Bestätigung | Host-Abfrage + semantisches `Exit` | Implementiertes Beispiel |
| `EditSchedule` mit Eingabepriorität | `EditorScheduler.RunCycleAsync`, `IEditorInputPump` | Implementiert |
| `EditSystem` bis Rundown | `EditorSystemLoop` | Implementiert |
| UserCommand-Idee | `IEditorHooks.FilterCommand` | Implementiert |
| UserError-Idee | `IEditorHooks.OnErrorAsync` | Implementiert |
| UserStatusLine-Idee | `IEditorHooks.TransformStatus` | Implementiert |
| UserReplace-Idee | `IEditorHooks.BeforeReplaceAsync` | Implementiert |
| UserTask-Idee | Hooks + `EditorScheduler` | Implementiert |
| Bildschirmprojektion | `EditorViewportBuilder` inkl. Build pro Fenster | Implementierte Grundlage |
| Gleichzeitiges gestapeltes Terminal-Rendering | `ConsoleFirstEdRenderer` + Layout Frames | Implementiertes Beispiel |
| Text-/Status-/Block-/Sonderattribute | Zeilenflags + Host-Renderer | Grundlage |
| Interaktiver FIRST-ED-Demohost | Terminalhost in `Sasd.Editor.FirstEd.Sample` | Implementiertes Beispiel |
| MicroStar-Menüs/Pop-ups | Host-Beispiele | Geplant |
| Hintergrunddruck | Scheduler-Beispiel | Geplant |
| Historischer Fehlertext-Katalog | typisierte Fehlercodes/Ressourcen | Geplant |
| DOS-/Videospeicher-Routinen | bewusst durch Host-Rendering ersetzt | Ersetzt |
| Overlays | auf modernen Plattformen nicht erforderlich | Entfällt |

## Cursor-Regel bei Page-Befehlen

Das Handbuch legt fest, dass Page-Bewegungen das Fenster um eine Zeile weniger als die Zahl sichtbarer Textzeilen verschieben. Es legt aber nicht separat fest, auf welcher Bildschirmzeile der Cursor danach stehen soll. SASD erhält deshalb nach Möglichkeit die relative sichtbare Cursorzeile und dokumentiert dies ausdrücklich als moderne Host-Regel statt als historische Aussage.

## Moderne Resize-Regel

Der historische Bildschirm hatte eine feste Größe. Die Größenänderung eines modernen Terminals ist deshalb keine historische Kompatibilitätsregel. Die .NET-Referenzimplementierung gibt zusätzliche Zeilen an das unterste Fenster und entzieht freie Zeilen von unten nach oben, ohne das Drei-Zeilen-Minimum zu unterschreiten. Diese Policy ist getrennt von der Kompatibilitätssemantik von `EditWindowCreate`/`EditWindowDelete` dokumentiert.

## V1-Abschlusskriterium

Die C#/.NET-Implementierung deckt jetzt die großen strukturellen FIRST-ED-Bereiche ausführbar ab, einschließlich gestapelter Fenstergeometrie und gleichzeitigem Multi-Window-Terminalhost. Vor V1 folgt ein Prozedur-für-Prozedur-Kompatibilitätsaudit, das Schließen verbleibender Verhaltens-/Testlücken sowie die Entscheidung, welcher Teil des historischen Fehlertext-Katalogs als typisierte Core-Ressourcen sinnvoll ist. MicroStar-spezifische Demonstrationen können als Samples statt als Core-Abhängigkeiten geliefert werden.
