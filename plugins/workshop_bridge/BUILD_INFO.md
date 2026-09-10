# Workshop Bridge – build notes

TesmioLoader API 4 (min 3), no game addresses. Sources: `workshop_bridge.cpp`, includes
`..\..\src\tesmio_plugin.h` and `..\tesmio_config.h`. Build: the standard line in the root `build.bat`
(`cl /O2 /MT /W3 /EHsc /LD ... /link kernel32.lib`). Exports: TsmPluginApiVersion, TsmPluginInit,
TsmPluginStart. Runtime files: `workshop_bridge.ini` beside the DLL (base), `user_config\workshop_bridge.ini`
(overlay; Republic Mod Manager writes the `[packages]` list there). Log prefix `bridge   ` in
tesmioloader.log, no detail log. Offline test: `..\tests\run_bridge_test.bat` builds
`tests\build\workshop_bridge.dll`, `bridge_child.dll` (stub hook) and `workshop_bridge_test.exe` and runs
the scenarios (list/all policy, overlay 0/1, duplicate in plugins\, manifest enabled=0, missing hook,
content-only package, path escape, declining hook, SML installed, bridge disabled, unknown root).
Deploy for development: `Install-Bridge.ps1` (`-Replace` overwrites with a backup). Users get the bridge
inside the Republic Mod Manager package. User documentation: README_DE.md / README_EN.md. History newest first.

## 0.2.0 (2026-09-10)

- Declared finished by the user; successor of 0.1.0-beta, DLL logic unchanged. Version string without
  `-beta`.
- Texts: "Tesmio Settings" replaced by "Republic Mod Manager" in the source comment, the shipped INI and
  the installer; installer also refuses to run while rmm.exe is open.
- READMEs rebuilt on the common template (quick start, features, installation via the Republic Mod
  Manager package or its "Manual Installation" folder, manager section, configuration, value ranges,
  flow and log, rules, compatibility, troubleshooting, file structure, licence, FAQ).
- Republic Mod Manager: local schema texts (settings_schemas, de/en) in player style and a card notice
  that the package list is kept through each package's "Plugin active" switch; the overlay header
  written by older versions ("Written by Tesmio Settings") is rewritten on the next change (0.4.52).
- Installer: `-Replace` overwrites the DLL only; an existing `workshop_bridge.ini` is the player's base
  configuration and stays unless `-ReplaceIni` is given (the first 0.2.0 deploy reset the test setup's
  `workshop_root` to `auto`).
- Backup: `_backups\workshop_bridge_0.1.0-beta_before_0.2.0_*`.

## 0.1.0-beta (2026-09-05)

- First version: walks the subscribed packages under `workshop_root`, loads the hook DLLs of the
  released packages with the bridge's own host table, Init inside Init and Start inside Start, idle
  under Soviet Mod Loader, local `plugins\<name>.dll` wins, no double load, no path escape.
- `[bridge] enabled / policy / workshop_root / log_verbose`, `[packages] <id> = 0|1`.
- Offline test suite with seven scenarios (51 checks).
