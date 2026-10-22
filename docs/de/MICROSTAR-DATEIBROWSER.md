# MicroStar – bedienbarer Dateibrowser für Read Block

Mit `MicroStarFileBrowser` sind Verzeichniskatalog und Auswahlliste
zu einer navigierbaren, **nur lesenden** Browserlogik verbunden.

Pfeiltasten, Bild-auf/ab und Pos1/Ende bewegen die Auswahl; `Enter`
öffnet ein gewähltes Unterverzeichnis oder liefert einen vollständig
aufgelösten Dateipfad zurück. `Backspace` wechselt zum übergeordneten
Verzeichnis. `Escape` oder `Strg+U` brechen ab.

Die Browserlogik enthält keine Konsolen- oder Editorbefehle. Erst der
aufrufende Host verwendet die ausgewählte Datei für die vorhandene
`ReadBlockIntoCurrentWindowAsync`-Operation. Der Browse-Vorgang selbst
liest keine Dateiinhalte und verändert weder Dokumente noch Undo.

Beim Wechsel in ein Verzeichnis wird zunächst die neue Liste vollständig
geladen; schlägt dies fehl, bleibt die vorige Auswahl erhalten. Die
Tastaturbedienung ist eine moderne SASD-Entscheidung. Integrationstests
mit temporären Verzeichnissen prüfen Navigation, Abbruch und Sicherheit.
