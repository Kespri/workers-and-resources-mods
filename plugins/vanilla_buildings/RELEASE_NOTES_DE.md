# Vanilla Buildings 1.3

Stand: 7. September 2026. Ziel: WRSR 1.1.1.9, TesmioLoader API 4.

## Aenderungen

- Die INI wird zuerst unter plugins\vanilla_buildings.ini gesucht, sonst neben der DLL (Workshop-Paket unter Soviet Mod Loader oder Workshop Bridge). Die gewaehlte Datei steht im Log (Configuration file:).
- Patchlogik, Befehle und Grenzen gegenueber 1.2 unveraendert.
- Workshop-Paket My Plugins\vanilla_buildings mit Editor-Schema fuer Republic Mod Manager (ab 0.31.0), README_DE/EN nach Vorlage; die ausgelieferte INI hat beide Beispiel-Regelsaetze ausgeschaltet.

## Verifikation

- MSVC x64, keine Warnungen; Exporte TsmPluginApiVersion/TsmPluginInit/TsmPluginStart.
- Ingame-Test des INI-Fallbacks steht aus (ueber Abo oder Bridge laden, im Log auf Configuration file: achten).

---

# Vanilla Buildings 1.2

Stand: 1. September 2026. Ziel: WRSR 1.1.1.9, TesmioLoader API 4.

## Änderungen

- Der native Gebäudeleser wird jetzt über seinen tatsächlichen `fopen`-Aufruf erreicht. Hinzu kommen `fopen_s`, `_wfopen` und `_wfopen_s`; der bisherige Engine-Pufferleser bleibt unterstützt.
- Ziele unter `buildings_types`, `dlcN/buildings` und Workshop-IDs werden unterstützt. Workshopdaten werden in der Steam-Bibliothek des Spiels unter `steamapps/workshop/content/784150` gesucht.
- Mehrere `target = ...`-Zeilen pro Abschnitt teilen sich alle Änderungsregeln. Jede Zieldatei wird unabhängig gegen ihre unveränderte Quelldatei geprüft. Maximal 256 Zieldateien insgesamt.
- Bestehende einzelne `target`-Angaben bleiben gültig. Die optionalen Schlüssel `target1`, `target2`, `target3` prüfen zusätzlich die Zielart Vanilla, DLC bzw. Workshop.
- Jedes abgewiesene aktive Ziel bekommt eine genaue Warnung, auch bei `debug = 0`: Abschnitt, Ziel, Ursache sowie bei Regelfehlern Befehl und INI-Zeilennummer. Ein fehlgeschlagenes Ziel erhält keine Teiländerung.
- Der erste erfolgreiche Zugriff auf eine Ersatzdatei wird mit `[overlay-opened]` protokolliert. Vorbereitete Regeln und tatsächlich gelesene Dateien sind damit unterscheidbar.
- Originaldateien werden nicht verändert. Schreib- und Aktualisierungszugriffe werden nicht umgeleitet.

## Verifikation

- Release-DLL und Testprogramme mit MSVC x64 gebaut, ohne Compilerwarnungen.
- 98/98 Validierungs- und Dateitests erfolgreich, einschließlich sechs real installierter Vanilla-/DLC-/Workshopgebäude und unveränderter Quelldateien.
- Vier DLL-Lebenszyklustests erfolgreich: deaktiviert, aktiv mit allen vier CRT-Lesern, teilweise fehlgeschlagene Hook-Installation sowie fehlender obligatorischer `fopen`-Import.
- Die DLL-Tests prüfen auch genaue Warnungen für eine bereits vorhandene Zeile und eine fehlende Datei. Gültige Ziele derselben Gruppe funktionieren trotzdem.
- Die aktuell aktivierten Benutzerregeln wurden geprüft. Die Konfiguration erhält nur aktualisierte Kommentare; sämtliche Regeln und Schalter bleiben unverändert.
- Ingame-Nachweis steht noch aus. Dafür Spiel vollständig neu starten und im Log die ersten `[overlay-opened]`-Meldungen prüfen.

## Abgrenzung

Bestehende gespeicherte Gebäude werden noch nicht um fehlende Lager erweitert. Diese Migration ist ein separater nächster Schritt. Der manuell bearbeitete kleine technische Dienst bleibt in der bestehenden Benutzerkonfiguration deaktiviert; seine Originaldatei wurde nicht zurückgesetzt.

Das Paket enthält eine neutrale Beispiel-INI. Eine vorhandene Benutzer-INI nicht ungeprüft durch diese Vorlage ersetzen.
