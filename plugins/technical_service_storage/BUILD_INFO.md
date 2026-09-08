# Technical Service Storage – build notes

Target: WRSR 1.1.1.9, TesmioLoader API 4. Build: MSVC x64, `/std:c++17 /O2 /MT /W4 /EHsc /LD`,
kernel32.lib only. Companion plugins: weather_roads (consumer of the `tss.grit_spreader`
service), vanilla_buildings (depot storage definitions), localization (text pack
`technical_service_storage`). History newest first.

## 0.3.2-beta (2026-09-08)

`SAND_DIAG_MAX_TRACKED_VEHICLES` raised from 256 to 1024 (sand_spreader_diagnostic.h); the
tracked-vehicle table costs about 250 bytes per slot. `[sand_diagnostic] max_vehicles` now defaults
to 512 and validates 1..1024 (config_validation.h, INI, RMM schema). When the table is full the
least recently seen vehicle is still recycled, unchanged. Shipped `[grit_materials]` list reduced
to `sand` and `gravel` (road_salt is a custom resource the player supplies). Package texts
(schema, de/en) rewritten in player style during the user's review; no other runtime change,
save and sidecar formats unchanged. Backup: `_backups\technical_service_storage_0.3.1-beta_before_0.3.2-beta_*`.

## 0.3.1-beta (2026-09-07)

One path decision added to `LoadConfigFile` (config_validation.h): when
`plugins\technical_service_storage.ini` does not exist, the INI beside the DLL is read instead
(`ConfigOwnDirectory`, GetModuleHandleEx on the DLL's own address). The chosen path is logged
as `ini-path` ("Configuration file: ..."). Parsing, validation, materials, runtime and sidecar
formats are byte for byte the 0.3.0 logic. Balance, native fuel, save formats, priorities and
migration contracts unchanged.

Workshop package `My Plugins\technical_service_storage`: soviet.mod.ini with local_copy,
RMM keyed_list schema for `[grit_materials]` (four tabs, eight cards, all 40 switches),
DE/EN, READMEs per template, LICENSE GPL v3. Backup of the previous state:
`_backups\technical_service_storage_0.3.0-beta_before_0.3.1-beta_*`.

Verification: migration, runtime, configuration/material-section tests and the DLL metadata
smoke test of 0.3.0 still apply; static analysis (`cl /analyze /W4`, 2026-09-07) reports no
plugin-code warnings, the remaining findings (SEH filters, migration vector reads) were
reviewed as intentional or false positives.

## 0.3.0-beta (2026-09-03)

Unifies plugin metadata, grit-spreader diagnostics and documentation under one version label.
`SPREADER_DIAGNOSTIC_VERSION` now aliases `PLUGIN_VERSION`. Game behaviour, INI settings,
localization/service API versions and priority/tank file-format versions remain unchanged.
Historical source and binaries are retained in the release backup, not renamed.

## 0.1.78 and earlier

0.1.78 renamed the material section to `grit_materials`. `ParseConfigText` normalises the
legacy `Streumaterialien` spelling before duplicate detection; the first section still wins
even when the two spellings are mixed. All catalogue entry validation, the scalar schema,
material order, runtime logic and sidecar identity/format semantics are unchanged from 0.1.77.

The hardening in 0.1.77 was based on 0.1.76-beta; no save format or gameplay balance change.

### Hardening boundaries

- Central startup-only configuration schema, complete integer validation, independent C
  numeric locale, bounded complete file read, per-line warnings. Missing optional keys retain
  defaults. First scalar/section declaration wins; materials retain the first valid
  declaration and their file order.
- Explicitly unusable material configuration suspends grit, migration and priority/tank
  persistence writes; existing native storage/sidecars are retained. Native UI remains
  available. No implicit sand replacement for an invalid config.
- Localization checks host table size, consume, resolver and reserved IDs only for
  plugin-owned keys. Native text IDs and built-in fallback rendering remain.
- Runtime warning causes are independent of the debug budget (one per minute per cause).
  Detail writes serialise whole lines and handle short writes. On disk failure, report once
  through the host outside the lock, without recursive file logging.
- Windows hook preparation rejects before publishing a branch on failure. Once patched,
  protection/cache errors mark health degraded and retain the DLL. The original save target
  is published before patching the native save branch.
- Initialisation/startup summaries use independent time/count baselines and report installed,
  disabled and unavailable features, including migration.
- No wholesale decomposition of `sand_spreader_diagnostic.h` and no gameplay rewrite.

### Retained contracts

- Runtime skill 35, two verified clear call sites, synchronous grit-spreader API.
- Fuel-proportional grit consumption, signed native fuel and stationary-only speed gating;
  no upper speed gate and no native fuel mutation.
- The reserve request keeps the real and the displayed remainder, then dry treatment at zero.
- The native home-refuelling destination latch owns return requests; the native AI owns route
  and target. A confirmed home refill debits the selected storage only once.
- Existing depot priorities/tanks and road protection sidecars remain compatible.
- Add-only storage migration after the native building-load call, with unchanged native
  signatures, deep-copy transaction, controls initialisation and worker lock.
- Core sampler, treatment creation, persistence, capacity and priority functions are
  unchanged. weather_roads and vanilla_buildings code remain unchanged.

### Removed development code

Broad numeric/linked-object/identity scans and reference-vehicle matching, manual F9-F12
markers and route trace snapshots, fixed per-road direct-depot consumption, the disabled
direct-refuel experiment, the compiler-proven unused task-fuel hook, the old home-boundary
hook and UI wrapper helpers. F8 remains a read-only diagnostic gated by `debug = 1`;
lifecycle tracking is not disabled with debug logging.

### Logging

Routine INFO events under Tank diagnostic, Grit diagnostic and Building lifecycle diagnostic
are opt-in. WARN/ERROR/FATAL and persistence/migration/configuration events are not filtered
by that switch. Shared SDK unused-function notices at /W4 are not gameplay faults.

### Verification

Tests use synthetic objects and isolated temporary save fixtures; no native game state is
executed or edited by the tests. A fresh in-game beta/long-duration test remains required.
