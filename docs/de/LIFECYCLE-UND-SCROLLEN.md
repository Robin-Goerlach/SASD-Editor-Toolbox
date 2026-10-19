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

## Sehr große Host-Viewport-Höhen

Die FIRST-ED-Kompatibilitätsschicht berechnet die letzte sichtbare Zeile und Page-Ziele jetzt mit einem größeren Integer-Zwischentyp und begrenzt die Ergebnisse erst anschließend auf die tatsächlich letzte Dokumentzeile. Eine positive `PageSize` kann selbst bei wenigen Textzeilen nahe `int.MaxValue` liegen. `ScrollUp` erkennt weiterhin die letzte Dokumentzeile als unteren Sichtrand, und `PageDown` kann keinen negativen Offset durch Overflow erzeugen. Vier `ViewportNavigationOverflowTests` prüfen diese Fälle. Dies ist eine moderne defensive API-Regel, keine historische 16-Bit-Zeilenbegrenzung.

## Einzeilige Page-Bewegung und portable Tests

Ist nur eine Textzeile sichtbar, ergibt `visibleRows - 1` den Wert null. Page Up und Page Down verändern daher Cursor und oberste Zeile nicht; der Dispatcher kann den gültigen Befehl dennoch als verarbeitet melden. `conformance/v1/cases/viewport-navigation.json` prüft das gemeinsam mit regulären und extrem großen Viewport-Höhen sowie dem unabhängigen Zustand verknüpfter Fenster.

### Beenden und dynamische Registrierung von Hintergrundaufgaben

Der moderne kooperative Scheduler prüft Abbruch und `RundownRequested` vor der Idle-Phase, nach dem Idle-Hook und vor jeder Hintergrund-Teilaufgabe. Fordert ein Hook oder eine Aufgabe das Beenden des Editors an, entfallen die weiteren Teilaufgaben dieses Zyklus. Eine Momentaufnahme der Registrierung erlaubt das Hinzufügen oder Entfernen während der Ausführung; vor jedem Aufruf wird zusätzlich geprüft, ob die Aufgabe **noch registriert** ist. Eine inzwischen entfernte Aufgabe läuft deshalb nicht mehr, eine neu hinzugefügte frühestens im nächsten Idle-Zyklus. Die Eingabeverarbeitung bleibt vorrangig; jede Hintergrundaufgabe muss weiterhin kurz und kooperativ sein.

## Direkte Seitennavigation bei sehr großen Seitenhöhen

Ein moderner Host kann `EditorEngine.MovePageDown` beziehungsweise `MovePageUp` mit einer Seitenhöhe bis `int.MaxValue` aufrufen. Die Zielzeile wird deshalb vor dem Eingrenzen mit breiten Ganzzahlen berechnet. Ein riesiger positiver PageDown darf vom Dokumentinneren nicht durch einen Überlauf in der ersten Zeile landen. Tests prüfen beide Bewegungsrichtungen, die unabhängigen Cursor verknüpfter Ansichten sowie ausbleibende Text-/Undo-Änderungen. Die historischen semantischen Viewport-Regeln bleiben separat getestet.
