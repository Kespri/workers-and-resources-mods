# Republic Mod Manager – Package and schema reference

**English** | [Deutsch](SCHEMA_DE.md)

This is the reference for plugin authors: what a Workshop package needs so that Republic Mod Manager (RMM) lists it, and how a settings page is described. Neither the manifest nor a schema contains executable commands; everything is plain INI text.

---

## Contents

1. [The manifest (`soviet.mod.ini`)](#1-the-manifest-sovietmodini)
2. [Settings page without a schema](#2-settings-page-without-a-schema)
3. [Presentation schema (`<name>.launcher.ini`)](#3-presentation-schema)
4. [Collections](#4-collections)
5. [Local schemas for installed plugins](#5-local-schemas-for-installed-plugins)
6. [Editor schemas: lists and sections](#6-editor-schemas)
7. [Reference checks](#7-reference-checks)
8. [Languages](#8-languages)
9. [Icons and path safety](#9-icons-and-path-safety)
10. [How RMM provisions a package](#10-how-rmm-provisions-a-package)

---

## 1. The manifest (`soviet.mod.ini`)

The smallest valid manifest is the one of the Soviet Mod Loader:

```ini
[mod]
id = example.my_plugin
name = My Plugin
version = 1.0.0

[hooks]
dll = hooks\my_plugin.dll
```

From it RMM derives: the target `my_plugin` (the DLL file name), the default INI `hooks\my_plugin.ini` next to the DLL (if present), the personal configuration `user_config\my_plugin.ini`, and the schema `config\my_plugin.launcher.ini` (if present, otherwise generated from the INI).

Exactly one `dll` line is allowed. Repeated `dll` lines, path escapes, duplicate ids and a `target` that does not match the DLL name are refused with an exact reason.

Optional overrides:

```ini
[mod]
enabled = 1
tesmio_api_min = 4
tesmio_api_max = 4

[configuration]
defaults = hooks\my_plugin.ini          ; default INI
launcher_schema = config\my_plugin.launcher.ini
user_config = my_plugin.ini             ; file name only, lies next to the DLL
user_overlay = 0                        ; 1: the DLL reads user_config\<target>.ini itself
local_copy = 0                          ; 1: offers "Files local only"

[assets]
dir = hooks\my_plugin                   ; folder that travels with the DLL on "Files local only"

[autoload]
format = 1
kind = plugin
target = my_plugin
requires_local = resources|other_plugin
conflicts_local = old_name|duplicate_name

[dependencies]
other.plugin = >=1.2.0
```

- `tesmio_api_min` / `tesmio_api_max`: if given, API 4 must be in range.
- `user_overlay = 1`: the DLL merges `user_config\<target>.ini` over its INI itself. RMM then provides the original INI unchanged and writes personal values only to `user_config`.
- `local_copy = 1`: the "Notes" card offers "Files local only" for packages that run through the Workshop Bridge. RMM copies DLL, INI and the `[assets] dir` folder to `plugins\` (the folder under its own name, `plugins\my_plugin\...`). The asset folder may not contain `.dll` or `.exe` files, at most 512 files of 64 MB each, no reparse points.
- `[dependencies]`: `mod.id = <condition>` with `>=`, `>`, `=`, `<=`, `<`, a bare version (minimum) or `*`. Resolved against the packages in the Workshop folder; a classic plugin `plugins\<name>.dll` also counts when `<name>` is the last part of the id (version unchecked). A hook package in the bridge list counts as satisfied. Unmet dependencies block provisioning.
- `[content]` without `[hooks] dll` is a pure content package: listed, never provisioned, applied only by the Soviet Mod Loader.

---

## 2. Settings page without a schema

Without a schema RMM builds one from the default INI:

- every INI section becomes a card on the "General" tab;
- every key becomes a field, its name is the label;
- the comment block directly above the key and a comment behind the value become the description; line breaks are kept;
- the type follows the value: `0`/`1` becomes `boolean`, unless the key name names an amount (`count`, `days`, `frequency`, `size`, `limit`, `level`, `scale`, `mode` and similar) or the comment names other whole numbers or a range (`2-12`, `1..6`, "up to 400"); version strings such as `v1.6` do not count. Names such as `enabled`, `debug`, `use_*`, `*_enabled` or a comment naming both states force `boolean`. Other whole numbers become `integer`, decimals `decimal`, everything else `text`;
- a key `enabled` of type `boolean` becomes the activation field;
- the comment block at the start of the file becomes the package description.

In this mode a local INI may contain keys the package INI does not know; the strict key check applies only with a schema. A comment behind the value, such as `130 ; (stock 121)`, stays in the file until the value changes.

---

## 3. Presentation schema

`config\<name>.launcher.ini` describes tabs, cards, fields and texts. Every effective key of the INI must be described as a field or a collection element.

### 3.1 `[launcher]`

```ini
[launcher]
layout_version = 1
visible = 1
id = example.my_plugin
name = My Plugin
config = my_plugin.ini
description = Fallback text
description_key = plugin.description
language_directory = config\languages
icon = builtin:gear
enabled_field = general/enabled        ; optional; boolean field for the activation switch
default_tab = general
maximum_value_length = 63              ; default 4096
notice = ...                           ; package-wide box in the Notes card of the first tab
notice_key = plugin.notice
notice_style = warning                 ; warning (yellow) or info (blue)
info = ...                             ; second, always blue box below the notice
info_key = plugin.info
```

`\n` in any description, notice or help text is a line break. A path such as `plugins\needs.ini` in such a text is therefore torn apart; write `/` there.

### 3.2 Tabs and groups

```ini
[tab:general]
label = General
label_key = tab.general
order = 10

[group:general]
tab = general
label = General settings
label_key = group.general
description = ...
description_key = group.general.description
layout = fields                        ; or matrix
order = 10
```

There is no fixed number of tabs. Fields without a group land in the shared area `settings`, shown as "General". Every explicit tab and group reference must exist. A group without visible fields and without texts is not drawn.

A group with `layout = matrix` arranges its fields by `row`, `column`, `row_label`, `column_label`, `unit` and `icon`; the `_key` variants translate. Duplicate cells are invalid.

### 3.3 Fields

```ini
[field:limit]
section = general
key = limit
label = Limit
label_key = field.limit
description = What it changes in the game.
description_key = field.limit.description
type = integer
minimum = 0
maximum = 10000
step = 1
group = general
order = 20
```

Types:

| Type | Value |
|---|---|
| `boolean` | `0` or `1`, shown as a switch |
| `integer` | whole number within `minimum`/`maximum`; `step` sets the plus/minus step (default 1) |
| `decimal` | finite decimal within the limits; `step` default 0.1 |
| `choice` | exactly one of `choices = a|b|c` |
| `text` | free text |
| `readonly` | shown, not edited |

Every `[field:]` binds exactly one existing value of the default INI. The activation field is not shown in a card; it belongs to the "Plugin active" switch in the header.

### 3.4 Buttons that open package files

```ini
[links]
tab = general
label_key = plugin.links
root = ..                              ; relative to the schema folder

[link:readme_de]
file = README_DE.md                    ; .md, .txt, .html or .pdf, no ..
label_key = plugin.link.readme
language = de                          ; optional: only in this UI language
order = 10
```

### 3.5 Action row

```ini
[action:prune]
group = bridge                         ; card the row is in; sorted with the fields by order
label = Tidy the bridge list
label_key = bridge.prune
description = ...
description_key = bridge.prune.description
button = Tidy up now
button_key = bridge.prune.button
command = bridge_prune                 ; the only command so far
order = 35
```

`bridge_prune` removes entries from the bridge's package list whose package is not in the RMM list. The button is never stretched.

### 3.6 Folder list

```ini
[folder_list:packs]
group = packs
paths = package:hooks\localization | build:plugins\localization   ; every subfolder becomes a row
description_prefix = loc.pack          ; short text per folder from the language file: <prefix>.<folder>
note = ...
note_key = loc.packs.note
order = 10
```

Each row names the folder, its short text and where it lies: "in the package" (`package:` path), "local" (`build:` / `vfs:` path) or "local, overlays the package" (both). Path prefixes: `package:` = `[links] root`, `build:` = loader folder, `vfs:` = the loader's VFS root (next to `build`, otherwise `build\vfs`).

---

## 4. Collections

A collection builds a user-managed list and a value matrix from a local plugin catalogue (Vehicle Materials uses it):

```ini
[collection:materials]
source = local-plugin:resources/list   ; reads only this section of build\plugins\resources.ini
source_ready_section = resources       ; optional readiness condition
source_ready_key = hook
source_ready_value = 2

resource_group = resources             ; group with the list
matrix_group = vehicles                ; group with the matrix
count_section = resources              ; a readonly field must exist for this key
count_key = count
item_prefix = resource                 ; resource0, resource1, ...
item_label = Material
item_label_key = material_number
item_description_key = field.resource.description

target_sections = road|rail|ship|airplane
target_labels = Road vehicles|Rail vehicles|Ships|Airplanes
target_label_keys = row.road|row.rail|row.ship|row.airplane
target_icons = builtin:truck|builtin:rail|builtin:ship|builtin:plane
unit = Coefficient
unit_key = unit.coefficient
coefficient_description_key = coefficient.description

empty_notice = No entries yet. Add one first.
empty_notice_key = collection.empty_notice
empty_notice_style = warning           ; warning or info; shown in matrix_group while empty

type = decimal
minimum = 0
maximum = 1000000
step = 0.001
default = 0
maximum_items = 32
ownership = user                       ; an existing list survives a change of the package defaults
allow_remove_defaults = 1              ; entries shipped by the package may be removed
require_positive_when_enabled = 1      ; at least one positive value while the plugin is on
```

The four `target_*` lists must have the same length. Adding creates gap-free `item_prefix0`, `item_prefix1`, … plus one value per target section; removing deletes all values and compacts the list. `require_positive_when_enabled` switches the plugin off when the last positive entry is removed; the error message names the visible tab and group.

---

## 5. Local schemas for installed plugins

A plugin that lies only as `plugins\<name>.dll` in the loader folder gets its schema from `settings_schemas\<name>.launcher.ini` next to `rmm.exe`, if that file exists, otherwise from its INI (section 2). Such a local schema is an ordinary presentation schema:

```ini
[launcher]
layout_version = 1
visible = 1
id = local.walking
name = Walking Distance
config = walking.ini                   ; must be <name>.ini
enabled_field = walking/enabled
language_directory = languages         ; relative to settings_schemas
```

RMM ships such schemas for accumulator, cities, daynight, depletion, easystart, walking and the Workshop Bridge; the German texts are in `settings_schemas\languages\de.ini` under the prefix `<name>.`, the English fallbacks in the schema itself. A local schema describes, it does not forbid: values may carry a comment (kept until the value changes), and keys the schema does not know are named in the notes and written back unchanged. Free values such as `auto` or years with `off`/`always` use `type = text`.

Installed plugins are managed as a protected base: original under `user_config\.autoload\<name>.upstream.ini`, effective INI in `plugins\`, personal values in `user_config\<name>.ini`. A `plugins\<name>.ini` changed outside RMM becomes the new original.

---

## 6. Editor schemas

For INIs that hold lists instead of single values there are three editor types. The schema lies in `settings_schemas\` (installed plugins) or in the package under `config\<name>.launcher.ini`; in a package `[editor] plugin` and `config` must name the DLL name and INI of the package. The package's INI is the original base, personal entries live in `user_config\<name>.editor.ini`, the effective file is `plugins\<name>.ini`. DLL, loader entry, bridge list and "Files local only" work as for every package.

| `editor_type` | Structure of the INI | Example |
|---|---|---|
| `keyed_resources` | list section plus one section per entry (`[custom:<id>]`) and keyed values | Resources |
| `keyed_list` | one line per entry: `<id> = <column 1>, <column 2>, ...` | Needs, Technical Service Storage, UI Layout Fixes |
| `keyed_sections` | one section per entry | Deposits, Deposits Plus, Research Expansion, Vanilla Buildings |

### 6.1 `[editor]`, `[activity]`, `[source]`

```ini
[launcher]
layout_version = 1
editor_type = keyed_list
id = tesmio.needs.editor
name = Needs
language_directory = languages
default_tab = needs
notice = ...                           ; as in presentation schemas
info = ...

[editor]
plugin = needs
config = needs.ini
list_section = list                    ; keyed_list / keyed_resources
item_section_prefix = custom:          ; keyed_resources
reserved_sections = deposits           ; keyed_sections: sections that are NOT entries
section_prefix = modify:               ; keyed_sections: only sections with this prefix are entries
maximum_items = 8

[activity]                             ; the plugin's on/off field; not shown in a card,
section = needs                        ; it belongs to the "Plugin active" switch
key = enabled
values = 1

[source]                               ; identifiers for the + dialog
plugin = resources                     ; keys of the section from plugins\resources.ini
section = list
ready_section = resources              ; optional: source counts only with [resources] hook = 2
ready_key = hook
ready_value = 2
```

Without `[source]` the + dialog asks for the identifier as an editable field and suggests the base-game resources plus the list of the Resources plugin. `[list] id_suggestions = 0` drops these suggestions (for text ids and the like). Identifiers follow the usual rules: letters, digits, `_`, `-`, `.`.

### 6.2 Tabs, list texts, dialog texts

```ini
[tab:general]
label_key = tab.general
order = 10

[list]
label_key = needs.list                 ; list title
add_label_key = needs.add              ; + button
select_help_key = needs.select_help    ; help text while nothing is selected
id_label_key = needs.resource          ; label of the identifier
id_help_key = needs.resource.help      ; help under the identifier in the detail area
add_id_help_key = needs.add_help       ; help under the identifier in the + dialog (otherwise id_help)
save_warning_key = needs.save_warning  ; yellow save-game warning on the list card
note_key = needs.note                  ; small text left under the list, next to +
summary = type|map                     ; keys shown under the name in the list
id_picker = game_texts                 ; game_texts: "Choose text..." button; game_research: "Choose research..."
id_suggestions = 0
remove_label_key = re.remove           ; "entry only" button in the delete dialog of entries with attachments

[new]                                  ; + dialog of keyed_sections
name_label_key = deposits.new_name
token_key = token                      ; field the dialog suggests from the name
token_template = $TYPE_MINE_{NAME}
hint_key = deposits.new_hint           ; text at the end of the dialog

[group:materials]                      ; the list card
tab = materials
label_key = tss.group.materials
description_key = tss.group.materials.description
notice_key = tss.group.notice          ; blue box between description and save warning
id_reference = resources               ; every own identifier must exist (section 7)

[global]                               ; card for plugin-wide values (scope = global)
tab = general
label_key = group.needs.plugin
description_key = ...
notice_key = ...
order = 10                             ; sorts among the [card:] cards of the tab; 0 = first

[card:log]                             ; further cards for plugin-wide fields
tab = general
label_key = tss.card.log
notice_key = ...
notice_style = info                    ; blue instead of yellow
order = 20
```

Several lists in one `keyed_sections` schema: a `[group:<id>]` with its own `section_prefix` is an additional list; its texts come from `[list:<id>]` and `[new:<id>]`, its fields carry `group = <id>`. The default list is the first `[group:]` without a prefix. Identifiers of additional lists are full section names (`research:quartz`), shown without the prefix.

### 6.3 Columns (`keyed_list`)

```ini
[column:donor]
type = choice                          ; text, integer, decimal, choice
choices = food|meat|clothes|eletronics|alcohol
allow_other = 1                        ; choice with free input
required = 1
default = auto                         ; fills missing columns
minimum = 0
maximum = 1
step = 0.1
label_key = needs.donor
description_key = needs.donor.description
heading_key = ...                      ; separator line with a title above the field
reference = resources                  ; section 7
order = 10
```

Lines are normalised on writing (`1.0` becomes `1`, choice values in the schema's spelling).

### 6.4 Detail fields (`keyed_sections`, `keyed_resources`, global values)

```ini
[detail:type]
scope = item                           ; item: key in the entry's section
                                       ; global: [section] key of the plugin INI, shown on [global] or a [card:]
                                       ; custom / keyed: keyed_resources
group = research                       ; additional list (keyed_sections)
card = log                             ; card for scope = global
section = deposits                     ; scope = global
key = type
type = integer                         ; text, integer, decimal, choice, boolean (global), lines, pair, triple
minimum = 10
maximum = 127
step = 1
choices = technical|soviet|medical
choices_source = registry              ; choice: identifiers from [source] plus base game
allow_other = 1
unique = 1                             ; no value twice
auto_increment = 1                     ; dialog suggests maximum + 1
dialog = 1                             ; part of the + dialog
default = {name}                       ; dialog default; {name} = name of the new entry
maximum_length = 7
length_rule = map=terrain:4            ; shorter while another key has this value
suffix = .name                         ; locked box with the suffix behind the field, identifier as placeholder
position = above_id                    ; field above the identifier line
heading_key = ...
label_key = ...
description_key = ...
reference = game_research              ; section 7
reference_format = requires
reference_own = research
picker = game_buildings                ; lines: building picker; game_research: research picker;
                                       ; research_lines: lines of the entry's Vanilla block; files: file list
picker_format = line_edit              ; research_lines: line, line_edit, line_anchor, anchor_edit, edit
picker_folders = package:hooks\x\assets | build:plugins\x\assets   ; files: first existing folder
picker_pattern = *.dds
count_label_key = ...                  ; lines: counter label (default "Number of lines")
maximum_lines = 64                     ; lines: cap, counter turns red above it
order = 20
```

`type = lines` (scope = item) describes a key the INI repeats in the section (`target = …`, `add = …`): one line per occurrence, empty lines dropped, written as one block at the place of the first occurrence, stored as `field.<id>.0`, `field.<id>.1`, … in the override. Lines fields have a fixed height with a scrollbar, a grip to resize, and plus/minus to fold. For `[list] summary` a lines field shows its first line with a counter `(+n)`.

Original entries are locked but can be hidden (`suppressed = 1` in `user_config\<plugin>.editor.ini`): they vanish from the effective file, stay in the original base and can be shown again. Personal entries are stored as `[item:<id>] owned = 1` with `field.<id>` values (or `list = <tuple>` for keyed_list) and written as a new section in field order; plugin-wide values as `[global] field.<id> = <value>`. A hidden original loses its whole section in the effective file; an overwritten original keeps section and comments.

### 6.5 Rows on cards: folder, file, picture, Vanilla block

```ini
[folder:icon_store]                    ; folder row on a card
card = icons                           ; card or global
path = vfs:media_soviet\research
missing_key = re.icon_store.missing    ; yellow box while the folder is missing
create = 1                             ; "Create folder" button while missing
label_key = re.icon_store              ; row with path field, "Open" and "Refresh" once it exists
order = 10

[file:noimage]                         ; file row: first existing candidate is shown
card = icons
paths = workshop=package:hooks\x\noimage.png | local=build:plugins\x\noimage.png
label_key = re.noimage
order = 20

[picture:icon]                         ; picture preview per entry with "Insert picture..."
group = research
folder = vfs:media_soviet\research
file = {id}.png
size = 128                             ; required size in pixels, 0 = any
order = 15

[research_block:block]                 ; read-only box with the entry's block from media_soviet\research\research.ini
label_key = re.block
description_key = re.block.description
group = modify
order = 12
```

Entries with attachments (texts, picture) get a delete dialog with three buttons: "Delete all" removes the entry, its texts from all language files and the picture; the `remove_label` button removes only the entry.

### 6.6 Text pack tab

```ini
[tab:localization]
label = Localization

[textpack]
tab = localization
folder = build:plugins\localization\research_expansion   ; local pack, RMM writes here
seed = dependency:tesmio.localization|hooks\localization\research_expansion   ; initial content from the dependency's package
namespace = research_expansion         ; for a new pack without seed
keys_from = research                   ; list group whose identifiers give the lines <id>.name / <id>.desc
label_key = ...
description_key = ...
missing_key = re.textpack.missing      ; yellow box with "Create locally" while the folder is missing
```

The tab shows the fallback language, the language files (with "+" from the game's languages), per own entry name and description, and further keys with a trash button that removes a key from all language files.

---

## 7. Reference checks

Values that must name something the game knows are checked before saving. A hit turns the footer red ("Configuration invalid") with the entry and field; Save and "Save + Start" stop with the same message. Only own entries and personally set values are checked. Without a game folder next to the loader the check is skipped.

```ini
[group:modify]
id_reference = game_research           ; the identifier of every own entry must be an active Vanilla research

[detail:r_requires]
type = lines
reference = game_research              ; game_research, game_buildings, game_texts, resources, files
reference_format = requires            ; id (default): whole value / every line
                                       ; requires: "<predecessor> | before/after | <anchor>", both checked
                                       ; directive:$UNLOCK_RESEARCH: only line parts with this directive
                                       ; file: file relative to media_soviet or the Workshop folder must exist
                                       ; exists | dds_dxt1 | dds_dxt5: for reference = files
reference_own = research               ; own, switched-on entries of this group count as known
```

Sets: `game_research` = active blocks of `research.ini`; `game_buildings` = building files of the game, the DLCs and subscribed Workshop items; `game_texts` = ids of the `.btf` of the game language; `resources` = the 57 base-game names plus `plugins\resources.ini [list]` when the Resources plugin is ready; `files` = the picker folders. The DDS formats additionally check compression, square power-of-two size 256..4096 and the full mipmap chain.

---

## 8. Languages

```ini
[language]
name = Deutsch

[strings]
plugin.description = Beschreibung
field.enabled = Plugin aktivieren
```

`language_directory` is relative to the package root (presentation schema) or to `settings_schemas` (local schema); files are `<code>.ini`. Order: chosen language, English, the plain text in the schema. Section, key, group, tab and entry identifiers are never translated. Language files are read literally: a semicolon inside a text is not a comment.

---

## 9. Icons and path safety

Built-in icons include `builtin:gear`, `builtin:truck`, `builtin:rail`, `builtin:ship` and `builtin:plane`. Alternatively a safe relative PNG/ICO path inside the package. Absolute paths, `..`, URLs, reparse points, oversized files and invalid image sizes are refused or logged as optional and replaced by a default icon. Asset names may contain spaces and `. _ + ( ) & , ' -`, but no leading dot, no trailing dot or space and no `..`.

---

## 10. How RMM provisions a package

Every save leaves a receipt under `user_config\.autoload\` with the package version and the hashes of the provided files. Its `mode` says who loads the DLL:

| `mode` | Meaning |
|---|---|
| `package` | RMM copied DLL and INI to `plugins\` (TesmioLoader classic) |
| `bridge` | only the INI; the Workshop Bridge loads the DLL; `[packages] <folder> = 1` in `user_config\workshop_bridge.ini` decides |
| `sml` | only the INI; the Soviet Mod Loader loads the DLL |
| `installed` | plugin without a package, protected base |

A changed package (new version, new DLL or default INI) shows the yellow "Update" marker; saving takes over the new version and keeps personal values. Under the bridge or SML only the default INI counts, because the game loads the DLL from the package anyway. Before every save RMM keeps one rolling restore point per plugin under `user_config\.autoload\backups\<package>\previous`; a pending marker protects against a half-written state after a crash.

For the plugin `workshop_bridge` itself the `[packages]` lines in its overlay file survive every save and "Restore original".
