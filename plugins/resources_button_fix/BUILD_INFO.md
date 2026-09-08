# Resources Button Fix – build notes

Target: WRSR 1.1.1.9, TesmioLoader API 4. Build: MSVC x64, `/std:c++17 /O2 /MT /W3 /EHsc /LD`,
kernel32.lib only. Workshop package: `My Plugins\resources_button_fix` (local_copy). History newest first.

## 0.4.0 (2026-09-09)

Declared finished by the user after the text review; version string `0.4.0` follows 1.1 (user's
numbering). No runtime change: hooks, layout logic, INI keys and defaults are those of 1.1.
Package only: RMM texts (57 keys de/en plus the English schema fallbacks) rewritten in player style;
`[launcher] notice` (restart only for the plugin switch, live reload when the window is reopened)
replaces the group notice; group "General and diagnostics" renamed "Log settings" with the detail
log only; group descriptions of both windows shortened; ON/OFF wording. READMEs updated.
Backup: `_backups\resources_button_fix_1.1_before_0.4.0_*`.

## 1.1 (2026-09-07)

INI fallback beside the DLL for Workshop packages (SML, Workshop Bridge); the chosen configuration
file is logged at start and the live reload follows it. Layout logic unchanged from 1.0.
