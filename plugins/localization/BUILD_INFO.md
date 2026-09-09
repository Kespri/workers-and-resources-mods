# Localization – build notes

TesmioLoader plugin (API 4) for WRSR 1.1.1.9, GPL v3. Build: the standard line of the root
`build.bat` (`cl /O2 /MT /W3 /EHsc /std:c++17 /LD`, kernel32.lib) compiles `localization.cpp`;
it includes `src/tesmio_plugin.h` and provides the `localization` service (key -> text id in the
reserved range 2,000,000..2,999,999). Output: extended `soviet<Language>.btf` overlays in the
loader's VFS (`<loader>\vfs\media_soviet`), removed again on a start with `enabled = 0`.
User documentation: README_DE.md / README_EN.md. History newest first.

## 1.3 (2026-09-10)

- Pack folders of the same name are merged instead of shadowed. `LoadPacks` loads the packs
  beside the DLL first, then `plugins\localization`; `MergePack` lays a local folder over the
  shipped one key by key (local `localization.ini` decides namespace, fallback and missingText;
  local language files add or replace keys; shipped-only languages stay). The fallback/text checks
  moved from `LoadOnePack` into `FinishPack`, which runs after the merge, so a local folder that
  carries only a new language plus its `localization.ini` is valid.
- Log: `Pack '<folder>': local folder merged over the shipped one (<n> keys added, <m> replaced,
  <k> local language file(s))`; a namespace mismatch between the two is a WARN, the local one applies.
- Reason (user decision 2026-09-10): Republic Mod Manager will edit the research_expansion pack in
  `plugins\localization`; a package update by the author must still reach players with local edits.
- In-game test 2026-09-10 01:17 (user): log "Pack 'research_expansion': local folder merged over the shipped one
  (4 keys added, 4 replaced, 2 local language file(s))", 3 packs / 14 keys ready; the local clay_study texts
  showed in the game.

## 1.2 (2026-09-07)

- INI and pack folder fallback beside the DLL; two pack folders (plugins\localization and
  localization beside the DLL) with the local folder winning; editor schema in the package.
