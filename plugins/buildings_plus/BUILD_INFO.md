# Buildings Plus – build notes

Target: WRSR 1.1.1.9, TesmioLoader API 4. Build: the standard line (`cl /O2 /MT /W3 /EHsc /std:c++17 /LD
... /link kernel32.lib`); exports TsmPluginApiVersion/TsmPluginInit/TsmPluginStart. Self-test: the same file with
`/DBUILDINGS_PLUS_TEST` as an executable, `buildings_plus_test.exe <fresh scratch folder>` builds a fake
game folder and checks generation, replacement rules, material rewrite, stamp, prune, refusal of foreign
folders, the object rename and the id catalog. `buildings_plus_test.exe --run <ini> <game folder> <out
folder>` generates the sections of a real INI from a real game folder into an out folder of your choice,
so a declaration can be checked before it goes into the game (the game folder is only read; the catalog
goes to `<out>\plugins`). History newest first.

## 0.1.3 (2026-09-13)

- `name =` may hold a localisation key instead of a caption. A value with at least one dot and
  nothing but letters, digits, dot, underscore and hyphen is handed to the Localization service; a
  resolved id is written as `$NAME <id>`, everything else stays `$NAME_STR "..."`. Exactly one of
  the two lines is ever written: the game's parser reads both into the same field and which one
  would win is not established. An unresolved key falls back to the part after the last dot and
  warns once, so a missing text never leaves a building without a name.
- Generation moved from `TsmPluginInit` to a new `TsmPluginStart`. A service only exists once every
  plugin's init has run, and buildings_plus loads before localization. Nothing here hooks the game,
  and the folders under `media_soviet\workshop_wip` are only scanned by the game much later.
- GENERATOR_VERSION 4, so every folder is rewritten once. The hash covers the name line that really
  goes into the file, so a key that resolves to a different id regenerates the folder.
- Three self-tests with a stand-in localisation service: a plain name stays literal, a key becomes
  `$NAME` with the id and never both lines, an unknown key falls back and warns once.

## 0.1.2 (2026-09-12)

- First published version. Fork of the buildings plugin from TesmioLoader (MaxLegend, GPL v3): a
  section per building names a donor under `media_soviet\buildings_types` and the building.ini lines
  that differ; at startup the plugin writes the complete Workshop item into
  `media_soviet\workshop_wip\<id>\` (mesh, material with `$TEXTURE_MTL` rewritten to `$TEXTURE
  buildings/...`, emissive material when the donor has one, collision box, fire points, icons,
  workshopconfig.ini, renderconfig.ini, building.ini). The same section format is what Soviet Mod
  Loader reads from a mod's `tesmio\buildings.ini`.
- `id =` is optional. A section without one gets the highest number seen in 9300000000..9399999999
  (catalog, declared ids, every folder under workshop_wip, foreign ones included) plus one; the range
  sits beside Soviet Mod Loader's 9100000000..9199999999. Assignments live in
  `plugins\buildings_plus.ids.ini` (`[ids] <section> = <id>`, lower-case section names) and are never
  removed or reused: a deleted section keeps its line, a renamed section is a new building, an explicit
  `id =` wins and is written into the catalog too; a section whose cataloged number is taken by another
  section's explicit id is renumbered with a warning. The catalog is written before generation, so a
  failed generation does not lose the number. Log line per assignment: `[x] assigned id N`.
- Changes against the original: configuration from `plugins\buildings_plus.ini`, otherwise the INI
  beside the DLL; detail log `logs\tesmioloader.buildings_plus.log` (verbose lines there, results in
  tesmioloader.log); `std::string`/`std::vector` instead of fixed buffers (256 sections, 512 lines,
  4096 characters per line); every value validated (id numeric in 9000000000..9999999999 and unique,
  donor/object plain names, name/desc without quotes, life 1..1000000, every line with a `$TOKEN`,
  strip a single token); files written to a temporary name and moved into place; a renamed object
  removes its stale subfolder; `prune = 1` removes stamped folders of this plugin whose section is gone
  or switched off; the hash also covers the donor's mesh and materials; wide-character file APIs so a
  game folder with non-ASCII characters works; `[buildings]` accepted as the switch section for INIs
  written for the original.
- A dropped donor line takes its data lines with it: the points of a `$CONNECTION_*` and the placement
  block of a `$RESOURCE_VISUALIZATION` (positon, rotation, scale, numstepx, numstept) have no token of
  their own and would otherwise stay behind as orphan lines when their token is replaced or stripped.
  A blank line, a separator line of dashes or a `;` line ends the block. Generator version 3.
- Init returns 0 with an empty registry (the plugin stays listed), 1 only with `enabled = 0` or when
  media_soviet cannot be found.

## Technical notes

- The folder is generated in `TsmPluginInit`: plugins are initialised from DllMain before the game's
  main thread runs, so a folder written then is found when the game lists `workshop_wip`. The loader's
  VFS cannot serve it, because the game enumerates that directory and the VFS redirects opens only.
- `tesmioloader.stamp` marks generated folders; a folder without it is never touched (id collision
  with a real subscription). The stamp carries a hash over the declaration, the generator version and
  the donor's ini/mtl/nmf stamps, so a folder is rewritten only when something changed.
- Replacement rules for donor lines: equal token; `$NAME`/`$NAME_STR`; every `$TYPE_*`; every
  `$STORAGE*` together with `$RESOURCE_VISUALIZATION`; `$PRODUCTION`/`$CONSUMPTION`/
  `$CONSUMPTION_PER_SECOND` as one recipe. The first `$TOKEN` anywhere in a line is the token, as the
  game's parser has no comment syntax. Piles can be declared again after new storages: `line =
  $RESOURCE_VISUALIZATION 0` followed by its five data lines as further `line =` entries (lines are
  emitted verbatim, in order).
- Republic Mod Manager profiles capture `plugins\*.ini`, so the id catalog travels with a profile; the
  uninstaller of the RMM package never removes it.
