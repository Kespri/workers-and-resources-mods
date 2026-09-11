# Republic Mod Manager (RMM) 0.4.26-beta – Plugin-Manager für TesmioLoader

Republic Mod Manager, bis 0.16.0 „Tesmio Settings“ mit der Datei
`tesmio_autoload.exe`, ist eine eigenständige Einstellungs- und
Bereitstellungsoberfläche für den unveränderten TesmioLoader. Programmdatei:
`rmm.exe`, Einstellungen daneben in `rmm.ini` (ein altes `tesmio_autoload.ini`
wird weiter gelesen, solange kein `rmm.ini` existiert). Die Anwendung durchsucht den Workshop-Ordner
nach Paketen mit `soviet.mod.ini`. Seit 0.9.0 genügt dafür das gewöhnliche
Manifest von Soviet Mod Loader: `[mod]` und `[hooks] dll`. Alles, was Autoload
früher zusätzlich verlangte, wird aus dem DLL-Namen abgeleitet; ein mitgeliefertes
Launcher-Schema verfeinert die Darstellung, ist aber nicht mehr Voraussetzung.
Fehlt es, entsteht die Oberfläche aus der INI des Plugins selbst.

Der Loader enthält keine Sonderbehandlung und keine eingebetteten Einstellungen
für Vehicle Materials. Ein zweites Plugin mit einem anderen Namen, einer anderen
INI und anderen Feldern kann ohne Programmänderung geladen werden.

## Bedienung

1. `rmm.exe` öffnen. Workshop-Hauptordner, letztes Paket, Reiter und
   Sprache werden wiederhergestellt.
2. Plugin links auswählen. Unvollständige oder inkompatible Pakete bleiben mit
   einer genauen Ablehnungsursache sichtbar, erhalten aber keinen Editor.
3. Einstellungen ändern. Reiter, Gruppen, Texte, Symbole, Feldtypen, Grenzen und
   Aktivierungsschalter stammen aus dem Paket-Schema.
4. **Speichern** legt einen rollierenden Wiederherstellungspunkt an, schreibt nur
   persönliche Abweichungen nach `build\user_config` und stellt DLL sowie wirksame
   INI nach `build\plugins` bereit.
5. **Speichern + Starten** führt denselben geprüften Vorgang aus, startet danach das
   Spiel über `tesmiolauncher.exe --nogui` und schließt Republic Mod Manager.

Workshop-Dateien werden nie verändert. Das bloße Öffnen, Suchen, Wechseln von
Plugins, Reitern oder Sprachen schreibt keine Plugin-Datei. Zahleneingaben dürfen
in der Oberfläche ein Dezimalkomma verwenden; gespeichert wird mit Dezimalpunkt.
Gleichwertige Schreibweisen wie `0.030` und `0.03` gelten nicht als Änderung.

## Unterstütztes Paketmodell

Ein Paket braucht in `soviet.mod.ini` nur `[mod]` mit `id` und `name` sowie
`[hooks] dll` mit genau einer x64-Tesmio-Plugin-DLL. Daraus wird abgeleitet:

- der lokale Zielname aus dem DLL-Dateinamen (`hooks\walking.dll` → `walking`);
- die Standard-INI als `<Ziel>.ini` neben der DLL, falls vorhanden;
- der Name der persönlichen Konfiguration, ebenfalls `<Ziel>.ini`;
- das Launcher-Schema aus `config\<Ziel>.launcher.ini`, falls vorhanden.

`[configuration]` und `[autoload]` bleiben als Übersteuerung gültig: `defaults`,
`launcher_schema`, `user_config`, `target`, `requires_local`, `conflicts_local`.
Ein deklariertes `target` muss weiterhin zum DLL-Namen passen. `tesmio_api_min`
und `tesmio_api_max` sind optional; wenn angegeben, muss API 4 im Bereich liegen.

**Ohne Launcher-Schema** entsteht die Oberfläche aus der INI: Jeder Schlüssel wird
ein Feld, der Kommentarblock darüber seine Beschreibung, der Wert bestimmt den
Typ: `0`/`1` wird zum Schalter, außer der Schlüsselname nennt eine Menge
(`count`, `days`, `frequency`, `size` und ähnliche) oder der Kommentar nennt
andere ganze Zahlen oder einen Bereich wie `2-12`; dann Ganzzahl. Andere
Ganzzahlen, Dezimalzahlen, sonst Text. Ein Schlüssel `enabled` wird zum Aktivierungsschalter. Ein
Kommentar hinter dem Wert wie `130 ; (stock 121)` bleibt in der Datei, bis der
Wert geändert wird. Weil kein Schema die erlaubten Schlüssel festlegt, werden in
diesem Modus unbekannte Schlüssel einer lokalen INI nicht abgewiesen. Mit
mitgeliefertem Schema gilt die strenge Prüfung wie bisher.

Unterstützte Feldtypen sind `boolean`, `integer`, `decimal`, `choice`, `text`
und `readonly`.

**Overlay-Plugins** kennzeichnen sich mit `[configuration] user_overlay = 1`.
Ihre DLL liest `user_config\<Ziel>.ini` selbst. Republic Mod Manager stellt dann die
Original-INI byteidentisch bereit und schreibt persönliche Werte ausschließlich
nach `user_config`. Ohne Republic Mod Manager läuft das Plugin mit seinen Standardwerten.

**Reine SML-Inhaltspakete** (`[content]` ohne `[hooks] dll`, etwa Ressourcen,
Vorkommen und Gebäude) erscheinen in der Liste als „SML-Inhalt“. Sie haben
keinen Editor und werden nicht bereitgestellt; das erledigt Soviet Mod Loader.
Ein Hook-Paket mit zusätzlichem `[content]` wird geladen; den Inhalt wendet nur
Soviet Mod Loader an. `[dependencies]` werden ausgewertet, siehe „Aktivität,
Konflikte und Sicherheit“. Eine DLL ohne INI wird ohne Einstellungen bereitgestellt.

Mehrere `dll`-Einträge unter `[hooks]` werden mit genauer Ursache abgewiesen.
Pfadausbrüche, doppelte IDs und nicht zur DLL passende Zielnamen ebenfalls.

Details und vollständige Beispiele stehen in `SCHEMA_DE.md`.

## Installierte Plugins ohne Paket

Seit 0.10.0 erscheint auch jedes Plugin, das nur als `plugins\<name>.dll` mit
`plugins\<name>.ini` im Loader-Ordner liegt, so wie es ein reiner
TesmioLoader-Nutzer installiert. Solche Einträge tragen den Status
„Installiert“; die Version stammt aus dem letzten `tesmioloader.log`. Plugins,
die ein gelistetes Workshop-Paket oder ein lokaler Editor (Resources) abdeckt,
werden nicht doppelt gezeigt.

Für diese Plugins gibt es kein Original im Workshop, deshalb gilt die
**geschützte Basis**:

- Die vorgefundene INI gilt als Original. Beim ersten Speichern wird sie nach
  `user_config\.autoload\<name>.upstream.ini` gesichert; erst dann wird die
  wirksame INI (Original plus persönliche Werte) nach `plugins\` geschrieben.
- Die DLL wird nie angefasst. Persönliche Werte liegen wie bei Paketen in
  `user_config\<name>.ini`.
- Wird `plugins\<name>.ini` später außerhalb von Republic Mod Manager geändert, etwa
  durch eine neue Plugin-Version, gilt diese Datei als neues Original: Sie wird
  erneut gesichert, und die persönlichen Werte werden wieder darübergelegt.
- **Original wiederherstellen** in der Fußzeile schreibt die gesicherte INI
  byteidentisch zurück und verwirft alle persönlichen Werte des Plugins.

Das Schema kommt in drei Stufen: aus dem Paket (bei Workshop-Paketen), sonst
aus `settings_schemas\<name>.launcher.ini` neben der EXE, sonst aus der INI
selbst. Ein lokales Schema muss `config = <name>.ini` nennen; `id` und `name`
darin bestimmen Kennung und Anzeigename. Eine DLL ohne INI wird gelistet, hat
aber nichts einzustellen.

Seit 0.13.0 liegen unter `settings_schemas` deutsch/englische Schemas für die
Upstream-Plugins accumulator, cities, daynight, depletion, easystart und
walking; deposits und needs (dynamische Abschnitte) behalten die INI-Ableitung,
resources den eigenen Editor. Ein lokales Schema beschreibt, es verbietet
nicht: Werte dürfen einen Kommentar hinter sich tragen, und Schlüssel, die eine
neuere Plugin-Version hinzufügt, werden in den Hinweisen genannt und bleiben in
der INI selbst editierbar.

## Plugin-Schalter, Hinweise und Spielversion

Seit 0.19.0 zeigt die Kopfzeile nur noch einen Schalter, **„Plugin aktiv“**.
Er ersetzt die früheren zwei Schalter „Plugin-Funktion“ und „Im Loader laden“,
denn ein geladenes Plugin mit abgeschalteter INI ergab keinen Sinn. Was der
Schalter tut, hängt davon ab, wer die DLL lädt:

- **Installierte Plugins, Workshop-Pakete ohne SML und die lokalen Editoren
  Resources und Needs:** Der Schalter ist das Kästchen des TesmioLaunchers, also
  `[plugins] <name>` in `tesmioloader.ini`. Beim Einschalten setzt Republic Mod
  Manager zusätzlich das `enabled`-Feld der INI auf 1, falls das Schema eines
  kennt, damit die DLL nicht lädt und sofort ablehnt. Beim Ausschalten bleibt
  die INI unverändert; nur der Loader-Eintrag wird 0. Der Schalter zeigt „an“
  nur, wenn beides stimmt, genau wie der grüne Punkt in der Liste.
  `tesmioloader.ini` wird an Ort und Stelle bearbeitet, Kommentare und die
  Schalter anderer Plugins bleiben erhalten, ein BOM wird nie geschrieben.
- **Workshop-Pakete unter der Workshop Bridge:** derselbe Schalter schreibt die
  Zeile `[packages] <Nummer>` in `user_config\workshop_bridge.ini`.
- **Workshop-Pakete unter aktivem SML:** Soviet Mod Loader lädt jedes abonnierte
  Paket, einen Loader-Eintrag gibt es nicht. Hat die INI ein `enabled`-Feld,
  schaltet der Schalter nur dieses Feld. Hat sie keins, fehlt der Schalter und
  ein Hinweis sagt, dass nur das Workshop-Abo das Paket abschaltet.
- Inhaltspakete haben keinen Schalter.

Das `enabled`-Feld selbst erscheint nicht mehr in den Karten; bei Needs fehlt
darum der Eintrag „Plugin aktiv“ in der Karte „Plugin-Einstellungen“.

Über den Einstellungen eines Eintrags erscheint eine Karte **„Hinweise“** mit
allem, was vorher nur im Protokoll stand: Overlay-Modus, INI-Ableitung,
geschützte Basis, SML-Betrieb, aufgelöste Abhängigkeiten. Nicht erfüllte
Abhängigkeiten und eine doppelt vorhandene DLL stehen rot darüber.

**Speichern + Starten** ruft seit 0.19.0 `tesmiolauncher.exe --nogui` auf: Das
Spiel startet sofort mit dem Stand aus `tesmioloader.ini`, das Fenster des
Launchers erscheint nicht mehr. Wer es wieder sehen will, setzt in `rmm.ini`
unter `[settings]` den Wert `tesmiolauncher_window = 1`. Die Versionsprüfung
des Launchers bleibt in beiden Fällen aktiv.

Beim Start liest Republic Mod Manager den Build-Stempel von `SOVIET64.exe` (den
Pfad aus `tesmioloader.ini`, sonst zwei Ordner über dem Loader). Gehört er zu
keiner Version, für die die Plugins gemacht sind, erscheint eine Warnung, und
„Speichern + Starten“ fragt vor dem Start nach. Bekannt ist 1.1.1.9; weitere
Stempel lassen sich in `settings_schemas\game_versions.ini` unter
`[supported]` als `<Hex-Stempel> = <Version>` eintragen. `version_check = 0`
unter `[settings]` in `rmm.ini` schaltet die Prüfung ab. Doppelt
vorhandene DLLs bei aktivem SML werden ebenfalls beim Start gemeldet.

## Workshop Bridge

TesmioLoader allein lädt nur DLLs aus `plugins\`. Ein Workshop-Paket braucht
deshalb Soviet Mod Loader oder eine Kopie der DLL, die Republic Mod Manager bisher
beim Speichern anlegte. Seit 0.14.0 gibt es den dritten Weg: das Plugin
**workshop_bridge** (aus `my_plugins`, als `plugins\workshop_bridge.dll` mit
`workshop_bridge.ini` installiert) lädt die Hook-DLLs der freigegebenen Pakete
direkt aus dem Workshop-Ordner und gibt ihnen dieselbe Host-Tabelle wie der
Loader. Steam hält die Pakete aktuell, nichts wird kopiert.

Ist die Bridge installiert, in `tesmioloader.ini` eingeschaltet und Soviet Mod
Loader nicht aktiv, arbeitet Republic Mod Manager für Workshop-Pakete im
**Bridge-Modus**:

- Der Schalter **„Plugin aktiv“** schreibt die Zeile
  `[packages] <Workshop-Nummer> = 1` beziehungsweise `0` nach
  `user_config\workshop_bridge.ini` statt nach `tesmioloader.ini`. Ein Paket,
  das nie eingeschaltet wurde, lädt die Bridge nicht (Vorgabe `policy = list`).
- Beim Speichern wird nur die INI nach `plugins\` gestellt, nie die DLL. Der
  Empfangsbeleg trägt `mode = bridge`.
- Eine `plugins\<name>.dll`, die Republic Mod Manager selbst früher kopiert hat (der
  Beleg beweist es), wird beim nächsten Speichern entfernt, damit die
  Workshop-Kopie gilt; der Wiederherstellungspunkt behält sie. Eine fremde
  Kopie bleibt: dann lädt der Loader diese, die Bridge überspringt das Paket,
  und die Hinweise sagen es rot.
- Die Aktivitätsanzeige folgt der Liste der Bridge und deren eigenem Schalter.
- Abhängigkeiten, die als Hook-Paket in der Liste der Bridge stehen, gelten
  als erfüllt.

Die Bridge selbst erscheint als installiertes Plugin mit eigenem Schema
(Bridge aktiv, `policy`, Workshop-Ordner, Protokoll). Ihre Paketliste bleibt
bei jedem Speichern dieser Einstellungen und auch bei „Original
wiederherstellen“ erhalten. Unter aktivem SML bleibt die Bridge untätig und
Republic Mod Manager verhält sich wie in „SML als Nachbar“ beschrieben.

## Dateien nur lokal

Ein Workshop-Paket, das über die Workshop Bridge läuft, kann seit 0.21.0 als
Ganzes nach `plugins` kopiert werden, wenn der Autor das im Manifest erlaubt
(`[configuration] local_copy = 1`). In der Karte „Hinweise“ erscheint dann der
Schalter **„Dateien nur lokal“**. Einschalten und Speichern zeigt zuerst ein
Fenster mit jeder Datei, die kopiert wird, samt SHA-256 der DLL; erst nach
„OK“ kopiert Republic Mod Manager in derselben Transaktion DLL, INI und den im
Manifest unter `[assets] dir` genannten Ordner nach `plugins\`. Die Workshop
Bridge überspringt das Paket danach von selbst, weil die DLL lokal liegt; der
Loader-Eintrag in tesmioloader.ini übernimmt den Schalter „Plugin aktiv“.

Der Beleg merkt sich die Wahl (`local_copy = 1`) und jede kopierte Datei
(`asset.N`). Ausschalten und Speichern entfernt genau diese Dateien wieder,
nach einer Bestätigung mit Liste, und das Paket läuft wieder über die Bridge.
Fremde Kopien in `plugins` werden nie angefasst. Steam-Updates wirken bei
lokalen Dateien erst nach erneutem Speichern; die gelbe Marke „Update“ zeigt
sie an wie bisher.

Der Assets-Ordner landet unter `plugins\<Ordnername>\`, bei Deposits Plus also
`plugins\deposits_plus\assets\...`, genau dort, wo das Plugin ohne Paket sucht.
Ausführbare Dateien sind im Assets-Ordner nicht erlaubt.

## Aktualisierte Pakete

Seit 0.15.0 merkt sich der Empfangsbeleg einer Bereitstellung die Paketversion
und die Hashes der bereitgestellten Dateien. Hat sich das Workshop-Paket
seitdem geändert, weil Steam ein Update geholt hat oder der Autor die Dateien
ausgetauscht hat, zeigt die Liste am Eintrag ein gelbes „Update“, die
Hinweiskarte nennt alte und neue Version und was sich geändert hat (DLL,
Standard-INI), und das Protokoll fasst beim Start alle betroffenen Pakete
zusammen. Nichts davon blockiert: **Speichern** übernimmt die neue Fassung,
persönliche Werte bleiben erhalten. Unter SML oder der Workshop Bridge zählt
nur die Standard-INI, weil das Spiel die DLL ohnehin aus dem Paket lädt.

Der Sicherheitsdialog vor dem Schreiben einer DLL erscheint seit 0.15.0 nur
noch, wenn tatsächlich eine neue oder geänderte DLL nach `plugins\` geschrieben
würde: nie für installierte Plugins, nie unter SML oder der Bridge, und nicht,
wenn die Kopie in `plugins\` bereits dieselbe ist.

## Protokolle

Das Protokoll-Symbol in der Seitenleiste öffnet seit 0.16.0 ein Fenster mit
drei Quellen: dem Journal dieser Tesmio-Settings-Sitzung, `tesmioloader.log`
aus dem Loader-Ordner und jedem `tesmioloader.<plugin>.log`, das ein Plugin
im Unterordner `logs\` oder direkt im Loader-Ordner schreibt (Einträge aus
`logs\` tragen den Ordner im Namen). Die Dateien werden mit geteiltem Zugriff gelesen, das Fenster
funktioniert also auch, während das Spiel läuft; „Aktualisieren“ liest neu.
Eine Suche filtert nach Text, „Absender“ nach dem ersten Wort einer Zeile
(`plugin`, `bridge`, `hook`, ein Plugin-Name, `game.ERROR`), und „Nur Probleme
und Warnungen“ blendet alles Unauffällige aus. Problemzeilen (error, fatal,
failed, refused, mismatch, exception) stehen rot, Warnungen (warn, declined,
skipped, missing, unknown) gelb; die Zusammenfassung „0 warning(s), 0 error(s)“
eines Plugins und die `hook ok`-Zeilen gelten als unauffällig. Die Fußzeile
nennt Zeilen, Treffer und die letzte Abschlusszeile des Loaders.

In der Hinweiskarte eines Plugins steht außerdem, welche Version der Loader
beim letzten Spielstart geladen hat, auch wenn die Workshop Bridge sie geladen
hat, oder dass das Plugin im letzten Lauf nicht geladen wurde.

## Profile und Wiederherstellungspunkte

Das Archiv-Symbol in der Seitenleiste öffnet seit 0.23.0 das Fenster „Profile
und Wiederherstellung“ mit zwei Reitern. Vorher werden ungespeicherte
Änderungen wie beim Aktualisieren abgefragt; nach einer Änderung im Fenster
liest die Plugin-Liste neu.

**Profile** (Wunsch 6) sichern den Konfigurationsstand des Loader-Ordners
unter einem Namen: `tesmioloader.ini`, `plugins\*.ini`, `user_config\*.ini`
und die Belege und geschützten Originale unter `user_config\.autoload\*.ini`.
DLLs gehören nie dazu, ebenso wenig die rollierenden Sicherungen. Ein Profil
liegt als Ordner unter `user_config\.autoload\profiles\<Name>_<Kennung>\` mit
`profile.ini` (Name, Zeitpunkt, Notiz, Dateiliste mit SHA-256) und `files\`.
„Aktuellen Stand sichern…“ fragt Name (1–40 Zeichen) und Notiz ab; ein
bestehender Name wird nach Rückfrage überschrieben. Die Dateiliste rechts
zeigt je Datei, ob sie unverändert, abweichend, lokal fehlend oder nur lokal
vorhanden ist. „Profil anwenden“ schreibt die gesicherten Dateien zurück und
entfernt `user_config`-Dateien, die das Profil nicht kennt; `plugins\*.ini`
eines später installierten Plugins bleibt. Das geschieht in derselben
Transaktion wie ein Speichern, geschützt gegen laufendes Spiel und Launcher,
und hinterlässt den Wiederherstellungspunkt „profile“ mit dem Stand davor.

**Wiederherstellungspunkte** (Wunsch 7) machen sichtbar, was jedes Speichern
unter `user_config\.autoload\backups\<Paket-ID>\previous` ablegt: den Stand
aller berührten Dateien von vor dem letzten Speichern dieses Plugins, mit
Zeitpunkt und je Datei der Angabe, ob sie seither verändert wurde oder damals
noch nicht vorhanden war. „Auf diesen Punkt zurücksetzen“ schreibt genau diese Dateien zurück
(damals fehlende werden entfernt, auch eine damals kopierte DLL); der jetzige
Stand wird dabei zum neuen Punkt desselben Plugins, das Zurücksetzen lässt
sich also wieder rückgängig machen. Unvollständige Punkte (Dateien fehlen oder
liegen außerhalb des Loader-Ordners) werden nur angezeigt. Ein Marker
`pending.txt` einer abgebrochenen Bereitstellung wird in der Fußzeile gemeldet.

Schnappschuss des Fensters: `--ui-snapshot <png> --window profiles|points|buildings|add` (`add` = Hinzufügen-Dialog des gewählten Listeneditors, seit 0.4.8).

## Resources-Editor: Vorbild, Transportklasse, Materialfamilie

Seit 0.16.0 sind die drei Felder Auswahllisten. **Vorbildressource** bietet die
57 Ressourcen des Grundspiels, gruppiert nach Grundlagen, Bau, Rohstoffe,
Nuklear, Konsum, Industrie, Wasser und Dünger sowie Abfall, jede mit ihrer
Transportklasse in Klammern, dazu `custom` für einen Datensatz ohne Vorbild.
Das Plugin akzeptiert nur diese Namen; ein Tippfehler würde still zu `custom`.
Im Anlegen-Dialog schlägt das gewählte Vorbild seine Klasse vor.

**Transportklasse** listet die 18 Klassen in der Reihenfolge des Spiels
(covered, open, gravel, oil, cement, cooler, livestock, passanger, concrete,
eletric, vehicles, general, nuclear1, nuclear2, heating, water, sewage, waste).
Der Name genügt: Das Plugin ergänzt Kapazitätsfaktor und Kennzahlen, die das
Grundspiel für diese Klasse verwendet. Steht in der INI eine lange Form wie
`oil, 1, 5, 5, 0`, zeigt die Liste `oil`; wer die Zahlen ändern will, tut das
in der INI selbst.

**Materialfamilie** bietet die elf Namen (none, gravel, steel, aluminium,
plastic, bio, food, burnable, toxic, other, ash). Die Nummern 10 bis 19 aus der
INI werden mit ihrem Namen angezeigt und als Name gespeichert. Die Familie
bestimmt, wie angeliefertes Material auf Baustellen aussieht, und sehr
wahrscheinlich, zu welcher Abfallsorte es wird.

## Dynamische Sammlungen

Ein Paket kann eine Sammlung aus einem lokalen Plugin-Katalog aufbauen. Das
Schema beschreibt Quelle, Bereitschaftsbedingung, Zähler, Eintragspräfix,
Zielabschnitte, Zeilenbeschriftungen, Symbole, Standardwert und Grenzwerte.
Die Anwendung ergänzt daraus zur Laufzeit die Auswahl, Eintragsliste und Matrix.

Vehicle Materials verwendet diesen allgemeinen Mechanismus. Sein Paketstandard
ist jetzt:

```ini
[general]
enabled = 0

[resources]
count = 0
```

Es werden weder Glas noch Kabel oder andere feste Anfangsressourcen ausgeliefert.
Benutzer wählen vorhandene Kennungen aus `plugins\resources.ini` über `+`, tragen
die vier Koeffizienten ein und können jeden so erzeugten Eintrag über den
Papierkorb wieder vollständig löschen. Wird der letzte positive Eintrag gelöscht,
wird das Plugin sicher deaktiviert. `resources.ini` bleibt immer unverändert.

`ownership = user` schützt solche Listen bei Paketupdates: Wenn ein älterer
Paketstandard feste Einträge enthielt und eine neue Version leer startet, übernimmt
Republic Mod Manager die bereits lokal wirksame Liste einmalig als persönliche Werte.
Eine anschließend bewusst geleerte Liste bleibt leer.

## Lokaler Resources-Editor

`settings_schemas\resources.launcher.ini` ergänzt einen schema-gesteuerten
Master-Detail-Editor für das bereits installierte Resources-Plugin. Links stehen
alle Kennungen aus `[list]`; rechts erscheinen Vorbild, Anzeigename, optionale
`[custom:<Kennung>]`-Eigenschaften sowie `[base_price]` und `[price]`.

Die beim ersten Speichern vorgefundene `plugins\resources.ini` wird als
Originalbasis nach `user_config\.autoload\resources.upstream.ini` kopiert.
Persönliche Änderungen liegen getrennt in `user_config\resources.editor.ini`.
Nur **Speichern** erzeugt daraus wieder die wirksame `plugins\resources.ini`;
`resources.dll` wird vom Editor weder ersetzt noch geladen. Wird die lokale
Resources-Datei später außerhalb von Republic Mod Manager aktualisiert, wird sie als
neue Originalbasis erkannt und die persönliche Ebene erneut darübergelegt.

Originale beziehungsweise extern übernommene `[list]`-Einträge tragen in der
Liste ein Schloss. Sie sind nicht löschbar, können aber ausgewählt, persönlich
ergänzt oder überschrieben werden. Mit `+` erzeugte Einträge sind löschbar. Beim
Entfernen bereinigt Republic Mod Manager nach einer ausdrücklichen Bestätigung auch
Verweise in anderen schema-gesteuerten Sammlungen, beispielsweise Vehicle
Materials. Weil Anzahl
und Reihenfolge der Ressourcen Bestandteil des Spielstands sind, weist die
Oberfläche beim Löschen und vollständigen Zurücksetzen ausdrücklich auf mögliche
Spielstandinkompatibilität hin. Ein scheinbarer Deaktivierungsschalter wird nicht
angeboten, da die aktuelle `resources.dll` keinen sicheren inaktiven Slot kennt.

## Lokaler Needs-Editor

`settings_schemas\needs.launcher.ini` beschreibt seit 0.18.0 einen zweiten
Master-Detail-Editor, diesmal für das installierte Needs-Plugin. Der Aufbau
entspricht dem Resources-Editor: links die Bedürfnisliste aus `[list]`, rechts
die Werte des gewählten Eintrags. Eine Zeile der `needs.ini` lautet
`<Ressource> = <Spender>, <Faktor>, <Kategorie>, <Wahrscheinlichkeit>, <Unzufriedenheit>`;
jede dieser fünf Spalten erhält ein eigenes Feld mit Beschreibung, Original-
und Standardwert und einem Zurücksetzen-Knopf, sobald der Wert vom Original
abweicht. Spender und Ladenkategorie sind Auswahllisten (`food`, `meat`,
`clothes`, `eletronics`, `alcohol` beziehungsweise `auto`, `none`, `basic`,
`medium`, `advanced`, `mediumadvanced`, `hotel`); bei der Kategorie darf
zusätzlich eine Zahl eingetippt werden, wie es die Original-INI erlaubt. Faktor,
Wahrscheinlichkeit und Unzufriedenheit sind Dezimalzahlen mit den Grenzen aus der
Plugin-Beschreibung.

Neue Einträge entstehen mit `+`. Der Dialog bietet als Ressource ausschließlich
Kennungen an, die in `plugins\resources.ini [list]` registriert und noch nicht
in der Bedürfnisliste sind; alle fünf Spalten werden im selben Dialog mit
Vorgaben (Faktor 1.0, Kategorie auto, Wahrscheinlichkeit 1.0, Unzufriedenheit 0)
ausgefüllt. Höchstens acht Einträge sind erlaubt, wie im Plugin.

Persönliche Einträge lassen sich über den roten Papierkorb entfernen.
Originaleinträge wie `furniture` und `medicine` werden über denselben Knopf
nur **ausgeblendet**: Sie verschwinden aus der wirksamen `needs.ini`, bleiben
aber in der gesicherten Originalbasis und tauchen unter „Ausgeblendete
Originaleinträge“ mit einem „Wieder anzeigen“-Knopf auf. Beide Aktionen
warnen, weil jedes Bedürfnis den betroffenen Läden ein Lagerfach hinzufügt, das
Teil des Spielstands ist.

Im Reiter „Allgemein“ liegt die Karte „Plugin-Einstellungen“ mit den Schaltern des
Abschnitts `[needs]`: Plugin aktiv, Bedürfnisse vergeben, Läden bestücken,
Bedürfnisse je Bürger (1 bis 7), Verhalten bei vollem Bürger (`skip`/`replace`),
Diagnose und Abstand der Fortschrittszeilen. Auch hier zeigt jede Zeile den
Originalwert und setzt sich per Knopf zurück.

Dateien wie beim Resources-Editor: Originalbasis
`user_config\.autoload\needs.upstream.ini`, persönliche Ebene
`user_config\needs.editor.ini` (Format 1, mit `[global]` für die
Plugin-Schalter und `suppressed = 1` für ausgeblendete Originale), wirksame
Datei `plugins\needs.ini`; `needs.dll` wird nicht angefasst. Solange das
Schema vorhanden ist, erscheint Needs nicht mehr als generischer INI-Eintrag.

## Lokaler Deposits-Editor

`settings_schemas\deposits.launcher.ini` beschreibt seit 0.20.0 den dritten
Master-Detail-Editor, für das installierte Deposits-Plugin. In der deposits.ini
ist jeder Abschnitt außer `[deposits]` ein Vorkommen und seine Schlüssel sind die
Einstellungen. Zwei Reiter:

- **Allgemein** mit der Spielstandwarnung und der Karte „Plugin-Einstellungen“
  für `code_patch`, `minimap` und `editor` aus `[deposits]`.
- **Vorkommen** mit der Liste links (Name, darunter Typnummer und Karte) und
  rechts allen Schlüsseln des gewählten Vorkommens: Token, Typnummer, Karte,
  Komponente, Gebäudetyp, Suchradius, Minimap-Symbol, Minimap-Knopf,
  Pinselname und Erschöpfung. Jede Zeile hat eine Beschreibung aus den
  Kommentaren der Original-INI, den Originalwert und einen Zurücksetzen-Knopf.
  Leer heißt kein Eintrag; die Beschreibung nennt, was das Plugin dann annimmt.

Der Plus-Dialog fragt nur die Standardangaben ab, wie beim Eintrag `clay`: die
Ressource aus `plugins\resources.ini [list]`, die Namen, Token
(`$TYPE_MINE_<NAME>`), Symbol und Pinselnamen vorschlägt, dazu Typnummer,
Karte (Vorgabe auto), Suchradius (Vorgabe ore), Symbol, Minimap-Knopf und
Pinselname. Die Typnummer ist die höchste vorhandene plus 1; eine bereits
belegte Nummer weist das Hinzufügen mit einer Meldung ab, auch später in den
Einstellungen. Ein Hinweis im Dialog verweist auf die übrigen Einstellungen
rechts. Der Pinselname darf sieben Zeichen haben, bei `map = terrain` vier.

Originale wie copper, sand, clay und gas lassen sich ausblenden, persönliche
Vorkommen löschen, beides mit Warnung: Typnummer, Kartenkanal und Token sind
Teil des Spielstands, sobald eine Mine sie verwendet. Dateien wie bei Needs:
`user_config\.autoload\deposits.upstream.ini`, `user_config\deposits.editor.ini`
und die wirksame `plugins\deposits.ini`; `deposits.dll` bleibt unangetastet.
Ein ausgeblendetes Original verliert seinen ganzen Abschnitt in der wirksamen
Datei, ein persönliches bekommt einen neuen Abschnitt am Ende, ein
überschriebenes Original behält Abschnitt und Kommentare.

## Editor-Schema im Workshop-Paket

Seit 0.22.0 darf ein Workshop-Paket unter `config\<name>.launcher.ini` statt
eines gewöhnlichen Schemas ein Editor-Schema mitbringen (`editor_type =
keyed_sections`, `keyed_list` oder `keyed_resources`). Republic Mod Manager
zeigt das Paket dann mit dem Master-Detail-Editor, so wie Deposits Plus mit
den Reitern Allgemein, Sand-Struktur und Vorkommen. Die Originalbasis ist die
INI des Pakets: Ein Steam-Update ist sofort die neue Basis, persönliche
Einträge liegen weiter in `user_config\<name>.editor.ini`, die wirksame Datei
ist `plugins\<name>.ini`. DLL, Loader-Eintrag, Brücken-Liste und "Dateien nur
lokal" laufen unverändert über das Paket; die Karte Hinweise steht auf dem
Reiter der Plugin-Einstellungen. Beim Speichern werden zuerst DLL und
Schalter bereitgestellt, dann die INI geschrieben.

Ein Editor-Schema kann mehrere Karten für plugin-weite Werte haben
(`[card:<id>]`, ein Feld nennt seine Karte) und Knöpfe, die die Anleitungen
des Pakets öffnen (`[links]`, `[link:<id>]`; Markdown, Text, HTML und PDF mit
der Standardanwendung des Systems).

## Symbole in der Plugin-Liste

Seit 0.22.0 zeigt das Symbol links in der Liste, woher ein Eintrag stammt:

- das Steam-Symbol für ein Paket aus einem Workshop-Ordner einer
  Steam-Bibliothek,
- das TesmioLauncher-Symbol für alles, was im plugins-Ordner des Loaders
  liegt, auch Resources, Needs und Deposits,
- ein Zahnrad, wenn die Herkunft unbekannt ist, etwa ein Paketordner außerhalb
  von Steam.

Die Symbole werden aus steam.exe und tesmiolauncher.exe gelesen; fehlt eines,
erscheint ein Ersatzsymbol.

## Aktivität, Konflikte und Sicherheit

Die Lampe in der Pluginliste ist grün, wenn die manifestbestimmte lokale DLL und
INI vorhanden sind, das Plugin-System sowie die DLL aktiviert sind und das optionale
`enabled_field` den Wert `1` besitzt. Grau bedeutet, dass mindestens eine dieser
Voraussetzungen fehlt.

Vor dem Bereitstellen müssen Spiel und TesmioLauncher beendet sein. Deklarierte
Abhängigkeiten müssen lokal vorhanden und aktiviert sein.

**Soviet Mod Loader als Nachbar** (seit 0.11.0): Ist `soviet_mod_loader.dll` im
Loader-Ordner vorhanden und in `tesmioloader.ini` eingeschaltet, lädt SML die
DLLs der Workshop-Pakete selbst. Republic Mod Manager stellt dann nur noch die INI
bereit, bei Overlay-Plugins die unveränderte Original-INI, und schreibt nie eine
DLL nach `plugins\`. Liegt dort noch eine DLL eines Workshop-Pakets aus einer
früheren Bereitstellung, verweigert die Bereitstellung mit dem Hinweis, dass das
Plugin sonst doppelt laden würde; die lokale DLL ist dann zu entfernen. Die vier
Fähigkeiten, die SML selbst mitbringt (`resources`, `deposits`, `needs`,
`buildings`), gelten mit aktivem SML als vorhanden, auch ohne eigene DLL. Die
Aktivitätslampe eines Pakets braucht mit SML keine lokale DLL mehr. Installierte
Plugins ohne Paket bleiben unverändert Sache von TesmioLoader.

**`[dependencies]`** aus dem SML-Manifest werden gegen den Workshop-Ordner
aufgelöst: Jede Kennung muss als Paket vorhanden sein und die Versionsbedingung
(`>=1.2.0`, `>1`, `=1.0`, `<2`, bloße Version, `*`) erfüllen. Ohne SML muss die
DLL eines Hook-Pakets, von dem ein Plugin abhängt, bereitgestellt und aktiviert
sein; ein Inhaltspaket als Abhängigkeit braucht SML. Nicht erfüllte
Abhängigkeiten stehen als Hinweis am Eintrag und verhindern die Bereitstellung.
Weitere alte oder doppelte DLL-Namen können paketweise über `conflicts_local`
gesperrt werden.

Bei jedem Einlesen wird die fertige `plugins\resources.ini` erneut geprüft.
Neue, außerhalb von Settings eingetragene Ressourcen werden als gesperrte externe
Basis übernommen. Verschwundene externe Ressourcen werden gemeldet. Vor
**Speichern + Starten** werden alle installierten, aktiven und per Schema
erkennbaren Ressourcensammlungen geprüft; fehlende Verweise nennen das betroffene
Plugin und verhindern den Spielstart.

Bei einer neuen oder geänderten DLL zeigt die Anwendung Paket, Quelle, Ziel und
SHA-256 und verlangt eine Bestätigung. Ein Hash ist keine digitale Signatur.
Externe Änderungen an einer bereits verwalteten DLL/INI sowie Paketänderungen
während der Bearbeitung blockieren den Schreibvorgang.

Pro Plugin bleibt genau ein vollständiger rollierender Wiederherstellungspunkt:

```text
build\user_config\.autoload\backups\<Paket-ID>\previous
```

Ein Transaktionsmarker verhindert nach einem Prozessabbruch unkontrolliertes
Weiterschreiben. Persönliche Ansichtsdaten liegen getrennt unter
`%LOCALAPPDATA%\TesmioAutoload\profiles`.

## Sprachen und Symbole

Deutsch und Englisch sind in der EXE enthalten. Weitere App-Sprachen können als
`languages\<code>.ini` neben der EXE ergänzt werden. Plugin-Texte kommen aus dem
im Schema angegebenen `language_directory`; die Reihenfolge ist gewählte Sprache,
Englisch, einfacher Schema-Text.

Paket- und Matrixsymbole können eingebaute Kennungen oder sichere relative
PNG-/ICO-Pfade im Paket verwenden. Das Zahnrad-Lkw-Logo ist als mehrgrößiges
Windows-Icon in der EXE eingebettet.

## Entwickeln und testen

`build.bat` erzeugt die x64-WinForms-Anwendung sowie Core- und UI-Tests. Die
Tests prüfen neben Vehicle Materials ein unabhängiges Beispielplugin mit anderem
Zielnamen und anderer INI sowie den lokalen Resources-Editor einschließlich
Originalschutz, Update-Übernahme und Spielstandwarnung. `tests\Verify.ps1` baut
zusätzlich die Plugin-DLL und führt die vollständige Offline-Prüfung aus.

CLI: `--build "..."`, `--workshop "..."`, Kompatibilitätsalias `--package "..."`,
`--language de|en|auto`, `--ui-snapshot "...png"`.
