# Workers & Resources: Soviet Republic – Plugins für den TesmioLoader

[English](README.md) | **Deutsch**

Quelltexte der TesmioLoader-Plugins von Kespri für *Workers & Resources: Soviet Republic* 1.1.1.9. Die Plugins werden als Workshop-Pakete verteilt; dieses Repository ist der Quelltext dazu, wie es die GNU GPL v3 verlangt. Fertige DLLs liegen hier nicht.

## Plugins

| Ordner | Version | Zweck | Herkunft |
|---|---|---|---|
| `plugins/deposits_plus` | 0.4.6 | Vorkommenstypen, natürliche Verteilung, sandige Wiese, Arbeitsfahrzeuge | Fork des Plugins `deposits` aus dem TesmioLoader von MaxLegend (Tesmio), GPL v3 |
| `plugins/depletion` | 1.1.2 | Erschöpfung von Vorkommen, Anzeige im Minenfenster | weiterentwickeltes Plugin `depletion` aus dem TesmioLoader von MaxLegend, GPL v3 |
| `plugins/localization` | 0.4.0 | Übersetzungsdienst: Textpakete in INI-Dateien werden zu erweiterten Sprachdateien im VFS | eigene Entwicklung; enthält die Textpakete `research_expansion`, `technical_service_storage`, `tesmio_lang` |
| `plugins/rail_physics_fix` | 1.3.5 | physikalische Zugdynamik: Antrieb, Bremsen, Steigung, Kurven, Bahnhöfe, Zoll, Verbrauch | Fork von [RailPhysics 1.3.0](https://github.com/TheRealMeowMeow00/WRSR_RailPhysics) von Meow Meow (TheRealMeowMeow00), GPL v3; [Original im Workshop](https://steamcommunity.com/sharedfiles/filedetails/?id=3776784867) |
| `plugins/research_expansion` | 0.4.0 | neue Forschungen und Änderungen am Forschungsbaum über das VFS | eigene Entwicklung |
| `plugins/resources_button_fix` | 0.4.0 | kompakte Schaltflächenraster im Geländeeditor | eigene Entwicklung |
| `plugins/technical_service_storage` | 0.3.3 | Streugutlager, Materialprioritäten und Schneepflugtanks im Technischen Service | eigene Entwicklung |
| `plugins/ui_layout_fixes` | 0.3.0 | Layoutkorrekturen in Spielfenstern | eigene Entwicklung |
| `plugins/vanilla_buildings` | 0.4.1 | Patches für Vanilla-, DLC- und Workshop-Gebäude ohne Änderung der Originaldateien | eigene Entwicklung |
| `plugins/vehicle_materials` | 0.4.0 | Materialbedarf von Fahrzeugen | eigene Entwicklung |
| `plugins/weather_roads` | 0.3.3 | Straßenschnee, Schmelze und Schutz nach dem Räumen | eigene Entwicklung |
| `plugins/workshop_bridge` | 0.2.0 | lädt Workshop-Hook-DLLs ohne Soviet Mod Loader | eigene Entwicklung |

Gemeinsame Dateien: `plugins/grit_spreader_api.h` (Dienst zwischen Technical Service Storage und Weather Roads), `plugins/tesmio_config.h` (INI-Basis und Overlay), `plugins/tests` (Offline-Tests). `src/` enthält die zwei SDK-Header `tesmio_api.h` und `tesmio_plugin.h` aus dem [TesmioLoader b0.3.6](https://github.com/MaxLegend/TesmioLoader) von MaxLegend (GPL v3), damit die Plugins mit ihren relativen Include-Pfaden aus diesem Repository heraus gebaut werden können.

Die Plugins wurden mit Unterstützung künstlicher Intelligenz entwickelt.

## Bauen

Visual Studio 2022 oder neuer mit den x64-C++-Werkzeugen, dann in einer Developer Command Prompt im jeweiligen Plugin-Ordner:

```
cl /nologo /O2 /MT /W3 /EHsc /std:c++17 /LD /Fo"build\\" /Fd"build\\" /Fe"build\<name>.dll" <name>.cpp /link kernel32.lib
```

Rail Physics Fix und Deposits Plus bringen ein eigenes `build.bat` mit. Die Exporte `TsmPluginApiVersion`, `TsmPluginInit` und `TsmPluginStart` sind der Plugin-Vertrag des Loaders (API 4). Adressen und Signaturen gelten nur für Spielversion 1.1.1.9 (Build 23935965).

## Installation

Jedes Plugin hat eine eigene Anleitung (`README_DE.md` im Plugin-Ordner) mit vier Wegen: klassischer TesmioLoader, Soviet Mod Loader, Workshop Bridge und Republic Mod Manager. Die Workshop-Pakete enthalten DLL, INI, Anleitung und ein Schema für den Republic Mod Manager.

## Fehlerberichte

Bei Problemen bitte ein Issue mit Spielversion, Loader-Version, aktiven Plugins und den Protokollen `tesmioloader.log` und `tesmioloader.<plugin>.log` aus `tesmioloader\build` eröffnen.

## Lizenz

Alle Inhalte dieses Repositories stehen unter der **GNU General Public License v3**, siehe `LICENSE`. Das gilt für die eigenen Plugins ebenso wie für die Forks; die Herkunft steht in der Tabelle oben und in den jeweiligen Anleitungen. Ausnahme: `plugins/deposits_plus/third_party` enthält Code von Microsoft unter der University of Illinois Open Source License (siehe `LICENSE.TXT` dort).
