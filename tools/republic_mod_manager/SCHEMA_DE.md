# Autoload-Format 1 – Paketformat für Republic Mod Manager (RMM)

Ein kompatibles Workshop-Paket enthält eine `soviet.mod.ini` und eine x64-DLL.
Eine Standard-INI neben der DLL und ein deklaratives Launcher-Schema sind
optional. Weder Manifest noch Schema enthalten ausführbare Befehle.

## Manifest

Das kleinste gültige Manifest ist das von Soviet Mod Loader:

```ini
[mod]
id = example.my_plugin
name = My Plugin
version = 1.0.0-beta

[hooks]
dll = hooks\my_plugin.dll
```

Daraus leitet Republic Mod Manager ab: Ziel `my_plugin`, Standard-INI
`hooks\my_plugin.ini` (falls vorhanden), persönliche Konfiguration
`my_plugin.ini`, Launcher-Schema `config\my_plugin.launcher.ini` (falls
vorhanden, sonst aus der INI erzeugt).

Alle weiteren Angaben sind Übersteuerungen:

```ini
[mod]
enabled = 1
tesmio_api_min = 4
tesmio_api_max = 4

[configuration]
defaults = hooks\my_plugin.ini
launcher_schema = config\my_plugin.launcher.ini
user_config = my_plugin.ini
user_overlay = 0

[autoload]
format = 1
kind = plugin
target = my_plugin
requires_local = resources|another_dependency
conflicts_local = old_my_plugin|duplicate_name
```

`target` bestimmt `build\plugins\<target>.dll` und den Schlüssel in
`tesmioloader.ini`; wird es angegeben, muss es mit dem DLL-Dateinamen
übereinstimmen. `user_config` ist ein einfacher INI-Dateiname und muss im Paket
direkt neben der DLL liegen. `requires_local` und `conflicts_local` sind
optionale, mit `|` getrennte lokale Plugin-Kennungen. `user_overlay = 1`
erklärt, dass die DLL `user_config\<target>.ini` selbst über ihre INI legt;
Republic Mod Manager stellt dann die Original-INI unverändert bereit.

`tesmio_api_min`/`tesmio_api_max` sind optional; wenn vorhanden, muss API 4
im Bereich liegen. `[dependencies]` (`mod.id = >=1.2.0`) wird gegen die Pakete
im Workshop-Ordner aufgelöst (seit 0.34.1 genügt auch ein klassisch installiertes
Plugin `plugins\<name>.dll`, wenn `<name>` der letzte Teil der Kennung ist; die
Version bleibt dann ungeprüft); Bedingungen sind `>=`, `>`, `=`, `<=`, `<`, eine
bloße Version (mindestens) oder `*`. `[content]` wird vermerkt; Inhalt wendet
nur Soviet Mod Loader an. Ein Manifest mit `[content]` und ohne `[hooks] dll`
ist ein reines Inhaltspaket: es wird gelistet, nicht bereitgestellt.

Format 1 verarbeitet genau eine DLL und eine Konfigurationsdatei. Wiederholte
`dll`-Direktiven werden mit genauer Ursache abgewiesen.

### Dateien nur lokal und Assets (seit 0.21.0)

```ini
[configuration]
local_copy = 1          ; erlaubt "Dateien nur lokal" (Schalter in der Karte Hinweise)

[assets]
dir = hooks\deposits_plus   ; Ordner, der mit der DLL reist
```

`local_copy = 1` bietet bei einem Paket, das über die Workshop Bridge läuft,
den Schalter „Dateien nur lokal“ an: Republic Mod Manager kopiert DLL, INI und
den Ordner aus `[assets] dir` nach `plugins\`, den Ordner unter seinem eigenen
Namen (`plugins\deposits_plus\...`). Der Beleg hält `local_copy = 1` und jede
kopierte Datei als `asset.N` fest; Ausschalten entfernt genau diese Dateien.
Im Assets-Ordner sind keine .dll- oder .exe-Dateien erlaubt, höchstens 512
Dateien zu je 64 MB, keine Reparse-Punkte.

## Schema aus der INI

Ohne Launcher-Schema erzeugt Republic Mod Manager eines aus der Standard-INI:

- jeder INI-Abschnitt wird eine Gruppe auf dem Reiter „Einstellungen“;
- jeder Schlüssel wird ein Feld mit dem Schlüsselnamen als Beschriftung;
- der zusammenhängende Kommentarblock direkt über dem Schlüssel und ein
  Kommentar hinter dem Wert werden zur Beschreibung, Zeilenumbrüche bleiben
  erhalten;
- der Typ folgt dem Wert: `0`/`1` wird `boolean`, außer der Schlüsselname
  nennt eine Menge (`count`, `days`, `frequency`, `size`, `limit`, `level`,
  `scale`, `mode` und ähnliche) oder der Kommentar nennt andere ganze Zahlen
  oder einen Bereich (`2-12`, `1..6`, „up to 400“); Versionsangaben wie
  `v1.6` zählen nicht. Namen wie `enabled`, `debug`, `use_*`, `*_enabled` oder
  ein Kommentar, der beide Zustände nennt, erzwingen `boolean`. Andere
  Ganzzahlen `integer`; Dezimalzahlen `decimal`; alles andere `text`;
- ein Schlüssel `enabled` vom Typ `boolean` wird zum `enabled_field`;
- der Kommentarblock am Dateianfang wird zur Paketbeschreibung.

Die strenge Schlüsselprüfung gilt in diesem Modus nicht: Eine lokale INI darf
Schlüssel enthalten, die die Paket-INI nicht kennt. Wer sie will, liefert ein
Schema mit. In Schema-Beschreibungen steht die Zeichenfolge `\n` für einen
Zeilenumbruch.

## Launcher und Felder

```ini
[launcher]
layout_version = 1
visible = 1
id = example.my_plugin
config = my_plugin.ini
description = Human readable fallback
description_key = plugin.description
language_directory = config\languages
icon = builtin:gear
enabled_field = general/enabled
default_tab = general
maximum_value_length = 63

[field:enabled]
section = general
key = enabled
label = Enable plugin
label_key = field.enabled
description = Restart required.
description_key = field.enabled.description
type = boolean
group = general
order = 10

[field:limit]
section = general
key = limit
label = Limit
type = integer
minimum = 0
maximum = 10000
step = 1
group = general
order = 20
```

Jedes `[field:...]` bindet genau einen vorhandenen Wert der Standard-INI. Alle
wirksamen Schlüssel müssen als Feld oder Sammlungselement beschrieben sein.
Unterstützte Typen:

- `boolean`: nur `0` oder `1`;
- `integer`: Ganzzahl innerhalb `minimum`/`maximum`;
- `decimal`: endliche Dezimalzahl innerhalb der Grenzen;
- `choice`: exakt ein Wert aus `choices = a|b|c`;
- `readonly`: wird angezeigt, aber nicht direkt editiert.

`enabled_field` ist optional und muss auf ein boolesches Schemafeld zeigen. Ohne
die Angabe erscheint kein Aktivierungsschalter. `maximum_value_length` gilt für
alle Werte und liegt standardmäßig bei 4096.

## Reiter, Gruppen und Matrix

```ini
[tab:general]
label = General
label_key = tab.general
order = 10

[group:general]
tab = general
label = General settings
description_key = group.general.description
layout = fields
order = 10
```

Es gibt keine fest eingebaute Reiterzahl. Ohne eigene Gruppen landen Felder im
gemeinsamen Bereich `settings`. Jeder explizite Gruppen- und Reiterverweis muss
existieren.

Eine Gruppe mit `layout = matrix` ordnet Felder zusätzlich über `row`, `column`,
`row_label`, `column_label`, `unit` und `icon` an. Die gleichnamigen `_key`-
Angaben liefern Übersetzungen. Doppelte Matrixzellen sind ungültig.

## Dynamische Sammlungen

Sammlungen bauen eine benutzerverwaltete Liste und eine Wertematrix aus einem
lokalen Plugin-Katalog auf:

```ini
[collection:materials]
source = local-plugin:resources/list
source_ready_section = resources
source_ready_key = hook
source_ready_value = 2

resource_group = resources
matrix_group = vehicles
count_section = resources
count_key = count
item_prefix = resource
item_label = Material
item_label_key = material_number
item_description_key = registered_resource_help

target_sections = road|rail|ship|airplane
target_labels = Road vehicles|Rail vehicles|Ships|Airplanes
target_label_keys = row.road|row.rail|row.ship|row.airplane
target_icons = builtin:truck|builtin:rail|builtin:ship|builtin:plane
unit = Coefficient
unit_key = unit.coefficient
coefficient_description_key = coefficient.description

empty_notice = Noch keine Einträge vorhanden. Füge zuerst einen Eintrag hinzu.
empty_notice_key = collection.empty_notice
empty_notice_style = warning

type = decimal
minimum = 0
maximum = 1000000
step = 0.001
default = 0
maximum_items = 32
ownership = user
allow_remove_defaults = 1
require_positive_when_enabled = 1
```

`source` hat die Form `local-plugin:<Plugin>/<Abschnitt>`. Die Anwendung liest
nur diesen Abschnitt aus `build\plugins\<Plugin>.ini`; die Quelldatei wird nie
verändert. Die drei `source_ready_*`-Werte sind optional und können eine genaue
Bereitschaftsbedingung vorgeben.

Die drei Listen `target_sections`, `target_labels`, `target_label_keys` und
`target_icons` müssen dieselbe Länge besitzen. Leere Beschriftungs-/Symbolwerte
können durch Weglassen der gesamten optionalen Zeile erreicht werden.

Für `count_section/count_key` muss zusätzlich ein `readonly`-Feld existieren.

`empty_notice` wird ausschließlich im zugehörigen `matrix_group` angezeigt, solange
die Sammlung leer ist. Nach dem ersten hinzugefügten Eintrag verschwindet der Hinweis
automatisch; nach dem Löschen des letzten Eintrags erscheint er wieder. Mit
`empty_notice_key` kann der Text übersetzt werden. `empty_notice_style` akzeptiert
`warning` (gelb) oder `info` (blau). Fehlen die Angaben, wird kein Leerhinweis gezeigt.

Beim Hinzufügen entstehen lückenlose `item_prefix0`, `item_prefix1`, … sowie ein
Wert pro Zielabschnitt. Entfernen löscht alle zugehörigen Werte und verdichtet
die Liste.

`ownership = user` bedeutet, dass eine bereits bereitgestellte Liste bei einer
Änderung der Paketstandards als persönliche Auswahl erhalten bleibt. Diese
Übernahme wird über den Zustandsbeleg genau einmal erkannt. Mit
`allow_remove_defaults = 1` sind auch ursprünglich vom Paket gelieferte Einträge
löschbar. `require_positive_when_enabled = 1` verlangt bei eingeschaltetem
Plugin mindestens einen positiven Sammlungswert und deaktiviert das Plugin beim
Löschen des letzten positiven Eintrags. Ist diese Bedingung beim Aktivieren nicht
erfüllt, nennt die Fehlermeldung automatisch den sichtbaren, übersetzten Reiter
und Gruppentitel. Dafür folgt Republic Mod Manager der Zuordnung
`collection.resource_group` → `group.tab`; Plugin-Autoren müssen keinen
festen Meldungstext hinterlegen.

## Lokale Schemas für installierte Plugins

Ein Plugin, das nur als `plugins\<name>.dll` im Loader-Ordner liegt, bekommt
sein Schema aus `settings_schemas\<name>.launcher.ini` neben der EXE, falls
diese Datei existiert, sonst aus seiner INI (siehe „Schema aus der INI“). Ein
solches lokales Schema ist ein gewöhnliches Launcher-Schema:

```ini
[launcher]
layout_version = 1
visible = 1
id = local.walking
name = Walking Distance
config = walking.ini
enabled_field = walking/enabled
language_directory = languages
```

`config` muss `<name>.ini` sein. `id` und `name` bestimmen Kennung und
Anzeigename des Eintrags; ohne Schema lauten sie `local.<name>` und `<name>`.
`language_directory` wird relativ zum Ordner `settings_schemas` aufgelöst.
Dateien mit `editor_type = keyed_resources` im selben Ordner sind Master-Detail-
Editoren (nächster Abschnitt) und werden nicht als Plugin-Schema verwendet.

Seit 0.13.0 liefert Republic Mod Manager solche Schemas für accumulator, cities,
daynight, depletion, easystart und walking mit; die deutschen Texte stehen in
`settings_schemas\languages\de.ini` unter den Präfixen `<name>.`. Ein lokales
Schema ist beschreibend, nicht abschließend: Werte mit Kommentar hinter dem
Wert werden angenommen (der Kommentar bleibt, bis der Wert geändert wird), und
Schlüssel der INI, die das Schema nicht kennt, werden nicht abgewiesen, sondern
in den Hinweisen genannt und beim Speichern unverändert übernommen. Freie
Werte wie `auto` oder Jahreszahlen mit `off`/`always` verwenden `type = text`.

Die Konfiguration eines installierten Plugins wird als geschützte Basis
geführt: Original unter `user_config\.autoload\<name>.upstream.ini`, wirksame
INI in `plugins\`, persönliche Werte in `user_config\<name>.ini`, und ein
Empfangsbeleg mit `mode = installed`. Eine außerhalb geänderte `plugins\<name>.ini`
wird als neues Original übernommen.

Der Empfangsbeleg eines Workshop-Pakets trägt `mode = package` (DLL und INI
von Republic Mod Manager bereitgestellt), `mode = sml` (nur INI, Soviet Mod Loader
lädt die DLL) oder seit 0.14.0 `mode = bridge` (nur INI, `workshop_bridge`
lädt die DLL; der Eintrag `[packages] <Workshop-Nummer>` in
`user_config\workshop_bridge.ini` entscheidet). Für das Plugin `workshop_bridge`
selbst gilt: `[packages]`-Zeilen in seiner Overlay-Datei überleben jedes
Speichern und „Original wiederherstellen“.

## Lokale Master-Detail-Editoren

Lokale Editor-Schemas liegen neben der EXE unter `settings_schemas` und werden
unabhängig von Workshop-Paketen erkannt. Der Resources-Adapter verwendet:

```ini
[launcher]
layout_version = 1
editor_type = keyed_resources
id = tesmio.resources.editor
name = Resources
language_directory = languages

[editor]
plugin = resources
config = resources.ini
list_section = list
item_section_prefix = custom:
maximum_items = 512

[detail:transport]
scope = custom
key = transport
type = text

[detail:pinned_base_price]
scope = keyed
section = base_price
key = value
type = pair
minimum = 0
maximum = 1000000000
```

`scope = custom` verbindet ein Feld mit
`[<item_section_prefix><Ressourcenkennung>]`. `scope = keyed` verbindet es mit
einem Eintrag, dessen Schlüssel die Ressourcenkennung ist. Optionale leere
Felder erzeugen keinen persönlichen Eintrag. Unterstützte Detailtypen sind
`text`, `integer`, `decimal`, `choice`, `pair`, `triple` und (für `scope = global`) `boolean`.

Die Originalbasis, persönliche Ebene und wirksame Datei werden getrennt geführt.
Originale Listeneinträge bleiben gesperrt, während persönliche Einträge entfernt
werden können. Das Schema bestimmt sämtliche sichtbaren Felder und Übersetzungen;
der Editor lädt die konfigurierte Plugin-DLL nicht.

### Listen-Editoren (`editor_type = keyed_list`)

Seit 0.18.0 gibt es neben `keyed_resources` den Typ `keyed_list` für INIs, in
denen jede Zeile eines Abschnitts ein Tupel ist:
`<Kennung> = <Spalte 1>, <Spalte 2>, ...`. Der Needs-Adapter
(`settings_schemas\needs.launcher.ini`) verwendet:

```ini
[launcher]
layout_version = 1
editor_type = keyed_list
id = tesmio.needs.editor
name = Needs
language_directory = languages

[editor]
plugin = needs
config = needs.ini
list_section = list
maximum_items = 8

[activity]
section = needs
key = enabled
values = 1

[source]
plugin = resources          ; Kennungen für neue Zeilen kommen aus
section = list              ; plugins\resources.ini [list]
ready_section = resources   ; optional: Quelle gilt nur mit
ready_key = hook            ; [resources] hook = 2
ready_value = 2

[list]
label_key = needs.list      ; Listentitel, +-Knopf, Hilfetext, Kennungsfeld
add_label_key = needs.add
select_help_key = needs.select_help
id_label_key = needs.resource
id_help_key = needs.resource.help

[global]
label_key = group.needs.plugin          ; Karte für scope = global
description_key = group.needs.plugin.description

[column:donor]
type = choice
choices = food|meat|clothes|eletronics|alcohol
required = 1
label_key = needs.donor
description_key = needs.donor.description
order = 10

[column:category]
type = choice
choices = auto|none|basic|medium|advanced|mediumadvanced|hotel
allow_other = 1             ; zusätzlich freie Eingabe (Zahl) erlaubt
default = auto
order = 30

[column:chance]
type = decimal
minimum = 0
maximum = 1
default = 1.0
order = 40

[detail:max_demands]
scope = global              ; ein Wert je Plugin statt je Eintrag
section = needs
key = max_demands
type = integer
minimum = 1
maximum = 7
```

`[column:<id>]` beschreibt die Spalten in `order`-Reihenfolge; Typen sind
`text`, `integer`, `decimal` und `choice` (mit `allow_other = 1` als
editierbare Liste). `required = 1` erzwingt die Spalte, `default` füllt
fehlende Spalten beim Anzeigen und beim Schreiben einer Zeile, deren spätere
Spalten belegt sind. Zeilen werden beim Schreiben normalisiert
(`1.0` wird zu `1`, Auswahlwerte in der Schreibweise des Schemas).

`[source]` bestimmt, welche Kennungen der `+`-Dialog anbietet: die Schlüssel
des Abschnitts `section` aus `plugins\<plugin>.ini`, abzüglich der bereits
vorhandenen oder ausgeblendeten Einträge. Ohne `[source]` (seit 0.34.0) fragt
der `+`-Dialog die Kennung als Text ab und schlägt die Ressourcennamen des
Grundspiels und, falls vorhanden, die Liste des Resources-Plugins vor; die
Kennung folgt den üblichen Regeln (Buchstaben, Ziffern, `_`, `-`, `.`).

Ein lokaler Editor darf seit 0.19.0 mehrere `[tab:<id>]`-Abschnitte haben
(`label`, `label_key`, `order`). Die Listenkarte liegt auf dem Reiter aus
`[group:*] tab = <id>`, die Karte der plugin-weiten Werte auf `[global] tab =
<id>`; `[launcher] default_tab` wählt den beim ersten Öffnen gezeigten Reiter.
`[global] order` (seit 0.4.7) sortiert die Karte der plugin-weiten Werte unter die `[card:*]`-Karten
desselben Reiters; ohne Angabe (0) steht sie zuerst.
Fehlen die Angaben, gibt es wie bisher einen Reiter mit allem. Der Needs-Adapter
verwendet `general` (Plugin-Einstellungen) und `needs` (Liste), Vorgabe `needs`.
`[launcher] notice` / `notice_key` / `notice_style` (seit 0.4.3, Presentation-Schemas; seit 0.4.5
auch Listen- und Abschnittseditoren) zeigt
einen paketweiten Hinweis in der Karte "Hinweise" des ersten Reiters, neben Abhaengigkeits-
und Bridge-Hinweisen; `notice_style = warning` macht ihn gelb, sonst blau.
`[launcher] info` / `info_key` (seit 0.4.10) zeigt darunter einen zweiten, immer blauen Kasten, etwa
für eine Kurzbeschreibung des Plugins; `\n` bricht um.
`[group:*] notice` / `notice_key` (seit 0.34.3) zeigt auf der Listenkarte einen
blauen Hinweiskasten zwischen `description` und der gelben `save_warning`.
`[list] note` / `note_key` (seit 0.4.8) steht klein und linksbündig unter der Liste, neben dem
+-Knopf (etwa "Höchstens 32 Materialien."). Der Hinzufügen-Dialog zeigt seit 0.4.8 unter den
Beschriftungen dieselben Hilfetexte wie der Detailbereich (`id_help`, Spalten-`description`);
`[list] id_suggestions = 0` (seit 0.4.20) laesst die Vorschlagsliste (eigene und Vanilla-Ressourcen) im Hinzufuegen-Dialog weg, wenn die Kennung keine Ressource ist, etwa eine Text-ID; das Feld bleibt frei beschreibbar.
`[card:<id>] notice_style = info` (seit 0.4.20) zeichnet den Karten-Hinweis der Listen-/Abschnittseditoren blau statt gelb (Standard bleibt `warning`).
`[list] id_picker = game_texts` (seit 0.4.21) macht die Kennung zum reinen Textfeld und stellt daneben den Knopf "Text auswaehlen...": ein Fenster mit allen Texten einer Spielsprache aus `media_soviet\soviet<Sprache>.btf` (ID, Zeilen, laengste Zeile, Textanfang), Suche und Mindestlaenge; die Sprache folgt `$TEXT LANGUAGE2` in `media_soviet\config.ini`, bei `auto` der RMM-Sprache, und ist im Fenster umschaltbar. Schnappschuss `--window texts`.
`[list] add_id_help` / `add_id_help_key` (seit 0.4.22) ist der Hilfetext unter der Kennung im Hinzufuegen-Dialog; ohne ihn gilt dort weiter `id_help` wie im Detailbereich. `\n` ist auch hier ein Zeilenumbruch.
ohne `[source]` ist die Kennung ein Eingabefeld mit gruppierten Vorschlägen: erst die
Ressourcen aus `plugins\resources.ini` (wenn das Resources-Plugin scharf ist), dann die des
Grundspiels.
`[detail:<id>] heading` / `heading_key` (seit 0.34.4) zeichnet im Detailbereich
eine Trennlinie mit kleinem Titel über diesem Feld; so zerfällt eine lange
Feldliste in benannte Blöcke. Ein wörtliches `\n` in `description` (und in den
Sprachdateien) wird seit 0.34.5 auch bei `[detail:*]`-Feldern zum Zeilenumbruch,
wie bisher schon bei `[column:*]`; seit 0.4.6 gilt das für alle Beschreibungs-,
Hinweis- und Hilfetexte der Listen- und Abschnittseditoren (`[launcher] description`,
`[group:*] description/notice`, `[global] description/notice`, `[card:*]`,
`save_warning`, `select_help`, `item_id_help`, `new_hint`). Ein Pfad wie
`plugins\needs.ini` in einem Text wird deshalb zerrissen; dort `/` schreiben.

Das Detailfeld, auf das `[activity]` zeigt (bei Needs `[needs] enabled`), wird
nicht in der Karte angezeigt: Es gehört zum Kopfschalter „Plugin aktiv“, der
beim Einschalten den Loader-Eintrag und dieses Feld setzt.

`scope = global` ist ein weiterer Detail-Scope: Das Feld gehört zu
`[section] key` der Plugin-INI und erscheint in der Karte aus `[global]`.
Dafür gibt es zusätzlich den Typ `boolean` (Schalter, schreibt `0`/`1`).

Originalzeilen sind auch hier gesperrt, können aber ausgeblendet werden
(`suppressed = 1` in `user_config\<plugin>.editor.ini`); sie fehlen dann in
der wirksamen Datei, bleiben in der Originalbasis und lassen sich in der
Oberfläche wieder anzeigen. Persönliche Zeilen stehen als
`[item:<Kennung>] owned = 1, list = <Tupel>`, Plugin-Schalter als
`[global] field.<id> = <Wert>`.

### Abschnitts-Editoren (`editor_type = keyed_sections`)

Seit 0.20.0 gibt es den dritten Typ für INIs, in denen jeder Abschnitt ein
Eintrag ist (deposits.ini). Der Deposits-Adapter
(`settings_schemas\deposits.launcher.ini`) verwendet:

```ini
[launcher]
editor_type = keyed_sections
default_tab = deposits

[editor]
plugin = deposits
config = deposits.ini
reserved_sections = deposits   ; Abschnitte, die KEIN Eintrag sind
maximum_items = 118

[source]                        ; Ressourcen für den +-Dialog
plugin = resources
section = list

[list]
summary = type|map              ; Schlüssel unter dem Namen in der Liste

[new]
name_label_key = deposits.new_name
token_key = token               ; Feld, das der Dialog aus dem Namen vorschlägt
token_template = $TYPE_MINE_{NAME}
hint_key = deposits.new_hint    ; Hinweis am Ende des Dialogs

[global]
tab = general
notice_key = deposits.notice    ; gelber Hinweis in der Karte

[detail:type]
scope = item                    ; Schlüssel im Abschnitt des Eintrags
key = type
type = integer
minimum = 10
maximum = 127
unique = 1                      ; kein Wert doppelt
auto_increment = 1              ; Dialog schlägt Maximum + 1 vor
dialog = 1                      ; Teil des +-Dialogs

[detail:icon]
scope = item
key = icon
type = choice
choices_source = registry       ; Kennungen aus [source] plus Grundspiel
allow_other = 1

[detail:editor]
scope = item
key = editor
type = text
maximum_length = 7
length_rule = map=terrain:4     ; kürzer, solange map = terrain
default = {name}                ; Dialog: Name des Eintrags
```

`scope = item` verbindet ein Feld mit `[<Eintrag>] key`. Zusätzliche
Feldangaben: `allow_other` (Auswahl mit Freitext), `unique`, `auto_increment`,
`dialog`, `default` (`{name}` = Name des neuen Eintrags), `maximum_length`,
`length_rule = <key>=<wert>:<n>` und `choices_source = registry`. Der Dialog
zeigt Ressource, Name, das `token_key`-Feld und alle Felder mit `dialog = 1`.
Originale Abschnitte sind gesperrt, können aber ausgeblendet werden
(`suppressed = 1`), persönliche stehen als `[item:<Name>] owned = 1` mit
`field.<id>`-Werten und werden als neuer Abschnitt in Feldreihenfolge
geschrieben.

Seit 0.22.0 darf ein solches Schema auch in einem Workshop-Paket liegen
(`config\<name>.launcher.ini`); `[editor] plugin` und `config` müssen dann
DLL-Name und INI des Pakets nennen. Zusätzlich:

```ini
[card:surface]              ; weitere Karte für plugin-weite Felder
tab = sand
label_key = dp.card.surface
notice_key = ...            ; optionaler gelber Hinweis

[detail:sand_surface]
scope = global
card = surface              ; Feld auf dieser Karte statt auf [global]
...

[links]                     ; Knöpfe, die Dateien des Pakets öffnen
tab = general
label_key = dp.links
root = ..                   ; relativ zum Schema-Ordner

[link:readme]
file = README_DE.md         ; .md, .txt, .html oder .pdf, kein ..
label_key = dp.link.readme
order = 10

[folder:icon_store]         ; seit 0.4.28: Ordnerzeile auf einer Karte
card = icons                ; Karte ([card:icons]) oder global
path = vfs:media_soviet\research   ; vfs: = VFS-Wurzel des Loaders, build: = Loader-Ordner, package: = [links] root
missing_key = re.icon_store.missing ; gelber Kasten, solange der Ordner fehlt
create = 1                  ; Knopf "Ordner erstellen", solange er fehlt
label_key = re.icon_store   ; Zeile mit Pfadfeld, "Öffnen" und "Aktualisieren", sobald er existiert
order = 10

[file:noimage]              ; Dateizeile: erster vorhandener Kandidat wird gezeigt
card = icons
paths = workshop=package:hooks\x\noimage.png | local=build:plugins\x\noimage.png   ; Kennzeichen workshop oder local
label_key = re.noimage
order = 20

[group:research]            ; seit 0.4.29: zweite Liste mit eigenem Präfix und Reiter
tab = research
section_prefix = research:  ; Einträge sind die Abschnitte [research:<id>]
label_key = re.group.research

[list:research]             ; Texte dieser Liste (wie [list]), maximum_items optional
label_key = re.list.research
summary = type|cost

[new:research]              ; Hinzufügen-Dialog dieser Liste (wie [new])
name_label_key = re.new.research

[detail:r_type]
scope = item
group = research            ; Feld gehört zur zweiten Liste
key = type
type = choice
choices = technical|soviet|medical

[detail:r_requires]
scope = item
group = research
key = requires
type = lines
picker = game_research      ; Forschungs-Picker aus der research.ini des Spiels

[detail:r_name]
scope = item
group = research
key = name
type = text
suffix = .name              ; seit 0.4.31: gesperrtes Kästchen ".name" hinter dem Feld, Kennung des Eintrags als grauer Platzhalter

[detail:r_enabled]
scope = item
group = research
key = enabled
type = boolean
position = above_id         ; seit 0.4.35: Feld steht über der Kennungszeile des Detailbereichs

[detail:r_cost]
scope = item
group = research
key = cost
type = integer
minimum = 1
maximum = 2147483647
step = 100                  ; seit 0.4.39: Schrittweite der Plus/Minus-Knöpfe (Standard 1 bei integer, 0.1 bei decimal; auch bei [column:])

[picture:icon]              ; seit 0.4.35: Bildvorschau je Eintrag mit "Bild einfügen…" (kopiert ein PNG unter <id>.png in den Ordner)
group = research
folder = vfs:media_soviet\research
file = {id}.png
size = 128                  ; Pflichtgröße in Pixeln, 0 = beliebig
order = 15

[list:research]
remove_label_key = re.remove.research   ; seit 0.4.35: Knopf "nur Eintrag löschen" im Löschdialog; "Alles löschen" nimmt Texte und Bild mit

[tab:localization]
label = Localization         ; bleibt unübersetzt, Bezug zum Plugin

[textpack]                  ; seit 0.4.30: Localization-Textpaket auf einem eigenen Reiter
tab = localization
folder = build:plugins\localization\research_expansion   ; lokales Paket, dort schreibt der RMM
seed = dependency:tesmio.localization|hooks\localization\research_expansion   ; Startbestand aus dem Paket der Abhängigkeit
namespace = research_expansion   ; für ein neues Paket ohne Startbestand
keys_from = research        ; Listengruppe, deren Kennungen die Zeilen <id>.name / <id>.desc liefern
missing_key = re.textpack.missing   ; gelber Kasten mit Knopf "Lokal anlegen", solange der Ordner fehlt
```

## Sprachen

```ini
[language]
name = Deutsch

[strings]
plugin.description = Beschreibung
field.enabled = Plugin aktivieren
row.road = Straßenfahrzeuge
unit.coefficient = Koeffizient
```

`language_directory` ist relativ zur Paketwurzel. Fallback-Reihenfolge:
gewählte Sprache, Englisch, einfacher Text im Schema. Technische Abschnitts-,
Schlüssel-, Gruppen-, Reiter- und Eintragskennungen werden nie übersetzt.

## Symbole und Pfadsicherheit

Eingebaute Symbole umfassen unter anderem `builtin:gear`, `builtin:truck`,
`builtin:rail`, `builtin:ship` und `builtin:plane`. Alternativ ist ein sicherer
relativer PNG-/ICO-Pfad innerhalb des Pakets möglich. Absolute Pfade, `..`, URLs,
Reparse Points, übergroße Dateien und ungültige Bildabmessungen werden abgewiesen
oder als optionales Symbol protokolliert und durch ein Standardsymbol ersetzt.

### Feldtyp `lines` (seit 0.31.0, nur `scope = item`)

Ein Schlüssel, den die Plugin-INI im Abschnitt wiederholt (`target = …`, `add = …`),
wird als `type = lines` beschrieben. Der Editor zeigt ein mehrzeiliges Feld, jede Zeile
ist ein Vorkommen des Schlüssels; leere Zeilen entfallen. Die wirksame INI erhält die
Zeilen als zusammenhängenden Block an der Stelle des ersten Vorkommens. Im Override
stehen sie als `field.<id>.0`, `field.<id>.1`, … Für `[list] summary` zeigt ein
lines-Feld die erste Zeile mit Zähler `(+n)`. `[source]` ist bei `keyed_sections`
optional: ohne Quelle fragt der Plus-Dialog nur Name und Dialogfelder ab.

### Gebäude-Auswahl an Zeilenfeldern (seit 0.32.0)

Ein lines-Feld kann `picker = game_buildings` tragen: Dann steht unter dem Feld der Knopf
„Gebäude auswählen…“, der die Gebäudedateien des Spiels (`buildings_types`), der DLCs und
der abonnierten Workshop-Objekte nach Art gruppiert zur Auswahl stellt und die Zielzeilen
in Plugin-Schreibweise einträgt. `count_label`/`count_label_key` benennen den Zähler unter
dem Feld (sonst „Anzahl Zeilen“), `maximum_lines` begrenzt die Zeilenzahl; darüber wird der
Zähler rot und der Wert abgewiesen. Zeilenfelder haben eine feste Höhe mit Scrollbalken,
lassen sich am Griff darunter aufziehen und mit Plus/Minus ganz auf- und zuklappen.

### Abschnittspräfix (seit 0.33.0, `keyed_sections`)

`[editor] section_prefix = modify:` macht nur Abschnitte zu Einträgen, die so beginnen; die
Kennung ist der Rest (`[modify:faculty_geology]` → `faculty_geology`). Alle anderen Abschnitte
und freie Zeilen der INI (etwa Forschungsblöcke) bleiben unverändert stehen. Ohne Präfix gilt
wie bisher: jeder nicht reservierte Abschnitt ist ein Eintrag.

## Verweisprüfung (seit 0.4.40)

Werte, die etwas benennen müssen, was das Spiel kennt, prüft der RMM vor dem Speichern. Ein Treffer macht die Fußzeile rot („Konfiguration ungültig“) mit Klartext, welcher Eintrag und welches Feld betroffen sind; Speichern und „Speichern + Starten“ brechen mit derselben Meldung ab. Geprüft werden nur eigene Einträge und persönlich gesetzte Werte. Fehlt der Spielordner neben dem Loader, entfällt die Prüfung.

```ini
[group:modify]
id_reference = game_research        ; die Kennung jedes eigenen Eintrags muss eine aktive Vanilla-Forschung sein

[detail:r_requires]
type = lines
reference = game_research           ; Menge: game_research, game_buildings, game_texts oder resources (seit 0.4.41: Grundspiel plus resources.ini mit hook = 2)
reference_format = requires         ; "<Vorgänger> | before/after | <Anker>": Vorgänger und Anker werden geprüft
reference_own = research            ; eigene, eingeschaltete Einträge dieser Gruppe gelten ebenfalls als bekannt

[detail:r_unlock]
type = lines
reference = game_research
reference_format = directive:$UNLOCK_RESEARCH   ; nur Zeilenteile mit dieser Direktive; das Wort dahinter ist die Kennung (auch bei "A | B")
reference_own = research

[detail:target]
type = lines
reference = game_buildings
reference_format = file             ; Datei relativ zu media_soviet oder zum Workshop-Ordner muss existieren
```

`reference_format = id` (Standard) prüft den ganzen Wert bzw. jede Zeile. `reference` gilt für `text`, `lines` und `choice` mit `scope = item`; `id_reference` steht an `[group:<id>]` (Zusatzliste) oder an der Standardgruppe, auch bei `keyed_list` (Text-IDs gegen `game_texts`).

## Vanilla-Block und Zeilenauswahl (seit 0.4.42)

Für Editoren, deren Einträge bestehende Forschungen des Spiels bearbeiten (Research Expansion, Reiter Vanilla-Änderungen):

```ini
[list]
id_picker = game_research           ; Hinzufügen-Dialog mit „Forschung wählen…“ (Vanilla-Forschungen, benutzte und eigene ausgeblendet); auch an [list:<id>]

[research_block:block]              ; schreibgeschützter Kasten mit dem Forschungsblock aus media_soviet\research\research.ini
label = Original im Spiel
description = …
order = 12                          ; wie Felder und [picture:] nach order eingeordnet; group = <id> für Zusatzlisten

[detail:replace]
type = lines
picker = research_lines             ; Knopf „Zeile wählen…“: Zeilen genau dieses Blocks
picker_format = line_edit           ; line = "<Zeile>", line_edit = "<Zeile> | <bearbeitete Kopie>", line_anchor = "<Zeile> | <Anker>",
                                    ; anchor_edit = "<Anker> | <neue Zeile>", edit = "<neue Zeile>"; bei neuen Zeilen bietet das Fenster
                                    ; „Forschung als $UNLOCK_RESEARCH…“ an
```

Die Liste zeigt bei `id_picker = game_research` den Spielnamen der Forschung unter der Kennung.

## Dateiauswahl aus dem Plugin-Ordner (seit 0.4.48)

```ini
[detail:t_color]
type = text
picker = files                       ; aufklappbare Liste der Dateien: erst der Ordner selbst, dann jeder Unterordner als Gruppe (Titel = Pfad); Tippen bleibt möglich
picker_folders = package:hooks\deposits_plus\assets | build:plugins\deposits_plus\assets   ; Pfadangaben wie bei [folder:], der erste vorhandene Ordner liefert die Liste
picker_pattern = *.dds
reference = files                    ; Prüfung vor dem Speichern: die Datei muss in einem der Ordner liegen (Wert = Pfad relativ zum Ordner, Schrägstriche)
reference_format = dds_dxt1          ; exists (Standard) | dds_dxt1 | dds_dxt5 - die DDS-Formen prüfen zusätzlich Kompression, quadratische Zweierpotenz 256..4096 und die Mipmap-Kette
```

## Aktionszeile in Presentation-Schemas (seit 0.4.53)

```ini
[action:prune]
group = bridge                      ; Karte, in der die Zeile steht; sortiert mit den Feldern nach order
label = Tidy the bridge list        ; Titel links (label_key fÃ¼r die Ãbersetzung)
description = ...                   ; Beschreibung unter dem Titel (description_key)
button = Tidy up now                ; Knopftext rechts (button_key), Knopf wird nie gedehnt
command = bridge_prune              ; bisher einziger Befehl: EintrÃ¤ge der Bridge-Paketliste ohne Paket in der RMM-Liste entfernen
order = 35
```

## Ordnerliste in Presentation-Schemas (seit 0.4.56)

```ini
[folder_list:packs]
group = packs                       ; Karte, unter deren Feldern die Liste steht
paths = package:hooks\localization | build:plugins\localization   ; Pfadangaben wie bei [folder:]; jeder Unterordner wird eine Zeile
description_prefix = loc.pack       ; Kurztext je Ordner aus der Sprachdatei: <prefix>.<ordnername>, fehlt er, bleibt die Spalte leer
note = ...                          ; eine kurze Zeile unter der Liste (note_key)
order = 10
```
Rechts steht je Zeile, wo der Ordner liegt: âim Paketâ (package:-Pfad), âlokalâ (build:/vfs:-Pfad) oder âlokal, Ã¼berlagert das Paketâ (beides).
