# UI Layout Fixes – build notes

- Plugin version 1.3. Target: SOVIET64.exe 1.1.1.9, TesmioLoader API 4.
- Source folder: `my_plugins\ui_layout_fixes\`. Build: `build.bat` (Microsoft Visual C++ x64,
  `/O2 /MT /W3 /EHsc /LD`, kernel32.lib). Output: `build\plugins\ui_layout_fixes.dll` and
  `build\plugins\ui_layout_fixes.ini`.

## 1.3 (2026-09-09) - TEXT_WRAP module (replaces VEHICLE_ROUTE_HINT)

- Why: the user wants the wrap to cover any caption, not one hard-wired id. The 1.2.1 design
  patched two call sites of the vehicle window and could not grow. 1.3 keeps the wrap logic and
  moves the drawing side to the import table, so a new id only needs an INI line.
- INI: `[text_wrap]` enabled (1), max_chars (58, 20..200), max_lines (4, 0..12), line_spacing
  (1.15, 0.50..3.00, multiple of the font size), keep_breaks (0), log_long_texts (0, 0..400);
  `[text_wrap_ids]` with `<id> = <chars>` lines (0 = default width, else 20..200), up to 64
  entries, read from the user_config overlay as a whole when it has the section, otherwise from
  the base INI (`GetPrivateProfileSectionA`, step measured before the `=` cut). A missing
  section keeps 1970 as the one entry. `[vehicle_route_hint]` is no longer read.
- Hook 1 (unchanged idea): `C3D_LANGUAGE::GetString` via the IAT; a table of entries with one
  source and one wrapped buffer each (`g_wrapped[64][1024]`, contiguous). With
  `log_long_texts > 0` every id seen once whose longest line exceeds the value is logged with
  its first 80 characters (bitset of 65536 ids).
- Hook 2 (new): the six print imports SOVIET64.exe has from C3DDLL64.dll -
  `C3D_FONTMANAGER::PrintLeftUnicode/PrintCenterUnicode/PrintRightUnicode/PrintLeftUnicodeNoArg`
  and `C3D_FONT::PrintLeftUnicode/PrintRightUnicode` - are redirected through generated stubs
  in one RWX page (59 bytes each, `mov rax,[rsp+0x30|0x28]` = the text pointer, two `mov r10,
  imm64 / cmp rax,r10` range checks against `g_wrapped`, `jmp` to the handler or to the
  original read from the slot before the patch). Encoding checked against ml64 (`cmp` uses
  the 4C 39 D0 form, ml64 the 49 3B C2 form; same operation). The stubs never touch the
  stack, so the variadic originals get every argument as the caller built it. Handlers take
  the fixed arguments (floats from xmm1..3 as the non-variadic prototype expects; the call
  sites duplicate them in r8/r9 too), split at '\n' and print each line through the original
  with `L"%ls"` (NoArg: the line itself), y advanced by `C3D_FONT::GetSize(font) *
  line_spacing` (export resolved with GetProcAddress; fallback 16 when missing or absurd).
  The first wrapped print logs font size and step for calibration.
- The two call-site patches and the build check of 1.2.1 are gone; the module no longer
  depends on the executable build, only on the text ids.
- Failures: stub page or all print imports failing is an ERROR and the module stays inactive;
  a single import missing is logged and skipped; caption hook failure after the print hooks is
  an ERROR (nothing wrapped). Start still returns 0 in every case.
- RMM package: keyed_list editor (`editor_type = keyed_list`, list section `text_wrap_ids`,
  column `chars`, `id_suggestions = 0`), tabs General / Customs house / Text wrap / Text ids;
  `user_overlay` dropped from the manifest because the list editor writes the effective INI.
  Needs RMM 0.4.20 (id_suggestions, card notice_style).
- In-game test: pending (user). Points to watch: font size / step line in the detail log,
  the route hint as three lines, other windows unchanged.

## 1.2.1 (2026-09-09) - VEHICLE_ROUTE_HINT module

- New INI section `[vehicle_route_hint]`: `enabled` (1), `text_id` (1970, 1..100000), `max_chars`
  (58, 20..200), `max_lines` (4, 0..12; 0 = no limit), `line_height` (18.0, 8.0..40.0 logical px),
  `keep_breaks` (0). Whole numbers and the decimal are validated like the row pitch
  (`invalid-config` warning, fallback to the default).
- Why 1.2 was not enough: the vehicle window draws the hint with
  `C3D_FONTMANAGER::PrintLeftUnicode(font, x, y, colour, format, ...)` (import slot
  `exe+0x86C880`), a single-line print that drops '\n'. The 1.2 in-game test showed the wrapped
  text as one line with the words at the break positions glued together ("einmögliches").
  The engine exports no wrapping print (`C3DDLL64.dll` exports: PrintLeft/Center/RightUnicode
  on C3D_FONT and C3D_FONTMANAGER, PrintLeftUnicodeNoArg, CalcRect* for bitmap fonts only).
- Patch 1, caption: the import `C3DDLL64.dll!?GetString@C3D_LANGUAGE@@QEAAPEA_WH@Z` in the IAT
  of SOVIET64.exe through the host's `patchIat` (chains with other plugins on the same slot,
  the resources plugin hooks it too). For `text_id` the string is copied into a static
  1024-wchar buffer and word-wrapped: the game's own line breaks become spaces unless
  `keep_breaks = 1`, greedy wrap at `max_chars`, then per paragraph the narrowest width that
  keeps the same line count (balanced lines), width widened in steps of 4 while the line count
  exceeds `max_lines`. Rebuilt only when the game returns a different string; guarded by
  `g_lock`; one INFO line per rebuild.
- Patch 2, print: the two `FF 15 disp32` calls of PrintLeftUnicode in the vehicle window panel,
  `exe+0x7DE5F8` (route hint, preceded by `mov edx,1970 / lea rcx,language / call [GetString]`
  at `exe+0x7DE5A0`, verified as 18-byte signature) and `exe+0x7DE6D5` (route status row,
  ids 0x7AD, 0x7B2, 0xB12..0xB1F), become `E8 rel32 90` to a near bridge (`allocNear`,
  `mov rax,imm64 / jmp rax`) that lands in `PrintLeftLines`. The detour keeps the variadic
  prototype (floats duplicated in r8/r9 and xmm2/xmm3, checked in the DLL disassembly), prints
  each line through the original slot value with `L"%ls"` and `y + line_height * ui_scale *
  index`; `ui_scale` is the float at `exe+0x992088` that every offset of the window is
  multiplied with (label rows advance by 20 * scale, `.rdata` 0x90A928). Texts without '\n'
  pass through unchanged, so the status messages are untouched.
- Order in Install: build check (image size, timestamp, three signatures), print redirection,
  then the caption hook. If the caption hook fails after the print patch, the native two-line
  text is still drawn as two lines. Any failure is an ERROR, the module stays inactive, Start
  still returns 0; the summary lists `active modules=CUSTOMHOUSE+VEHICLE_ROUTE_HINT`.
- Offline check of the wrap with the real texts of id 1970 (`sovietGerman.btf` /
  `sovietEnglish.btf`, big-endian tables, UTF-16BE payload): DE 74/76 chars -> 3 lines with
  merged paragraphs at `max_chars = 58`, EN 50/66 -> 2. In-game test 2026-09-09: confirmed by the user (German, three lines drawn downward, text ends above the gauges).

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
