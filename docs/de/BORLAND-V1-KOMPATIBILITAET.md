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
| `EditWindowDeleteText` destruktives Zurücksetzen | `DeleteCurrentWindowText`, getrennte neue Leerdokumente | Implementiert |
| Insert / Overtype | `EditorWindowOptions.InsertMode` | Implementiert |
| Word-Wrap | `EditorWindowOptions.WordWrap` | Implementiert |
| Auto-Indent | `EditorWindowOptions.AutoIndent` | Implementiert |
| Linker/rechter Rand | `EditorWindowOptions` | Implementiert |
| Tabulatorbreite | pro Fenster `EditorWindowOptions.TabSize`; historischer globaler Scope separat dokumentiert | Grundlage / dokumentierte Abweichung |
| `EditTab` Insert-/Overtype-Verhalten | `FirstEdPrimitiveCompatibilityProcessor.Tab` | Implementiert |
| Cursorbewegung | `EditorEngine` plus Kompatibilitäts-Navigation | Implementiert |
| `EditLeftChar`: vorherige Zeile hinter letztem Nicht-Leerzeichen | Primitive Compatibility Processor | Implementiert |
| `EditRightChar`: nur Spalte / virtuelle Spalte | Primitive Compatibility Processor | Implementiert |
| `EditLeftWord`: Einrückung und vorheriges Wort | Primitive Compatibility Processor | Implementiert |
| `EditRightWord`: Alphanumerik/Interpunktion/Leerraum + Zeilenwechsel | Primitive Compatibility Processor | Implementiert |
| Historische Begin/End/Goto-Details | `FirstEdCompatibilityProcessor` | Implementiert |
| Up/Down-Line mit Viewport-Nachführung | Compatibility Processor + sichtbare Zeilenzahl | Implementiert |
| Scroll Up/Down mit Cursor-Randverhalten | `FirstEdCompatibilityProcessor` | Implementiert |
| Page Up/Down, dokumentierte Viewport-Verschiebung | Compatibility Processor, `visibleLines - 1` | Implementiert |
| Top/Bottom File inklusive Viewport-Platzierung | Compatibility Processor | Implementiert |
| Zeichen-/Wortlöschen | `EditorEngine` | Implementierte Grundlage; Detailaudit für Delete Word offen |
| `EditDeleteLine`: Ein-Zeilen-Regel, Marker-Ungültigkeit, Blockgrenzen | `FirstEdPrimitiveCompatibilityProcessor.DeleteLine` | Implementiert |
| `EditDelline` / `EditRealign`: Fenster-/Marker-/Block-Referenzen reparieren | `EditorLineTopology` | Grundlage / wird erweitert |
| Referenz-Realignment beim kompatiblen Datei-Insert | `EditorFileService` + `EditorLineTopology.LinesInserted` | Implementiert |
| Verbleibende Topologie für Newline/Wrap/Join/Block/Reformat | Topologie-Audit | Geplant |
| Groß-/Kleinschreibung, Zentrieren, Absatzformatierung | `EditorEngine` | Implementierte Grundlage; Helfer-Parität offen |
| `EditLongLine` / `EditShortLine` / `EditShiftLine` | Reformat-Kompatibilitätsaudit | Geplant |
| Ganze-Zeilen-Blöcke | `EditorBlock`, `EditorSession` | Implementiert |
| Block kopieren/verschieben/löschen/verbergen | Engine/Session | Implementierte Grundlage; Topologie-Audit offen |
| `EditOffblock`: InBlock global löschen, Grenzen behalten | `EditorSession.ClearBlockHighlights` | Implementiert |
| `EditMarkblock`: aktiven Bereich in Flags projizieren | `EditorSession.MarkBlockHighlights` | Implementiert |
| Blockanfang/-ende anspringen | Compatibility Processor | Implementiert |
| Marker 1..20 | `EditorMarker` | Implementiert |
| Marker bezeichnet Zeile; Sprung erhält Zielspalte | zeilenorientierter Marker / Session-Jump | Implementiert |
| Marker-Realignment in auditierten Insert-/Delete-Pfaden | `EditorLineTopology` | Implementierte Grundlage |
| Undo inkl. Limit | `EditorUndoManager` + Command Binding | Implementiert (Snapshot-Backend) |
| Undo-Bereinigung zerstörter Dokumente | `EditorUndoManager.DiscardDocument` | Implementiert |
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
| Historische Typeahead-Standardkapazität | `EditorTypeaheadBuffer.DefaultCapacity = 500` | Implementiert |
| `Pokechr`-artige Queue-Eingabe | `EditorTypeaheadBuffer.EnqueueFromHost` | Implementiert |
| `EditPushtbf` Front-Einfügen | `EditorTypeaheadBuffer.PushNext` | Implementiert |
| `EditUserpush` Sequenz-/Makro-Eingabe | `PushSequence`, `PushText` | Implementiert |
| Typeahead-Overflow leert wartende Eingabe | begrenzter Puffer + Write-Result | Implementiert |
| Sofortiges Host-Ctrl-U / `EditAbort` für den Puffer | Queue leeren + `AbortRequested` | Implementierte Grundlage |
| Ctrl-U-Polling in allen unterbrechbaren Langläufern | Abort-State + moderne Cancellation-Grenze | Geplanter Audit |
| Console-Tastennormalisierung | `ConsoleKeyTranslator` | Implementiertes Beispiel |
| Terminaleingabe durch editor-eigenen Typeahead | `ConsoleFirstEdHost` | Implementiertes Beispiel |
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

## Modernisierung der Wortklassen

Die drei Wortklassen des Handbuchs bleiben erhalten. Für modernen Unicode-Text behandelt die .NET-Implementierung Unicode-Buchstaben/-Ziffern als alphanumerisch, Unicode-Whitespace als Leerraum und alle übrigen Zeichen als Interpunktion. Für ASCII-Eingaben bleibt das historische Verhalten erhalten, ohne den wiederverwendbaren Kern auf ASCII zu begrenzen.

## Modernisierung der Zeilentopologie

Die historische Implementierung erhält stabile Zeilenidentität über Deskriptor-Pointer. SASD bildet dieselben beobachtbaren Beziehungen über `DocumentId`, logische Zeilennummern und den expliziten Realignment-Dienst `EditorLineTopology` ab. Pointeradressen, manuelles Splicing und das Freigeben von Deskriptoren gehören nicht zur portablen API.

Das erste .NET-Blockmodell speichert ein vollständiges Start-/End-Paar. Wird eine Blockgrenze gelöscht, wird deshalb der vollständige aktive Block samt Hervorhebung entfernt. Das ist eine konservative Abbildung des historischen Ergebnisses (eine Grenze wird undefiniert) und keine Behauptung, dass historisch beide Pointer auf `nil` gesetzt wurden.

## Low-Level-Kompatibilitätspolitik

Rohe Pascal-Pointer, Deskriptor-Freelists und 16-Bit-Integergrenzen sind Implementierungsmechanismen und keine portablen Anforderungen. Der V1-Audit erhält das beobachtbare Verhalten: welche Zeile, welches Fenster, welcher Block oder Marker ausgewählt bleibt, welcher Text verändert wird, was Undo kann und was der Host sieht. Wenn moderne Datenstrukturen eine historische Speicherverwaltungsroutine vollständig überflüssig machen, dokumentieren wir die Ersetzung statt künstlich eine Pointer-API zu erzeugen.

## Modernisierung des Typeahead-Puffers

Die Reihenfolge und die historische Standardgröße 500 bleiben erhalten. Rohe DOS-Bytes, Ringpuffer-Indizes und Scan-Codes gehören jedoch bewusst nicht zum portablen V1-Vertrag. Makro- und physische Eingaben werden zuerst in `EditorKeyStroke` normalisiert. Ein vom Host stammendes Ctrl-U nimmt den unmittelbaren Abort-Pfad; eine vorne eingeschobene Makro-Eingabe wird nicht stillschweigend zu physischer Tastatureingabe umgedeutet.

## Cursor-Regel bei Page-Befehlen

Das Handbuch legt fest, dass Page-Bewegungen das Fenster um eine Zeile weniger als die Zahl sichtbarer Textzeilen verschieben. Es legt aber nicht separat fest, auf welcher Bildschirmzeile der Cursor danach stehen soll. SASD erhält deshalb nach Möglichkeit die relative sichtbare Cursorzeile und dokumentiert dies ausdrücklich als moderne Host-Regel statt als historische Aussage.

## Moderne Resize-Regel

Der historische Bildschirm hatte eine feste Größe. Die Größenänderung eines modernen Terminals ist deshalb keine historische Kompatibilitätsregel. Die .NET-Referenzimplementierung gibt zusätzliche Zeilen an das unterste Fenster und entzieht freie Zeilen von unten nach oben, ohne das Drei-Zeilen-Minimum zu unterschreiten. Diese Policy ist getrennt von der Kompatibilitätssemantik von `EditWindowCreate`/`EditWindowDelete` dokumentiert.

## V1-Abschlusskriterium

Die C#/.NET-Implementierung deckt die großen strukturellen FIRST-ED-Bereiche ausführbar ab und besitzt jetzt eine zentrale Zeilentopologie-Grundlage für die auditierten Delete-/Read-Insert-Pfade. Vor V1 werden die verbleibenden strukturellen Mutationsstellen bei Bedarf durch den Topologievertrag geführt, danach schließen wir Lücken bei Reformat-Helfern, Parameterprüfung, Unterbrechbarkeit und historischen Fehlerressourcen und führen einen finalen Audit des Handbuch-Indexes durch. MicroStar-spezifische Demonstrationen können als Samples statt als Core-Abhängigkeiten geliefert werden.
