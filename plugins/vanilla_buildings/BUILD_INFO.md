# Vanilla Buildings – build notes

Target: WRSR 1.1.1.9, TesmioLoader API 4. Build: the standard line (`cl /O2 /MT /W3 /EHsc /LD
... /link kernel32.lib`); exports TsmPluginApiVersion/TsmPluginInit/TsmPluginStart. History
newest first.

## 0.4.1 (2026-09-09)

- `insert` accepts as anchor a line that an earlier `add`, `insert` or `replace` of the same
  section produces (`Operation.anchorOp`). Validation: when the anchor has no match in the
  original, the earlier operations are searched (add field 0, insert field 2, replace field 1);
  none found is still "0 matches"; the same line twice at one produced anchor is rejected.
  Produced anchors skip the neighbour and collision checks (they are not original lines).
- `ApplyOperations` now builds a vector of (producer, line) pairs and splices produced-anchor
  inserts afterwards in INI order, so chains work (add sand -> insert 1 after sand: gravel ->
  insert 0 before gravel: road_salt gives sand, road_salt, gravel). The duplicate-position check
  ignores operations without a slot.
- Self-test extended with exactly that chain. Reason: the user's rule set anchored a storage
  line on another storage line his own `add` produced; every target was rejected with
  "anchor has 0 matches" and the plugin stayed inactive.
- In-game test 2026-09-09: confirmed by the user (storages present, insert before and after the anchor work).

## 0.4.0 (2026-09-09)

First published version.

- Temporary changes to building INIs of the game, the DLCs and the Workshop without touching the
  original files: the native building reader is reached through its actual `fopen` call
  (`fopen_s`, `_wfopen`, `_wfopen_s` covered as well, the engine buffer reader stays supported);
  modified copies are written to a temporary folder at every start and only the game's read
  accesses are redirected. Write and update accesses are not redirected.
- Targets below `buildings_types`, `dlcN/buildings` and Workshop ids (looked up in the game's Steam
  library under `steamapps/workshop/content/784150`); several `target = ...` lines per section
  share all patch rules; every target file is checked independently against its unchanged source
  file; at most 256 target files in total. The optional keys `target1..3` additionally check the
  target kind Vanilla, DLC or Workshop.
- Commands: `add`, `replace`, `remove`, `insert = <position> | <anchor> | <line>` (0 before, 1 after
  the anchor; `$COST_RESOURCE_AUTO` only with a `$COST_WORK` anchor, position 1 with the anchor `end`
  rejected), `add_connection`, `replace_connection`, `remove_connection`. Every rejected active
  target gets a precise warning, even with `debug = 0`: section, target, cause, and for rule errors
  the command and the INI line number. A failed target receives no partial change. The first
  successful access to a replacement file is logged as `[overlay-opened]`.
- Configuration: `plugins\vanilla_buildings.ini` when present, otherwise the INI beside the DLL
  (Workshop package under Soviet Mod Loader or the Workshop Bridge); the chosen file is logged
  (`Configuration file:`).
- Self-test: `cl /DVANILLA_BUILDINGS_TEST` builds `vb_selftest.exe <media_soviet> <ini>`; the
  transformed-output check covers the exact `$COST_RESOURCE_AUTO` sequences.
- Package: editor schema for Republic Mod Manager (keyed_sections with `lines` fields, building
  picker, launcher notice, [activity], Troubleshooting card, group notice, multi-line field texts);
  the shipped INI carries the plastics factory example only, switched off.

Verification of the file layer: 98/98 validation and file tests succeeded, including six really
installed Vanilla/DLC/Workshop buildings and unchanged source files; four DLL lifecycle tests
(disabled, active with all four CRT readers, partially failed hook installation, missing mandatory
`fopen` import); precise warnings for an already existing line and a missing file.

Scope: existing saved buildings are not extended by missing storages; that migration is
implemented by Technical Service Storage for depots.
