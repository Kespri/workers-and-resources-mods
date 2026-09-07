# Build-Nachweis

## Aktuelle Version: 1.3.4-beta (07.09.2026)

Anlass: Quelltextvergleich mit dem Original RailPhysics 1.3.0 (`Desktop\railphysics`).
Ergebnis des Vergleichs: Physik, Entscheidungslogik, Signaturen, Offsets, Schluessel und
Standardwerte identisch; die Portierung behebt drei ABI-Fehler der Original-Stubs
(5. Argument des Bremshelfers, Register im Shadow Space, xmm0 im Kurven-Stub), einen
Use-after-free im Korridoraufbau, ungepruefte Lesezugriffe und das locale-abhaengige
`atof`. Kritik: an einigen Stellen stoppte 1.3.2 den Zug oder verwarf alle Zonen, wo das
Original degradierte. 1.3.4 stellt dort das Original-Verhalten wieder her; keine Formel,
kein Hook und kein Schluessel wurde geaendert:

- `ComputePhysics`: Vorpruefung ueber `ReadableFast` (Regionscache) statt `ReadablePtr`
  (ein VirtualQuery je Wagen und Frame); inaktive Wagen werden vor dem Typzugriff
  uebersprungen (Original Z. 307-308). Bremshelfer und Kurvencache pruefen ebenfalls
  ueber den Cache.
- `rp_curve_helper`: nicht endliches natives Limit wird durchgereicht (vorher 0.0).
- `CollectStationNodes`: unlesbare Kette oder Tabelle -> `continue` statt Abbruch.
- `BuildCorridors`: Abbruch wegen Speicher/Kapazitaet veroeffentlicht die gefundenen
  Korridore (Original) mit Warnung RP103.
- `RescanStations`: schlaegt der Aufbau fehl, bleiben die vorherigen kompletten Tabellen
  (`RestoreZones`) statt 30 s ohne Zonen.
- Streckenvorschau: unlesbares Segment, ungueltige Laenge, unlesbarer Endknoten,
  ungueltige Polylinie, nicht endlicher Punkt -> Lauf endet mit den gesammelten Punkten
  (Original `break`), Warnung RP_ROUTE. Nur Speicherfehler bleiben `CurveScanFailure`
  (letztes Limit bei gleichem Ursprung).
- `SpanCount`: Rest wird abgerundet wie die Zeigerdivision des Originals.

Offline-Tests (`tests\run_tests.bat`, 17 Prozesse): `allocations` prueft jetzt je
Fehlstelle "vorherige Tabellen erhalten oder Teilkorridore veroeffentlicht, kein Leck,
Wiederholung erfolgreich" (15 erhalten, 3 partiell); `guards` neu: inaktiver Wagen mit
totem Typzeiger wird uebersprungen, aktiver Wagen haelt die nativen Aufrufe fern,
zerrissenes zweites Streckenstueck liefert Teilvorschau ohne Scanfehler, NaN-Limit
wird durchgereicht; `SpanCount(8,25,8) == 2`. Protokolle:
`verification\version-1.3.4-beta-build.log`, `verification\version-1.3.4-beta-tests.log`.

Spieltest: NICHT ausgefuehrt. Vorgeschlagenes Protokoll (Kopie eines Spielstands):
1. Langer Gueterzug (>= 20 Wagen) am Berg: Anfahren, Steigung, Gefaelle; Log auf RP100/
   RP101 pruefen (darf bei gesunden Zuegen nicht erscheinen).
2. Zug mit abgekuppelten/inaktiven Wagen im Depot und auf Strecke.
3. Bahnhofseinfahrt mit `log_decisions = 1`: Bremsband sichtbar (planned), keine
   Pulszuege.
4. Grosse Karte, 30 min Zeitraffer: `subsystem(s) patched`, RP103/RP104/RP105 zaehlen,
   Frame-Zeit gegen 1.3.3 vergleichen.
5. Zollanfahrt: Korridore vorhanden (`log_curves = 1`), Einfahrt mit customs_entry_kmh.

Sicherung des Vorstands: `_backups\rail_physics_fix_1.3.3-beta_before_1.3.4-beta_*`.

## Aktuelle Version: 1.3.3-beta (07.09.2026)

Einzige Aenderung am Plugin: die Wahl der Konfigurationsdatei. `ResolveConfigFile`
prueft `plugins\rail_physics_fix.ini` unter dem Loader-Basisordner; existiert die
Datei, laufen alle Lesezugriffe wie bisher ueber `H->configInt`/`H->configString`
mit dem unveraenderten Dateinamen. Fehlt sie, wird `rail_physics_fix.ini` neben
der eigenen DLL (GetModuleHandleEx auf die eigene Adresse) mit
GetPrivateProfileIntA/GetPrivateProfileStringA und demselben Trim wie im Loader
gelesen (`CfgInt`/`CfgString`). Schluessel, Code-Standardwerte, RP201/RP202,
grid_boost-Regel, Hooks, Bruecken und Physik sind unveraendert. Neue Logzeile
`rail_physics_fix  configuration file: <pfad>`.

Offline-Tests: `tests\run_tests.bat` legt jetzt `build\plugins\rail_physics_fix.ini`
an (der Offline-Host nennt `build` als Basisordner, die klassische INI liegt dort
unter plugins\) und fuehrt zusaetzlich das Szenario `beside_dll` aus (Basisordner
ohne plugins\-INI, kein Host-Leseaufruf, gelieferte Werte aus der INI neben dem
Testprogramm). 17 Prozesse. Versionspruefung im direkten und DLL-Exportpfad auf
`1.3.3-beta` umgestellt.

Workshop-Paket: `My Plugins\rail_physics_fix` (soviet.mod.ini mit local_copy,
Presentation-Schema mit vier Reitern, DE/EN, READMEs nach Vorlage, LICENSE GPL v3).
Sicherung des Vorstands: `_backups\rail_physics_fix_1.3.2-beta_before_1.3.3-beta_*`.

## Umbenennung zu rail_physics_fix (05.09.2026)

Das Plugin heisst seit dem 05.09.2026 `rail_physics_fix`, damit es sich vom
Standard-`rail_physics` unterscheidet. Umbenannt wurden Plugin-Name, DLL, INI,
Detail-Log (`tesmioloader.rail_physics_fix.log`), Log-Praefix, die Quelldateien
`rail_physics_fix.cpp`, `rail_physics_fix_support.h`, `rail_physics_fix_stubs.h/.S`,
`tests\test_rail_physics_fix.cpp` sowie Build-Skripte, Werkzeuge und READMEs.
Unveraendert: der INI-Abschnitt `[railphysics]` (Kompatibilitaet zum Original),
die Version 1.3.2-beta und die Physik. Die folgenden Abschnitte und Hash-Tabellen
beziehen sich auf die Dateinamen vor der Umbenennung.

## Aktuelle Version: 1.3.2-beta

Erstellt am 03.09.2026, Windows x64. Reine Versionskorrektur von
`1.3.2-tesmio-beta` auf `1.3.2-beta`, keine Änderung der Physik oder Einstellungen.
Im Produktionsquelltext wurden nur die Kopfzeile und `kRailVersion` geändert.
Die Loader-Schnittstelle bleibt API 4; Brücken, Support-Header und SDK unverändert.

Erneut geprüft:

- Release-Build mit `build.bat` erfolgreich (MSVC 14.52.36615, x64, `/MT`).
- Alle 16 Prozesse von `tests/run_tests.bat` erfolgreich, inklusive ausdrücklicher
  Prüfung von `TsmPluginInfo.version == "1.3.2-beta"` im direkten und DLL-Exportpfad.
- Kompatibilitätsbuild mit den Root-Build-Flags erfolgreich.
- Exporte weiterhin `TsmPluginApiVersion`, `TsmPluginInit`, `TsmPluginStart`;
  einzige DLL-Abhängigkeit weiterhin `KERNEL32.dll`.
- INI-Inhalt bis auf den Versionskommentar unverändert; dies gilt getrennt für
  Quellprojekt, Desktop-Build und installierte Spielkonfiguration.

Neue Protokolle: `verification/version-1.3.2-beta-build.log`,
`verification/version-1.3.2-beta-tests.log`,
`verification/version-1.3.2-beta-root-build.log`.
Die Tests laufen mit synthetischen Daten und einer privaten Datenkopie der EXE;
kein Spielcode wird ausgeführt. Kein neuer Ingame-Test dieser Versionskorrektur.

| Aktuelle Datei | SHA-256 |
|---|---|
| `rail_physics.dll` | `E9B4CBF46BF40E12AD9124AEFC3B36705E535808FAA7B3AF28235CC05A13DB01` |
| `rail_physics.cpp` | `77B8242FD403FAD89C883C4231E71A3AA8A77CA35F2BD4CC094BCA6C10A6FF15` |
| `rail_physics.ini` | `C2ED544B7AC5987B5AF90AB1730D9F7958DB69B18936F1FF8297C2C54099B105` |

## Historischer Nachweis vom 02.09.2026 (unverändert)

Die folgenden Angaben und die nicht mit `version-1.3.2-beta-` beginnenden
Protokolle dokumentieren ausschließlich den damaligen Build, nicht die aktuelle DLL.

Erstellt am 02.09.2026, Windows x64, Version `1.3.2-tesmio-beta`.
Kein laufendes Spiel wurde verändert. Keine Datei wurde in den aktiven
Spiel-/Loader-Ordner installiert. Noch kein Ingame-Test dieser Portierung.

## Werkzeugkette

- Microsoft C++ / Linker 14.52.36615, x64, statische Laufzeit (`/MT`).
- Release: `/std:c++17 /utf-8 /O2 /MT /W4 /wd4505 /EHsc /LD`.
- `4505` betrifft unbenutzte statische SDK-Hilfsfunktionen; keine anderen
  Warnungskategorien wurden für diesen Build abgeschaltet.
- Separater Kompatibilitätsbuild mit den normalen Root-Build-Flags
  `/O2 /MT /W3 /EHsc /LD`: erfolgreich, ohne zusätzlichen Assembler-Linkschritt.
- Clang 22.1.3 nur zum Generieren/Prüfen der Brücken und für den Test-Harness.
- DLL-Abhängigkeit: `KERNEL32.dll`. Keine zusätzliche MinGW-/Clang-Laufzeit-DLL.
- Exporte: `TsmPluginApiVersion`, `TsmPluginInit`, `TsmPluginStart`; API 4.

## Prüfungen

Alle 16 Testprozesse aus `tests/run_tests.bat` erfolgreich. Die Tests verwenden
synthetische Fahrzeuge/Gleise und eine Datenkopie der EXE, keine Spielfunktionen.
Der echte DLL-Exportpfad wird in einem künstlichen Host geprüft.

- Alle zwölf Quellsignaturen geprüft: elf eindeutige Treffer; zwei Referenzen
  der zwölften Signatur stimmen auf dieselbe globale Adresse überein.
- Fünf ersetzte Befehlsblöcke, drei Funktionsaufrufe und zwei Zweigziele geprüft.
- Alle fünf ausführbaren Brücken getestet; 996 Byte, 15 interne Symbole.
- Kräfte/Bremsen/Verbrauch, gerade/gebogene Route, Bahnhof und Halteparabel geprüft.
- Zoll-Korridor mit 1.100 Knoten, 1.099 Segmenten und mehreren Hash-Rehashes geprüft.
- Falsche Version, kaputte/mehrdeutige Signaturen, doppelte DLL und Fehlerpfade geprüft.
- Vier unveränderte Cache-/Tabellenfunktionen im Quelltextvergleich erhalten.
- Rechenvergleich mit 1.3.1: 5.120 gültige Eingabefälle, 62.464 Zahlenwerte;
  bitweise Ergebnis-Prüfsumme beider Programme: `E927BA1BB84487F7`.
- 18 fehlschlagende Reservierungsstellen des Tabellen-Testfalls, Freigabe aller
  beteiligten Puffer und erfolgreicher Wiederanlauf geprüft.
- Guard-Pages, NaN/Überlauf, Fehler bei Routenreservierung mit Cache-Erhalt,
  genau eine native Treibstoffbuchung, Warnungsbegrenzung und Win32-Fehler geprüft.
- Alle 30 gelieferten INI-Werte unverändert.

Die Meldung `only 3/9 requested patches installed` im Testprotokoll gehört zum
**absichtlich simulierten Ausfall** aller Inline-Hooks. Im regulären Offline-Test
werden 9/9 installiert. Der Test prüft, dass nach Teilinstallation die DLL nicht
entladen und der Steigungszweig nicht unvollständig umgeschaltet wird.

Ausführliche Protokolle: `verification/build.log`, `verification/tests.log`,
`verification/root-build.log`, `verification/game-verification.log`,
`verification/parity.log`, `verification/stubs.log`, `verification/source-parity.log`.

## SHA-256

| Datei | SHA-256 |
|---|---|
| Ausgelieferte `rail_physics.dll` | `ACE4EED6C0E57780E4ECF0F08F6D033A8E9F004481748E06196DACE6A31DECD1` |
| `rail_physics.cpp` | `0D4D898D17C7DD8F864D9D041CC1153F8FCE0CB28CFF4C94A0AB9D0154B55075` |
| `rail_physics_support.h` | `04C5BF42699536FB44428A4BFAF573EB06912813CD700E41CD4BAAF0B4E37A83` |
| `rail_physics.ini` | `D61C22946A9BA0503977580A0343DEAE2BE0715FEED43E6EA531A06E9F3B8DB8` |
| `rail_physics_stubs.h` | `3516D15E6CDA89216B462AC800EFA37824424E19BF78ECBFCEAB28D556D168D5` |
| Ursprüngliche `railphysics.dll` | `EE54F6430BF0773CCD79FD7452B108B364F8778326EB6D3D3342863A36B9FCC7` |
| Ursprüngliche `railphysics.cpp` | `7F40A955B2CA54E448650C669BFDC9AFAD78F94BA4D15B05C34A1B08D3D19340` |
| Ursprüngliche `railphysics.ini` | `F22BE07D5AF977B4F3E7D71A7DADE0AECE11369680FADE1DEEE82B06E1FB7981` |
| Geprüfte `SOVIET64.exe` | `296644A9F207D609031FC2AE73FED2DCB34619A1D55A35D1C7B51965CE6841B8` |

Die geprüfte EXE hat PE-Zeitstempel `0x6A3EB6AD` und ImageSize `0xA9D000`.
Die Header entsprechen bytegleich dem zum Build vorhandenen TesmioLoader-SDK.
Die EXE, Original-DLL und Originalquelle werden nicht als Teil dieses Plugin-Ordners verteilt.

## Einordnung

Die neuen Schutzmaßnahmen greifen bei fehlgeschlagenen Reservierungen, ungültigen
Speicherbereichen/Zahlen oder Windows-API-Fehlern. Gültige Physik- und Routendaten
werden mit den bisherigen Formeln verarbeitet. Ein fehlgeschlagener Zonenaufbau
schaltet diese zusätzlichen Zonen bis zum nächsten erfolgreichen 30-Sekunden-Scan
ab; Teilzustände und ungeprüfte alte Weltzeiger werden nicht wiederverwendet.

Zeitbasis, Simulationstick-Erkennung, Objektlebensdauer/Caches, INI-Politik,
Scanbudgets und Hook-Gruppierung wurden nicht grundlegend umgebaut. Der direkte
Rechenvergleich und die Offline-Tests ersetzen keinen Test im laufenden Spiel.
