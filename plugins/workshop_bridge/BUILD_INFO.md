# workshop_bridge – build notes

- Version 0.1.0-beta (2026-09-05). TesmioLoader API 4 (min 3), no game addresses.
- Sources: `workshop_bridge.cpp`, includes `..\..\src\tesmio_plugin.h` and `..\tesmio_config.h`.
- Build: the standard line in `build.bat` (`cl /O2 /MT /W3 /EHsc /LD ... /link kernel32.lib`).
  Exports: TsmPluginApiVersion, TsmPluginInit, TsmPluginStart.
- Runtime files: `workshop_bridge.ini` beside the DLL (base), `user_config\workshop_bridge.ini` (overlay, written by Tesmio Settings).
- Offline test: `..\tests\run_bridge_test.bat` – builds `tests\build\workshop_bridge.dll`, `bridge_child.dll` (stub hook) and `workshop_bridge_test.exe`; seven scenarios (list/all policy, overlay 0/1, duplicate in plugins\, manifest enabled=0, missing hook, content-only package, path escape, declining hook, SML installed, bridge disabled, unknown root).
- Log prefix `bridge   ` in tesmioloader.log; no detail log.
