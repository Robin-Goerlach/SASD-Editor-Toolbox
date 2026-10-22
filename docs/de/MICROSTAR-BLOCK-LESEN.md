# MicroStar – Blockdatei einlesen

Die im Handbuch genannte MicroStar-Funktion **Read Block** verwendet nun
die bereits getestete Dateiarchitektur: denselben Codec, vollständige
Eingabevalidierung, Abbruchpunkte, Reparatur von Zeilenreferenzen und
**eine gemeinsame Undo-Transaktion**.

`ReadBlockIntoCurrentWindowAsync` markiert genau die neu eingefügten
logischen Zeilen als Block. Leere Eingabedateien lassen vorhandene
Markierungen unverändert; fehlerhafte Daten führen zu keiner Teiländerung.
Der bisherige normale `ReadIntoCurrentWindowAsync` bleibt unverändert.

Das F10-Menü bietet dafür den modernen SASD-Buchstaben `L`.
Er ist keine behauptete Rekonstruktion der historischen Menübelegung.
Neue Tests prüfen verknüpfte Fenster, Umbruchflags, Undo und Fehlerfälle.
