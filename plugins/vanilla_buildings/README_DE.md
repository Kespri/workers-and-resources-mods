# 🏗️ Vanilla Buildings 1.3.1

**TesmioLoader-Plugin für temporäre Änderungen an Gebäudedateien**

Passt Gebäude des Spiels, der DLCs und des Workshops in *Workers & Resources: Soviet Republic* 1.1.1.9 an, ohne eine Originaldatei zu verändern: Bei jedem Spielstart entstehen temporäre Kopien mit den gewünschten Zeilen, und nur die Leseaufrufe des Spiels werden dorthin umgeleitet. Typischer Einsatz: neuen Materialien wie Streusalz, Kies oder Sand ein Lager in den Technischen Diensten geben, damit sie geliefert und angenommen werden.

---

## 📋 Inhaltsverzeichnis

- [Schnellstart](#-schnellstart)
- [Features](#-features)
- [Installation](#-installation)
- [Republic Mod Manager](#-republic-mod-manager)
- [Konfiguration](#-konfiguration)
- [Befehle](#-befehle)
- [Wertebereiche](#-wertebereiche)
- [Sicherheitsregeln](#-sicherheitsregeln)
- [Kompatibilität](#-kompatibilität)
- [Fehlerbehandlung](#-fehlerbehandlung)
- [Dateistruktur](#-dateistruktur)

---

## 🚀 Schnellstart

### Voraussetzungen
- Windows x64
- WRSR 1.1.1.9 (Referenzversion; nach einem Spielupdate können sich Originalzeilen ändern, nicht mehr passende Regeln werden dann sicher abgewiesen)
- TesmioLoader API 4
- Keine Abhängigkeit zu anderen Plugins. Ressourcen, auf die eine Regel verweist (etwa `road_salt`), müssen aber im Spiel existieren, zum Beispiel über das Resources-Plugin.

### In drei Schritten
1. **Eine Installationsmethode wählen** (siehe unten) und das Plugin aktivieren.
2. **Regelsätze einrichten:** Die mitgelieferte INI enthält zwei Beispiele, beide ausgeschaltet: `[example_plastics_factory]` zeigt alle Befehle, `[technical_services_grit]` gibt den Technischen Diensten Lager für Streusalz, Kies und Sand. In Republic Mod Manager schaltest du einen Regelsatz ein oder legst mit + einen eigenen an.
3. **Spiel vollständig neu starten.** Im Log steht je Ziel `[overlay-opened]`, sobald das Spiel die geänderte Datei liest.

---

## ✨ Features

### 🎯 Grundfunktion
- ✅ Gebäude des Spiels (`buildings_types\*.ini`), der DLCs (`dlcN\buildings\...\building.ini`) und des Workshops (`WorkshopID\...\building.ini`) in einem Regelsatz
- ✅ Mehrere Zieldateien je Regelsatz teilen sich dieselben Befehle; jedes Ziel wird für sich gegen seine unveränderte Originaldatei geprüft
- ✅ Befehle: Zeile ersetzen, entfernen, hinzufügen, vor einem Anker einfügen; Anschlussblöcke hinzufügen, ersetzen, entfernen
- ✅ Originaldateien im Spiel- und Workshopordner werden nie angefasst; ein Neustart ohne Plugin stellt alles zurück
- ✅ Ein abgewiesenes Ziel blockiert die anderen nicht; jede Ablehnung wird mit Abschnitt, Ziel und Ursache protokolliert

### 🆕 Neu in 1.3.1
- ✅ **`insert` statt `insert_before`:** `insert = 0 | ANKER | ZEILE` fügt vor dem Anker ein, `insert = 1 | ANKER | ZEILE` danach. Bei `$COST_RESOURCE_AUTO` hängt 1 das Material an die Phase der `$COST_WORK`-Ankerzeile, 0 an die Phase davor. `insert_before = ANKER | ZEILE` wird weiter gelesen und wirkt wie `insert = 0 | …`.

### 🆕 Neu in 1.3
- ✅ **INI neben der DLL:** Fehlt `plugins\vanilla_buildings.ini`, liest die DLL die INI aus dem eigenen Ordner, also aus dem Workshop-Paket unter Soviet Mod Loader oder der Workshop Bridge. Das Plugin läuft damit direkt aus dem Steam-Abo.
- ✅ Die gewählte Konfigurationsdatei steht beim Start im Log.
- ✅ Editor-Schema für Republic Mod Manager im Paket: Regelsätze mit Zielen und Befehlen als Liste, deutsch und englisch.

---

## 💾 Installation

Wähle **eine** der vier Methoden. Dieselbe DLL darf nie zweimal geladen werden.

---

### Methode 1️⃣: Klassischer TesmioLoader

```
1. Kopiere vanilla_buildings.dll und vanilla_buildings.ini aus hooks\
   → tesmioloader\build\plugins\

2. Aktiviere vanilla_buildings im TesmioLauncher
3. Regelsätze in der INI einschalten oder anlegen, Spiel vollständig neu starten
```

---

### Methode 2️⃣: Soviet Mod Loader (SML)

```
1. Workshop-Objekt abonnieren – SML liest abonnierte Pakete von selbst
2. SML lädt die DLL über soviet.mod.ini aus dem Paket, die INI liegt daneben
3. Eine lokale vanilla_buildings.dll in plugins\ vorher entfernen oder abschalten
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
1. Paket in Republic Mod Manager auswählen, Regelsätze einrichten, Speichern
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

Das Paket enthält im Ordner `config` ein Editor-Schema. Republic Mod Manager (ab 0.32.0) zeigt Vanilla Buildings damit in zwei Reitern, deutsch und englisch:

- **Allgemein:** Hinweise, „Dateien nur lokal“, Knopf für diese Anleitung und die Plugin-Schalter (Plugin aktiv, Diagnoseprotokoll)
- **Gebäude:** links die Regelsätze, rechts der gewählte Regelsatz mit Schalter, Zieldateien und je einem Feld pro Befehlsart. Jede Zeile im Feld ist eine INI-Zeile; unter dem Feld steht die Anzahl, am Griff lässt es sich aufziehen. „Gebäude auswählen…“ öffnet die Liste aller Gebäude des Spiels, der DLCs und der abonnierten Workshop-Objekte, nach Art gruppiert mit Suche und Filtern; angehakte Gebäude werden als Zielzeilen eingetragen. Der Plus-Knopf legt einen neuen Regelsatz mit Namen und erster Zieldatei an

Original-Regelsätze aus dem Paket können ausgeblendet oder Zeile für Zeile übersteuert werden. Persönliche Änderungen liegen in `user_config\vanilla_buildings.editor.ini`, die wirksame Datei ist `plugins\vanilla_buildings.ini`; die INI im Paket bleibt unverändert. Wer die INI lieber von Hand bearbeitet, findet alles Weitere unten.

---

## ⚙️ Konfiguration

### Hauptdatei: `vanilla_buildings.ini`

Die DLL liest in dieser Reihenfolge:
- **Zuerst:** `tesmioloader\build\plugins\vanilla_buildings.ini`, falls vorhanden (klassische Installation, „Dateien nur lokal“ oder die von Republic Mod Manager geschriebene wirksame INI)
- **Sonst:** die INI neben der DLL, im Paket `hooks\vanilla_buildings.ini` (Soviet Mod Loader, Workshop Bridge)

⚠️ **Kommentare nur in eigenen Zeilen mit `;` oder `#`.** Die Datei muss UTF-8 ohne BOM sein. Abschnitts- und Schlüsselnamen sind ohne Groß-/Kleinschreibung, Spiel-Direktiven und gesuchte Zeilen dagegen genau so wie im Original. Jeder Befehl steht in einer physischen Zeile; `|` trennt seine Felder. Änderungen gelten nach einem vollständigen Neustart des Spiels.

### Abschnitt `[general]`

```ini
[general]
; 1 aktiviert das Plugin, 0 installiert keinen Hook; nur genau 0 oder 1
enabled = 1
; 1 protokolliert zusätzlich ausgeschaltete Regelsätze und jeden umgeleiteten Dateizugriff
debug = 0
```

Die alten Namen `[vanilla_buildings]` und `verbose` bleiben mit einer Warnung im Log unterstützt; verwende nur einen globalen Abschnitt und nur einen Debug-Schlüssel.

### Regelsätze: ein Abschnitt je Regelsatz

Der Abschnittsname ist frei wählbar und muss eindeutig sein. Jede `target`-Zeile ergänzt eine Zieldatei; alle Befehle gelten für jedes Ziel, egal wo sie im Abschnitt stehen. Beispiel, so wie ausgeliefert:

```ini
[technical_services_grit]
; 0 lässt den Regelsatz in der Datei, wendet aber nichts an
enabled = 0
; Zieldateien, relativ zu media_soviet bzw. zum Workshop-Ordner des Spiels
target = buildings_types\technical_services_small.ini
target = buildings_types\technical_services.ini
target = buildings_types\technical_services_big.ini
target = dlc3\buildings\technical_services_small\building.ini
target = 2496571917\utrzymanie1\building.ini
; neue Lagerzeilen, je eine pro Material
add = $STORAGE_IMPORT_SPECIAL RESOURCE_TRANSPORT_GRAVEL 120 road_salt
add = $STORAGE_IMPORT_SPECIAL RESOURCE_TRANSPORT_GRAVEL 120 gravel
add = $STORAGE_IMPORT_SPECIAL RESOURCE_TRANSPORT_GRAVEL 120 sand
```

Pfade bestimmen den Stammordner selbst: `buildings_types\...` und `dlcN\buildings\...` liegen unter `<Spiel>\media_soviet`, eine numerische Workshop-ID unter `<Steam-Bibliothek>\steamapps\workshop\content\784150`. DLC und Workshop-Objekt müssen installiert sein. Absolute Pfade, `.` und `..` werden abgewiesen. `target1`, `target2`, `target3` sind optionale Aliase, die zusätzlich die Zielart Spiel, DLC oder Workshop prüfen.

Dieselbe Zieldatei darf in aktiven Regelsätzen nur einmal vorkommen; alle Änderungen an einer Datei gehören in einen Regelsatz. Bereits gebaute Gebäude werden nicht nachgerüstet: neue Lager erscheinen an Neubauten.

---

## 🛠️ Befehle

### Einzelzeilen

| Befehl | Bedeutung |
|---|---|
| `replace = ALT \| NEU` | Ersetzt eine vollständige Zeile, die genau einmal vorkommt. |
| `remove = ZEILE` | Entfernt eine vollständige Zeile, die genau einmal vorkommt. |
| `add = ZEILE` | Fügt eine neue Einzelzeile vor dem letzten `end` ein; abgewiesen, wenn sie schon existiert. |
| `insert = 0 \| ANKER \| ZEILE` | Fügt eine Zeile vor einer eindeutigen Ankerzeile ein; mehrere mit demselben Anker behalten ihre Reihenfolge. |
| `insert = 1 \| ANKER \| ZEILE` | Dasselbe nach der Ankerzeile. `insert_before = ANKER \| ZEILE` ist die alte Schreibweise von `insert = 0 \| …`. |

Leerzeichen am Zeilenanfang und -ende werden ignoriert, Unterschiede innerhalb der Zeile nicht.

### Anschlussblöcke

| Befehl | Bedeutung |
|---|---|
| `add_connection = TOKEN \| PUNKT1 \| PUNKT2` | Fügt einen neuen Dreizeilenblock nach dem letzten Anschluss ein. |
| `replace_connection = TOKEN \| PUNKT1 \| PUNKT2 \| NEUES_TOKEN` | Ändert nur das Token des über beide Punkte eindeutig gefundenen Blocks. |
| `remove_connection = TOKEN \| PUNKT1 \| PUNKT2` | Entfernt den ganzen Dreizeilenblock. |

Ein Punkt besteht aus drei Zahlen, etwa `14.5 0 2`; der Vergleich ist numerisch, `0` und `0.0000` gelten als gleich. Die beiden Punkte müssen verschieden sein und in der Reihenfolge der Originaldatei stehen. Belegte Punkte und neue `*_ALLOWPASS`-Anschlüsse werden abgewiesen.

Vollständiges Beispiel mit allen Befehlen, ausgeliefert als `[example_plastics_factory]` mit `enabled = 0`:

```ini
[example_plastics_factory]
enabled = 0
target = buildings_types\plastics_factory.ini
replace = $PRODUCTION plastics 0.11 | $PRODUCTION plastics 0.20
replace_connection = $CONNECTION_CONNECTION | 14.5 0.0 23.3 | 14.5 0.0 21.3 | $CONNECTION_WATERPIPE_INPUT
remove = $CONSUMPTION_PER_SECOND eletric 0.26
remove_connection = $CONNECTION_CONNECTION | -23.4 0.0 15.9 | -21.4 0.0 15.9
add = $PRODUCTION glass 0.45
add_connection = $CONNECTION_WATERPIPE_OUTPUT | 30 0 0 | 32 0 0
insert = 0 | $COST_WORK SOVIET_CONSTRUCTION_STEEL_LAYING 1.0 | $COST_RESOURCE_AUTO steel 2.0
```

---

## 📏 Wertebereiche

| Grösse | Grenze |
|---|---|
| `enabled`, `debug` | genau 0 oder 1 |
| Regelsätze | höchstens 256 |
| Zieldateien insgesamt | höchstens 256 |
| Befehle je Regelsatz | höchstens 512 |
| Abschnittsname | höchstens 128 Bytes |
| Zielpfad | höchstens 220 Bytes |
| aktive Konfigurationszeile | höchstens 4096 Bytes |
| Plugin-INI und jede Originaldatei | höchstens 4 MiB |
| erzeugte Datei | höchstens 8 MiB |

Fehler in `[general]`, mehrdeutige Abschnittsgrenzen, falsche Kodierung oder überschrittene Dateigrenzen lehnen die ganze INI ab; dann wird kein Hook installiert.

---

## 🔒 Sicherheitsregeln

- Alle Regeln werden gegen die **unveränderte Originaldatei** geprüft, bevor eine Kopie entsteht.
- `replace`, `remove`, `insert` und die Anschlussbefehle brauchen genau einen Treffer; sonst wird das Ziel als Ganzes abgewiesen, ohne Teiländerung.
- Überlappende Änderungen und Anker, die eine andere Regel verändert, sind unzulässig.
- `add` akzeptiert keine `$COST_`-Zeilen; `insert` als neue `$COST_`-Zeile nur `$COST_RESOURCE_AUTO` mit einer eindeutigen `$COST_WORK`-Zeile als Anker (1 = Material dieser Phase, 0 = der Phase davor).
- Nur Leseaufrufe (`fopen`, `fopen_s`, `_wfopen`, `_wfopen_s`, Engine-Pufferleser) auf Dateien unter dem echten Spiel- oder Workshopordner werden umgeleitet; Schreibzugriffe nie.
- Die Prüfung schützt die Patchstruktur, nicht die fachlichen Werte der Spiel-Direktiven.

---

## 💾 Kompatibilität

### Spielstände
Das Plugin ändert Gebäudedefinitionen, keine gespeicherten Gebäude. Neue Lager gelten für Neubauten; bestehende Gebäude brauchen eine separate Nachrüstung. Spielstände und Dateien im Spielordner bleiben unverändert.

### Andere Plugins
Andere Plugins, die dieselbe Gebäudedatei ersetzen, werden nicht mit diesen Änderungen zusammengeführt. Für Materialien aus Vehicle Materials liefert dieses Plugin die passende `$STORAGE_IMPORT_SPECIAL`-Zeile in den Fahrzeugfabriken.

### Versionskompatibilität
- **1.3.1:** Befehl `insert` mit Position vor/nach dem Anker; `insert_before` bleibt lesbar
- **1.3:** INI-Fallback neben der DLL, Editor-Schema im Paket; Patchlogik gegenüber 1.2 unverändert
- **1.2:** Zieldateien unter `buildings_types`, `dlcN\buildings` und Workshop-IDs, mehrere Ziele je Regelsatz
- **Zurück auf eine ältere Fassung:** alte DLL und die dazugehörige INI wiederherstellen

---

## ⚙️ Fehlerbehandlung

### Häufige Probleme

| Problem | Ursache | Lösung |
|---|---|---|
| Nichts passiert | alle Regelsätze `enabled = 0` (Auslieferung) oder Plugin aus | Regelsatz einschalten, Spiel neu starten |
| `expected buildings_types...` | Zielpfad hat keine unterstützte Form | Pfad wie oben schreiben |
| `source line has 0 matches` | gesuchte Zeile fehlt im Original | vollständige Zeile aus der Originaldatei kopieren |
| `... matches instead of exactly one` | Zeile oder Anschluss nicht eindeutig | Zeile prüfen, bei Anschlüssen die Koordinaten |
| `duplicate target` | Ziel in zwei aktiven Regelsätzen | Änderungen in einen Regelsatz zusammenführen |
| `could not read building source file` | DLC oder Workshop-Objekt nicht installiert | Pfad und Installation prüfen |
| `[utf8-bom]`, `[duplicate-key]`, `[boolean]` | INI-Format | UTF-8 ohne BOM, Schlüssel einmal, Schalter 0 oder 1 |

### Logging

Alle Meldungen stehen in `tesmioloader.log` und im Detail-Log `tesmioloader.vanilla_buildings.log`, mit Stufe (INFO, DEBUG, WARN, ERROR, FATAL) und Prüfungsname. Jedes abgewiesene Ziel bekommt auch ohne Debug eine `WARN [target-rejected]`-Meldung mit Abschnitt, Ziel, Ursache und bei Befehlsfehlern der INI-Zeile.
- In **Republic Mod Manager** öffnet das Symbol mit dem Dokument unten in der Plugin-Leiste die Protokollansicht mit Filter und Absender.

Suche nach:
- `Configuration file` → welche INI die DLL gewählt hat
- `target-rejected` → abgewiesene Ziele mit Ursache
- `overlay-opened` → das Spiel hat eine geänderte Datei tatsächlich gelesen
- `configuration` → die ganze INI wurde abgelehnt

---

## 📦 Dateistruktur

**Workshop-Paket** (Steam-Abo, SML, Workshop Bridge)
```
vanilla_buildings\
├── hooks\
│   ├── vanilla_buildings.dll       (Plugin)
│   └── vanilla_buildings.ini       (Original-INI, Beispiele ausgeschaltet)
├── config\                         (Editor-Schema für Republic Mod Manager)
│   ├── vanilla_buildings.launcher.ini
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
│   ├── vanilla_buildings.dll
│   └── vanilla_buildings.ini       (wirksame INI)
└── user_config\
    └── vanilla_buildings.editor.ini (persönliche Regelsätze aus Republic Mod Manager)
```

Die temporären Kopien liegen unter `%TEMP%\TesmioLoader\vanilla_buildings\<Prozess-ID>-<Startkennung>-<Versuch>\`; jeder Start legt einen neuen Ordner an, alte können gefahrlos gelöscht werden.

---

## 📜 Lizenz & Credits

**GNU GPL v3**, siehe `LICENSE` im Paket. Das Plugin enthält keinen fremden Code; der Loader-SDK-Header stammt aus dem TesmioLoader von MaxLegend (GPL v3). Der vollständige Quelltext liegt unter https://github.com/Kespri/workers-and-resources-mods/tree/main/plugins/vanilla_buildings.

---

## ❓ FAQ

**F: Werden meine Spieldateien verändert?**
A: Nein. Es entstehen nur temporäre Kopien; ohne das Plugin ist alles wie vorher.

**F: Bekommen bestehende Gebäude die neuen Lager?**
A: Nein, nur Neubauten. Bestehende Gebäude brauchen eine separate Nachrüstung.

**F: Kann ich ein Workshop-Gebäude ändern?**
A: Ja, mit der Workshop-ID als Pfadbeginn, etwa `2496571917\utrzymanie1\building.ini`. Das Objekt muss abonniert und installiert sein.

**F: Was passiert nach einem Spielupdate?**
A: Regeln, deren Originalzeilen sich geändert haben, werden für das betroffene Ziel abgewiesen und im Log gemeldet; alles andere läuft weiter.

**F: Muss ich die INI von Hand bearbeiten?**
A: Nein. Republic Mod Manager zeigt die Regelsätze als Liste mit Zielen und Befehlen, je Zeile ein Eintrag, und legt neue Regelsätze mit dem Plus-Knopf an.

---

**Letzte Aktualisierung:** Vanilla Buildings 1.3.1  
**Für:** WRSR 1.1.1.9 | TesmioLoader API 4
