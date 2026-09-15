# FIRST-ED Lifecycle und Viewport-Navigation

## Umfang

Dieser Meilenstein überträgt das Lifecycle-/Scheduler-Modell sowie die von sichtbaren Bildschirmzeilen abhängigen Bewegungsbefehle aus dem Handbuch der Turbo Editor Toolbox. Die Umsetzung bleibt Host-unabhängig: Der Core fragt keine DOS-Tastaturhardware ab und schreibt nicht in Videospeicher.

## Exit und Rundown

Das Handbuch trennt zwei Konzepte:

- `EditExit` setzt die globale Variable `Rundown` auf true und speichert **keine** Dateien.
- `EditCpExit` fragt den Benutzer und ruft `EditExit` nur auf, wenn `YES` eingegeben wurde (Groß-/Kleinschreibung egal).

SASD bildet diese Verantwortlichkeiten getrennt ab:

- `EditorSession.RundownRequested` ist der moderne Rundown-Zustand.
- `EditorSession.RequestRundown()` entspricht dem direkten `EditExit`.
- `EditorCommandId.Exit` fordert Rundown an und speichert absichtlich keine Dokumente.
- `FirstEdKeyMap` kennzeichnet Ctrl-K X bereits mit `Confirmation`. Der interaktive Host stellt die Frage und dispatcht `Exit` erst nach einer positiven Antwort.

Damit bleibt UI-Interaktion außerhalb des wiederverwendbaren Core, während die historische Trennung zwischen Bestätigung und eigentlichem Exit erhalten bleibt.

## EditSchedule und EditSystem

Das Handbuch beschreibt `EditSchedule` so: Zuerst wird geprüft, ob Editor-Eingabe vorhanden ist. Ist Eingabe da, läuft der Input-Classifier; sonst wird der Hintergrundprozess ausgeführt. `EditSystem` ruft `EditSchedule` wiederholt auf, bis `Rundown` true ist.

Die C#-Abbildung lautet:

| Historisches Konzept | SASD-Implementierung |
|---|---|
| Typeahead/Eingabeverfügbarkeit | `IEditorInputPump.TryProcessInputAsync` |
| `EditSchedule` | `EditorScheduler.RunCycleAsync` |
| Hintergrund-/UserTask-Slice | `EditorScheduler.RunIdleCycleAsync`, `IEditorBackgroundTask`, `IEditorHooks.OnIdleAsync` |
| `Rundown` | `EditorSession.RundownRequested` |
| `EditSystem` | `EditorSystemLoop.RunAsync` |

Eingabe hat Priorität: Verarbeitet ein Scheduler-Zyklus Eingabe, läuft in diesem Zyklus keine Hintergrundarbeit. Hintergrundaufgaben bleiben kooperativ und müssen nach einer begrenzten Arbeitseinheit zurückkehren. Das entspricht der Forderung des Handbuchs, dass `UserTask` seinen Zustand behält und schnell die Kontrolle zurückgibt.

## Zeilenbewegung und Scrollen

Der Compatibility Processor verwendet für bildschirmabhängige Befehle jetzt die vom Host gelieferte Zahl sichtbarer Textzeilen (`EditorCommandRequest.PageSize`).

- `EditUpLine`: eine logische Zeile nach oben; steht der Cursor in der obersten sichtbaren Zeile, scrollt der Viewport mit.
- `EditDownLine`: eine logische Zeile nach unten; steht der Cursor in der letzten sichtbaren Zeile, scrollt der Viewport mit.
- `EditScrollUp`: Viewport eine Zeile nach oben; stand der Cursor in der letzten sichtbaren Zeile, bewegt er sich ebenfalls eine Zeile nach oben.
- `EditScrollDown`: Viewport eine Zeile nach unten; stand der Cursor in der obersten sichtbaren Zeile, bewegt er sich ebenfalls eine Zeile nach unten.
- `EditUpPage` / `EditDownPage`: der Viewport verschiebt sich um eine Zeile weniger als die Anzahl sichtbarer Textzeilen.

Das Handbuch definiert für die Page-Befehle ausdrücklich die Verschiebung des Fensters, nennt aber keine separate Regel dafür, auf welcher Bildschirmzeile der Cursor danach liegen soll. SASD verwendet deshalb eine ausdrücklich dokumentierte moderne Regel: Die relative sichtbare Cursorzeile bleibt nach Möglichkeit erhalten und wird nur an Dokumentgrenzen begrenzt. Diese Regel wird nicht als historische Tatsache ausgegeben.

## Anfang und Ende der Datei

`EditWindowTopFile` bedeutet erste Dokumentzeile, erste Spalte und oberste Viewport-Zeile null. `EditWindowBottomFile` springt in die letzte Dokumentzeile, erste Spalte und stellt diese letzte Zeile zugleich als oberste Viewport-Zeile dar, wie im Handbuch beschrieben.

## Noch offene Host-Arbeit

Die physische Erzeugung und Komprimierung von Fenstern bleibt Aufgabe eines Layout-fähigen Hosts, da die historische Routine direkt Bildschirmzeilen manipuliert. Außerdem muss das interaktive FIRST-ED-Beispiel noch eine reale Eingabequelle, Prompt-Schicht und einen Renderer mit den nun vorhandenen Core-Verträgen verbinden.
