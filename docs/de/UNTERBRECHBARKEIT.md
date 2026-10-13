# Unterbrechbarkeit und Ctrl-U

## Erhaltenes historisches Verhalten

Das Handbuch legt drei relevante Verhaltensregeln fest:

1. physisches/Host-Ctrl-U wird sofort behandelt und nicht hinter ältere Typeahead-Eingaben gestellt;
2. der Abort löscht wartende Typeahead-Eingabe und setzt einen Abortzustand, den eingabeorientierte Routinen prüfen können;
3. längere Operationen können eine EditBreathe-artige Routine aufrufen, damit physische Eingabe auch während der Operation eingesammelt wird. Das kompatible Einlesen einer Datei wird ausdrücklich als während des Lesens abbrechbar beschrieben.

Die moderne Implementierung erhält diese Wirkungen, ohne BIOS-/DOS-Tastaturpuffer nachzubauen oder Plattform-APIs aus dem wiederverwendbaren Core zu pollen.

## Architektur

`EditorTypeaheadBuffer.EnqueueFromHost` bleibt die einzige Stelle, an der physisches Ctrl-U seine Sofortsemantik erhält. Wartender Typeahead wird gelöscht und `AbortRequested` gesetzt.

`EditorInterruptionService` koordiniert längere Operationen. Ein Checkpoint:

1. beachtet externe `CancellationToken`-Cancellation;
2. ruft `IEditorHooks.PollInputDuringOperationAsync` auf;
3. prüft Cancellation erneut;
4. konsumiert einen wartenden Editor-Abort genau einmal und wirft `EditorOperationAbortedException`.

Der semantische Dispatcher wandelt diese Exception in den typisierten Editorfehler `OperationAborted` um. Externe `OperationCanceledException` wird von diesem Pfad bewusst nicht abgefangen und bleibt Kontrollfluss des Aufrufers beziehungsweise der Plattform.

## Host-Grenze

Der Core ruft weder Console noch WPF-, WinForms- oder Browser-Eingabe-APIs auf. Ein Host kann `PollInputDuringOperationAsync` implementieren und normalisierte Eingabe über den Typeahead-Puffer der Session einspeisen.

Das FIRST-ED-Terminalbeispiel pollt höchstens eine physische Taste pro Checkpoint. Normale Tasten bleiben für spätere reguläre Befehlsverarbeitung in der Queue. Ctrl-U verwendet den unmittelbaren Abortpfad. Diese Begrenzung verhindert, dass aus einem Unterbrechungs-Checkpoint eine verschachtelte Befehlsschleife wird.

Trifft Ctrl-U im normalen Top-Level-Input-Pump statt während einer langen Operation ein, konsumiert dieser Pump nach der Meldung den einmaligen Abortzustand. So kann kein veraltetes Abort-Flag einen späteren, unabhängigen Befehl abbrechen.

## Asynchrone I/O

`RunInterruptibleAsync` ist für abbrechbare asynchrone I/O gedacht. Solange eine Operation aussteht, erhält der Host regelmäßig eine Polling-Gelegenheit. Bei Ctrl-U wird ein verknüpfter Token abgebrochen, die abgebrochene Operation beobachtet und anschließend der editorspezifische Abort weitergegeben.

Der eingebaute Kompatibilitäts-Codec beachtet Cancellation-Tokens. Eigene `IEditorFileCodec`-Implementierungen sollen dasselbe tun.

Read File bleibt bezüglich des Editorzustands atomar: Dekodierte Daten werden erst in das Dokument übernommen, wenn Lesen und letzter Checkpoint erfolgreich beendet sind. Write/Save können bei Cancellation naturgemäß auf externer Dateisystem-/Provider-Ebene teilweise sichtbar sein; Editor-Metadaten ändern sich erst nach erfolgreichem Abschluss.

## CPU-Schleifen und sichere Commit-Punkte

Suche und Reformatierung verwenden explizite Checkpoints statt eines timergetriebenen Hintergrundthreads.

Find/Find Again prüfen an logischen Zeilengrenzen und übernehmen Cursor beziehungsweise gemerkten Suchzustand erst nach beendetem Scan.

Reformat prüft beim Lesen, Komprimieren und Planen sowie unmittelbar vor der Mutation. Sobald der vorbereitete Plan in das Dokument geschrieben wird, gibt es keinen weiteren Abort-Checkpoint. Dadurch bleibt die strukturelle Änderung atomar und die normale Topologie-Reparatur kann vollständig abgeschlossen werden.

Kurze strukturelle Befehle wie Block-Splice-Operationen folgen demselben Prinzip: zuerst validieren, danach die begrenzte Mutation vollständig ausführen, statt mitten zwischen Zeilen-Inserts/-Deletes einen Abort zu akzeptieren.

## Cancellation-Semantik

Ctrl-U und externe Cancellation sind bewusst verschieden:

- **Ctrl-U** ist Editorsemantik und wird als `EditorErrorId.OperationAborted` gemeldet.
- **CancellationToken** ist Anwendungs-/Plattform-Kontrollfluss und propagiert als `OperationCanceledException`.

So entsteht kein zweites konkurrierendes Cancellation-System, während die historisch wichtige Sofort-Abort-Semantik für interaktive Kompatibilitäts-Hosts erhalten bleibt.

## Regressionstests

`InterruptionCompatibilityTests` prüft:

- Ctrl-U bricht bereits laufende Datei-I/O vor einer Editormutation ab;
- Reformat bleibt bei Abort während der Planung atomar;
- Find übernimmt bei Abort weder Cursor noch Suchzustand;
- gewöhnliche EditBreathe-artige Eingabe bleibt für später in der Queue;
- externe Cancellation wird nicht in einen Editor-Abort umgewandelt.

Das terminal-spezifische Polling physischer Tasten bleibt Aufgabe des Sample-Hosts; die wiederverwendbaren Tests prüfen denselben Vertrag deterministisch über Hooks.
