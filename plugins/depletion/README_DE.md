# ⛏️ Depletion 1.1.2

**TesmioLoader-Plugin für Vorkommen, die sich erschöpfen**

Im Grundspiel von *Workers & Resources: Soviet Republic* 1.1.1.9 ist ein Vorkommen unendlich: Eine Mine misst die Ergiebigkeit einmal beim Bau und fördert danach für immer mit derselben Rate. Depletion nimmt die geförderten Tonnen aus der Vorkommenskarte heraus, leitet die Förderqualität laufend aus dem Rest ab und zeigt im Minenfenster, wie viel noch da ist. Die Erschöpfung liegt in der Karte selbst, also im Spielstand, unter dem Minimap-Overlay und geteilt zwischen allen Minen über demselben Fleck.

Das Plugin ist die weiterentwickelte Fassung des Plugins `depletion` aus dem TesmioLoader von MaxLegend (Tesmio).

---

## 📋 Inhaltsverzeichnis

- [Schnellstart](#-schnellstart)
- [Features](#-features)
- [Installation](#-installation)
- [Republic Mod Manager](#-republic-mod-manager)
- [Konfiguration](#-konfiguration)
- [Funktionsweise](#-funktionsweise)
- [Wertebereiche](#-wertebereiche)
- [Kompatibilität](#-kompatibilität)
- [Fehlerbehandlung](#-fehlerbehandlung)
- [Dateistruktur](#-dateistruktur)

---

## 🚀 Schnellstart

### Voraussetzungen
- Windows x64
- WRSR 1.1.1.9 (die Hook-Adressen gelten nur für diesen Stand)
- TesmioLoader API 4
- **Optional:** das Plugin `deposits` oder Deposits Plus, damit auch Mod-Vorkommen erschöpfen
- **Optional:** Localization mit dem Textpaket `tesmio_lang` für die übersetzte Beschriftung im Minenfenster

### In drei Schritten
1. **DLL und INI in den Plugin-Ordner** des Loaders legen und `depletion` im TesmioLauncher aktivieren; auf einer **Kopie des Spielstands** beginnen.
2. **Balance prüfen:** `tonnes_per_texel` bestimmt, wie lange ein Vorkommen reicht. Das Protokoll nennt beim ersten Abtasten jeder Mine die Texelzahl und die daraus berechnete Reserve.
3. **Spiel vollständig neu starten.** Im Minenfenster erscheint die Zeile „Deposit remaining“, im Protokoll alle 60 Sekunden je Mine eine Fortschrittszeile.

---

## ✨ Features

### 🎯 Grundfunktion
- ✅ Fördermenge wird je Tick integriert und aus der Vorkommenskarte abgebucht; die Förderqualität folgt dem Rest
- ✅ Grundspiel-Vorkommen wählbar: Öl, Eisen, Kohle, Uran, Bauxit, Kies, mit eigenem Tonnenwert je Vorkommen
- ✅ Mod-Vorkommen aus dem `deposits`-Plugin werden automatisch einbezogen, `deplete = 0` nimmt ein Vorkommen aus
- ✅ Zeile „Deposit remaining: 251.4 kt / 259.3 kt (96.9 %)“ im Minenfenster, das Fenster wächst um eine Zeile
- ✅ Fortschrittszeilen im Protokoll zum Kalibrieren
- ✅ Kartenschreibzugriffe nur vom Render-Thread, gebündelt und höchstens viermal pro Sekunde

### 🆕 Neu in 1.1.2
- ✅ **Optionale Übersetzung:** Die Beschriftung der Zeile kommt aus dem Localization-Schlüssel `tesmio_lang.depletion.deposit_remaining` (Deutsch „Restvorkommen“, Englisch „Deposit remaining“). Ohne Localization, ohne Schlüssel oder bei zu langem Text bleibt `panel_caption` aus der INI. Zahlen, Einheiten und Position der Zeile sind unverändert.

### 🔧 Korrekturen aus 1.1.1
- ✅ Der Minen-Cache wird beim Laden aus dem Hauptmenü vollständig verworfen, samt ausstehender Abbuchungen; alte Gebäudeadressen erreichen die Anzeige nicht mehr
- ✅ Die Erfassung startet erst beim ersten Render-Aufruf des neu geladenen Geländes
- ✅ Minen-Cache und Weltwechsel sind gegen gleichzeitige Tick- und Render-Zugriffe gesperrt; native Aufrufe laufen außerhalb der Sperre
- ✅ Vor Schreiben und Anzeige wird geprüft, ob eine gespeicherte Adresse noch zur erwarteten Minenart gehört
- ✅ Geöffnete Texturen werden auch bei einer Ausnahme geschlossen
- ✅ Schlägt nach dem ersten Hook ein weiterer fehl, wird das Plugin deaktiviert, die DLL bleibt geladen

---

## 💾 Installation

Depletion ist kein Workshop-Paket, sondern ein Plugin des Loader-Ordners. Es gibt darum nur den klassischen Weg; Republic Mod Manager bearbeitet es als installiertes Plugin.

```
1. Kopiere depletion.dll und depletion.ini
   → tesmioloader\build\plugins\

2. Aktiviere depletion im TesmioLauncher
3. Spiel vollständig neu starten
```

Für Mod-Vorkommen muss `deposits.dll` oder Deposits Plus geladen sein, sonst erschöpfen nur die Vorkommen des Grundspiels. Für die übersetzte Beschriftung Localization mit dem Textpaket `tesmio_lang` aktivieren.

---

## 🧰 Republic Mod Manager

Republic Mod Manager liefert ein lokales Schema (`settings_schemas\depletion.launcher.ini`) und zeigt Depletion in zwei Reitern, deutsch und englisch:

- **Allgemein:** Plugin aktiv, Zeile im Minenfenster, Beschriftung, Diagnose (Abstand der Fortschrittszeilen)
- **Abbau:** Tonnen je Texel, Grundspiel-Vorkommen, Sekunden zwischen Kartenschreibzugriffen

Die vorgefundene INI wird als Original nach `user_config\.autoload\depletion.upstream.ini` gesichert, die wirksame INI dauerhaft geschrieben; „Original wiederherstellen“ steht in der Karte Hinweise.

---

## ⚙️ Konfiguration

### Hauptdatei: `plugins\depletion.ini`

Ein Abschnitt `[depletion]`, UTF-8 ohne BOM, Kommentare mit `;` in eigenen Zeilen. Änderungen gelten nach einem vollständigen Neustart.

| Schlüssel | Standard | Bedeutung |
|---|---:|---|
| `enabled` | 1 | 0 = Grundspiel, das Plugin hookt nichts und entlädt sich |
| `tonnes_per_texel` | 1200 | Wert eines voll gesättigten Texels in Tonnen Förderung; der Balance-Regler |
| `vanilla` | `oil,iron,coal,uranium,bauxite,gravel:30000` | Grundspiel-Vorkommen, die erschöpfen; `all`, `none`, eigener Tonnenwert nach Doppelpunkt |
| `flush_seconds` | 5 | Echtsekunden zwischen Schreibzugriffen auf die Karte |
| `log_seconds` | 60 | Echtsekunden zwischen Fortschrittszeilen je Mine, 0 = aus |
| `panel` | 1 | Zeile im Minenfenster |
| `panel_caption` | `Deposit remaining` | Beschriftung der Zeile, nur ASCII; mit Localization nur Ersatztext |

Mod-Vorkommen aus `deposits.ini`: `deplete = 0` im Vorkommensabschnitt nimmt es aus, `deplete = <Tonnen>` gibt ihm einen eigenen Wert.

⚠️ **Kies ist anders.** Kies liegt nicht in einer Ressourcenkarte, sondern in Komponente 2 der Geländemaske, die auch der Materialpinsel des Editors malt. Ein Kiesabbau nutzt darum sichtbar und dauerhaft die Bodentextur ab. Sein Suchradius ist 30 statt 210, der Fußabdruck also nur einige Dutzend Texel; deshalb der viel höhere eigene Wert. Wer das Grundspielverhalten behalten will, streicht `gravel` aus der Liste.

⚠️ **Spielstände:** Depletion senkt die Ressourcenkarte des Geländes, und der Weltschreiber speichert sie. `enabled = 0` oder das Entfernen des Plugins stellt das Abgebaute nicht wieder her. Auf einer Kopie testen.

---

## 🔬 Funktionsweise

- Der Minen-Tick (eine Mine, ein Tick) integriert nur Arithmetik: Er rechnet die Förderrate dieses Ticks gegen die im Plugin gehaltene Reserve und schreibt die Förderqualität. Gelesen werden zwei Gleitkommawerte aus dem Gebäude, mehr nicht.
- Der Flusher läuft aus einem Import-Hook auf dem Rendern des Geländes, also sicher auf dem Render-Thread: Er sät die Reserve einer Mine aus der Karte und schreibt angesammelte Erschöpfung zurück, gebündelt für alle Minen, höchstens viermal pro Sekunde und nicht öfter als `flush_seconds`.
- Die Karte darf nie aus dem Tick angefasst werden: Der Texturzugriff des Spiels ist kein Lock, sondern Staging-Kopie, Map und CopyResource auf dem unmittelbaren D3D11-Kontext. Aus einem Simulations-Thread bringt das den Grafiktreiber zum Absturz; genau das war der Fehler der ersten Fassung.
- Der zweite Wert in „Rest / Referenz“ wird nach dem Laden aus dem aktuellen Kartenbestand neu gebildet. Eine dauerhaft gespeicherte Anfangsmenge gibt es nicht.
- Mit `tonnes_per_texel = 1200` entspricht ein Byte-Schritt der Karte etwa 4,71 t; Bruchteile eines Schritts liegen nur im Arbeitsspeicher.

---

## 📏 Wertebereiche

| Größe | Grenze |
|---|---|
| `tonnes_per_texel` | 1 bis 10.000.000 (Editorgrenze) |
| `flush_seconds` | 1 bis 600, effektiv nie öfter als viermal pro Sekunde |
| `log_seconds` | 0 bis 3600 |
| `panel_caption` | ASCII, eine Zeile; die übersetzte Beschriftung höchstens 63 UTF-16-Codeeinheiten |
| Ressourcenkarten | die zwei Karten der Engine plus die vom `deposits`-Plugin angelegten, soweit das Plugin sie verfolgt |

---

## 💾 Kompatibilität

### Spielstände
Das Speicherformat ist unverändert, der Inhalt der Ressourcenkarte nicht: Abgebautes bleibt abgebaut. Ohne das Plugin fördern Minen wieder unendlich aus dem verbliebenen Rest.

### Andere Plugins
- **deposits / Deposits Plus:** liefert das Register der Mod-Vorkommen; ohne es nur Grundspiel-Vorkommen.
- **Localization** mit `tesmio_lang`: übersetzte Beschriftung; optional.
- Die Erstausstattung alter Spielstände mit Mod-Vorkommen ist Sache von deposits, nicht von Depletion.

### Versionskompatibilität
- **1.1.2:** optionale Übersetzung der Beschriftung
- **1.1.1:** Lade-Korrekturen (Cache, Weltwechsel, Sperren, Adressprüfung, Hook-Fehler)
- **1.1:** Stand des TesmioLoader-Quellbaums
- Keine neuen Einstellungen seit 1.1, keine Änderung des Speicherformats

---

## ⚙️ Fehlerbehandlung

### Häufige Probleme

| Meldung oder Verhalten | Ursache | Vorgehen |
|---|---|---|
| `enabled = 0 - deposits stay infinite` | Plugin ausgeschaltet | `enabled = 1` setzen |
| `no deposits plugin - only the base game's own run out` | deposits/Deposits Plus fehlt | nur relevant für Mod-Vorkommen |
| `nothing declared depletable - not hooking` | `vanilla = none` und keine Mod-Vorkommen | Liste prüfen |
| `vanilla: no base-game deposit "…"` | Tippfehler in `vanilla` | Namen prüfen |
| `no import slot for …` / `terrain init/render import missing` | anderer Spielstand oder fremder Hook | Spielversion 1.1.1.9 prüfen |
| `disabled after a fault` / `disabled after a fault in the flush` | Ausnahme im Tick oder Flusher | Protokoll sichern, Spiel neu starten |
| `localization key … missing/invalid` | Textpaket fehlt | `panel_caption` bleibt aktiv; Textpaket `tesmio_lang` prüfen |
| Alte Mine mit falscher Qualität nach dem Laden | Fehler vor 1.1.1 | seit 1.1.1 behoben; `deplete  world load` muss vor der neuen Erfassung erscheinen |

### Logging

Alle Meldungen stehen in `tesmioloader.log` mit dem Präfix `deplete`.
- In **Republic Mod Manager** öffnet das Symbol mit dem Dokument unten in der Plugin-Leiste die Protokollansicht mit Filter und Absender.

Suche nach:
- `deposit type(s) will run out` → welche Vorkommen mit welchem Tonnenwert erschöpfen
- `texel box` → Fußabdruck und Reserve jeder Mine beim ersten Abtasten
- `% left` → Fortschrittszeilen zum Kalibrieren
- `world load` und `world ready` → Weltwechsel und Freigabe der Erfassung

---

## 📦 Dateistruktur

**Loader-Ordner**
```
tesmioloader\build\
├── plugins\
│   ├── depletion.dll
│   ├── depletion.ini                (wirksame INI)
│   ├── deposits.dll                 (optional, Mod-Vorkommen)
│   └── localization\tesmio_lang\    (optional, übersetzte Beschriftung)
├── user_config\.autoload\
│   └── depletion.upstream.ini       (Original-INI, von Republic Mod Manager gesichert)
└── tesmioloader.log
```

**Quelltext**
```
plugins\depletion\
├── depletion.cpp
├── depletion.ini
├── README_DE.md
└── README_EN.md
```

---

## 📜 Lizenz & Credits

**GNU GPL v3.** Depletion ist die weiterentwickelte Fassung des Plugins `depletion` aus dem TesmioLoader von MaxLegend (Tesmio), https://github.com/MaxLegend/TesmioLoader, GPL v3; die Adressen und die Beschreibung der Texturzugriffe stammen aus dessen Reverse Engineering (`docs\08-depletion.md` des Loaders). Der vollständige Quelltext dieser Fassung liegt unter https://github.com/Kespri/workers-and-resources-mods/tree/main/plugins/depletion.

**Genosse, Achtung:** Dieses Plugin wurde mit Hilfe einer künstlichen Intelligenz geschrieben. Die Fünfjahrespläne dazu hat trotzdem ein Mensch aufgestellt, getestet und beim Abstürzen des Spiels geflucht. Wer keine KI im Code möchte, bleibt einfach beim Grundspiel. Kein Hass, keine Umerziehung.

---

## ❓ FAQ

**F: Wie lange reicht ein Vorkommen?**
A: Das bestimmt `tonnes_per_texel`. Das Protokoll nennt beim ersten Abtasten die Reserve jeder Mine; die Fortschrittszeilen zeigen, wie schnell sie sinkt.

**F: Warum sieht die Bodentextur unter einem Kiesabbau anders aus?**
A: Kies liegt in der Geländemaske, nicht in einer unsichtbaren Karte; der Abbau ist sichtbar. `gravel` aus `vanilla` streichen, wenn du das nicht willst.

**F: Kann ich die Erschöpfung rückgängig machen?**
A: Nein. Die Karte ist gespeichert; nur ein älterer Spielstand oder der Editor-Pinsel bringen Material zurück.

**F: Warum erschöpfen meine Mod-Vorkommen nicht?**
A: Das `deposits`-Plugin oder Deposits Plus fehlt, oder das Vorkommen hat `deplete = 0`.

**F: Muss ich die INI von Hand bearbeiten?**
A: Nein. Republic Mod Manager bietet alle sieben Werte mit Beschreibung und Bereichsprüfung an.

---

**Letzte Aktualisierung:** Depletion 1.1.2  
**Für:** WRSR 1.1.1.9 | TesmioLoader API 4
