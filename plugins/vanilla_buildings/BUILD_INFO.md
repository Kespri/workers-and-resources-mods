# Vanilla Buildings – build notes

Target: WRSR 1.1.1.9, TesmioLoader API 4. Build: the standard line (`cl /O2 /MT /W3 /EHsc /LD
... /link kernel32.lib`); exports TsmPluginApiVersion/TsmPluginInit/TsmPluginStart. History
newest first.

## 1.3 (2026-09-07)

- The INI is looked up as `plugins\vanilla_buildings.ini` first, otherwise beside the DLL
  (Workshop package under Soviet Mod Loader or the Workshop Bridge). The chosen file is logged
  (`Configuration file:`).
- Patch logic, commands and limits unchanged from 1.2.
- Workshop package `My Plugins\vanilla_buildings` with an editor schema for Republic Mod
  Manager (0.31.0 or newer: keyed_sections with `lines` fields, building picker since 0.32.0),
  README_DE/EN per template; the shipped INI has both example rule sets switched off.

Verification: MSVC x64, no warnings; exports verified. The in-game test of the INI fallback is
still open (load through a subscription or the Bridge and look for `Configuration file:` in
the log).

## 1.2 (2026-09-01)

- The native building reader is now reached through its actual `fopen` call. `fopen_s`,
  `_wfopen` and `_wfopen_s` were added; the previous engine buffer reader stays supported.
- Targets below `buildings_types`, `dlcN/buildings` and Workshop IDs are supported. Workshop
  data is looked up in the game's Steam library under `steamapps/workshop/content/784150`.
- Several `target = ...` lines per section share all patch rules. Every target file is
  checked independently against its unchanged source file. At most 256 target files in total.
- Existing single `target` entries stay valid. The optional keys `target1`, `target2`,
  `target3` additionally check the target kind Vanilla, DLC or Workshop.
- Every rejected active target gets a precise warning, even with `debug = 0`: section, target,
  cause, and for rule errors the command and the INI line number. A failed target receives no
  partial change.
- The first successful access to a replacement file is logged as `[overlay-opened]`, so
  prepared rules and actually read files can be told apart.
- Original files are not changed. Write and update accesses are not redirected.

Verification:

- Release DLL and test programs built with MSVC x64, without compiler warnings.
- 98/98 validation and file tests succeeded, including six really installed Vanilla/DLC/
  Workshop buildings and unchanged source files.
- Four DLL lifecycle tests succeeded: disabled, active with all four CRT readers, partially
  failed hook installation, and a missing mandatory `fopen` import.
- The DLL tests also check precise warnings for an already existing line and a missing file.
  Valid targets of the same group still work.
- The user's active rules were checked. The configuration only received updated comments;
  all rules and switches stay unchanged.
- In-game evidence still open: restart the game completely and check the first
  `[overlay-opened]` messages in the log.

Scope: existing saved buildings are not yet extended by missing storages; that migration is a
separate next step (implemented by Technical Service Storage for depots). The manually edited
small technical service stays disabled in the existing user configuration; its original file
was not reset. The package ships a neutral example INI; do not replace an existing user INI
with that template unchecked.
