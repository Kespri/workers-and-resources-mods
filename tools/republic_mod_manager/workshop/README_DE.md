# 🛠️ Republic Mod Manager

[English](README_EN.md) | **Deutsch**

Einstellungen, Ein- und Ausschalten und Laden deiner TesmioLoader-Plugins in einem Fenster. Dieses Workshop-Paket bringt das Programm `rmm.exe`, die Einstellungsseiten für die mitgelieferten Loader-Plugins und die Workshop Bridge mit, die abonnierte Plugin-Pakete direkt aus dem Steam-Workshop lädt.

---

## 📋 Inhaltsverzeichnis

1. [Schnellstart](#-schnellstart)
2. [Was drin ist](#-was-drin-ist)
3. [Installation](#-installation)
4. [Update und Entfernen](#-update-und-entfernen)
5. [Dateistruktur](#-dateistruktur)
6. [Fehlerbehandlung](#-fehlerbehandlung)
7. [Lizenz & Credits](#-lizenz--credits)
8. [FAQ](#-faq)

---

## 🚀 Schnellstart

### Voraussetzungen

- *Workers & Resources: Soviet Republic* 1.1.1.9
- **TesmioLoader** von MaxLegend, im Spielordner installiert (im Steam-Workshop abonnieren und nach seiner Anleitung einrichten; dieses Paket enthält ihn nicht)
- Windows 10 oder 11 (.NET Framework 4 ist dabei)

### In drei Schritten

1. In Steam dieses Workshop-Objekt abonnieren und warten, bis der Download fertig ist.
2. Den Ordner des Objekts öffnen (`Steam\steamapps\workshop\content\784150\<Objektnummer>`) und **Install-RMM.bat** doppelklicken. Das Fenster sagt am Ende „Republic Mod Manager ist installiert“.
3. `rmm.exe` im Ordner `<Spiel>\tesmioloader\build` starten, oder die Desktop-Verknüpfung, wenn du sie angelegt hast.

---

## ✨ Was drin ist

### 🎯 Republic Mod Manager (`rmm.exe`)

- Eine Liste mit jedem abonnierten Plugin-Paket und jedem Plugin, das in `tesmioloader\build\plugins` liegt.
- Einstellungsseiten aus dem Schema des Pakets: Reiter, Karten, Schalter, Zahlenfelder mit Plus und Minus, Listen, Auswahlfenster für Gebäude, Forschungen und Spieltexte, alles mit Beschreibung und Bereichsprüfung.
- Schalter „Plugin aktiv“ je Paket. Deine persönlichen Werte liegen getrennt von der ausgelieferten INI, ein Paket-Update überschreibt sie nie.
- Profile und Wiederherstellungspunkte für die ganze Plugin-Konfiguration.
- Protokollfenster mit `tesmioloader.log` und allen Plugin-Protokollen, Probleme hervorgehoben.
- „Speichern + Starten“ startet das Spiel über `tesmiolauncher.exe`.
- Deutsch und Englisch, folgt der Windows-Sprache.

### 🌉 Workshop Bridge (`plugins\workshop_bridge.dll`)

Lädt die Plugin-Pakete, die du im Republic Mod Manager eingeschaltet hast, direkt aus dem Steam-Workshop-Ordner. Ohne Soviet Mod Loader. Die Liste der Pakete pflegt der Manager, du musst nichts von Hand eintragen.

### 📄 Einstellungsseiten (`settings_schemas\`)

Deutsche und englische Einstellungsseiten für die Plugins, die mit dem TesmioLoader kommen: Accumulator, Cities, Day and Night, Depletion, Deposits, Easy Start, Needs, Resources, Walking Distance und die Workshop Bridge.

---

## 💾 Installation

### Weg 1️⃣: Installer (empfohlen)

1. Spiel, TesmioLauncher und einen laufenden Republic Mod Manager beenden.
2. Im Ordner des Workshop-Objekts **Install-RMM.bat** doppelklicken.

Der Installer macht der Reihe nach:

- findet den Spielordner (er liegt in derselben Steam-Bibliothek wie der Workshop-Ordner; sonst fragt er dich),
- prüft, ob `tesmioloader\build\tesmioloader.dll` und `tesmioloader.ini` vorhanden sind, und bricht sonst mit einem Hinweis ab,
- kopiert `rmm.exe`, `rmm.ini`, die Anleitungen, `settings_schemas\` und die Workshop Bridge nach `tesmioloader\build`; ersetzte Dateien landen in `tesmioloader\rmm_install_backup\<Datum>`,
- lässt eine vorhandene `rmm.ini` und `plugins\workshop_bridge.ini` unangetastet (deine Grundeinstellungen),
- trägt `workshop_bridge=1` unter `[plugins]` in `tesmioloader.ini` ein,
- fragt, ob eine Desktop-Verknüpfung angelegt werden soll.

Geht etwas schief, bleibt das Fenster offen und zeigt in Rot, was fehlgeschlagen ist und warum. Läuft alles durch, schließt es sich nach fünf Sekunden von selbst.

Wenn der Spielordner nicht gefunden wird, kannst du ihn mitgeben:

```
Install-RMM.bat -GamePath "D:\Spiele\Steam\steamapps\common\SovietRepublic"
```

### Weg 2️⃣: Von Hand

Der Ordner **Manual Installation** enthält den kompletten Ordner `tesmioloader` genau so, wie er im Spielordner liegen muss.

1. Spiel, TesmioLauncher und Republic Mod Manager beenden.
2. Den Ordner `Manual Installation\tesmioloader` auf den Spielordner ziehen und das Zusammenführen bestätigen.
3. In `tesmioloader\build\tesmioloader.ini` unter `[plugins]` die Zeile `workshop_bridge=1` ergänzen (UTF-8 ohne BOM speichern).
4. `rmm.exe` starten.

Die Datei `Manual Installation\WOHIN.txt` wiederholt diese Schritte.

---

## 🔄 Update und Entfernen

**Update:** Steam aktualisiert nur den Ordner des Workshop-Objekts, nicht die Kopie im Spielordner. Nach einem Update einfach **Install-RMM.bat** erneut ausführen. Deine `rmm.ini`, `workshop_bridge.ini` und alles unter `user_config` bleiben erhalten.

**Entfernen:** **Uninstall-RMM.bat** doppelklicken. Es löscht die installierten Dateien, setzt `workshop_bridge=0` in `tesmioloader.ini` und entfernt die Desktop-Verknüpfung. Der Ordner `user_config` mit deinen Einstellungen bleibt stehen; der TesmioLoader wird nicht angefasst.

---

## 📦 Dateistruktur

**Workshop-Paket** (das, was du abonnierst)
```
<Objektnummer>\
├── Install-RMM.bat                 (Installer, Doppelklick)
├── Uninstall-RMM.bat               (Entfernen, Doppelklick)
├── Install-RMM.ps1                 (macht die eigentliche Arbeit)
├── Manual Installation\
│   ├── WOHIN.txt / WHERE.txt       (die drei Schritte von Hand)
│   └── tesmioloader\build\         (Zielstruktur, zum Drüberziehen)
│       ├── rmm.exe
│       ├── rmm.ini
│       ├── rmm.README_DE.md, rmm.README_EN.md   (Anleitung)
│       ├── rmm.SCHEMA_DE.md, rmm.SCHEMA_EN.md   (Referenz für Plugin-Autoren)
│       ├── settings_schemas\
│       └── plugins\
│           ├── workshop_bridge.dll
│           └── workshop_bridge.ini
├── workshopconfig.ini              (Steam-Workshop-Eintrag)
├── previewimage.png
├── LICENSE
├── README_DE.md
└── README_EN.md
```

**Spielordner** nach der Installation
```
SovietRepublic\tesmioloader\
├── build\
│   ├── tesmioloader.dll, tesmiolauncher.exe, tesmioloader.ini   (TesmioLoader, nicht aus diesem Paket)
│   ├── rmm.exe, rmm.ini, rmm.README_DE/EN.md, rmm.SCHEMA_DE/EN.md
│   ├── settings_schemas\
│   ├── plugins\
│   │   ├── workshop_bridge.dll
│   │   └── workshop_bridge.ini
│   ├── user_config\                (deine persönlichen Werte, vom Manager geschrieben)
│   └── logs\                       (Detail-Protokolle der Plugins)
└── rmm_install_backup\<Datum>\     (nur, wenn der Installer Dateien ersetzt hat)
```

---

## ⚙️ Fehlerbehandlung

| Meldung des Installers | Ursache | Abhilfe |
|---|---|---|
| Der TesmioLoader ist nicht installiert | `tesmioloader\build\tesmioloader.dll` oder `tesmioloader.ini` fehlt | TesmioLoader von MaxLegend abonnieren und nach seiner Anleitung installieren, dann erneut starten |
| Der Spielordner wurde nicht gefunden | Spiel liegt in einer anderen Steam-Bibliothek oder das Paket wurde woanders hin kopiert | `Install-RMM.bat -GamePath "<Spielordner>"` |
| Das Spiel oder der Republic Mod Manager läuft noch | Dateien sind in Benutzung | Programme beenden, erneut starten |
| Datei konnte nicht kopiert werden | Schreibrechte oder Virenscanner | Als Administrator starten oder den Ordner im Virenscanner freigeben |
| Das Paket ist unvollständig | Steam-Download unvollständig | Objekt abbestellen und neu abonnieren |

Windows SmartScreen kann `rmm.exe` beim ersten Start als unbekanntes Programm melden, weil es nicht signiert ist. „Weitere Informationen“ und „Trotzdem ausführen“ genügt einmal.

Der Manager selbst hat ein Protokollfenster (Symbol in der Seitenleiste) mit `tesmioloader.log` und den Plugin-Protokollen aus `build\logs\`. Die Anleitung des Managers liegt als `rmm.README_DE.md` (englisch `rmm.README_EN.md`) neben `rmm.exe`.

---

## 📜 Lizenz & Credits

**GNU GPL v3**, siehe `LICENSE` im Paket. Republic Mod Manager und die Workshop Bridge sind eigene Entwicklungen; der TesmioLoader von MaxLegend (GPL v3) ist nicht Teil dieses Pakets. Der vollständige Quelltext liegt unter https://github.com/Kespri/workers-and-resources-mods.

**Genosse, Achtung:** Dieses Programm wurde mit Hilfe einer künstlichen Intelligenz geschrieben. Die Fünfjahrespläne dazu hat trotzdem ein Mensch aufgestellt, getestet und beim Abstürzen des Spiels geflucht. Wer keine KI im Code möchte, bleibt einfach beim Grundspiel. Kein Hass, keine Umerziehung.

---

## ❓ FAQ

**F: Brauche ich den Soviet Mod Loader?**
A: Nein. Die mitgelieferte Workshop Bridge lädt die Pakete. Ist der Soviet Mod Loader trotzdem installiert, hält sich die Bridge zurück und der Manager schreibt nur die Einstellungen.

**F: Warum installiert das Abonnieren allein nichts?**
A: Steam legt Workshop-Dateien nur in seinen eigenen Ordner. Ein Programm im Spielordner muss jemand dorthin kopieren, das macht der Installer.

**F: Verliere ich meine Einstellungen bei einem Update?**
A: Nein. `rmm.ini` und `workshop_bridge.ini` werden nur angelegt, wenn sie fehlen, und `user_config` fasst der Installer nie an.

**F: Wo sind die Plugins?**
A: Jedes Plugin ist ein eigenes Workshop-Objekt. Abonnieren, im Manager einschalten, fertig.

---

**Letzte Aktualisierung:** Republic Mod Manager 0.4.69 mit Workshop Bridge 0.2.0  
**Für:** WRSR 1.1.1.9 | TesmioLoader API 4
