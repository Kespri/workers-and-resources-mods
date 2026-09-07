# ❄️ Weather Roads 0.2.11-beta

**TesmioLoader-Plugin für Straßenschnee, Schmelze und Schutz nach dem Räumen**

Steuert in *Workers & Resources: Soviet Republic* 1.1.1.9, wie schnell sich Schnee auf Straßen aufbaut, wie schnell er natürlich schmilzt, und gibt geräumten Straßen einen zeitlich begrenzten Schutz: eine starke Phase in Spielminuten, danach eine schwächere „Salzphase“ in Spielstunden. Mit Technical Service Storage entscheidet das geladene Streugut, wie stark dieser Schutz ist. Schnee und Schmelze funktionieren auch ohne Technischen Service und ohne Schneepflug.

---

## 📋 Inhaltsverzeichnis

- [Schnellstart](#-schnellstart)
- [Features](#-features)
- [Installation](#-installation)
- [Republic Mod Manager](#-republic-mod-manager)
- [Konfiguration](#-konfiguration)
- [Schnee, Schmelze und Darstellung](#-schnee-schmelze-und-darstellung)
- [Schutz nach dem Räumen](#-schutz-nach-dem-räumen)
- [Overlay](#-overlay)
- [Speichern und Laden](#-speichern-und-laden)
- [Wertebereiche](#-wertebereiche)
- [Kompatibilität](#-kompatibilität)
- [Fehlerbehandlung](#-fehlerbehandlung)
- [Dateistruktur](#-dateistruktur)

---

## 🚀 Schnellstart

### Voraussetzungen
- Windows x64
- WRSR 1.1.1.9 (die Kennungen von `SOVIET64.exe` und `C3DDLL64.dll` sowie die Hook-Signaturen werden geprüft; andere Stände werden abgewiesen)
- TesmioLoader API 4
- **Optional:** Technical Service Storage mit seinem Streugut-Dienst für materialabhängigen Schutz und die Erkennung von Trockenpflügen; ohne ihn zählt jedes Räumen mit Stärke 1.00
- **Kein** Localization-Plugin nötig; das Overlay hat eingebaute deutsche und englische Texte

### In drei Schritten
1. **Eine Installationsmethode wählen** (siehe unten) und das Plugin aktivieren.
2. **Werte prüfen:** Die mitgelieferten Einstellungen sind abgestimmt (Schnee 30 %, Schmelze 45 %, starke Phase 240 Spielminuten, Salzphase 24 Spielstunden). Overlay und Detailprotokoll bleiben für den normalen Betrieb aus.
3. **Spiel vollständig neu starten.** `weather_roads.log` nennt Version, Konfigurationspfad, Signaturprüfung, aktive Teilfunktionen und den Status des Streugut-Dienstes.

---

## ✨ Features

### 🎯 Grundfunktion
- ✅ Skalierter und wahlweise schrittweiser Aufbau des internen Straßenschnees mit Obergrenze je Schneeschub
- ✅ Unabhängig einstellbare natürliche Schneereduktion
- ✅ Starke Schutzphase nach dem Räumen, anschließend eine schwächere Salzphase mit einstellbarem Faktor
- ✅ Materialabhängige Wirkung über den Streugut-Dienst von Technical Service Storage; Erhalt der Behandlung beim Trockenpflügen, wenn gewünscht
- ✅ Vorrang stärkerer aktiver Behandlungen vor schwächerem Material
- ✅ Visuelle Schneekorrektur auf erfassten Straßenbereichen
- ✅ Spielstandbezogene Speicherung von Behandlung und visuellen Schattenwerten, ohne Neustart der Schutzdauer beim Laden
- ✅ Optionales Diagnose-Overlay (F10) und ausführliche Ereignisprotokolle
- ✅ Keine VFS-Overrides, keine Änderung an Spiel- oder Speicherdateien; unbekannte Spielstände werden vor der Hook-Installation abgewiesen

### 🆕 Neu in 0.2.11
- ✅ **INI neben der DLL:** Fehlt `plugins\weather_roads.ini`, liest die DLL die INI aus dem eigenen Ordner, also aus dem Workshop-Paket unter Soviet Mod Loader oder der Workshop Bridge. Der gewählte Pfad steht als `configuration file:` im Protokoll. Werte werden genau wie bisher gelesen.
- ✅ Schema für Republic Mod Manager im Paket: fünf Reiter mit allen Einstellungen, deutsch und englisch.
- Schneeabstimmung, Schutzstärken und -dauern, Trockenpflügen, Materialkommunikation, Speicherformat und Hook-Reihenfolge sind gegenüber 0.2.10 unverändert.

---

## 💾 Installation

Wähle **eine** der vier Methoden. Dieselbe DLL darf nie zweimal geladen werden. `weather_roads_probe.dll` (alter Vorgänger) nicht gleichzeitig laden; das Plugin verweigert dann die Initialisierung.

---

### Methode 1️⃣: Klassischer TesmioLoader

```
1. Kopiere weather_roads.dll und weather_roads.ini aus hooks\
   → tesmioloader\build\plugins\

2. Aktiviere weather_roads im TesmioLauncher
3. Spiel vollständig neu starten
```

---

### Methode 2️⃣: Soviet Mod Loader (SML)

```
1. Workshop-Objekt abonnieren – SML liest abonnierte Pakete von selbst
2. SML lädt die DLL über soviet.mod.ini aus dem Paket, die INI liegt daneben
3. Eine lokale weather_roads.dll in plugins\ vorher entfernen oder abschalten
```

---

### Methode 3️⃣: Workshop Bridge (ohne SML)

```
1. Paket in Republic Mod Manager auswählen
2. „Plugin aktiv“ einschalten
3. workshop_bridge lädt die DLL direkt aus dem Paket; die INI findet sie neben sich
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

Das Paket enthält im Ordner `config` ein Darstellungsschema. Republic Mod Manager zeigt Weather Roads damit in fünf Reitern, deutsch und englisch:

- **Allgemein:** Hinweise, „Dateien nur lokal“, Knopf für diese Anleitung; Plugin, Straßenschutz speichern, Detailereignisse
- **Schnee und Schmelze:** Schneeaufbau, natürliches Abschmelzen, Darstellung geräumter Bereiche
- **Schneeräumen:** starke Phase, Salzphase, Faktor, Trockenpflügen
- **Overlay:** Fenster, Sprache, Taste, Position und Aussehen
- **Erweitert:** Zeitabstände und Bündelung des schrittweisen Aufbaus

Republic Mod Manager schreibt die wirksame Datei `plugins\weather_roads.ini` mit allen Schlüsseln; die INI im Paket bleibt unverändert.

---

## ⚙️ Konfiguration

### Hauptdatei: `weather_roads.ini`

Die DLL liest in dieser Reihenfolge:
- **Zuerst:** `tesmioloader\build\plugins\weather_roads.ini`, falls vorhanden (klassische Installation, „Dateien nur lokal“ oder die von Republic Mod Manager geschriebene wirksame INI)
- **Sonst:** die INI neben der DLL, im Paket `hooks\weather_roads.ini` (Soviet Mod Loader, Workshop Bridge)

**UTF-8 ohne BOM.** `;` nur in eigenen Zeilen, keine Kommentare hinter Werten. Dezimalwerte mit Punkt (`0.30`), Schalter als `0` oder `1`. Änderungen gelten nach einem vollständigen Neustart.

Bekannte Zahlenwerte werden auf gültige Zahlen und Bereiche geprüft; ungültige Werte führen zu einer Meldung `invalid [Abschnitt]` und zum eingebauten Ersatzwert. Unbekannte oder doppelte Schlüssel werden **nicht** umfassend geprüft: Namen genau übernehmen, nichts doppelt eintragen.

⚠️ **Die INI installiert lassen.** Fehlende Schlüssel nehmen die eingebauten Ersatzwerte der DLL, und die weichen teils von der mitgelieferten Datei ab: Fehlt `[overlay] enabled`, gilt `1` und das Overlay erscheint. Zum Abschalten des Plugins `[general] enabled = 0` setzen und neu starten.

### Abschnitte

| Abschnitt | Aufgabe | Mitgelieferte Werte |
|---|---|---|
| `[general]` | gesamtes Plugin | `enabled = 1` |
| `[snow]` | Schneeaufbau | `enabled = 1`, Multiplikator `0.30`, Obergrenze `50`, schrittweise `1` |
| `[melting]` | natürliche Schneereduktion | `enabled = 1`, Multiplikator `0.45` |
| `[visual_snow]` | Darstellung erfasster Bereiche | Stufen `0`, Shader-Bereich `0.85`, Kurve `1.00` |
| `[snowplow]` | Schutz nach dem Räumen | `enabled = 1`, `240.00` Spielminuten, danach `24.00` Spielstunden, Faktor `0.50`, Trockenpflügen erhält `1` |
| `[persistence]` | Zusatzdaten speichern und laden | `enabled = 1` |
| `[overlay]` | Informationsfenster | `enabled = 0`, `auto`, `top_right`, 20/80, Breite 350, Deckkraft 220, 250 ms, Taste 121 (F10), Vordergrund `0` |
| `[logging]` | Detailereignisse und Spiegelung | beide `0` |
| `[advanced]` | Zeitabstände und Bündelung | `1000` ms, `1` Einheit, `125` ms, `10` Einheiten |

Weitere Schlüssel, die die DLL kennt, aber die INI nicht enthält, stehen unter [Wertebereiche](#-wertebereiche).

---

## 🌨️ Schnee, Schmelze und Darstellung

- `accumulation_multiplier` skaliert positive interne Schneezuwächse; `maximum_accumulation_per_burst` begrenzt die Summe eines zusammengehörigen Schubs (`0` hebt nur diese Grenze auf). `50` heißt nicht, dass jeder Schneefall 50 Einheiten bringt.
- `gradual_accumulation = 1` verteilt geprüfte Wetterzuwächse auf kleine Schritte. Die Abstände in `[advanced]` sind echte Millisekunden; die Freigabe braucht zusätzlich fortschreitende Spielzeit und pausiert im Spiel.
- `[melting]` skaliert den geprüften nativen Reduktionswert `-30`. `0.00` unterdrückt diesen Schmelzpfad, nicht das Räumen durch Fahrzeuge oder vollständige `-255`-Rücksetzungen.
- `[visual_snow]` verändert die visuelle Zuordnung der Nachwirkung auf erfassten Straßenpixeln, nicht den Schnee der ganzen Karte und nicht die interne Schneemenge. Dafür müssen der Schneepflug-Nacheffekt und seine Textur-Hooks aktiv sein.

---

## 🚜 Schutz nach dem Räumen

Die Materialstärke `S` liefert der Streugut-Dienst von Technical Service Storage (Liste `[grit_materials]`). Sie bestimmt die verhinderte Schneemenge, **nicht die Schutzdauer**:

| Phase | Verbleibender Anteil neuen Schnees | Beispiel `S = 0.50` |
|---|---|---|
| starke Phase (`protection_minutes`, Spielminuten) | `1 - S` | 50 % |
| Salzphase (`salt_effect_hours`, Spielstunden) mit Faktor `M` | `1 - S × (1 - M)` | bei `M = 0.50`: 75 % |

Die Faktoren wirken zusätzlich zur Schneeskalierung; Rundung und Schubgrenze beeinflussen die einzelnen Schritte.

- Schwächeres Material vermindert oder erneuert keine stärkere aktive Behandlung; gleich starkes oder stärkeres erneuert sie.
- `dry_plowing_preserves_treatment = 1` erhält beim **vom Dienst erkannten** Trockenpflügen eine bestehende Behandlung samt Alter; eine unbehandelte Straße erhält dadurch keinen Schutz. Bei `0` entfernt Trockenpflügen die Behandlung.
- Ohne kompatiblen Dienst oder bei einem nicht zuordenbaren Räumvorgang gilt Stärke `1.00`. Das ist keine Bestätigung, dass Streugut geladen war; den Status `grit-spreader` im Protokoll prüfen.
- „Salz“ benennt nur die Wirkungsphase. Weather Roads erzeugt und verbraucht keine Ressource.

---

## 🪟 Overlay

Das Overlay ist mitgeliefert **ausgeschaltet**. Zum Anzeigen `[overlay] enabled = 1` setzen und neu starten.

- `language = auto`: Deutsch bei deutscher Windows-Oberfläche, sonst Englisch; **nicht** die Spielsprache. `de` oder `en` legen die Sprache fest.
- `toggle_key = 121`: F10 blendet das Fenster ein oder aus; `0` deaktiviert die Taste. Die Fußzeile nennt weiterhin F10.
- `foreground_only = 1` zeigt das Fenster nur bei Spiel im Vordergrund.
- Das Fenster nimmt keine Mausklicks entgegen. Ohne gefundenes Spielfenster wird eine Monitor-Ersatzposition verwendet.

Das Overlay zeigt Stichproben und Diagnosezähler, keine Statistik aller Straßen. Sein Ausfall schaltet die Wetterfunktionen nicht ab.

---

## 💾 Speichern und Laden

Bei aktiver Persistenz entsteht im Spielstandordner `tesmioloader.weather_roads.protection.bin` mit Materialstärke, ursprünglichem Behandlungszeitpunkt, Rundungsresten und bekannten visuellen Straßenschneedaten. Die Datei wird nach dem erfolgreichen letzten nativen Dateiabschluss über eine temporäre Datei geschrieben; native Speicherdateien werden nicht angefasst.

Beim Laden werden Format, Größen, Datensätze, Prüfsummen und Fingerabdrücke von `header.bin`, `road.bin` und `mask.dds` geprüft. Straßen werden über Geometrie und Abschnitt zugeordnet, nicht über alte Adressen; mehrdeutige Zuordnungen werden übersprungen.

- Laden startet die Schutzdauer nicht neu; höhere INI-Zeitwerte verlängern gespeicherte Behandlungen nicht nachträglich.
- Ohne Zusatzdatei lädt der Spielstand normal (`status=no-sidecar`); frühere Behandlungen sind dann nicht wiederherstellbar.
- Beschädigte oder nicht passende Zusatzdaten werden verworfen. Sehr alte Dateien aus 0.2.7 können wegen des damaligen Speicherzeitpunkts abgelehnt werden; neu behandeln lassen und speichern.
- `[persistence] enabled = 0` unterbindet Lesen und Schreiben; vorhandene Dateien bleiben liegen.
- Beim Übertragen eines Spielstands den ganzen Ordner mitnehmen. Steam Cloud und ZIP-Autosaves sind nicht als geprüft bestätigt.

---

## 📏 Wertebereiche

| Größe | Grenze |
|---|---|
| `accumulation_multiplier`, `reduction_multiplier` | 0.00 bis 10.00 |
| `maximum_accumulation_per_burst` | 0 bis 255 |
| `snow_levels` / `shader_range` / `visual_curve` | 0–8 / 0.00–1.00 / 0.10–5.00 |
| `protection_minutes` / `salt_effect_hours` | 0.00–1440.00 Spielminuten / 0.00–168.00 Spielstunden |
| `salt_accumulation_multiplier` | 0.00 bis 1.00 |
| Overlay `offset_x`, `offset_y` / `window_width` / `opacity` | 0–4000 / 280–600 / 80–255 |
| `update_interval_ms` / `toggle_key` | 100–2000 ms / 0–255 |
| `burst_reset_after_ms` / `gradual_step_units` / `gradual_step_interval_ms` / `visual_update_batch_units` | 100–10000 ms / 1–32 / 16–5000 ms / 1–255 |

Weitere unterstützte Schlüssel mit eingebauten Ersatzwerten (nicht in der mitgelieferten INI, nur mit Anlass ändern):

| Abschnitt | Schlüssel | Ersatzwert | Bereich |
|---|---|---|---|
| `[logging]` | `weather_sample_interval_ms` | 500 | 100–60000 ms |
| `[logging]` | `weather_heartbeat_seconds` / `road_heartbeat_seconds` / `road_state_heartbeat_seconds` / `mask_sample_heartbeat_seconds` | 60 / 30 / 30 / 30 | 0–3600 s (`0` unterdrückt die Meldung) |
| `[logging]` | `road_state_sample_interval_ms` / `mask_batch_interval_ms` / `mask_sample_interval_ms` | 500 / 5000 / 2000 | 50–60000 / 500–60000 / 250–60000 ms |
| `[safety]` | `road_state_tracking_seconds` | 900 | 10–86400 s |
| `[safety]` | `maximum_tracked_roads` / `maximum_road_bytes` | 16 / 16384 | 1–32 / 16–1048576 |
| `[safety]` | `maximum_tracked_plow_points` / `maximum_tracked_mask_points` | 8192 / 8 | 128–8192 / 1–16 |

---

## 💾 Kompatibilität

### Spielstände
Ohne das Plugin lädt der Spielstand normal; die Zusatzdatei wird dann nicht angewendet. Bereits geschriebene Zusatzdateien werden beim Abschalten nicht gelöscht.

### Andere Plugins
- **Technical Service Storage** liefert Materialstärke und Trockenpflügen; ohne es gilt Stärke 1.00.
- Die Wetteraufruf-Verkettung berücksichtigt den Hook von `daynight`; weitere Kombinationen sind nicht geprüft.
- `weather_roads_probe.dll` darf nicht gleichzeitig geladen sein.

### Versionskompatibilität
- **0.2.11:** INI-Fallback neben der DLL, Schema im Paket; Betrieb und Speicherformat unverändert
- **0.2.10:** defensive Loader-Prüfungen, Aufräumen bei Ausnahmen, klarere Startmeldungen
- **0.2.8:** korrigierter Speicherzeitpunkt der Zusatzdatei
- **Zurück auf eine ältere Fassung:** alte DLL und die dazugehörige INI wiederherstellen

---

## ⚙️ Fehlerbehandlung

### Häufige Probleme

| Problem | Ursache | Lösung |
|---|---|---|
| `unsupported SOVIET64.exe` / `unsupported C3DDLL64.dll` | nicht unterstützter Spielstand | passende Plugin-Version verwenden |
| `weather_roads_probe.dll is already loaded` | alter Vorgänger aktiv | Vorgänger abschalten, neu starten |
| `invalid [Abschnitt] …` | Wert ungültig oder außerhalb des Bereichs | Wert prüfen; der Ersatzwert wurde verwendet |
| `grit-spreader service unavailable` | Technical Service Storage fehlt | Stärke 1.00 aktiv; Plugin und Dienst prüfen |
| `protection-persistence=unavailable` | Speicher-Hook fehlt oder abgelehnt | vorherige Warnungen prüfen |
| `rejected-invalid-or-stale-sidecar` | Zusatzdatei passt nicht zum Spielstand | zusammengehörige Sicherung verwenden oder Schutz neu aufbauen |
| `atomic sidecar write failed` | Schreibzugriff, Speicherplatz | Rechte und freien Platz prüfen |
| `detail-open` / `detail-write` / `wait-failed` | Protokoll oder Worker gestört | Hauptlog prüfen, Spiel neu starten |
| Overlay fehlt | `enabled = 0`, Taste, Vordergrundregel | Einstellung und Meldung zur Fenstererstellung prüfen |
| Details fehlen | `detailed_events = 0` | `1` setzen; die Spiegelung allein reicht nicht |

### Logging

Meldungen stehen in `tesmioloader.log` (gespiegelte Status-, Warn- und Fehlermeldungen) und in `weather_roads.log` (Plugin-Protokoll, mit `detailed_events = 1` auch EVENT-Meldungen). Nach der Fehlersuche die Details wieder ausschalten.
- In **Republic Mod Manager** öffnet das Symbol mit dem Dokument unten in der Plugin-Leiste die Protokollansicht mit Filter und Absender.

Suche nach:
- `configuration file` → welche INI gewählt wurde
- `protection persistence save`, `load`, `restore-road`, `restore-visual` → Speichern und Laden; `stage=after-final-native-close` ist der Speicherzeitpunkt, `native_save_fingerprints=matched` bestätigt passende native Dateien
- `grit-spreader` → Anbindung an Technical Service Storage

---

## 📦 Dateistruktur

**Workshop-Paket** (Steam-Abo, SML, Workshop Bridge)
```
weather_roads\
├── hooks\
│   ├── weather_roads.dll           (Plugin)
│   └── weather_roads.ini           (Original-INI mit Erklärungen)
├── config\                         (Schema für Republic Mod Manager)
│   ├── weather_roads.launcher.ini
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
├── plugins\
│   ├── weather_roads.dll
│   └── weather_roads.ini           (wirksame INI)
├── tesmioloader.log
└── weather_roads.log
```

---

## 📜 Lizenz & Credits

Das Plugin enthält keinen fremden Code. Der Quellcode liegt im TesmioLoader-Quellbaum unter `my_plugins\weather_roads`, nicht in diesem Paket. Der Service-Header `grit_spreader_api.h` verbindet es mit Technical Service Storage.

---

## ❓ FAQ

**F: Brauche ich Technical Service Storage?**
A: Nein. Schnee, Schmelze und der Schutz nach dem Räumen funktionieren ohne; nur die Materialstärke und das Trockenpflügen kommen von dort.

**F: Warum zeigt sich das Overlay, obwohl ich nichts eingestellt habe?**
A: Die INI fehlt oder hat keinen Schlüssel `[overlay] enabled`; der eingebaute Ersatzwert ist 1. Die mitgelieferte INI setzt ausdrücklich 0.

**F: Wird der Schutz beim Laden erneuert?**
A: Nein. Alter und Reststärke werden aus der Zusatzdatei übernommen.

**F: Woran erkenne ich, dass das Plugin läuft?**
A: An `weather_roads.log`: Version, `configuration file`, Signaturprüfung und die Liste der aktiven Teilfunktionen.

**F: Kann ich die INI von Hand bearbeiten?**
A: Ja, mit den Regeln aus [Konfiguration](#-konfiguration). Republic Mod Manager bietet dieselben Werte mit Beschreibung und Bereichsprüfung.

---

**Letzte Aktualisierung:** Weather Roads 0.2.11-beta  
**Für:** WRSR 1.1.1.9 | TesmioLoader API 4
