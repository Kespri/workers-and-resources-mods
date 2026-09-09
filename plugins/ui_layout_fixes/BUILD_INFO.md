# UI Layout Fixes – build notes

- Plugin version 1.2. Target: SOVIET64.exe 1.1.1.9, TesmioLoader API 4.
- Source folder: `my_plugins\ui_layout_fixes\`. Build: `build.bat` (Microsoft Visual C++ x64,
  `/O2 /MT /W3 /EHsc /LD`, kernel32.lib). Output: `build\plugins\ui_layout_fixes.dll` and
  `build\plugins\ui_layout_fixes.ini`.

## 1.2 (2026-09-09) - VEHICLE_ROUTE_HINT module

- New INI section `[vehicle_route_hint]`: `enabled` (1), `text_id` (1970, 1..100000), `max_chars`
  (58, 20..200), `max_lines` (4, 0..12; 0 = no limit). Whole numbers are validated like the
  decimal row pitch (`invalid-config` warning, fallback to the default).
- Hook: the import `C3DDLL64.dll!?GetString@C3D_LANGUAGE@@QEAAPEA_WH@Z` in the IAT of
  SOVIET64.exe through the host's `patchIat`. The loader hands back the previous slot value, so
  the hook chains with other plugins on the same slot (the resources plugin hooks it too).
  No executable code is changed and the module does not run the build check.
- Detour: for `text_id` the original string is copied into a static 1024-wchar buffer and
  word-wrapped: greedy wrap at `max_chars`, then per paragraph the narrowest width that keeps
  the same line count (balanced lines), the game's own line breaks kept as paragraph breaks,
  width widened in steps of 4 while the line count exceeds `max_lines`. Rebuilt only when the
  game returns a different string (language switch); guarded by `g_lock`; one INFO line per
  rebuild. Texts longer than the buffer pass through unchanged (`text-length` warning once).
- Start: a failed IAT patch is an ERROR (`iat-patch`), the module stays inactive, Start still
  returns 0; the summary line lists `active modules=CUSTOMHOUSE+VEHICLE_ROUTE_HINT`.
- Offline check of the wrap with the real texts of id 1970 (`sovietGerman.btf` /
  `sovietEnglish.btf`, big-endian tables, UTF-16BE payload): DE 74/76 chars -> 32/40/37/38,
  EN 50/66 chars -> 49/32/35 at `max_chars = 58`. In-game test: pending (user).

## Configuration (1.1, `my_plugins\tesmio_config.h`)

- base: `<loader>\plugins\ui_layout_fixes.ini`, otherwise the INI beside the DLL
- overlay: `<loader>\user_config\ui_layout_fixes.ini`, key by key, if present
- Both paths are logged at start. The DLL never writes either file.

## Verified executable identity

- PE TimeDateStamp `0x6A3EB6AD`, SizeOfImage `0x00A9D000`
- Reference SHA-256 `296644A9F207D609031FC2AE73FED2DCB34619A1D55A35D1C7B51965CE6841B8`

## CUSTOMHOUSE module

| Site | Address |
|---|---|
| Panel | `exe+0x71B410` |
| Resource-list measurement | `exe+0x71C475` |
| Resource-list drawing | `exe+0x71C4B5` |
| Native resource-column helper | `exe+0x7C6720` |
| Native row-pitch instruction | `exe+0x7C678C` |

The two CUSTOMHOUSE calls are redirected through one near bridge. The shared native row-pitch
operand is redirected to plugin-owned nearby storage whose default is the original 25.0f.
Only either verified CUSTOMHOUSE call changes that storage temporarily. Native measurement,
downstream vertical positioning and scroll-range propagation remain in control.
