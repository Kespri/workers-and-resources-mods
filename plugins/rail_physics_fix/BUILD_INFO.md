# Rail Physics Fix – build notes

Windows/TesmioLoader port of RailPhysics 1.3.0 by Meow Meow (TheRealMeowMeow00,
https://github.com/TheRealMeowMeow00/WRSR_RailPhysics, Workshop item 3776784867), GPL v3.
Target: SOVIET64.exe 1.1.1.9 (build 23935965), TesmioLoader API 4. History newest first.

## 1.3.4-beta (2026-09-07)

Trigger: a full source review against the original RailPhysics 1.3.0. Result of the review:
physics, decision logic, signatures, offsets, keys and defaults are identical; the port fixes
three ABI bugs of the original stubs (fifth argument of the brake helper, saved registers in
the callee's shadow space, xmm0 in the curve stub), a use-after-free in the corridor build,
unchecked reads and the locale-dependent `atof`. Criticism: in a few places 1.3.2 stalled the
train or dropped every zone where the original degraded. 1.3.4 restores the original's
behaviour there; no formula, hook or key changed:

- `ComputePhysics`: preflight through `ReadableFast` (region cache) instead of `ReadablePtr`
  (one VirtualQuery per wagon and frame); inactive wagons are skipped before their type is
  read (original lines 307-308). The brake helper and the curve cache check through the
  cache as well.
- `rp_curve_helper`: a non-finite native limit passes through (previously 0.0).
- `CollectStationNodes`: an unreadable chain or table -> `continue` instead of failing the scan.
- `BuildCorridors`: an allocation/capacity stop publishes the corridors found so far (as the
  original does) with warning RP103.
- `RescanStations`: when the rebuild fails, the previous complete tables stay (`RestoreZones`)
  instead of 30 s without zones.
- Route lookahead: unreadable segment, invalid length, unreadable end node, invalid polyline,
  non-finite point -> the walk ends with the points collected so far (original `break`),
  warning RP_ROUTE. Only allocation failures remain `CurveScanFailure` (last limit kept for the
  same origin).
- `SpanCount`: a remainder is truncated like the original's pointer division.

Offline tests (`tests\run_tests.bat`, 17 processes): `allocations` now checks per failure
site "previous tables kept or partial corridors published, no leak, retry succeeds"
(15 kept, 3 partial); new in `guards`: an inactive wagon with a dead type pointer is skipped,
an active one keeps the native calls away, a torn second route leg yields a partial lookahead
without a scan failure, a NaN limit passes through; `SpanCount(8,25,8) == 2`. Logs:
`verification\version-1.3.4-beta-build.log`, `verification\version-1.3.4-beta-tests.log`.

In-game test: NOT run. Proposed protocol (copy of a savegame):
1. Long freight train (>= 20 wagons) on a hill: start, climb, descent; check the log for
   RP100/RP101 (must not appear for healthy trains).
2. Train with uncoupled/inactive wagons in the depot and on the line.
3. Station approach with `log_decisions = 1`: braking band visible (planned), no pulse trains.
4. Large map, 30 min fast forward: `subsystem(s) patched`, count RP103/RP104/RP105, compare
   frame time against 1.3.3.
5. Customs approach: corridors present (`log_curves = 1`), entry at customs_entry_kmh.

Backup of the previous state: `_backups\rail_physics_fix_1.3.3-beta_before_1.3.4-beta_*`.

## 1.3.3-beta (2026-09-07)

Only change to the plugin: the choice of the configuration file. `ResolveConfigFile` checks
`plugins\rail_physics_fix.ini` below the loader base directory; when the file exists, every
read goes through `H->configInt`/`H->configString` with the unchanged file name as before.
When it is missing, `rail_physics_fix.ini` beside the DLL (GetModuleHandleEx on the DLL's own
address) is read with GetPrivateProfileIntA/GetPrivateProfileStringA and the same trim the
loader uses (`CfgInt`/`CfgString`). Keys, code defaults, RP201/RP202, the grid_boost rule,
hooks, bridges and physics are unchanged. New log line
`rail_physics_fix  configuration file: <path>`.

Offline tests: `tests\run_tests.bat` now creates `build\plugins\rail_physics_fix.ini` (the
offline host names `build` as its base directory, so the classic INI lives below plugins\
there) and additionally runs the `beside_dll` scenario (base directory without a plugins\
INI, no host read call, supplied values from the INI beside the test executable).
17 processes. Version check in the direct and DLL-export paths switched to `1.3.3-beta`.

Workshop package: `My Plugins\rail_physics_fix` (soviet.mod.ini with local_copy,
presentation schema with four tabs, DE/EN, READMEs per template, LICENSE GPL v3).
Backup of the previous state: `_backups\rail_physics_fix_1.3.2-beta_before_1.3.3-beta_*`.

## Rename to rail_physics_fix (2026-09-05)

Since 2026-09-05 the plugin is called `rail_physics_fix` to keep it apart from the stock
`rail_physics`. Renamed: plugin name, DLL, INI, detail log (`tesmioloader.rail_physics_fix.log`),
log prefix, the sources `rail_physics_fix.cpp`, `rail_physics_fix_support.h`,
`rail_physics_fix_stubs.h/.S`, `tests\test_rail_physics_fix.cpp`, build scripts, tools and
READMEs. Unchanged: the INI section `[railphysics]` (compatibility with the original), the
version 1.3.2-beta and the physics. The sections and hash tables below refer to the file
names before the rename.

## 1.3.2-beta (2026-09-03)

Built on 2026-09-03, Windows x64. Pure version relabel from `1.3.2-tesmio-beta` to
`1.3.2-beta`, no change to physics or settings. In the production source only the header
line and `kRailVersion` changed. The loader interface stays API 4; bridges, support header
and SDK unchanged.

Re-verified:

- Release build with `build.bat` succeeded (MSVC 14.52.36615, x64, `/MT`).
- All 16 processes of `tests/run_tests.bat` succeeded, including an explicit check of
  `TsmPluginInfo.version == "1.3.2-beta"` in the direct and the DLL-export path.
- Compatibility build with the root build flags succeeded.
- Exports still `TsmPluginApiVersion`, `TsmPluginInit`, `TsmPluginStart`; the only DLL
  dependency is still `KERNEL32.dll`.
- INI content unchanged apart from the version comment; verified separately for the source
  project, the desktop build and the installed game configuration.

New logs: `verification/version-1.3.2-beta-build.log`,
`verification/version-1.3.2-beta-tests.log`, `verification/version-1.3.2-beta-root-build.log`.
The tests run with synthetic data and a private data copy of the executable; no game code is
executed. No new in-game test of this relabel.

| File at that time | SHA-256 |
|---|---|
| `rail_physics.dll` | `E9B4CBF46BF40E12AD9124AEFC3B36705E535808FAA7B3AF28235CC05A13DB01` |
| `rail_physics.cpp` | `77B8242FD403FAD89C883C4231E71A3AA8A77CA35F2BD4CC094BCA6C10A6FF15` |
| `rail_physics.ini` | `C2ED544B7AC5987B5AF90AB1730D9F7958DB69B18936F1FF8297C2C54099B105` |

## Historical evidence of 2026-09-02 (unchanged)

The following statements and the logs not starting with `version-1.3.2-beta-` document
that build only, not the current DLL.

Built on 2026-09-02, Windows x64, version `1.3.2-tesmio-beta`. No running game was changed.
No file was installed into the active game/loader folder. No in-game test of this port yet.

### Toolchain

- Microsoft C++ / linker 14.52.36615, x64, static runtime (`/MT`).
- Release: `/std:c++17 /utf-8 /O2 /MT /W4 /wd4505 /EHsc /LD`.
- `4505` concerns unused static SDK helper functions; no other warning category was
  disabled for this build.
- Separate compatibility build with the normal root build flags `/O2 /MT /W3 /EHsc /LD`:
  succeeded, without an additional assembler link step.
- Clang 22.1.3 only to generate/verify the bridges and for the test harness.
- DLL dependency: `KERNEL32.dll`. No additional MinGW/Clang runtime DLL.
- Exports: `TsmPluginApiVersion`, `TsmPluginInit`, `TsmPluginStart`; API 4.

### Checks

All 16 test processes of `tests/run_tests.bat` succeeded. The tests use synthetic
vehicles/tracks and a data copy of the executable, no game functions. The real DLL export
path is exercised in an artificial host.

- All twelve source signatures verified: eleven unique hits; two references of the twelfth
  signature agree on the same global address.
- Five replaced instruction blocks, three function calls and two branch targets verified.
- All five executable bridges tested; 996 bytes, 15 internal symbols.
- Forces/braking/consumption, straight/curved route, station and stop parabola verified.
- Customs corridor with 1,100 nodes, 1,099 segments and several hash rehashes verified.
- Wrong version, broken/ambiguous signatures, duplicate DLL and error paths verified.
- Four unchanged cache/table functions preserved in the source comparison.
- Numeric comparison with 1.3.1: 5,120 valid input cases, 62,464 values; bitwise result
  checksum of both programs: `E927BA1BB84487F7`.
- 18 failing allocation sites of the table test case, release of every involved buffer and
  successful restart verified.
- Guard pages, NaN/overflow, route allocation failure with cache retention, exactly one
  native fuel call, warning throttling and Win32 errors verified.
- All 30 supplied INI values unchanged.

The message `only 3/9 requested patches installed` in the test log belongs to the
**deliberately simulated failure** of all inline hooks. In the regular offline test 9/9 are
installed. The test verifies that after a partial installation the DLL is not unloaded and
the slope branch is not switched incompletely.

Detailed logs: `verification/build.log`, `verification/tests.log`,
`verification/root-build.log`, `verification/game-verification.log`,
`verification/parity.log`, `verification/stubs.log`, `verification/source-parity.log`.

### SHA-256

| File | SHA-256 |
|---|---|
| Shipped `rail_physics.dll` | `ACE4EED6C0E57780E4ECF0F08F6D033A8E9F004481748E06196DACE6A31DECD1` |
| `rail_physics.cpp` | `0D4D898D17C7DD8F864D9D041CC1153F8FCE0CB28CFF4C94A0AB9D0154B55075` |
| `rail_physics_support.h` | `04C5BF42699536FB44428A4BFAF573EB06912813CD700E41CD4BAAF0B4E37A83` |
| `rail_physics.ini` | `D61C22946A9BA0503977580A0343DEAE2BE0715FEED43E6EA531A06E9F3B8DB8` |
| `rail_physics_stubs.h` | `3516D15E6CDA89216B462AC800EFA37824424E19BF78ECBFCEAB28D556D168D5` |
| Original `railphysics.dll` | `EE54F6430BF0773CCD79FD7452B108B364F8778326EB6D3D3342863A36B9FCC7` |
| Original `railphysics.cpp` | `7F40A955B2CA54E448650C669BFDC9AFAD78F94BA4D15B05C34A1B08D3D19340` |
| Original `railphysics.ini` | `F22BE07D5AF977B4F3E7D71A7DADE0AECE11369680FADE1DEEE82B06E1FB7981` |
| Verified `SOVIET64.exe` | `296644A9F207D609031FC2AE73FED2DCB34619A1D55A35D1C7B51965CE6841B8` |

The verified executable has PE timestamp `0x6A3EB6AD` and ImageSize `0xA9D000`. The headers
match the TesmioLoader SDK available at build time byte for byte. The executable, the
original DLL and the original source are not distributed as part of this plugin folder.

### Assessment

The added safeguards act on failed allocations, invalid memory ranges/numbers or Windows
API errors. Valid physics and route data are processed with the previous formulas. A failed
zone build switches those additional zones off until the next successful 30-second scan;
partial states and unverified old world pointers are not reused. (Superseded in 1.3.4:
the previous tables are kept.)

Time base, simulation-tick detection, object lifetime/caches, INI policy, scan budgets and
hook grouping were not fundamentally rebuilt. The direct numeric comparison and the offline
tests do not replace a test in the running game.
