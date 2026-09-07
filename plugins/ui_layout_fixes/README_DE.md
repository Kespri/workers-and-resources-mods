# 🪟 UI Layout Fixes 1.1

**TesmioLoader-Plugin für gezielte Korrekturen an einzelnen Infofenstern**

Bündelt fensterbezogene Korrekturen für *Workers & Resources: Soviet Republic* 1.1.1.9. Jede Fensterart ist ein eigenes Modul mit eigener Konfiguration, Zielprüfung und eigenem Schalter. Derzeit enthalten: das Modul **CUSTOMHOUSE**, das den Zeilenabstand der Ressourcenliste im Zollhaus vergrößert. Es gibt bewusst keinen globalen Zeilenabstand.

---

## 📋 Inhaltsverzeichnis

- [Schnellstart](#-schnellstart)
- [Features](#-features)
- [Installation](#-installation)
- [Republic Mod Manager](#-republic-mod-manager)
- [Konfiguration](#-konfiguration)
- [Wertebereiche](#-wertebereiche)
- [Funktionsweise](#-funktionsweise)
- [Kompatibilität](#-kompatibilität)
- [Fehlerbehandlung](#-fehlerbehandlung)
- [Dateistruktur](#-dateistruktur)

---

## 🚀 Schnellstart

### Voraussetzungen
- Windows x64
- WRSR 1.1.1.9
- TesmioLoader API 4
- Keine Abhängigkeit zu anderen Plugins

### In drei Schritten
1. **Eine Installationsmethode wählen** (siehe unten) und das Plugin aktivieren.
2. **Zeilenabstand einstellen**, wenn gewünscht: `resource_row_pitch` von 25.0 bis 60.0 in `ui_layout_fixes.ini`, am bequemsten über Republic Mod Manager (Standard 30.0).
3. **Spiel vollständig neu starten** und ein Zollhaus öffnen. Im Detail-Log steht `[CUSTOMHOUSE] active`.

---

## ✨ Features

### 🎯 Grundfunktion
- ✅ Modul CUSTOMHOUSE: größerer Zeilenabstand der Ressourcenliste in Gebäuden mit `$TYPE_CUSTOMHOUSE`, also den Zollhäusern des Spiels und kompatiblen Mod-Gebäuden mit demselben Infofenster
- ✅ Nur die zwei geprüften Aufrufe der Ressourcenliste werden geändert: der Messdurchlauf (Listenhöhe) und der Zeichendurchlauf (Symbole und Texte); beide verwenden denselben Abstand
- ✅ Position der darunterliegenden Fensterabschnitte und Scrollbereich rechnet weiterhin das native Layout des Spiels; alle anderen Fenster behalten den nativen Abstand von 25,0 logischen Pixeln
- ✅ Nur geprüfter Code und Daten im Speicher des laufenden Spiels werden geändert; Spieldateien, Gebäudedateien und Spielstände bleiben unverändert

### 🆕 Seit 1.1
- ✅ **Gemeinsame Konfigurationsregel** (`tesmio_config.h`): Basis ist `plugins\ui_layout_fixes.ini`, sonst die INI neben der DLL (im Workshop-Paket). Liegt `user_config\ui_layout_fixes.ini` im Loader-Ordner, gewinnen dessen Schlüssel einzeln; diese Datei schreibt nur Republic Mod Manager. Beide Pfade stehen beim Start im Detail-Log.

---

## 💾 Installation

Wähle **eine** der vier Methoden. Dieselbe DLL darf nie zweimal geladen werden.

---

### Methode 1️⃣: Klassischer TesmioLoader

```
1. Kopiere ui_layout_fixes.dll und ui_layout_fixes.ini aus hooks\
   → tesmioloader\build\plugins\

2. Aktiviere ui_layout_fixes im TesmioLauncher
3. Spiel vollständig neu starten
```

---

### Methode 2️⃣: Soviet Mod Loader (SML)

```
1. Workshop-Objekt abonnieren – SML liest abonnierte Pakete von selbst
2. SML lädt die DLL über soviet.mod.ini aus dem Paket, die INI liegt daneben
3. Eine lokale ui_layout_fixes.dll in plugins\ vorher entfernen oder abschalten
```

---

### Methode 3️⃣: Workshop Bridge (ohne SML)

```
1. Paket in Republic Mod Manager auswählen
2. „Plugin aktiv“ einschalten
3. workshop_bridge lädt die DLL direkt aus dem Paket
4. Steam-Updates gelten sofort
```

---

### Methode 4️⃣: Republic Mod Manager mit „Dateien nur lokal“

```
1. Paket in Republic Mod Manager auswählen, Einstellungen anpassen, Speichern
2. Reiter „Allgemein“, Karte „Hinweise“: „Dateien nur lokal“ einschalten
3. Bestätigung mit Dateiliste → DLL und INI werden beim Speichern
   nach tesmioloader\build\plugins\ kopiert
4. Die Workshop Bridge überspringt das Paket danach automatisch
```

**„Dateien nur lokal“ im Detail**
- Für alle, die das Plugin ohne Steam-Abo weiterbenutzen wollen
- Steam-Updates gelten bei lokalen Dateien erst nach erneutem Speichern (gelbe Marke „Update“)
- Ausschalten entfernt nur die von Republic Mod Manager kopierten Dateien wieder

---

## 🧰 Republic Mod Manager

Das Paket enthält im Ordner `config` ein Launcher-Schema. Republic Mod Manager zeigt UI Layout Fixes damit in zwei Reitern, deutsch und englisch:

- **Allgemein:** Hinweise, „Dateien nur lokal“ und der Knopf für diese Anleitung
- **Zollhaus:** Modulschalter und Zeilenabstand der Ressourcenliste

Der Schalter „Plugin aktiv“ im Kopf setzt beim Einschalten auch `enabled = 1`. Persönliche Werte liegen in `user_config\ui_layout_fixes.ini`; die ausgelieferte INI bleibt unverändert. Wer die INI lieber von Hand bearbeitet, findet alles Weitere unten.

---

## ⚙️ Konfiguration

### Hauptdatei: `ui_layout_fixes.ini`

Die DLL liest in dieser Reihenfolge:
- **Basis:** `tesmioloader\build\plugins\ui_layout_fixes.ini`, falls vorhanden, sonst die INI neben der DLL (im Paket `hooks\ui_layout_fixes.ini`)
- **Overlay:** `tesmioloader\build\user_config\ui_layout_fixes.ini`, Schlüssel für Schlüssel darüber; hierhin schreibt Republic Mod Manager

⚠️ **Kommentare nur in eigenen Zeilen mit `;`.** Die Datei muss UTF-8 ohne BOM sein; Abschnitte und Schlüssel nicht wiederholen. Unbekannte oder falsch geschriebene Namen werden ignoriert, das Plugin verwendet dann unbemerkt den eingebauten Standard; deshalb die dokumentierten Namen unverändert lassen und nach Änderungen das Log prüfen. Die Einstellungen werden nur beim Start gelesen: INI ändern, Spiel vollständig neu starten.

```ini
[general]
; 1 aktiviert die einzeln eingestellten Module, 0 startet kein Modul und schreibt keinen Patch
enabled = 1

[customhouse]
; 1 aktiviert nur das Modul CUSTOMHOUSE; ohne Wirkung, solange [general] enabled = 0 ist
enabled = 1
; Zeilenabstand der Ressourcenliste in logischen Pixeln, 25.0 bis 60.0, Dezimalpunkt
; 25.0 = nativer Spielwert, 30.0 = Plugin-Standard
resource_row_pitch = 30.0
```

`enabled` wird als Ganzzahl gelesen: `0` schaltet ab, jeder andere Wert schaltet ein. Verwende trotzdem nur `0` oder `1`.

---

## 📏 Wertebereiche

| Schlüssel | Bereich | Standard |
|---|---|---|
| `[general] enabled` | 0 oder 1 | 1 |
| `[customhouse] enabled` | 0 oder 1 | 1 |
| `resource_row_pitch` | 25.0 bis 60.0 einschließlich, Dezimalpunkt | 30.0 |

Für `resource_row_pitch` gilt: `25.0` entspricht dem nativen Spielwert und bewirkt keinen größeren Abstand; nichtnumerische, unendliche oder außerhalb liegende Werte werden verworfen, das Plugin protokolliert eine Warnung (`invalid-config`) und verwendet `30.0`, das Modul bleibt aktiv.

---

## 🔬 Funktionsweise

Das Spiel verwendet für die Ressourcenliste nativ einen Abstand von `25.0f`. Das Plugin leitet nur die zwei geprüften CUSTOMHOUSE-Aufrufe über eine nahe Speicherbrücke um; während eines solchen Aufrufs wird der native Abstand vorübergehend durch den konfigurierten Wert ersetzt und unmittelbar danach wiederhergestellt. Unverändert bleiben dadurch: Ressourcenlisten anderer Fenster, die native Berechnung der Listenhöhe, die Positionierung der nachfolgenden Fensterabschnitte, der Scrollbereich, Warenmengen, Preise, Gebäudeeigenschaften, Spielstände und Dateien.

Vor dem Schreiben eines Patches prüft das Modul die Größe des geladenen Abbilds, den PE-Zeitstempel von `SOVIET64.exe`, die Signatur der CUSTOMHOUSE-Panel-Funktion, Mess- und Zeichenaufruf samt Ziel, die native Instruktion für den Zeilenabstand und die Erreichbarkeit der Speicherbrücke. Schlägt ein Schritt fehl, schreibt das Plugin keinen Patch und das Modul bleibt inaktiv.

| Eigenschaft | Erwarteter Wert |
|---|---|
| Spielversion | `SOVIET64.exe 1.1.1.9` |
| PE-Zeitstempel | `0x6A3EB6AD` |
| Größe des ausführbaren Abbilds | `0x00A9D000` |

---

## 💾 Kompatibilität

### Spiel und Loader
- Exakt unterstützte Spielversion 1.1.1.9; andere Versionen werden abgelehnt, weil Speicheradressen und Signaturen versionsabhängig sind
- Keine Abhängigkeit zum Localization-Plugin oder zu anderen TesmioLoader-Plugins
- Ein vollständiger Neustart des Spiels entfernt alle aktiven Speicheränderungen

### Spielstände
Das Plugin verändert weder Spieldateien noch Spielstände.

### Versionskompatibilität
- **1.1:** Konfiguration über `tesmio_config.h` (Basis plus persönliches Overlay); das Modul CUSTOMHOUSE ist gegenüber 1.0 unverändert
- **Zurück auf eine ältere Fassung:** alte DLL und die dazugehörige INI wiederherstellen

---

## ⚙️ Fehlerbehandlung

### Häufige Probleme

| Problem | Ursache | Lösung |
|---|---|---|
| Kein größerer Abstand | `[general]` oder `[customhouse]` `enabled = 0`, oder `resource_row_pitch = 25.0` | Schalter einschalten, Wert über 25.0 setzen, Spiel neu starten |
| `invalid-config` im Log | `resource_row_pitch` ungültig | Wert von 25.0 bis 60.0 mit Dezimalpunkt eintragen |
| `unsupported-build` | Spielversion nicht 1.1.1.9 | passende Plugin-Version verwenden |
| `panel-signature`, `row-pitch-signature`, `measure-call`, `draw-call` | erwarteter Maschinencode verändert | Spielversion und Konflikte mit anderen UI-Plugins prüfen |
| `near-allocation`, `bridge-range` | Speicherbrücke konnte nicht erzeugt werden | Spiel neu starten, Log aufbewahren |
| `call-protection`, `pitch-protection` | Speicherbereich nicht beschreibbar | Sicherheitssoftware und konkurrierende Plugins prüfen |
| `all window modules are disabled` | Plugin an, aber `[customhouse] enabled = 0` | Modul einschalten |
| `log-open` | Detail-Log nicht anlegbar | Schreibrechte prüfen; `tesmioloader.log` bleibt verfügbar |

### Logging

Meldungen stehen in `tesmioloader.log` (Warnungen, Fehler, Phasenzusammenfassungen) und im Detail-Log `tesmioloader.ui_layout_fixes.log` (vollständiges strukturiertes Protokoll mit Modul, Prüfregel und Windows-Fehlercode).
- In **Republic Mod Manager** öffnet das Symbol mit dem Dokument unten in der Plugin-Leiste die Protokollansicht mit Filter und Absender.

Erwartet bei erfolgreichem Start:
```
[CUSTOMHOUSE] active
```

---

## 📦 Dateistruktur

**Workshop-Paket** (Steam-Abo, SML, Workshop Bridge)
```
ui_layout_fixes\
├── hooks\
│   ├── ui_layout_fixes.dll         (Plugin)
│   └── ui_layout_fixes.ini         (Original-INI)
├── config\                         (Launcher-Schema für Republic Mod Manager)
│   ├── ui_layout_fixes.launcher.ini
│   └── languages\
│       ├── de.ini
│       └── en.ini
├── soviet.mod.ini                  (Manifest für SML, Bridge und Republic Mod Manager)
├── workshopconfig.ini              (Steam-Workshop-Eintrag)
├── previewimage.png
├── README_DE.md
└── README_EN.md
```

**Loader-Ordner** (Methode 1 von Hand oder „Dateien nur lokal“)
```
tesmioloader\build\
├── tesmioloader.ui_layout_fixes.log
├── plugins\
│   ├── ui_layout_fixes.dll
│   └── ui_layout_fixes.ini         (Original-INI)
└── user_config\
    └── ui_layout_fixes.ini         (persönliche Werte aus Republic Mod Manager)
```

Über die Bridge oder SML liegt in `plugins` nur die Original-INI, die DLL bleibt im Paket. Die persönlichen Werte liegen in jedem Fall in `user_config`.

---

## 📜 Lizenz & Credits

**GNU GPL v3**, siehe `LICENSE` im Paket. Das Plugin enthält keinen fremden Code; der Loader-SDK-Header stammt aus dem TesmioLoader von MaxLegend (GPL v3). Der vollständige Quelltext liegt unter https://github.com/Kespri/workers-and-resources-mods/tree/main/plugins/ui_layout_fixes.

---

## ❓ FAQ

**F: Gilt der Abstand auch für andere Gebäude?**
A: Nein. Nur für Gebäude mit `$TYPE_CUSTOMHOUSE`; alle anderen Fenster behalten den nativen Abstand.

**F: Muss ich das Spiel nach einer Änderung neu starten?**
A: Ja. Die Einstellungen werden nur beim Start gelesen.

**F: Was passiert bei einem ungültigen Zeilenabstand?**
A: Das Plugin warnt im Log und verwendet 30.0; das Modul bleibt aktiv.

**F: Kommen weitere Fenster dazu?**
A: Das Plugin ist dafür angelegt: jedes Fenster ist ein eigenes Modul mit eigenem Abschnitt in der INI.

**F: Muss ich die INI von Hand bearbeiten?**
A: Nein. Republic Mod Manager zeigt beide Schalter und den Zeilenabstand mit Beschreibung und prüft den Wertebereich.

---

**Letzte Aktualisierung:** UI Layout Fixes 1.1  
**Für:** WRSR 1.1.1.9 | TesmioLoader API 4
