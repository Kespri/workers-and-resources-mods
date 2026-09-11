# Workers & Resources: Soviet Republic – plugins for the TesmioLoader

[Deutsch](README_DE.md) | English

Source code of Kespri's TesmioLoader plugins for *Workers & Resources: Soviet Republic* 1.1.1.9. The plugins are distributed as Workshop packages; this repository is their source, as the GNU GPL v3 requires. Built DLLs are not kept here.

## Plugins

| Folder | Version | Purpose | Origin |
|---|---|---|---|
| `plugins/deposits_plus` | 0.4.6 | deposit types, natural generation, sandy meadow surface, working vehicles | fork of the `deposits` plugin from the TesmioLoader by MaxLegend (Tesmio), GPL v3 |
| `plugins/depletion` | 1.1.2 | deposit depletion, mine window display | continued `depletion` plugin from the TesmioLoader by MaxLegend, GPL v3 |
| `plugins/localization` | 0.4.0 | translation service: text packs in INI files become extended language files in the VFS | own work; ships the text packs `research_expansion`, `technical_service_storage`, `tesmio_lang` |
| `plugins/rail_physics_fix` | 1.3.5 | physical train dynamics: traction, braking, grade, curves, stations, customs, consumption | fork of [RailPhysics 1.3.0](https://github.com/TheRealMeowMeow00/WRSR_RailPhysics) by Meow Meow (TheRealMeowMeow00), GPL v3; [original on the Workshop](https://steamcommunity.com/sharedfiles/filedetails/?id=3776784867) |
| `plugins/research_expansion` | 0.4.0 | new research and changes to the research tree through the VFS | own work |
| `plugins/resources_button_fix` | 0.4.0 | compact button grids in the terrain editor | own work |
| `plugins/technical_service_storage` | 0.3.3 | grit storages, material priorities and snowplow tanks in the Technical Services | own work |
| `plugins/ui_layout_fixes` | 0.3.0 | layout corrections in game windows | own work |
| `plugins/vanilla_buildings` | 0.4.1 | patches for Vanilla, DLC and Workshop buildings without touching the original files | own work |
| `plugins/vehicle_materials` | 0.4.0 | material demand of vehicles | own work |
| `plugins/weather_roads` | 0.3.3 | road snow, melting and protection after plowing | own work |
| `plugins/workshop_bridge` | 0.2.0 | loads Workshop hook DLLs without Soviet Mod Loader | own work |

Shared files: `plugins/grit_spreader_api.h` (service between Technical Service Storage and Weather Roads), `plugins/tesmio_config.h` (INI base and overlay), `plugins/tests` (offline tests). `src/` holds the two SDK headers `tesmio_api.h` and `tesmio_plugin.h` from [TesmioLoader b0.3.6](https://github.com/MaxLegend/TesmioLoader) by MaxLegend (GPL v3), so the plugins build from this repository with their relative include paths.

The plugins were developed with the assistance of artificial intelligence.

## Building

Visual Studio 2022 or newer with the x64 C++ tools, then in a Developer Command Prompt inside the plugin folder:

```
cl /nologo /O2 /MT /W3 /EHsc /std:c++17 /LD /Fo"build\\" /Fd"build\\" /Fe"build\<name>.dll" <name>.cpp /link kernel32.lib
```

Rail Physics Fix and Deposits Plus ship their own `build.bat`. The exports `TsmPluginApiVersion`, `TsmPluginInit` and `TsmPluginStart` are the loader's plugin contract (API 4). Addresses and signatures apply to game version 1.1.1.9 (build 23935965) only.

## Installation

Every plugin has its own guide (`README_EN.md` in the plugin folder) with four ways: classic TesmioLoader, Soviet Mod Loader, Workshop Bridge and Republic Mod Manager. The Workshop packages contain DLL, INI, guide and a schema for Republic Mod Manager.

## Bug reports

Please open an issue with game version, loader version, active plugins and the logs `tesmioloader.log` and `tesmioloader.<plugin>.log` from `tesmioloader\build`.

## Licence

Everything in this repository is licensed under the **GNU General Public License v3**, see `LICENSE`. This applies to the own plugins as well as to the forks; the origin is given in the table above and in the individual guides. Exception: `plugins/deposits_plus/third_party` contains Microsoft code under the University of Illinois Open Source License (see `LICENSE.TXT` there).
