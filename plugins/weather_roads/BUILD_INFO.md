# Weather Roads – build notes

TesmioLoader plugin (API 4) for WRSR 1.1.1.9, GPL v3. Build: the standard line of the root
`build.bat` (`cl /O2 /MT /W3 /EHsc /std:c++17 /LD`, kernel32.lib) compiles `weather_roads.cpp`;
it includes `src/tesmio_plugin.h`, `my_plugins/grit_spreader_api.h` (consumer side of the
`tss.grit_spreader` service of Technical Service Storage) and the two persistence headers
`weather_roads_persistence.h` / `weather_roads_persistence_format.h` (sidecar
`tesmioloader.weather_roads.protection.bin` beside the savegame). Hooks are installed only on the
verified SOVIET64.exe / C3DDLL64.dll build. User documentation: README_DE.md / README_EN.md.
History newest first.

## 0.3.0 (2026-09-08)

- Final number of the September rework (user's call); follows 0.2.11-beta, the DLL logic is
  unchanged. Scheme from here on: 0.M.P, patch increments per change, the next minor when a change
  is large.
- Workshop package texts (RMM schema, de/en) rewritten in the user's gamer style: 47/46 keys,
  every boolean described with ON/OFF, line breaks (`\n`) in longer descriptions, the group
  description of the General card dropped, the restart/Technical Service Storage note moved to
  `[launcher] notice` (needs Republic Mod Manager 0.4.3), the plow formula removed from the UI,
  "strong phase" renamed "protection phase after plowing".
- READMEs: same naming, plain wording for melting, visual mapping and the sidecar check.

## 0.2.11-beta (2026-09-07)

- INI fallback beside the DLL: `plugins\weather_roads.ini` when present, otherwise the INI next to
  the DLL (Workshop package under Soviet Mod Loader or the Workshop Bridge); own `ConfigString`
  reader identical to the loader's (GetPrivateProfileStringA plus trim). Chosen path logged as
  `configuration file:`.
- Workshop package `My Plugins\weather_roads` with a five-tab RMM schema (33 fields).

## 0.2.10 and earlier

- 0.2.10: defensive loader checks, cleanup on exceptions, clearer start messages.
- 0.2.8: corrected save timing of the sidecar (written after the final native file close).
- 0.2.x: scaled and optionally gradual road-snow build-up with a per-burst cap, independent natural
  melting scale, protection phase (game minutes) followed by a weaker salt phase (game hours) with a
  configurable factor, material strength and dry-plowing detection through the grit spreader
  service, visual snow correction on tracked plowed areas, F10 diagnostic overlay with built-in
  German/English texts, protection persistence beside the savegame.
