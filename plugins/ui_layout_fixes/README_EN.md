# 🪟 UI Layout Fixes 1.2

**TesmioLoader plugin for targeted corrections to single info windows**

Bundles window-specific corrections for *Workers & Resources: Soviet Republic* 1.1.1.9. Every window type is its own module with its own configuration, target checks and switch. Currently included: the **CUSTOMHOUSE** module (wider row spacing of the resource list in the customs house) and the **VEHICLE_ROUTE_HINT** module (the route hint in the vehicle window is re-wrapped instead of running off the right edge). There is deliberately no global row spacing.

---

## 📋 Contents

- [Quick start](#-quick-start)
- [Features](#-features)
- [Installation](#-installation)
- [Republic Mod Manager](#-republic-mod-manager)
- [Configuration](#-configuration)
- [Value ranges](#-value-ranges)
- [How it works](#-how-it-works)
- [Compatibility](#-compatibility)
- [Troubleshooting](#-troubleshooting)
- [File structure](#-file-structure)

---

## 🚀 Quick start

### Requirements
- Windows x64
- WRSR 1.1.1.9
- TesmioLoader API 4
- No dependency on other plugins

### In three steps
1. **Choose one installation method** (see below) and enable the plugin.
2. **Set the row spacing** if you like: `resource_row_pitch` from 25.0 to 60.0 in `ui_layout_fixes.ini`, most conveniently through Republic Mod Manager (default 30.0).
3. **Restart the game completely** and open a customs house or a vehicle with a route problem. The detail log shows `[CUSTOMHOUSE] active` and `[VEHICLE_ROUTE_HINT] active`.

---

## ✨ Features

### 🎯 Core function
- ✅ CUSTOMHOUSE module: wider row spacing of the resource list in buildings with `$TYPE_CUSTOMHOUSE`, i.e. the game's customs houses and compatible mod buildings that use the same info window
- ✅ Only the two verified calls of the resource list are changed: the measurement pass (list height) and the drawing pass (icons and texts); both use the same spacing
- ✅ The position of the window sections below and the scroll range are still computed by the game's native layout; every other window keeps the native spacing of 25.0 logical pixels
- ✅ Only verified code and data in the memory of the running game are changed; game files, building files and savegames stay untouched

### 🆕 Since 1.2
- ✅ **VEHICLE_ROUTE_HINT module:** the hint "View area where a possible issue exists on route!" in the vehicle window is stored as two overlong lines and runs past the right edge of the window. The module intercepts the game's text lookup for exactly this text id (1970) and returns a re-wrapped copy: at most `max_chars` characters per line (default 58), words are never split, the lines of a paragraph get similar lengths, the game's paragraphs are kept. If the text needs more than `max_lines` lines (default 4), the lines get wider step by step. No game code is changed; the module works in every game language.

### 🆕 Since 1.1
- ✅ **Shared configuration rule** (`tesmio_config.h`): the base is `plugins\ui_layout_fixes.ini`, otherwise the INI beside the DLL (in the Workshop package). If `user_config\ui_layout_fixes.ini` exists in the loader folder, its keys win one by one; only Republic Mod Manager writes that file. Both paths are logged at start.

---

## 💾 Installation

Choose **one** of the four methods. The same DLL must never be loaded twice.

---

### Method 1️⃣: Classic TesmioLoader

```
1. Copy ui_layout_fixes.dll and ui_layout_fixes.ini from hooks\
   → tesmioloader\build\plugins\

2. Enable ui_layout_fixes in TesmioLauncher
3. Restart the game completely
```

---

### Method 2️⃣: Soviet Mod Loader (SML)

```
1. Subscribe to the Workshop item – SML reads subscribed packages by itself
2. SML loads the DLL through soviet.mod.ini from the package, the INI sits beside it
3. Remove or disable a local ui_layout_fixes.dll in plugins\ first
```

---

### Method 3️⃣: Workshop Bridge (without SML)

```
1. Select the package in Republic Mod Manager
2. Switch "Plugin active" on
3. workshop_bridge loads the DLL straight from the package
4. Steam updates apply immediately
```

---

### Method 4️⃣: Republic Mod Manager with "Files local only"

```
1. Select the package in Republic Mod Manager, adjust the settings, Save
2. "General" tab, "Notes" card: switch "Files local only" on
3. Confirmation with file list → DLL and INI are copied to
   tesmioloader\build\plugins\ on save
4. The Workshop Bridge skips the package from then on
```

**"Files local only" in detail**
- For everyone who wants to keep using the plugin without the Steam subscription
- With local files, Steam updates apply only after saving again (yellow "Update" badge)
- Switching it off removes only the files Republic Mod Manager copied

---

## 🧰 Republic Mod Manager

The package ships a launcher schema in the `config` folder. Republic Mod Manager shows UI Layout Fixes in three tabs, German and English:

- **General:** notes, "Files local only" and the button for this guide
- **Customs house:** module switch and row spacing of the resource list
- **Vehicle window:** module switch, text id, characters per line and line limit of the route hint

The "Plugin active" switch in the header also sets `enabled = 1` when switched on. Personal values live in `user_config\ui_layout_fixes.ini`; the shipped INI stays untouched. If you prefer editing the INI by hand, everything else is below.

---

## ⚙️ Configuration

### Main file: `ui_layout_fixes.ini`

The DLL reads in this order:
- **Base:** `tesmioloader\build\plugins\ui_layout_fixes.ini` if it exists, otherwise the INI beside the DLL (in the package `hooks\ui_layout_fixes.ini`)
- **Overlay:** `tesmioloader\build\user_config\ui_layout_fixes.ini`, key by key on top; this is where Republic Mod Manager writes

⚠️ **Comments only on their own lines with `;`.** The file must be UTF-8 without BOM; do not repeat sections or keys. Unknown or misspelled names are ignored and the plugin silently uses the compiled default, so keep the documented names unchanged and check the log after changes. Settings are read only at start: change the INI, restart the game completely.

```ini
[general]
; 1 enables the individually configured modules, 0 starts no module and writes no patch
enabled = 1

[customhouse]
; 1 enables only the CUSTOMHOUSE module; no effect while [general] enabled = 0
enabled = 1
; row spacing of the resource list in logical pixels, 25.0 to 60.0, decimal point
; 25.0 = native game value, 30.0 = plugin default
resource_row_pitch = 30.0

[vehicle_route_hint]
; 1 enables only the VEHICLE_ROUTE_HINT module; no effect while [general] enabled = 0
enabled = 1
; number of the game text that gets re-wrapped; 1970 = route hint in WRSR 1.1.1.9
text_id = 1970
; longest line in characters, 20 to 200; words are never split
max_chars = 58
; line limit, 0 to 12; 0 = no limit; above it the lines get wider step by step
max_lines = 4
```

`enabled` is read as an integer: `0` disables, any other value enables. Use only `0` or `1` anyway.

---

## 📏 Value ranges

| Key | Range | Default |
|---|---|---|
| `[general] enabled` | 0 or 1 | 1 |
| `[customhouse] enabled` | 0 or 1 | 1 |
| `resource_row_pitch` | 25.0 to 60.0 inclusive, decimal point | 30.0 |
| `[vehicle_route_hint] enabled` | 0 or 1 | 1 |
| `text_id` | 1 to 100000, whole number | 1970 |
| `max_chars` | 20 to 200, whole number | 58 |
| `max_lines` | 0 to 12, whole number (0 = no limit) | 4 |

For `resource_row_pitch`: `25.0` equals the native game value and adds no spacing; non-numeric, infinite or out-of-range values are discarded, the plugin logs a warning (`invalid-config`) and uses `30.0`, the module stays active.

The same applies to `text_id`, `max_chars` and `max_lines`: invalid or out-of-range values trigger a warning (`invalid-config`) and fall back to the default.

---

## 🔬 How it works

The game natively uses a spacing of `25.0f` for the resource list. The plugin redirects only the two verified CUSTOMHOUSE calls through a near memory bridge; during such a call the native spacing is temporarily replaced by the configured value and restored right after. This leaves untouched: resource lists of other windows, the native list-height computation, the positioning of the following window sections, the scroll range, goods quantities, prices, building properties, savegames and files.

Before writing a patch the module checks the size of the loaded image, the PE time stamp of `SOVIET64.exe`, the signature of the CUSTOMHOUSE panel function, the measurement and drawing calls with their targets, the native row-pitch instruction and the reachability of the memory bridge. If any step fails, the plugin writes no patch and the module stays inactive.

The VEHICLE_ROUTE_HINT module works differently: it changes no game code but hooks the engine DLL's text lookup `C3D_LANGUAGE::GetString` through the import table of `SOVIET64.exe`. Only the configured text id receives a re-wrapped copy from the plugin's own buffer; every other id passes through unchanged. The copy is built once and rebuilt only when the game returns a different text for the id (language switch). Other plugins hooking the same lookup (for example the resources plugin) chain in load order; each one answers only its own ids.

| Property | Expected value |
|---|---|
| Game version | `SOVIET64.exe 1.1.1.9` |
| PE time stamp | `0x6A3EB6AD` |
| Size of the executable image | `0x00A9D000` |

---

## 💾 Compatibility

### Game and loader
- Exactly supported game version 1.1.1.9 for CUSTOMHOUSE; other versions are refused because memory addresses and signatures depend on the version. VEHICLE_ROUTE_HINT changes no game code and depends only on the text id
- Compatible with the resources plugin, which hooks the same text lookup; the hooks chain in load order
- No dependency on the Localization plugin or any other TesmioLoader plugin
- A complete game restart removes every active memory change

### Savegames
The plugin changes neither game files nor savegames.

### Version compatibility
- **1.2:** new VEHICLE_ROUTE_HINT module with the `[vehicle_route_hint]` section; CUSTOMHOUSE and all earlier keys unchanged
- **1.1:** configuration through `tesmio_config.h` (base plus personal overlay); the CUSTOMHOUSE module is unchanged from 1.0
- **Going back to an older version:** restore the old DLL and its INI

---

## ⚙️ Troubleshooting

### Common problems

| Problem | Cause | Solution |
|---|---|---|
| No wider spacing | `[general]` or `[customhouse]` `enabled = 0`, or `resource_row_pitch = 25.0` | switch on, set a value above 25.0, restart the game |
| `invalid-config` in the log | `resource_row_pitch`, `text_id`, `max_chars` or `max_lines` invalid | enter a value in the documented range (row spacing with a decimal point) |
| Route hint still too wide | `[vehicle_route_hint] enabled = 0`, or `max_lines` forces wider lines | enable the module, lower `max_chars` or raise `max_lines` (0 = no limit) |
| `iat-patch` | text lookup import could not be redirected | check `tesmioloader.log`; only this module stays inactive |
| `text-length` | game text longer than the buffer (1023 characters) | check the text id; the native text is shown unchanged |
| `unsupported-build` | game version not 1.1.1.9 | use the matching plugin version |
| `panel-signature`, `row-pitch-signature`, `measure-call`, `draw-call` | expected machine code changed | check the game version and conflicts with other UI plugins |
| `near-allocation`, `bridge-range` | memory bridge could not be created | restart the game, keep the log |
| `call-protection`, `pitch-protection` | memory area not writable | check security software and competing plugins |
| `all window modules are disabled` | plugin on but both modules `enabled = 0` | enable at least one module |
| `log-open` | detail log could not be created | check write permissions; `tesmioloader.log` stays available |

### Logging

Messages go to `tesmioloader.log` (warnings, errors, phase summaries) and the detail log `tesmioloader.ui_layout_fixes.log` (complete structured log with module, rule and Windows error code).
- In **Republic Mod Manager** the document icon at the bottom of the plugin bar opens the log view with filter and sender.

Expected on a successful start:
```
[CUSTOMHOUSE] active
[VEHICLE_ROUTE_HINT] active
```

---

## 📦 File structure

**Workshop package** (Steam subscription, SML, Workshop Bridge)
```
ui_layout_fixes\
├── hooks\
│   ├── ui_layout_fixes.dll         (plugin)
│   └── ui_layout_fixes.ini         (original INI)
├── config\                         (launcher schema for Republic Mod Manager)
│   ├── ui_layout_fixes.launcher.ini
│   └── languages\
│       ├── de.ini
│       └── en.ini
├── soviet.mod.ini                  (manifest for SML, Bridge and Republic Mod Manager)
├── workshopconfig.ini              (Steam Workshop entry)
├── previewimage.png
├── README_DE.md
└── README_EN.md
```

**Loader folder** (method 1 by hand or "Files local only")
```
tesmioloader\build\
├── tesmioloader.ui_layout_fixes.log
├── plugins\
│   ├── ui_layout_fixes.dll
│   └── ui_layout_fixes.ini         (original INI)
└── user_config\
    └── ui_layout_fixes.ini         (personal values from Republic Mod Manager)
```

Through the Bridge or SML only the original INI sits in `plugins`, the DLL stays in the package. Personal values always live in `user_config`.

---

## 📜 Licence & credits

**GNU GPL v3**, see `LICENSE` in the package. The plugin contains no third-party code; the loader SDK header comes from the TesmioLoader by MaxLegend (GPL v3). The complete source lives at https://github.com/Kespri/workers-and-resources-mods/tree/main/plugins/ui_layout_fixes.

---

## ❓ FAQ

**Q: Does the spacing apply to other buildings as well?**
A: No. Only to buildings with `$TYPE_CUSTOMHOUSE`; every other window keeps the native spacing.

**Q: Do I have to restart the game after a change?**
A: Yes. Settings are read only at start.

**Q: What happens with an invalid row spacing?**
A: The plugin warns in the log and uses 30.0; the module stays active.

**Q: The route hint in the vehicle window is still too wide?**
A: Lower `max_chars`; the module spreads the words evenly over the lines. If the text needs more lines than `max_lines`, the module widens the lines again; raise `max_lines` or set it to 0.

**Q: Will more windows be added?**
A: The plugin is built for it: every window is its own module with its own section in the INI.

**Q: Do I have to edit the INI by hand?**
A: No. Republic Mod Manager shows every switch and value with descriptions and checks the value ranges.

---

**Last update:** UI Layout Fixes 1.2  
**For:** WRSR 1.1.1.9 | TesmioLoader API 4
