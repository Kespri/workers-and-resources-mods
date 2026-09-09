# Research Expansion – build notes

TesmioLoader plugin (API 4) for WRSR 1.1.1.9, GPL v3. Build: the standard line of the root
`build.bat` (`cl /O2 /MT /W3 /EHsc /std:c++17 /LD`, kernel32.lib) compiles `research_expansion.cpp`;
it includes `src/tesmio_plugin.h` and consumes the `localization` service of the Localization
plugin. Output: a generated `research.ini` plus one `<id>.png` per new research in the loader's VFS
(`<loader>\vfs\media_soviet\research`). User documentation: README_DE.md / README_EN.md.
History newest first.

## 1.8 (2026-09-10)

- `[research:]` sections: `name` / `desc` are optional and may be a bare word. `ExpandResearchSections`
  completes a value without a dot to `TEXT_NAMESPACE.<value>.name` / `.desc`
  (`TEXT_NAMESPACE` = "research_expansion", the namespace of the plugin's own text pack); a
  missing key uses the research id. A value with dots is emitted unchanged, so 1.7 files keep
  working. Free `$RESEARCH` blocks are untouched.
- Reason (user decision 2026-09-10): the namespace is fixed by the modder and the suffixes are
  fixed by the plugin, so typing the full key was pure error potential. Republic Mod Manager
  0.4.31 shows the fields as `[<word>].[name]` with the id as placeholder.
- In-game test: pending (user).

## 1.7 (2026-09-10)

- `[research:<id>]` sections: the INI form of a new research block for Republic Mod Manager.
  `ValidateGeneralConfigLayout` collects them (`ResearchSection`, keys parsed by
  `ParseResearchKey`: enabled, type, cost, name, desc, requires, unlock, line);
  `ExpandResearchSections` turns the enabled ones into `NewBlock` line lists after `ParseNewBlocks`
  (free blocks first, then sections in INI order), each line carrying the INI line of its key, so
  `ValidateNewBlock`/`ValidateAllNewBlocks` check both forms with the same rules and messages.
- `requires = <dependency> | before/after/normal | <anchor>` becomes `+<dependency>` plus
  `@before_<anchor>` / `@after_<anchor>`; the position belongs to that dependency, as in blocks.
- Duplicate ids between a free block and a section are rejected (`research-duplicate`); unknown
  keys (`research-key`), bad values and repeated single keys fail closed like everything else.
- Unknown-section message now names `[research:id]`. INI and READMEs document the section form.
- In-game test: pending (user, through Republic Mod Manager 0.4.29).

## 1.6 (2026-09-09)

- The VFS research folder is the only icon store. `PlanIcons`: an existing `<id>.png` there is
  kept (validated as 128 x 128 PNG, an unusable one fails closed with `icon-invalid`); a missing
  one is seeded from `<id>.png` in `research_expansion\icons` beside the DLL, else created from
  `noimage.png` (`icon-fallback` WARN). `noimage.png` candidates in order: `research_expansion\icons`
  beside the DLL (Workshop package or local copy), `plugins\research_expansion\noimage.png`,
  `plugins\research_expansion\icons\noimage.png` (1.5 layout). `ApplyIconPlan` skips kept icons.
- `[general] icon_folder` and `noimage_name` are no longer used. Both keys stay accepted by the
  strict INI layout check so existing effective INIs keep loading; a value other than the 1.5
  defaults logs one `legacy-key` WARN. Removed from the shipped INI, the RMM schema and the READMEs.
- Log lines: `Icon store: <path>`, `Fallback icon: <path or reason>`, `Icon for <id> kept from the
  VFS research folder`, `Icon for <id> created from noimage.png|the package icon`.
- Reason (user decision 2026-09-09): keeping user icons in the Workshop package or in
  `plugins\research_expansion\icons` meant a second copy of every icon and a folder Steam may
  replace on update; the game reads from the VFS folder anyway.
- Package: schema fields for the two keys removed; the card "Research icons" is kept for the
  folder/file rows that Republic Mod Manager 0.4.28 adds. Version 1.6 until the user names the
  final number of the text round.
- In-game test: pending (user).

## 1.5 (2026-09-07)

- INI and icon fallback beside the DLL (Workshop package under SML or the Workshop Bridge),
  editor schema shipped in the package. Validation and generation unchanged from 1.4.
