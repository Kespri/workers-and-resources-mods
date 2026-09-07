# Technical Service Storage v0.3.1-beta

v0.3.1-beta adds one path decision to `LoadConfigFile` (config_validation.h): when
`plugins\technical_service_storage.ini` does not exist, the INI beside the DLL is read
instead (`ConfigOwnDirectory`, GetModuleHandleEx on the DLL's own address). The chosen
path is logged as `ini-path`. Parsing, validation, materials, runtime and sidecar
formats are byte-for-byte the 0.3.0 logic. The Workshop package (`My Plugins\
technical_service_storage`) ships an RMM keyed_list schema for `[grit_materials]`.

# History: v0.3.0-beta

v0.3.0-beta unifies plugin metadata, grit-spreader diagnostics and documentation
under one version label. SPREADER_DIAGNOSTIC_VERSION now aliases PLUGIN_VERSION.
Game behavior, INI settings, localization/service API versions and priority/tank
file-format versions remain unchanged.

History: v0.1.78 renamed the material section to grit_materials. ParseConfigText
normalizes the legacy Streumaterialien spelling before duplicate detection;
the first section still wins even when the two spellings are mixed. All
catalogue entry validation, scalar schema, material order, runtime logic and
sidecar identity/format semantics are unchanged from v0.1.77.

Earlier hardening in v0.1.77 was based on v0.1.76-beta; no save format or gameplay balance
change. Build with MSVC x64, C++17, /O2 /MT /W4 /EHsc, Loader API 4.

## Hardening boundaries

- Central startup-only configuration schema, complete integer validation,
  independent C numeric locale, bounded complete file read, per-line warnings.
  Missing optional keys retain defaults. First scalar/section declaration wins;
  materials retain the first valid declaration and their file order.
- Explicitly unusable material configuration suspends grit, migration and
  priority/tank persistence writes; existing native storage/sidecars are retained.
  Native UI remains available. No invalid-config implicit sand replacement.
- Localization checks host table size, consume, resolver and reserved IDs only
  for plugin-owned keys. Native text IDs and built-in fallback rendering remain.
- Runtime warning causes are independent of debug budget (one per minute/cause).
  Detail writes serialize whole lines and handle short writes. On disk failure,
  report once through the host outside the lock, without recursive file logging.
- Windows hook preparation rejects before publishing a branch on failure.
  Once patched, protection/cache errors mark health degraded and retain the DLL.
  Original save target is published before patching the native save branch.
- Initialization/startup summaries use independent time/count baselines and
  report installed, disabled and unavailable features, including migration.
- No wholesale decomposition of sand_spreader_diagnostic.h or gameplay rewrite.

## Retained contracts

- Runtime skill 35, two verified clear call sites, synchronous grit-spreader API.
- Fuel-proportional grit consumption, signed native fuel and stationary-only
  speed gating; no upper speed gate and no native fuel mutation.
- Reserve request keeps real and displayed remainder, then dry treatment at zero.
- Native home-refuelling destination latch owns return requests; native AI owns
  route and target. Confirmed home refill debits the selected storage only once.
- Existing depot priorities/tanks and road protection sidecars remain compatible.
- Add-only storage migration after the native building-load call, with unchanged
  native signatures, deep-copy transaction, controls initialization and worker lock.
- Core sampler, treatment creation, persistence, capacity and priority functions
  are unchanged. weather_roads and vanilla_buildings code remain unchanged.

## Removed development code

Broad numeric/linked-object/identity scans and reference-vehicle matching,
manual F9-F12 markers and route trace snapshots, fixed per-road direct-depot
consumption, disabled direct-refuel experiment, compiler-proven unused
task-fuel hook, old home-boundary hook and UI wrapper helpers.
F8 remains a read-only diagnostic gated by debug=1; lifecycle tracking is not
disabled with debug logging.

## Logging

Routine INFO events under Tank diagnostic, Grit diagnostic and Building
lifecycle diagnostic are opt-in. WARN/ERROR/FATAL and persistence/migration/
configuration events are not filtered by that switch.
Shared SDK unused-function notices at /W4 are not gameplay faults.

## Verification

See the bundle's TEST_REPORT.md, tests and build_tests.bat. Tests use synthetic
objects and isolated temporary save fixtures; no native game state is executed
or edited by the tests. A fresh in-game beta/long-duration test remains required.
