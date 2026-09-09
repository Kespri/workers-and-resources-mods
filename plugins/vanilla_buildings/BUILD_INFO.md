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

Declared finished by the user after the text review; version string `0.4.0` follows 1.3.1 (user's
numbering, back into beta). No runtime change: patch logic, commands, INI keys and defaults are
those of 1.3.1. Package texts in player style (launcher notice, [activity], Log settings card,
group notice, multi-line field texts). Backup: `_backups\vanilla_buildings_1.3.1_before_0.4.0_*`.
The in-game test of the `insert` command (1.3.1) was not reported by the user before the release.
Later the same day the user removed the shipped `[technical_services_grit]` rule set from the INI (kept
as a README example only; the plastics factory example is the one shipped rule set); source, package
and READMEs follow that.

## 1.3.1 (2026-09-09)

- New command `insert = <position> | <anchor> | <line>`: position 0 inserts before the anchor
  (the old `insert_before` behaviour), 1 after it. `insert_before = anchor | line` is still
  parsed and rewritten to `insert = 0 | ...` (fields shifted, no log noise). Validation as
  before (anchor exactly once, no duplicate directly next to the anchor, `$COST_RESOURCE_AUTO`
  only with a `$COST_WORK` anchor); position 1 with the anchor `end` is rejected. The
  collision check with replaced/removed lines now uses the anchor index (`Operation.anchorAt`)
  instead of the insertion slot, which for position 1 is the line after the anchor.
- Self-test: the two `$COST_RESOURCE_AUTO` inserts as `insert = 0`, one new `insert = 1`
  (gravel after the `$COST_WORK` anchor), transformed-output check extended to the exact
  four-line sequence.
- Package: schema detail `insert` (label "Insert before or after an anchor"), INI example and
  comments, READMEs. Requested by the user during the text review ("0 = Vor und 1 = Nach").
- In-game test: pending (user).

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
