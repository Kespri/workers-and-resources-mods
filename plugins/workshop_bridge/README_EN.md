# Workshop Bridge 0.1.0-beta

Loads the hook DLLs of subscribed Workshop packages into TesmioLoader, without
Soviet Mod Loader and without copying anything by hand.

## Why

TesmioLoader loads DLLs out of `tesmioloader\plugins\` and nowhere else. A
plugin published as a Workshop package (`soviet.mod.ini` with `[hooks] dll`)
therefore needs either Soviet Mod Loader or a hand-made copy of its DLL. The
bridge is the third way: an ordinary TesmioLoader plugin that walks the
subscribed packages, loads the hook DLLs it is told to, and hands each of them
the very same host table it received itself. A plugin loaded this way sees
exactly what it would see under Soviet Mod Loader: the same loader folder, the
same `user_config`, the same services.

## What the bridge never does

- **Soviet Mod Loader is loaded**, or installed and switched on in
  `tesmioloader.ini`: SML loads the hooks itself, the bridge stays idle.
- **`plugins\<name>.dll` exists:** that copy belongs to the loader, on or off
  as the launcher says. The bridge does not load the package and says so in
  the log. Delete the local copy to make the package count.
- A DLL already in the process under the same file name is not loaded again.
- Hook paths leaving the package (`..`, absolute paths) are refused.

## Configuration

`workshop_bridge.ini` beside the DLL is the base; `user_config\workshop_bridge.ini`
overlays it key by key. Tesmio Settings writes only the overlay file.

```ini
[bridge]
enabled = 1           ; 0 = bridge idle
policy = list         ; list = only packages with 1 under [packages]; all = every package with hooks unless 0
workshop_root = auto  ; auto = the game's Steam library, otherwise an absolute path
log_verbose = 0       ; 1 = every skipped decision goes to the log

[packages]
3794994476 = 1        ; Workshop item number = 1 loads, 0 skips
```

A package with `enabled = 0` in its own `soviet.mod.ini` is always skipped.

## Phases

Every hook is initialised inside the bridge's own `TsmPluginInit`, so whatever
a hook `provide`s is on the noticeboard before any plugin's `TsmPluginStart`.
The hooks' Starts run inside the bridge's Start. The loader credits a hook's
services to `workshop_bridge.dll` in its log; the bridge logs the real name
beside it:

```
plugin   service "tss.grit_spreader" v1 from workshop_bridge.dll
bridge   hook technical_service_storage 0.3.0    from 3795181788\hooks\technical_service_storage.dll
bridge   1 hook(s) loaded, 0 skipped, 1 package(s) with hooks
```

The bridge itself hooks nothing and patches nothing.

## Build and test

Like every plugin in `my_plugins`, through `build.bat`. Offline test without
the game: `my_plugins\tests\run_bridge_test.bat` (builds the bridge, a stub
hook and the test program; exit code 0 = all checks passed).
