# Kompatibilität für benutzerdefinierte Zeilenfarbe

## Historisches Verhalten und moderne Abbildung

Das Handbuch der Turbo Editor Toolbox 1.0 beschreibt `EditColorLine` (gedruckte S. 138) als Setzen des `Colored`-Bits in der aktuellen logischen Zeile und `EditColorFile` (S. 137) als Setzen dieses Bits in allen Zeilen des aktuellen Fenster-Textstroms. Bereits markierte Zeilen bleiben markiert. Die Routinen wechseln nicht von sich aus das Dokument, bewegen den Cursor nicht und ändern keinen Text. Historisch wählte das Flag beim Zeichnen die Benutzerfarbe statt der normalen oder der Blockfarbe.

Die UI-unabhängigen SASD-Gegenstücke sind `EditorSession.ColorCurrentLine()` und `EditorSession.ColorCurrentDocument()`. Beide ergänzen `EditorLineFlags.UserColored` per bitweisem OR und erhalten `InBlock` und `Wrapped`. Das Flag gehört zum `EditorDocument.Buffer`; alle verknüpften Fenster sehen dieselben Zeilenmetadaten. `EditorViewportBuilder` liefert die Flags an den Host, der die tatsächlichen Farben festlegt.

## Bewusste moderne Policy

Historisch belegt ist das **Setzen des Flags**. Die Beschreibung verlangt jedoch nicht, diese reine Anzeigeinformation in einer modernen Klartextdatei zu speichern oder einen Undo-Schritt anzulegen. In V1 sind diese Operationen idempotente Darstellungsannotationen. Sie verändern weder `IsDirty`, `Version`, das aktive Fenster, den Cursor noch die Undo-History. Der moderne UTF-8-Textcodec enthält deshalb keine historische Farbkodierung. Ein späteres Rich-Document-Format könnte solche Metadaten ausdrücklich separat speichern.

Die Hilfsmethoden besitzen **keine automatische Tastenzuordnung**. Hosts dürfen sie durch Menüs oder eigene Aktionen anbieten; der Core bleibt unabhängig von Bildschirm-APIs.

## Regressionstests

`LineColorCompatibilityTests` prüft Zeilengrenzen, Erhalt von Wrap-/Block-Bits, unveränderten Dirty-/Versions-/Undo-Zustand, Idempotenz über alle Zeilen, Sichtbarkeit in verknüpften Fenstern und Abgrenzung zu anderen Dokumenten.
