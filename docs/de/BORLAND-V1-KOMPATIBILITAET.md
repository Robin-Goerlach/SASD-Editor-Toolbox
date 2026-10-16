# Borland Turbo Editor Toolbox - V1-Kompatibilitätsmatrix

Status: **Implementiert**, **Grundlage**, **Geplant**.

Ziel ist funktionale Abdeckung, nicht eine identische Quellcode- oder API-Struktur.

| Historischer Bereich | SASD-V1-Abbildung | Status |
|---|---|---|
| Textzeilen / Text Streams | `ITextBuffer`, `EditorDocument` | Implementiert |
| Verkettete Zeilen | `LinkedLineTextBuffer` | Implementiert |
| Zeilenflags Block / Wrapped / Sonderfarbe | `EditorLineFlags` | Implementiert |
| `EditColorLine` / `EditColorFile`: Benutzerfarbe | `EditorSession.ColorCurrentLine` / `ColorCurrentDocument` | Implementiert; Darstellungs-Policy dokumentiert |
| `Wrapped` als weiche ausgehende Zeilengrenze | Zeilenflags + Engine/Reformat/Dateicodec | Implementiert |
| Mehrere Fenster | `EditorSession`, `EditorWindow` | Implementiert |
| Verknüpfte Fenster auf denselben Text | mehrere Fenster auf einem `EditorDocument` | Implementiert |
| Physische gestapelte Fensterzeilen | `EditorWindowLayout`, `EditorWindowFrame` | Implementiert |
| `EditWindowCreate(Size, Win)` Split-/Komprimierungsregeln | Compatibility Processor + Layout | Implementiert |
| `EditWindowDelete(Wno)` Übernahme freier Zeilen | Compatibility Processor + Layout | Implementiert |
| `EditWindowDeleteText` destruktives Zurücksetzen | `DeleteCurrentWindowText`, getrennte neue Leerdokumente | Implementiert |
| Insert / Overtype | `EditorWindowOptions.InsertMode` | Implementiert |
| Word-Wrap | `EditorWindowOptions.WordWrap`, Engine-Wrap | Implementierte Grundlage |
| Auto-Indent | `EditorWindowOptions.AutoIndent` | Implementiert |
| Linker/rechter Rand | `EditorWindowOptions` | Implementiert |
| Tabulatorbreite | pro Fenster `EditorWindowOptions.TabSize`; historischer globaler Scope separat dokumentiert | Grundlage / dokumentierte Abweichung |
| `EditTab` Insert-/Overtype-Verhalten | `FirstEdPrimitiveCompatibilityProcessor.Tab` | Implementiert |
| `EditInsertLine`: leer oben / leer unten / Split | Primitive Compatibility Processor + Topologie | Implementiert |
| `EditNewLine`: Insert-/Overtype-Verhalten | getrennter `NewLine`-Befehl + Compatibility Processor | Implementiert |
| Unterscheidung Return vs Ctrl-N | `FirstEdKeyMap`: `NewLine` vs `InsertLine` | Implementiert |
| New Line Autoindent und Reset des vorherigen `Wrapped`-Flags | Primitive Compatibility Processor | Implementiert |
| Cursorbewegung | `EditorEngine` plus Kompatibilitäts-Navigation | Implementiert |
| `EditLeftChar`: vorherige Zeile hinter letztem Nicht-Leerzeichen | Primitive Compatibility Processor | Implementiert |
| `EditRightChar`: nur Spalte / virtuelle Spalte | Primitive Compatibility Processor | Implementiert |
| Virtuelle Spalten überleben strukturelle Normalisierung | `EditorWindow.ClampCursor` erhält nichtnegative Spalte | Implementiert |
| `EditLeftWord`: Einrückung und vorheriges Wort | Primitive Compatibility Processor | Implementiert |
| `EditRightWord`: Alphanumerik/Interpunktion/Leerraum + Zeilenwechsel | Primitive Compatibility Processor | Implementiert |
| Historische Begin/End/Goto-Details | `FirstEdCompatibilityProcessor` | Implementiert |
| Up/Down-Line mit Viewport-Nachführung | Compatibility Processor + sichtbare Zeilenzahl | Implementiert |
| Scroll Up/Down mit Cursor-Randverhalten | `FirstEdCompatibilityProcessor` | Implementiert |
| `EditHscroll`: horizontale Sichtbarkeit aller Fenster-Cursor | `EditorWindow.EnsureCursorHorizontallyVisible` + `EditorSession.EnsureAllWindowCursorsHorizontallyVisible` | Implementiert, Breite vom Host |
| `EditGenlineno`: absolute Zeilennummern | indexiertes `EditorWindow.Cursor.Line` + `EditorViewportBuilder` | Moderne Ersetzung, O(1) |
| Page Up/Down, dokumentierte Viewport-Verschiebung | Compatibility Processor, `visibleLines - 1` | Implementiert |
| Top/Bottom File inklusive Viewport-Platzierung | Compatibility Processor | Implementiert |
| `EditDeleteLeftChar`: Zeichen + Join zur vorherigen Zeile | Primitive Compatibility Processor + Topologie | Implementiert; Virtual-Space-Regel separat dokumentiert |
| `EditDeleteRightChar`: cursorpositionierter Join hinter letztem Nicht-Leerzeichen | Primitive Compatibility Processor + Topologie | Implementiert |
| `EditDeleteRightWord`: drei Klassen, folgende Leerzeichen, cursorpositionierter Join | Primitive Compatibility Processor + Topologie | Implementiert |
| `EditDeleteLine`: Ein-Zeilen-Regel, Marker-Ungültigkeit, Blockgrenzen | `FirstEdPrimitiveCompatibilityProcessor.DeleteLine` | Implementiert |
| `EditDelline` / `EditRealign`: beobachtbare Fenster-/Marker-/Block-Referenzen reparieren | `EditorLineTopology` | Implementierte Grundlage; Einzelzeilen-, Reformat- und Block-Bulk-Pfade auditiert |
| Referenz-Realignment beim kompatiblen Datei-Insert | `EditorFileService` + `EditorLineTopology.LinesInserted` | Implementiert |
| Realignment bei generischem Newline / automatischem Wrap | `EditorEngine` + `EditorLineTopology.LinesInserted` | Implementiert |
| Referenz-Realignment bei Reformat-Vergrößerung/-Verkleinerung | Reformat Processor + `EditorLineTopology` | Implementiert |
| Groß-/Kleinschreibung / Zentrieren | `EditorEngine` | Implementierte Grundlage; ggf. Helfer-Parität separat |
| `EditCompressLine` / `EditShiftLine` | Normalisierung / Linksrandverschiebung im Reformat-Plan | Implementiert |
| `EditLongLine` / `EditShortLine` | Push-down / Pull-up im Reformat-Plan | Implementiert |
| `EditReformat`: Wrapped-Durchlauf / Randformatierung | `FirstEdReformatCompatibilityProcessor` | Implementiert |
| Reformat: zu langes Wort vor Mutation erkennen | Plan-Validierung | Implementiert |
| Ganzzeilen-Blockmodell | `EditorBlockState` + vollständige `EditorBlock`-Projektion | Implementiert |
| `EditBlockCopy` | `FirstEdBlockCompatibilityProcessor.CopyToCursor` + Topologie | Implementiert |
| `EditBlockMove` | Block Compatibility Processor + Topologie | Implementiert |
| `EditBlockMove`: Cursor im Quellblock ablehnen | Validierung im Compatibility Processor | Implementiert |
| `EditBlockDelete` | Block Compatibility Processor + Topologie | Implementiert |
| Fenster-/dokumentübergreifende Blockoperationen | Quellblock + aktuelles Zielfenster/-dokument | Implementiert |
| Zeilenmetadaten bei Block Copy/Move | `Wrapped`/`UserColored` erhalten; `InBlock` neu projizieren | Implementiert |
| Block Hide/Display | `EditorSession.ToggleBlockHidden` | Implementiert |
| Unabhängig undefinierte `Blockfrom`-/`Blockto`-Zustände | nullable `EditorBlockState.Start`-/`End`-Endpunkte | Implementiert |
| `EditOffblock`: InBlock global löschen, Grenzen behalten | `EditorSession.ClearBlockHighlights` | Implementiert |
| `EditMarkblock`: aktiven Bereich in Flags projizieren | `EditorSession.MarkBlockHighlights` | Implementiert |
| Blockanfang/-ende anspringen | Compatibility Processor | Implementiert |
| Marker 1..20 | `EditorMarker` | Implementiert |
| Marker bezeichnet Zeile; Sprung erhält Zielspalte | zeilenorientierter Marker / Session-Jump | Implementiert |
| Marker-Realignment in auditierten Insert-/Delete-Pfaden | `EditorLineTopology` | Implementiert |
| Stabile Anker bei Reformat-/Block-Bulk-Topologie | Compatibility Processor + Topologie | Implementiert |
| Marker-Policy für aus dem Quellblock verschobene Zeilen | Quell-Löschsemantik; Block am Ziel neu erzeugt | Dokumentierte moderne Policy |
| Undo inkl. Limit | `EditorUndoManager` + validierter Kompatibilitätsbefehl | Implementiert (transaktionales Snapshot-Backend; Kompatibilitätslimit >= 1) |
| Eine logische Mutation = ein Undo-History-Eintrag | Commit-Grenze von `EditorUndoTransaction` | Implementierte moderne Policy |
| Zusammengesetztes Undo bei dokumentübergreifendem Block-Move | eine Multi-Dokument-Transaktion stellt Quelle + Ziel gemeinsam wieder her | Implementiert |
| Wiederherstellung von Navigation/Ankern bei Undo | Post-State-aware Fenster-/Marker-/Block-/Current-Window-Restoration | Implementierte moderne Policy |
| Undo-Bereinigung zerstörter Dokumente | `EditorUndoManager.DiscardDocument` entfernt den vollständigen Compound-Eintrag | Implementiert |
| Vorwärtssuche / gemerktes Find Again | `EditorSearchService` | Implementiert |
| Replace Next + Replace-Hook | `EditorSearchService`, `IEditorHooks` | Implementiert |
| Moderne UTF-8-Dokumentpersistenz | `ITextStorage`, `FileTextStorage` | Implementierte Grundlage |
| Historische Read-/Write-Command-Prozessoren | `EditorFileService` | Implementiert |
| High-Bit-CR-Konvention für Wrapped-Zeilen | `FirstEdLegacyFileCodec` | Implementiert |
| Host-Abfragevertrag | `EditorCommandArgumentKind` | Implementiert |
| Terminal-Abfragen | `ConsoleFirstEdPromptService` | Implementiertes Beispiel |
| Dirty-/Change-Flag | `EditorDocument.IsDirty` | Implementiert |
| Allgemeiner Command Dispatcher | `EditorCommandDispatcher` | Implementierte Grundlage |
| Semantische Validierung an der Befehlsgrenze | `EditorCommandValidator` + typisierte Validierungskategorien | Implementiert |
| UI-unabhängige normalisierte Tasten | `EditorKeyStroke` | Implementiert |
| Historische Typeahead-Standardkapazität | `EditorTypeaheadBuffer.DefaultCapacity = 500` | Implementiert |
| `Pokechr`-artige Queue-Eingabe | `EditorTypeaheadBuffer.EnqueueFromHost` | Implementiert |
| `EditPushtbf` Front-Einfügen | `EditorTypeaheadBuffer.PushNext` | Implementiert |
| `EditUserpush` Sequenz-/Makro-Eingabe | `PushSequence`, `PushText` | Implementiert |
| Typeahead-Overflow leert wartende Eingabe | begrenzter Puffer + Write-Result | Implementiert |
| Sofortiges Host-Ctrl-U / `EditAbort` für den Puffer | Queue leeren + `AbortRequested` | Implementiert |
| `EditBreathe`-artiges begrenztes Host-Polling | `IEditorHooks.PollInputDuringOperationAsync` + `EditorInterruptionService.CheckpointAsync` | Implementiert |
| Ctrl-U in unterbrechbaren Langläufern | Datei-I/O-Wrapper + Such-/Reformat-Checkpoints + typisiertes `OperationAborted` | Implementiert / auditiert |
| Externe Cancellation bleibt von Ctrl-U getrennt | `CancellationToken` propagiert; kein Editor-Abortfehler | Implementiert |
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
| Historischer Fehlertext-Katalog | bewusst nicht kopiert; modernes `EditorErrorId` + stabile Codes/Ressourcenschlüssel ersetzen präsentationsgebundene Texte | Ersetzt / Implementiert |
| Typisiertes hostneutrales Fehler-/Ressourcenmodell | `EditorErrorCatalog`, `IEditorHooks.OnErrorAsync` | Implementiert |
| DOS-/Videospeicher-Routinen | bewusst durch Host-Rendering ersetzt | Ersetzt |
| Overlays | auf modernen Plattformen nicht erforderlich | Entfällt |

## Modernisierung der Wortklassen

Die drei Wortklassen des Handbuchs bleiben erhalten. Für modernen Unicode-Text behandelt die .NET-Implementierung Unicode-Buchstaben/-Ziffern als alphanumerisch, Unicode-Whitespace als Leerraum und alle übrigen Zeichen als Interpunktion. Für ASCII-Eingaben bleibt das historische Verhalten erhalten, ohne den wiederverwendbaren Kern auf ASCII zu begrenzen. Derselbe Klassifikator wird in den auditierten Pfaden für Rechts-Wortbewegung und Rechts-Wortlöschen verwendet.

## Policy für Wrapped-Grenzen

`EditorLineFlags.Wrapped` bezeichnet die weiche Grenze nach der markierten logischen Zeile. Diese Policy wird gemeinsam von expliziter Neuformatierung, automatischem Wrap und dem Legacy-Codec mit High-Bit-Carriage-Return verwendet. Ein explizites New Line löscht das `Wrapped`-Bit der zuvor aktuellen Zeile und erzeugt damit eine harte Absatzgrenze.

Die historische Beschreibung von `EditCompressLine` spricht ausdrücklich von Spaces. Der .NET-Reformat-Planer akzeptiert zusätzlich Unicode-Whitespace und erhält für ASCII dasselbe Verhalten; dies ist eine moderne Erweiterung und keine Aussage über den historischen Zeichensatz.

## Modernisierung der Zeilentopologie

Die historische Implementierung erhält stabile Zeilenidentität über Deskriptor-Pointer. SASD bildet dieselben beobachtbaren Beziehungen über `DocumentId`, logische Zeilennummern und den expliziten Realignment-Dienst `EditorLineTopology` ab. Pointeradressen, manuelles Splicing und das Freigeben von Deskriptoren gehören nicht zur portablen API.

Die auditierten Pfade für einzelne Zeileneinfügungen, New Line, automatischen Wrap, Datei-Insert, Löschen/Join, Absatz-Neuformatierung und mehrzeilige Blockoperationen Copy/Move/Delete melden Topologieänderungen jetzt zentral. Die frühere Block-Bulk-Topologielücke ist geschlossen.

Der .NET-Blockzustand speichert unabhängig nullable Begin-/End-Endpunkte und stellt einen vollständigen `EditorBlock` nur bereit, wenn beide Grenzen einen geordneten Bereich in einem Dokument bilden. Strukturelle Einfügungen realignen beide Endpunkte unabhängig; wird eine Grenzzeile gelöscht, wird nur dieser Endpunkt undefiniert und die Hervorhebung bleibt aus, bis wieder ein vollständiges Paar existiert.

Für Block Move legt das Handbuch nicht separat fest, ob Marker oder andere Anker innerhalb des verschobenen Quellbereichs den physischen Zeilen an den neuen Ort folgen. Die .NET-Implementierung wendet auf solche Quellanker normale Löschsemantik an und erzeugt nur den logischen Block am Ziel neu. Das ist dokumentierte moderne Policy und keine historische Behauptung.

## Policy für virtuelle Spalten

FIRST-ED erlaubt ausdrücklich, den Cursor hinter die physische Pufferlänge einer Zeile zu bewegen. Nichtnegative virtuelle Spalten sind daher gültiger Editorzustand. `EditorWindow.ClampCursor` normalisiert nach strukturellen Änderungen Zeile und Viewport, erhält aber bewusst die Spalte. Für Backspace im nicht gespeicherten virtuellen Bereich ist das Handbuch nicht eindeutig; die .NET-V1-Implementierung verwendet dort reine Cursorbewegung und dokumentiert dies als moderne Policy.

## Low-Level-Kompatibilitätspolitik

Rohe Pascal-Pointer, Deskriptor-Freelists und 16-Bit-Integergrenzen sind Implementierungsmechanismen und keine portablen Anforderungen. Der V1-Audit erhält das beobachtbare Verhalten: welche Zeile, welches Fenster, welcher Block oder Marker ausgewählt bleibt, welcher Text verändert wird, was Undo kann und was der Host sieht. Wenn moderne Datenstrukturen eine historische Speicherverwaltungsroutine vollständig überflüssig machen, dokumentieren wir die Ersetzung statt künstlich eine Pointer-API zu erzeugen.

## Modernisierung des Typeahead-Puffers

Die Reihenfolge und die historische Standardgröße 500 bleiben erhalten. Rohe DOS-Bytes, Ringpuffer-Indizes und Scan-Codes gehören jedoch bewusst nicht zum portablen V1-Vertrag. Makro- und physische Eingaben werden zuerst in `EditorKeyStroke` normalisiert. Ein vom Host stammendes Ctrl-U nimmt den unmittelbaren Abort-Pfad; eine vorne eingeschobene Makro-Eingabe wird nicht stillschweigend zu physischer Tastatureingabe umgedeutet.

## Modernisierung der Unterbrechbarkeit

Das Handbuch beschreibt `EditAbort` ausdrücklich als sofortige Ctrl-U-Behandlung und `EditBreathe` als Tastatur-Polling aus längeren Operationen; das kompatible Einlesen einer Datei ist ausdrücklich während des Lesens abbrechbar. Die .NET-Implementierung erhält dieses Verhalten, ohne Console-APIs in den Core zu ziehen: Ein Host-Hook pollt begrenzt physische Eingabe, `EditorInterruptionService` konsumiert den einmaligen Abortzustand und unterbrechbare asynchrone Datei-I/O erhält einen verknüpften Cancellation-Token.

Suche und Reformatierung verwenden zusätzlich sichere Checkpoints als moderne Erweiterung für gute Reaktionsfähigkeit. Zustand wird erst nach erfolgreichem Scan beziehungsweise nach erfolgreicher Planungsphase übernommen. Kurze strukturelle Commit-Phasen akzeptieren bewusst kein Ctrl-U mitten in der Mutation, weil ein halb gesplictes Dokument unsicherer wäre als der Abschluss der begrenzten In-Memory-Änderung.

Externe `CancellationToken`-Cancellation wird nicht als Ctrl-U gemeldet und bleibt Kontrollfluss des Aufrufers.

## Cursor-Regel bei Page-Befehlen

Das Handbuch legt fest, dass Page-Bewegungen das Fenster um eine Zeile weniger als die Zahl sichtbarer Textzeilen verschieben. Es legt aber nicht separat fest, auf welcher Bildschirmzeile der Cursor danach stehen soll. SASD erhält deshalb nach Möglichkeit die relative sichtbare Cursorzeile und dokumentiert dies ausdrücklich als moderne Host-Regel statt als historische Aussage.

## Moderne Resize-Regel

Der historische Bildschirm hatte eine feste Größe. Die Größenänderung eines modernen Terminals ist deshalb keine historische Kompatibilitätsregel. Die .NET-Referenzimplementierung gibt zusätzliche Zeilen an das unterste Fenster und entzieht freie Zeilen von unten nach oben, ohne das Drei-Zeilen-Minimum zu unterschreiten. Diese Policy ist getrennt von der Kompatibilitätssemantik von `EditWindowCreate`/`EditWindowDelete` dokumentiert.

## Portable Tests für mehrstufige Befehle

Die portablen Fälle decken inzwischen Block-Begin/-End über mehrere Befehle, Markeridentität bei Zeilentopologie-Änderungen, Block-Kopieren/Verschieben/Löschen und atomaren dokumentübergreifenden Undo ab. Die Fälle stehen in `conformance/v1/cases/block-topology-and-undo.json` und sind den historischen Routinen einzeln zugeordnet. Dies ist gezielte Verhaltensevidenz, kein vollständiger Beweis für Pascal-Implementierungsparität.

## Portable Belege für die vertikale Navigation

`conformance/v1/cases/viewport-navigation.json` prüft acht historische Cursor-, Scroll-, Page- und Dateigrenz-Routinen. Die Tests sichern minimale Randbewegung, die dokumentierte Regel zum relativen Cursor-Viewport-Abstand, unabhängige verknüpfte Fenster und Integer-Grenzen ab. Ein Viewport mit nur einer Textzeile besitzt **keine Page-Verschiebung**; der Dispatcher kann den gültigen Navigationsauftrag dennoch als verarbeitet melden. Die Fälle bleiben für eigenständige C++- und Java-Adapter nutzbar.

## Portable Tests für Suchen und Ersetzen

`conformance/v1/cases/search-and-replace.json` prüft die historische Wiederholung bei leerem Suchprompt, die Ablehnung eines fehlenden Parameters, Find Again, Ersetzen durch leeren Text sowie Undo. Damit liegen zusätzliche sprachneutrale Belege für `EditFind` und `EditReplace` vor, ohne interne Details des C#-Suchdienstes festzuschreiben.

## Portable Belege für die Dateieinfügung

`conformance/v1/cases/file-read-topology.json` testet die sprachneutrale Read-File-Semantik mit bereits dekodierten logischen Zeilen. Die Fälle prüfen Einfügeposition, Undo, unabhängige Dokumente, Metadaten und Zeilen-Realignment. Dateisystem und Bytes werden absichtlich ausgeklammert; die historische High-Bit-CR-Dekodierung besitzt eigene .NET-Codec-Tests.

## V1-Abschlusskriterium

Die C#/.NET-Implementierung deckt die großen strukturellen FIRST-ED-Bereiche inzwischen ausführbar ab, einschließlich Absatz-Neuformatierung, mehrzeiliger Block-Bulk-Topologie, unabhängiger Block-Endpunkte und atomarer Multi-Dokument-Undo-/Navigationswiederherstellung. Der [Handbuch-Prozeduren-/Funktionsindex-Audit](HANDBUCH-PROZEDURENINDEX-AUDIT.md) erfasst inzwischen alle 123 gedruckten Einträge mit ersten Klassifikationen; die zugeordneten Routinen benötigen vor V1 noch einen abschließenden Testabdeckungs- und Host-Audit. MicroStar-spezifische Demonstrationen können als Samples statt als Core-Abhängigkeiten geliefert werden.
