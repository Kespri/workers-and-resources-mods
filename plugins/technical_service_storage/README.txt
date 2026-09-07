Technical Service Storage Plugin 0.3.1-beta
==========================================

DLL-Version: 0.3.1-beta
Plugin und Streugutkomponente verwenden dieselbe Versionsnummer.
0.3.1 liest die INI aus dem Ordner der DLL, wenn plugins\technical_service_storage.ini
fehlt (Workshop-Paket unter Soviet Mod Loader oder Workshop Bridge); der gewaehlte
Pfad steht als "Configuration file:" im Protokoll. Funktionen, Einstellungen und
Speicherformate sind unveraendert.

Materialabschnitt: [grit_materials]. Der bisherige Name [Streumaterialien]
funktioniert weiterhin. Nur einen der Namen verwenden; sonst gewinnt der
erste Abschnitt in Dateireihenfolge mit einer Warnung fuer den zweiten.
Werte, Reihenfolge, Berechnungen und gespeicherte Daten bleiben unveraendert.
Referenz: WRSR 1.1.1.9 (64-Bit), TesmioLoader API 4

Ausführliche Dokumentation:
  Deutsch: README_DE.md
  English: README_EN.md

Das Plugin erweitert die Lageranzeige im Technischen Service und verwaltet
Streugutprioritäten, Schneepflugtanks, Depotnachfüllung und das Ergänzen
fehlender Lager beim Laden. Materialabhängiger Straßenschutz wird über die
Anbindung an ein kompatibles weather_roads umgesetzt.

Installation
------------
Das Spiel vollständig schließen. DLL und INI unter
SovietRepublic\tesmioloader\build\plugins\ ablegen.
Für übersetzte Plugin-Texte localization.dll aktivieren und das mitgelieferte
Paket unter plugins\localization\technical_service_storage\ bereitstellen.
Fehlende Übersetzungen verwenden Ersatztexte; das Plugin bleibt grundsätzlich
auch ohne Localization-Dienst nutzbar.

Wichtig
-------
- Eigene INI-Werte und Materialreihenfolge bei Updates erhalten.
- Die INI-Liste erzeugt keine Ressourcen oder Gebäudedefinitionen.
- Ergänzte Depotlager beginnen leer, können aber beim normalen Speichern
  dauerhaft in den nativen Spielstand gelangen. Ausschalten macht das nicht
  rückgängig. Vor Migrations- und Langzeittests den Spielstand sichern.
- Fehlende INI/Materialsektion verwendet Standardwerte einschließlich Sand.
  Eine ausdrücklich leere/ungültige Materialliste ist ein anderer Fall:
  Streugutbetrieb, Migration und Persistenz werden für die Sitzung ausgesetzt.
- [general] enabled = 0 und vollständiger Neustart deaktivieren das Plugin.
  Die INI zu löschen ist keine Deaktivierung.
- Spielstand-Zusatzdateien werden durch Ausschalten nicht gelöscht.

Protokolle im Loader-Basisordner
------------------------------
tesmioloader.log
tesmioloader.technical_service_storage.log

[general] debug = 1 aktiviert zusätzliche Details; debug_limit begrenzt nur
UI-Debugmeldungen. Echte Warnungen bleiben auch bei debug_limit = 0 sichtbar.
Nach Änderungen das Spiel vollständig neu starten.

README_DE.md und README_EN.md erklären Wertebereiche, Materialwirkung,
Tankberechnung, Speichern/Laden, Lokalisierung und die Fehlersuche.
BUILD_INFO.txt und TECHNICAL_NOTES.md enthalten ergänzende Entwicklungsdaten.
