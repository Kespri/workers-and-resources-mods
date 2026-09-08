# Vehicle Materials – build notes

TesmioLoader plugin (API 4) for WRSR 1.1.1.9, GPL v3. Build: the standard line of the root
`build.bat` (`cl /O2 /MT /W3 /EHsc /LD`, kernel32.lib) compiles `vehicle_materials.cpp`; it
includes `src/tesmio_plugin.h` and `my_plugins/tesmio_config.h` (base INI beside the DLL or in
`plugins\`, personal overlay `user_config\vehicle_materials.ini`). Offline test:
`my_plugins\tests\run_vehicle_materials_test.bat` (builds the DLL and runs six configuration
scenarios). User documentation: README_DE.md / README_EN.md. History newest first.

## 0.4.0 (2026-09-08)

- Version numbering restarts in beta with the September rework: 0.4.0 follows 1.2.0-beta, the DLL
  logic is unchanged. Scheme from here on: 0.M.P, patch increments per change, the next minor when
  a change is large.
- Workshop package texts (RMM schema, de/en) rewritten in the user's gamer style; the storage-line
  notice names `$STORAGE_IMPORT_SPECIAL` again (the token had been lost in an earlier text pass);
  two schema keys that had no language entry (`field.enabled.description`,
  `mapping.group.description`) now exist in both languages; schema fallbacks are English.
- READMEs: two sentences reworded.

## 1.2.0-beta (2026-09-05)

- Configuration read as base plus personal overlay through `tesmio_config.h`; the base is
  `plugins\vehicle_materials.ini` when present, otherwise the INI beside the DLL; the overlay is
  merged key by key and validated with the same strict reader. Material calculation unchanged
  since 1.1.0.
- Workshop package 3794994476 with `user_overlay = 1`; package revision 2 (2026-09-06) added
  `local_copy = 1` to the manifest, DLL and INI unchanged.

## 1.1.0 and earlier

- Additional production materials per vehicle class (road, rail, ship, airplane) registered
  through the Resources plugin; coefficients multiplied with the vehicle's internal production
  value; automatic vehicle-type mapping (type 1 road, 6 ship, 7 airplane, others rail) with
  `[mapping]` overrides; strict configuration validation that rejects the whole plugin on any
  error and leaves the game untouched.
