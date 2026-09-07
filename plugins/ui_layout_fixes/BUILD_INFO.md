# UI Layout Fixes – build notes

- Plugin version 1.1. Target: SOVIET64.exe 1.1.1.9, TesmioLoader API 4.
- Source folder: `my_plugins\ui_layout_fixes\`. Build: `build.bat` (Microsoft Visual C++ x64,
  `/O2 /MT /W3 /EHsc /LD`, kernel32.lib). Output: `build\plugins\ui_layout_fixes.dll` and
  `build\plugins\ui_layout_fixes.ini`.

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
