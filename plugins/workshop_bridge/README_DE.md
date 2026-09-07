# Workshop Bridge 0.1.0-beta

Lädt die Hook-DLLs abonnierter Workshop-Pakete in TesmioLoader, ohne Soviet
Mod Loader und ohne Kopieren von Hand.

## Wozu

TesmioLoader lädt nur DLLs aus `tesmioloader\plugins\`. Ein Plugin, das als
Workshop-Paket (`soviet.mod.ini` mit `[hooks] dll`) veröffentlicht ist, braucht
deshalb entweder Soviet Mod Loader oder eine von Hand kopierte DLL. Die Bridge
ist der dritte Weg: ein gewöhnliches TesmioLoader-Plugin, das die abonnierten
Pakete durchgeht, die freigegebenen Hook-DLLs lädt und ihnen dieselbe
Host-Tabelle übergibt, die es selbst bekommen hat. Ein so geladenes Plugin sieht
genau das, was es unter Soviet Mod Loader sähe: denselben Loader-Ordner,
dieselbe `user_config`, dieselben Dienste.

## Was die Bridge nie tut

- **Soviet Mod Loader ist geladen** oder installiert und in `tesmioloader.ini`
  eingeschaltet: SML lädt die Hooks selbst, die Bridge bleibt untätig.
- **`plugins\<name>.dll` existiert:** Diese Kopie gehört dem Loader, ein- oder
  ausgeschaltet, wie der Launcher es sagt. Die Bridge lädt das Paket dann nicht
  und schreibt eine Zeile ins Log. Die lokale Kopie löschen, wenn das Paket
  gelten soll.
- Eine DLL, die unter demselben Dateinamen schon im Prozess ist, wird nicht
  noch einmal geladen.
- Hook-Pfade, die das Paket verlassen (`..`, absolute Pfade), werden abgewiesen.

## Konfiguration

`workshop_bridge.ini` neben der DLL ist die Basis; `user_config\workshop_bridge.ini`
liegt darüber, Schlüssel für Schlüssel. Tesmio Settings schreibt nur die Overlay-Datei.

```ini
[bridge]
enabled = 1           ; 0 = Bridge untätig
policy = list         ; list = nur Pakete mit 1 unter [packages]; all = alle Pakete mit Hooks, außer 0
workshop_root = auto  ; auto = Steam-Bibliothek des Spiels, sonst absoluter Pfad
log_verbose = 0       ; 1 = jede übersprungene Entscheidung ins Log

[packages]
3794994476 = 1        ; Workshop-Nummer = 1 lädt, 0 überspringt
```

Ein Paket mit `enabled = 0` in seiner eigenen `soviet.mod.ini` wird immer
übersprungen.

## Ablauf

Alle Hooks werden im `TsmPluginInit` der Bridge initialisiert; was ein Hook
per `provide` anmeldet, steht damit vor jedem `TsmPluginStart` auf dem
Schwarzen Brett. Die Starts der Hooks laufen im Start der Bridge. Der Loader
schreibt die Dienste eines Hooks seinem Log nach `workshop_bridge.dll` gut; die
Bridge nennt daneben den echten Namen:

```
plugin   service "tss.grit_spreader" v1 from workshop_bridge.dll
bridge   hook technical_service_storage 0.3.0    from 3795181788\hooks\technical_service_storage.dll
bridge   1 hook(s) loaded, 0 skipped, 1 package(s) with hooks
```

Die Bridge selbst hookt nichts und patcht nichts.

## Bauen und prüfen

Wie jedes Plugin in `my_plugins` über `build.bat`. Offline-Test ohne Spiel:
`my_plugins\tests\run_bridge_test.bat` (baut die Bridge, einen Stub-Hook und
das Testprogramm; Exit-Code 0 = alle Prüfungen bestanden).
