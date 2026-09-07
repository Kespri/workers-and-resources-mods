# ⛏️ Depletion 1.1.2

**TesmioLoader plugin for deposits that run out**

In the base game of *Workers & Resources: Soviet Republic* 1.1.1.9 a deposit is infinite: a mine samples the richness once when it is built and produces at that rate forever. Depletion takes the mined tonnes out of the deposit map, derives the quality of source from what is left and shows in the mine window how much remains. The depletion lives in the map itself, so it is in the savegame, under the minimap overlay and shared by every mine over the same patch.

The plugin is the continued version of the `depletion` plugin from the TesmioLoader by MaxLegend (Tesmio).

---

## 📋 Contents

- [Quick start](#-quick-start)
- [Features](#-features)
- [Installation](#-installation)
- [Republic Mod Manager](#-republic-mod-manager)
- [Configuration](#-configuration)
- [How it works](#-how-it-works)
- [Value ranges](#-value-ranges)
- [Compatibility](#-compatibility)
- [Troubleshooting](#-troubleshooting)
- [File structure](#-file-structure)

---

## 🚀 Quick start

### Requirements
- Windows x64
- WRSR 1.1.1.9 (the hook addresses apply to this build only)
- TesmioLoader API 4
- **Optional:** the `deposits` plugin or Deposits Plus, so mod deposits run out as well
- **Optional:** Localization with the text pack `tesmio_lang` for the translated caption in the mine window

### In three steps
1. **Put DLL and INI into the loader's plugin folder** and enable `depletion` in the TesmioLauncher; start on a **copy of your savegame**.
2. **Check the balance:** `tonnes_per_texel` decides how long a deposit lasts. The log prints the texel count and the resulting reserve for every mine the first time it is sampled.
3. **Restart the game completely.** The mine window shows the row "Deposit remaining", the log a progress line per mine every 60 seconds.

---

## ✨ Features

### 🎯 Core function
- ✅ Output is integrated per tick and taken out of the deposit map; the quality of source follows what is left
- ✅ Base-game deposits selectable: oil, iron, coal, uranium, bauxite, gravel, each with its own tonnes per texel
- ✅ Mod deposits from the `deposits` plugin are included automatically, `deplete = 0` exempts one
- ✅ Row "Deposit remaining: 251.4 kt / 259.3 kt (96.9 %)" in the mine window, the window grows by one line
- ✅ Progress lines in the log for calibration
- ✅ Map writes only from the render thread, batched and at most four times per second

### 🆕 New in 1.1.2
- ✅ **Optional translation:** the row caption comes from the Localization key `tesmio_lang.depletion.deposit_remaining` (German "Restvorkommen", English "Deposit remaining"). Without Localization, without the key or with an overlong text `panel_caption` from the INI stays. Numbers, units and the row position are unchanged.

### 🔧 Fixes from 1.1.1
- ✅ The mine cache is discarded completely when loading from the main menu, including pending debits; stale building addresses no longer reach the display
- ✅ Sampling starts only at the first render call of the newly loaded terrain
- ✅ Mine cache and world change are locked against concurrent tick and render access; native calls run outside the lock
- ✅ Before writing and displaying, a stored address is checked to still belong to the expected mine kind
- ✅ Opened textures are closed even when an exception occurs
- ✅ If another hook fails after the first one, the plugin is disabled but the DLL stays loaded

---

## 💾 Installation

Depletion is not a Workshop package but a plugin of the loader folder. There is only the classic way; Republic Mod Manager edits it as an installed plugin.

```
1. Copy depletion.dll and depletion.ini
   → tesmioloader\build\plugins\

2. Enable depletion in the TesmioLauncher
3. Restart the game completely
```

For mod deposits `deposits.dll` or Deposits Plus has to be loaded, otherwise only the base game's deposits run out. For the translated caption enable Localization with the text pack `tesmio_lang`.

---

## 🧰 Republic Mod Manager

Republic Mod Manager ships a local schema (`settings_schemas\depletion.launcher.ini`) and shows Depletion in two tabs, German and English:

- **General:** plugin enabled, row in the mine window, caption, diagnostics (interval of the progress lines)
- **Mining:** tonnes per texel, base-game deposits, seconds between map writes

The INI found is saved as the original to `user_config\.autoload\depletion.upstream.ini`, the effective INI is written permanently; "Restore original" is offered in the Notes card.

---

## ⚙️ Configuration

### Main file: `plugins\depletion.ini`

One section `[depletion]`, UTF-8 without BOM, comments with `;` on their own lines. Changes apply after a complete restart.

| Key | Default | Meaning |
|---|---:|---|
| `enabled` | 1 | 0 = base game, the plugin hooks nothing and unloads |
| `tonnes_per_texel` | 1200 | value of one fully saturated texel in tonnes of output; the balance knob |
| `vanilla` | `oil,iron,coal,uranium,bauxite,gravel:30000` | base-game deposits that run out; `all`, `none`, own tonnes after a colon |
| `flush_seconds` | 5 | real seconds between writes to the map |
| `log_seconds` | 60 | real seconds between progress lines per mine, 0 = off |
| `panel` | 1 | row in the mine window |
| `panel_caption` | `Deposit remaining` | caption of the row, ASCII only; with Localization only the fallback |

Mod deposits from `deposits.ini`: `deplete = 0` in the deposit section exempts it, `deplete = <tonnes>` gives it its own figure.

⚠️ **Gravel is different.** Gravel does not live in a resource map but in component 2 of the terrain mask, the one the editor's material brush paints. A gravel pit therefore visibly and permanently wears the ground texture away. Its search radius is 30 instead of 210, so its footprint is a few dozen texels; hence the far larger own figure. Drop `gravel` from the list to keep the base game's behaviour.

⚠️ **Savegames:** Depletion lowers the terrain's resource map, and the world writer saves it. `enabled = 0` or removing the plugin does not restore what was mined. Test on a copy.

---

## 🔬 How it works

- The mine tick (one mine, one tick) is arithmetic only: it charges this tick's production rate against the reserve held in the plugin and writes the quality of source. It reads two floats out of the building and nothing else.
- The flusher runs from an import hook on the terrain render, unambiguously the render thread: it seeds a mine's reserve from the map and writes accumulated depletion back, batched for every mine, at most four times per second and no more often than `flush_seconds`.
- The map must never be touched from the tick: the game's texture access is not a lock but a staging copy, Map and CopyResource on the immediate D3D11 context. From a simulation thread that crashes the graphics driver; that was exactly the bug of the first version.
- The second value in "remaining / reference" is rebuilt from the current map after loading. There is no permanently stored initial amount.
- With `tonnes_per_texel = 1200` one byte step of the map is about 4.71 t; fractions of a step live in memory only.

---

## 📏 Value ranges

| Quantity | Limit |
|---|---|
| `tonnes_per_texel` | 1 to 10,000,000 (editor limit) |
| `flush_seconds` | 1 to 600, effectively never more than four times per second |
| `log_seconds` | 0 to 3600 |
| `panel_caption` | ASCII, one line; the translated caption at most 63 UTF-16 code units |
| resource maps | the engine's two maps plus those the `deposits` plugin creates, as far as the plugin tracks them |

---

## 💾 Compatibility

### Savegames
The save format is unchanged, the content of the resource map is not: what was mined stays mined. Without the plugin mines again produce infinitely from what is left.

### Other plugins
- **deposits / Deposits Plus:** provides the registry of mod deposits; without it only base-game deposits.
- **Localization** with `tesmio_lang`: translated caption; optional.
- Seeding old savegames with mod deposits is the job of deposits, not of Depletion.

### Version compatibility
- **1.1.2:** optional translation of the caption
- **1.1.1:** load fixes (cache, world change, locks, address check, hook failure)
- **1.1:** state of the TesmioLoader source tree
- No new settings since 1.1, no change to the save format

---

## ⚙️ Troubleshooting

### Common problems

| Message or behaviour | Cause | What to do |
|---|---|---|
| `enabled = 0 - deposits stay infinite` | plugin switched off | set `enabled = 1` |
| `no deposits plugin - only the base game's own run out` | deposits/Deposits Plus missing | only relevant for mod deposits |
| `nothing declared depletable - not hooking` | `vanilla = none` and no mod deposits | check the list |
| `vanilla: no base-game deposit "…"` | typo in `vanilla` | check the names |
| `no import slot for …` / `terrain init/render import missing` | other game build or foreign hook | check game version 1.1.1.9 |
| `disabled after a fault` / `disabled after a fault in the flush` | exception in tick or flusher | save the log, restart the game |
| `localization key … missing/invalid` | text pack missing | `panel_caption` stays active; check the text pack `tesmio_lang` |
| old mine with a wrong quality after loading | bug before 1.1.1 | fixed since 1.1.1; `deplete  world load` must appear before the new sampling |

### Logging

All messages go to `tesmioloader.log` with the prefix `deplete`.
- In **Republic Mod Manager** the document icon at the bottom of the plugin bar opens the log view with filter and sender.

Search for:
- `deposit type(s) will run out` → which deposits run out with which tonnes
- `texel box` → footprint and reserve of every mine at its first sampling
- `% left` → progress lines for calibration
- `world load` and `world ready` → world change and release of sampling

---

## 📦 File structure

**Loader folder**
```
tesmioloader\build\
├── plugins\
│   ├── depletion.dll
│   ├── depletion.ini                (effective INI)
│   ├── deposits.dll                 (optional, mod deposits)
│   └── localization\tesmio_lang\    (optional, translated caption)
├── user_config\.autoload\
│   └── depletion.upstream.ini       (original INI, saved by Republic Mod Manager)
└── tesmioloader.log
```

**Source**
```
plugins\depletion\
├── depletion.cpp
├── depletion.ini
├── README_DE.md
└── README_EN.md
```

---

## 📜 Licence & credits

**GNU GPL v3.** Depletion is the continued version of the `depletion` plugin from the TesmioLoader by MaxLegend (Tesmio), https://github.com/MaxLegend/TesmioLoader, GPL v3; the addresses and the description of the texture access come from its reverse engineering (`docs\08-depletion.md` of the loader). The complete source of this version lives at https://github.com/Kespri/workers-and-resources-mods/tree/main/plugins/depletion.

---

## ❓ FAQ

**Q: How long does a deposit last?**
A: `tonnes_per_texel` decides. The log names the reserve of every mine at its first sampling; the progress lines show how fast it drops.

**Q: Why does the ground texture under a gravel pit look different?**
A: Gravel lives in the terrain mask, not in an invisible map; mining it is visible. Drop `gravel` from `vanilla` if you do not want that.

**Q: Can I undo the depletion?**
A: No. The map is saved; only an older savegame or the editor brush bring material back.

**Q: Why do my mod deposits not run out?**
A: The `deposits` plugin or Deposits Plus is missing, or the deposit has `deplete = 0`.

**Q: Do I have to edit the INI by hand?**
A: No. Republic Mod Manager offers all seven values with descriptions and range checks.

---

**Last update:** Depletion 1.1.2  
**For:** WRSR 1.1.1.9 | TesmioLoader API 4
