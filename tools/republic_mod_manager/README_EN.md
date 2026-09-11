# Republic Mod Manager – Guide

**English** | [Deutsch](README_DE.md)

Republic Mod Manager (RMM for short) is one window for all your TesmioLoader plugins. You see which plugins you have, switch them on or off, set their values with a description and range check, and start the game. The TesmioLoader itself stays untouched; RMM only writes settings files.

---

## 📋 Contents

1. [Requirements](#-requirements)
2. [The window](#-the-window)
3. [The "Plugin active" switch](#-the-plugin-active-switch)
4. [Save and start](#-save-and-start)
5. [The "Notes" card](#-the-notes-card)
6. [How a plugin gets into the game](#-how-a-plugin-gets-into-the-game)
7. [Files local only](#-files-local-only)
8. [The log window](#-the-log-window)
9. [Profiles and restore points](#-profiles-and-restore-points)
10. [List editors: Resources, Needs, Deposits and package editors](#-list-editors)
11. [Game version and rmm.ini](#-game-version-and-rmmini)
12. [Where your files are](#-where-your-files-are)
13. [When something does not work](#-when-something-does-not-work)
14. [For plugin authors](#-for-plugin-authors)

---

## 🚀 Requirements

- *Workers & Resources: Soviet Republic* 1.1.1.9
- TesmioLoader by MaxLegend in the game folder (`tesmioloader\build` with `tesmioloader.dll` and `tesmioloader.ini`)
- Windows 10 or 11

RMM lives as `rmm.exe` in `tesmioloader\build`. Start it from there or through the desktop shortcut. It finds the loader, the game and the Steam Workshop folder on its own. Only one RMM window is open at a time; a second start brings the existing one to the front.

---

## 🖥️ The window

**The list on the left.** Every row is a plugin: subscribed Workshop packages and everything that lies as a DLL in `tesmioloader\build\plugins`. The icon shows where it comes from: the Steam icon for Workshop packages, the TesmioLauncher icon for plugins in the plugins folder, a gear for anything else. A green dot means this plugin runs on the next game start. A yellow "Update" means the Workshop package is newer than what you saved last. An amber dot means you changed something here and have not saved yet.

**The header at the top.** Name, version and description of the plugin, on the right the "Plugin active" switch and the language of the window. RMM speaks German and English; "Automatic" picks German when Windows runs in German, otherwise English.

**The tabs below.** The tabs come from the plugin. A plugin without its own settings page gets a "General" tab that RMM builds from the comments of its INI file.

**The status line** under the tabs says how the plugin is loaded (Workshop Bridge, Soviet Mod Loader or TesmioLoader), where its files are (Workshop package, local in plugins\ or plugins\) and when the loader loaded it last. Hover a dot and a tip explains it.

**The cards** carry the settings. Every row has a title, a short text on what the setting changes in the game, and the field on the right:

- Switches for ON and OFF.
- Number fields with plus and minus. Hover the field to see the allowed range. You may type a decimal comma; it is saved with a point.
- Choice lists, text fields and multi-line fields with a counter.
- A reset button appears only when your value differs from the plugin's default.

Right-click on any text copies it to the clipboard, handy for bug reports.

**The footer** shows the state: green "Saved", amber "Unsaved changes", red "Configuration invalid" with the reason. Next to it the buttons Reset, Save and Save + Start. Save is only clickable when there is something valid to save. If you changed several plugins, the footer names them all.

The window remembers size, language, selected plugin and tab. It needs at least 1560 by 760 points; in narrow windows the labels move above the fields.

---

## 🔛 The "Plugin active" switch

The switch in the header decides whether the plugin runs on the next game start. What exactly it writes depends on who loads the DLL (see the status line):

- **Workshop Bridge:** the switch adds the package to the bridge's list (`user_config\workshop_bridge.ini`) or removes it. A package you never switched on is not loaded by the bridge.
- **TesmioLoader:** the switch is the TesmioLauncher's checkbox, the entry `[plugins] <name>` in `tesmioloader.ini`. When switching on, RMM also sets the `enabled` field in the plugin's INI to 1, if there is one, so the DLL does not refuse right away.
- **Soviet Mod Loader:** SML loads every subscribed package itself. The switch then only sets the `enabled` field in the INI. If the INI has no such field, the switch is missing and a note says that only unsubscribing turns the package off.

The switch shows "on" only when everything fits, just like the green dot in the list. Pure content packages for the Soviet Mod Loader (resources, buildings without a DLL) have no switch; RMM only lists them.

---

## 💾 Save and start

**Save** writes your changes:

- Your personal values go to `tesmioloader\build\user_config\<name>.ini`. The package's INI stays as it is, so a package update never overwrites your values.
- For plugins that lie only as a DLL in the plugins folder, RMM backs up the INI it found as the original under `user_config\.autoload\<name>.upstream.ini` on the first save and then writes the finished INI with your values to `plugins\`. "Restore original" in the footer brings the backed-up file back byte for byte.
- Before every save RMM creates a restore point (see [Profiles and restore points](#-profiles-and-restore-points)).
- If a new DLL has to go to `plugins\` (only on the TesmioLoader load path), RMM first shows package, source, target and the file's checksum and asks.

Saving works only while the game and the TesmioLauncher are closed and all values are within their ranges. Just opening, searching and switching between plugins or tabs never writes a file.

**Several plugins at once:** You do not have to save after every plugin. Just switch to the next entry; your changes stay in memory, and the entry gets an amber dot in the list. The footer lists every unsaved plugin. Save or Save + Start then writes them all at once, one after the other. If a value in one plugin is wrong, Save stays locked and the footer tells you which plugin it is. Reset only ever applies to the plugin that is open. When you close RMM it asks once more: save all, discard all or cancel.

**Save + Start** saves the same way and then starts the game through `tesmiolauncher.exe` without its window. RMM closes. If you want to see the launcher window, set `tesmiolauncher_window = 1` under `[settings]` in `rmm.ini`.

Before the start RMM also checks that every resource that switched-on plugins refer to exists in the Resources plugin. If one is missing, the message names the plugin and the start waits.

---

## 💡 The "Notes" card

The first tab of every plugin has a "Notes" card at the top. Everything you should know before saving appears there:

- **Blue:** information, such as a note from the plugin author, a pending update with old and new version, or that a dependency is active.
- **Yellow:** warnings, such as a dependency that exists but is not switched on. Switch it on, otherwise the plugin cannot be saved.
- **Red:** errors, such as a missing dependency, a DLL that exists twice, or a Workshop folder that differs between RMM and the Workshop Bridge. A red box blocks saving.

For packages that allow it, the "Files local only" switch is here as well.

---

## 🛤️ How a plugin gets into the game

The TesmioLoader alone loads only DLLs from `tesmioloader\build\plugins`. For Workshop packages there are three ways, and RMM detects on its own which one applies to you:

1. **Workshop Bridge** (comes with RMM): the plugin `workshop_bridge` loads the DLLs of the switched-on packages straight from the Steam Workshop folder. Steam keeps them up to date, nothing is copied. RMM only provides the INI.
2. **Soviet Mod Loader:** if SML is installed and switched on, it loads every subscribed package. RMM then writes only INI files and never a DLL. If a DLL of a package still lies in `plugins\`, RMM refuses to save because the plugin would load twice. The bridge stays idle under SML.
3. **TesmioLoader classic:** without bridge and SML, RMM copies DLL and INI to `plugins\` and switches the plugin on in `tesmioloader.ini`.

The Workshop Bridge appears as a plugin in the list itself. Its card has the "Bridge active" switch, the Workshop folder (normally "auto" = your game's Steam Workshop folder), the rule which packages are loaded, and a button "Tidy up now" that removes entries of packages you no longer have. You never edit the package list by hand; the "Plugin active" switch of the packages does that.

---

## 📁 Files local only

Some packages allow their files to be copied completely into the plugins folder, for example because they bring textures. The "Notes" card then has the switch "Files local only". Switching it on and saving first shows every file that will be copied, with the DLL's checksum; after "OK" RMM copies DLL, INI and the asset folder to `plugins\`. The bridge skips the package from then on, and the "Plugin active" switch works through `tesmioloader.ini`.

Switching it off and saving removes exactly these files again, after a confirmation with the list. RMM never touches foreign files in `plugins\`. Steam updates reach local files only when you save again; the yellow "Update" in the list reminds you.

---

## 📜 The log window

The log icon in the sidebar opens a window with three kinds of sources: the journal of this RMM session, `tesmioloader.log` from the loader folder, and every plugin log `tesmioloader.<plugin>.log`, whether it lies in the `logs\` subfolder or directly in the loader folder. The files can be read while the game runs; "Refresh" reads again, "Open folder" shows the loader folder in Explorer.

"Search" filters by text, "Sender" by the first word of a line (`plugin`, `bridge`, `hook`, a plugin name), "Problems and warnings only" hides everything unremarkable. Error lines are red, warnings yellow. The footer names lines, hits and the loader's last closing line.

---

## 🗂️ Profiles and restore points

The archive icon in the sidebar opens the window "Profiles and restore" with two tabs.

**Profiles** save the complete settings state under a name: `tesmioloader.ini`, all INIs in `plugins\` and `user_config\`, and the backed-up originals. DLLs are never part of it. "Save current state…" asks for a name and a note. On the right you see per file whether it matches the saved state. "Apply profile" writes everything back; INIs of plugins the profile does not know stay. Profiles live under `user_config\.autoload\profiles\`.

**Restore points** are created on every save: the state of all touched files from before the save, exactly one per plugin. "Reset to this point" writes these files back, including a DLL copied at the time; the current state becomes the new point, so you can go back again. Points whose files are missing are only listed.

Both actions need a closed game and a closed TesmioLauncher.

---

## 📝 List editors

Some plugins manage lists instead of single values. RMM shows them as a list on the left and fields on the right. This applies to the three plugins that come with the TesmioLoader and to Workshop packages that bring such a page (Deposits Plus, Research Expansion, Technical Service Storage, Vanilla Buildings, UI Layout Fixes, for example).

**Common to all:**

- Entries from the plugin or package carry a lock. You can change them but not delete them; the red button hides them, and "Hidden original entries" shows them again.
- You create your own entries with "+". The dialog asks only for what is needed and suggests what it can; everything else you set on the right afterwards. Own entries can be deleted.
- Identifiers that must name something in the game (a resource, a research entry, a building file, a text id) are checked by RMM before saving. If one is wrong, the footer turns red and names entry and field.
- A yellow warning reminds you when a change affects the save game, such as new resources or deposits that a running game then needs.

**Resources:** the list of all resources of the plugin. Template resource, transport class and material family are choice lists with the names from the game. There is no off switch, because the plugin has no safe one.

**Needs:** the list of needs with donor, factor, shop category, chance and dissatisfaction. Eight entries at most. The "General" tab carries the plugin's switches.

**Deposits:** the "General" tab with the plugin switches and the "Deposits" tab with one entry per deposit: identifier, type number, map, slot on the map, icon and more. The dialog suggests the next free type number; a duplicate is refused.

**Package editors** bring their own tabs, lists and help texts. Some have buttons that open the package's guides, picker windows for buildings, research entries or game texts, picture previews or a tab for translations.

---

## 🎮 Game version and rmm.ini

On start RMM reads the build stamp of `SOVIET64.exe`. If it belongs to no game version the plugins are made for, a warning appears and "Save + Start" asks before starting. Known is 1.1.1.9. A new stamp goes into `settings_schemas\game_versions.ini` under `[supported]`.

`rmm.ini` next to `rmm.exe` has a few switches, all explained in the file:

| Key | Meaning |
|---|---|
| `[paths] workshop_root` | Folder with the packages. Without the line (or with a `;` in front) RMM uses your game's Steam Workshop folder. An empty value is not allowed. |
| `[settings] language` | `auto` (German on a German Windows, otherwise English), `de` or `en`. |
| `[settings] version_check` | 0 turns the game version warning off. |
| `[settings] tesmiolauncher_window` | 1 shows the TesmioLauncher window on "Save + Start". |

What you set in the window takes priority over the file.

---

## 📦 Where your files are

```
SovietRepublic\tesmioloader\build\
├── rmm.exe, rmm.ini                    RMM and its base settings
├── settings_schemas\                   settings pages for the loader plugins
├── plugins\                            DLLs and effective INIs of the plugins
│   └── workshop_bridge.dll, .ini       the Workshop Bridge
├── user_config\
│   ├── <name>.ini                      your personal values per plugin
│   ├── workshop_bridge.ini             package list of the bridge
│   └── .autoload\
│       ├── <name>.upstream.ini         backed-up originals
│       ├── backups\<package>\previous\ restore points
│       └── profiles\                   your profiles
└── logs\                               logs of the plugins
```

Window size, language and selection are remembered under `%LOCALAPPDATA%\RepublicModManager`.

---

## 🛠️ When something does not work

| What you see | What is behind it | What helps |
|---|---|---|
| Red box "Dependency missing" | A package this plugin needs is not subscribed or not switched on | Subscribe to the package and switch it on with "Plugin active" |
| Red box "Duplicate DLL" | The DLL lies in `plugins\` and is loaded by SML or the bridge as well | Remove the copy in `plugins\` or use "Files local only" |
| Red box about the Workshop folder | RMM and the bridge read different folders | Set the folder to "auto" in the Workshop Bridge's card |
| "Not loaded on the last game start" | The plugin was off on the last start, or the game did not run since you switched it on | Save and start the game once |
| Save does not work | The game or the TesmioLauncher is running, or a value is out of range | Close the programs, read the red note in the footer |
| A value is different again after saving | The plugin read another INI than expected | Check the status line: where are the files? Open the log window |

For a bug report: right-click the message, "Copy text", plus the log window with "Problems and warnings only".

---

## 🧩 For plugin authors

How a Workshop package has to be built so that RMM shows it, and how a settings page with tabs, cards, fields, lists and translations is described, is in `SCHEMA_EN.md` (German: `SCHEMA_DE.md`).

---

**Attention, comrade:** this program was written with the help of an artificial intelligence. The five-year plans behind it were still drawn up, tested and sworn at by a human every time the game crashed. If you do not want AI in your code, just stick to the base game. No hard feelings, no re-education.

**GNU GPL v3.** Source: https://github.com/Kespri/workers-and-resources-mods/tree/main/tools/republic_mod_manager
