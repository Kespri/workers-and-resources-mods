# Republic Mod Manager – Anleitung

[English](README_EN.md) | **Deutsch**

Republic Mod Manager (kurz RMM) ist ein Fenster für alle deine TesmioLoader-Plugins. Du siehst, welche Plugins du hast, schaltest sie ein oder aus, stellst ihre Werte mit Beschreibung und Bereichsprüfung ein und startest das Spiel. Der TesmioLoader selbst bleibt unverändert; RMM schreibt nur Einstellungsdateien.

---

## 📋 Inhaltsverzeichnis

1. [Voraussetzungen](#-voraussetzungen)
2. [Das Fenster](#-das-fenster)
3. [Der Schalter „Plugin aktiv“](#-der-schalter-plugin-aktiv)
4. [Speichern und Starten](#-speichern-und-starten)
5. [Die Karte „Hinweise“](#-die-karte-hinweise)
6. [Wie ein Plugin ins Spiel kommt](#-wie-ein-plugin-ins-spiel-kommt)
7. [Dateien nur lokal](#-dateien-nur-lokal)
8. [Das Protokollfenster](#-das-protokollfenster)
9. [Profile und Wiederherstellung](#-profile-und-wiederherstellung)
10. [Listen-Editoren: Resources, Needs, Deposits und Paket-Editoren](#-listen-editoren)
11. [Spielversion und rmm.ini](#-spielversion-und-rmmini)
12. [Wo deine Dateien liegen](#-wo-deine-dateien-liegen)
13. [Wenn etwas nicht klappt](#-wenn-etwas-nicht-klappt)
14. [Für Plugin-Autoren](#-für-plugin-autoren)

---

## 🚀 Voraussetzungen

- *Workers & Resources: Soviet Republic* 1.1.1.9
- TesmioLoader von MaxLegend im Spielordner (`tesmioloader\build` mit `tesmioloader.dll` und `tesmioloader.ini`)
- Windows 10 oder 11

RMM liegt als `rmm.exe` im Ordner `tesmioloader\build`. Starte es von dort oder über die Desktop-Verknüpfung. Es findet den Loader, das Spiel und den Steam-Workshop-Ordner von selbst. Nur ein RMM-Fenster ist gleichzeitig offen; ein zweiter Start holt das vorhandene nach vorn.

---

## 🖥️ Das Fenster

**Links die Liste.** Jede Zeile ist ein Plugin: abonnierte Workshop-Pakete und alles, was als DLL in `tesmioloader\build\plugins` liegt. Das Symbol zeigt die Herkunft: Steam-Symbol für Workshop-Pakete, TesmioLauncher-Symbol für Plugins im plugins-Ordner, Zahnrad für alles andere. Ein grüner Punkt heißt: Dieses Plugin läuft beim nächsten Spielstart. Ein gelbes „Update“ heißt: Das Workshop-Paket ist neuer als das, was du zuletzt gespeichert hast. Ein bernsteinfarbener Punkt heißt: Hier hast du etwas geändert und noch nicht gespeichert.

**Oben der Kopf.** Name, Version und Beschreibung des Plugins, rechts der Schalter „Plugin aktiv“ und die Sprache (Deutsch, Englisch oder automatisch nach Windows).

**Darunter die Reiter.** Die Reiter kommen vom Plugin. Ein Plugin ohne eigene Einstellungsseite bekommt einen Reiter „Allgemein“, den RMM aus den Kommentaren der INI-Datei baut.

**Die Statuszeile** unter den Reitern sagt, wie das Plugin geladen wird (Workshop Bridge, Soviet Mod Loader oder TesmioLoader), wo seine Dateien liegen (Workshop-Paket, lokal in plugins\ oder plugins\) und wann der Loader es zuletzt geladen hat. Fahre mit der Maus über einen Punkt, dann erklärt ein Hinweis, was er bedeutet.

**Die Karten** tragen die Einstellungen. Jede Zeile hat einen Titel, einen kurzen Text, was die Einstellung im Spiel verändert, und rechts das Feld:

- Schalter für EIN und AUS.
- Zahlenfelder mit Plus und Minus. Fahre mit der Maus über das Feld, dann siehst du den erlaubten Bereich. Ein Dezimalkomma darfst du tippen, gespeichert wird mit Punkt.
- Auswahllisten, Textfelder und mehrzeilige Felder mit Zähler.
- Ein Zurücksetzen-Knopf erscheint nur, wenn dein Wert vom Standard des Plugins abweicht.

Rechtsklick auf einen Text kopiert ihn in die Zwischenablage, praktisch für Fehlerberichte.

**Die Fußzeile** zeigt den Zustand: grün „Gespeichert“, bernstein „Ungespeicherte Änderungen“, rot „Konfiguration ungültig“ mit dem Grund. Daneben die Knöpfe Zurücksetzen, Speichern und Speichern + Starten. Speichern ist nur anklickbar, wenn es etwas Gültiges zu speichern gibt.

Das Fenster merkt sich Größe, Sprache, gewähltes Plugin und Reiter. Es braucht mindestens 1560 mal 760 Punkte; bei schmalen Fenstern rutschen die Beschriftungen über die Felder.

---

## 🔛 Der Schalter „Plugin aktiv“

Der Schalter im Kopf entscheidet, ob das Plugin beim nächsten Spielstart läuft. Was er genau schreibt, hängt davon ab, wer die DLL lädt (siehe Statuszeile):

- **Workshop Bridge:** Der Schalter trägt das Paket in die Liste der Bridge ein (`user_config\workshop_bridge.ini`) oder streicht es dort. Ein Paket, das du nie eingeschaltet hast, lädt die Bridge nicht.
- **TesmioLoader:** Der Schalter ist das Kästchen des TesmioLaunchers, also der Eintrag `[plugins] <name>` in `tesmioloader.ini`. Beim Einschalten setzt RMM zusätzlich das Feld `enabled` in der INI des Plugins auf 1, falls es eines gibt, damit die DLL nicht sofort wieder ablehnt.
- **Soviet Mod Loader:** SML lädt jedes abonnierte Paket selbst. Der Schalter setzt dann nur das Feld `enabled` in der INI. Hat die INI kein solches Feld, fehlt der Schalter, und ein Hinweis sagt, dass nur das Abbestellen das Paket abschaltet.

Der Schalter zeigt „an“ nur, wenn alles zusammenpasst, genau wie der grüne Punkt in der Liste. Reine Inhaltspakete für den Soviet Mod Loader (Ressourcen, Gebäude ohne DLL) haben keinen Schalter; die zeigt RMM nur an.

---

## 💾 Speichern und Starten

**Speichern** schreibt deine Änderungen:

- Deine persönlichen Werte landen in `tesmioloader\build\user_config\<name>.ini`. Die INI des Pakets bleibt, wie sie ist. Ein Update des Pakets überschreibt deine Werte deshalb nie.
- Bei Plugins, die nur als DLL im plugins-Ordner liegen, sichert RMM beim ersten Speichern die vorgefundene INI als Original unter `user_config\.autoload\<name>.upstream.ini` und schreibt dann die fertige INI mit deinen Werten nach `plugins\`. „Original wiederherstellen“ in der Fußzeile holt die gesicherte Datei byteweise zurück.
- Vor jedem Speichern legt RMM einen Wiederherstellungspunkt an (siehe [Profile und Wiederherstellung](#-profile-und-wiederherstellung)).
- Muss eine neue DLL nach `plugins\` (nur beim Ladeweg TesmioLoader), zeigt RMM vorher Paket, Quelle, Ziel und die Prüfsumme der Datei und fragt nach.

Speichern geht nur, wenn Spiel und TesmioLauncher beendet sind und alle Werte im erlaubten Bereich liegen. Nur das Öffnen, Suchen und Wechseln von Plugins oder Reitern schreibt nie eine Datei.

**Speichern + Starten** speichert genauso und startet dann das Spiel über `tesmiolauncher.exe` ohne dessen Fenster. RMM schließt sich dabei. Willst du das Launcher-Fenster sehen, setze in `rmm.ini` unter `[settings]` den Wert `tesmiolauncher_window = 1`.

Vor dem Start prüft RMM außerdem, ob alle Ressourcen, auf die eingeschaltete Plugins verweisen, im Plugin Resources vorhanden sind. Fehlt eine, nennt die Meldung das Plugin, und der Start wartet.

---

## 💡 Die Karte „Hinweise“

Auf dem ersten Reiter jedes Plugins steht oben eine Karte „Hinweise“. Dort erscheint alles, was du wissen solltest, bevor du speicherst:

- **Blau:** Informationen, etwa ein Hinweis des Plugin-Autors, ein anstehendes Update mit alter und neuer Version, oder dass eine Abhängigkeit aktiv ist.
- **Gelb:** Warnungen, etwa eine Abhängigkeit, die zwar da, aber nicht eingeschaltet ist. Schalte sie ein, sonst lässt sich das Plugin nicht speichern.
- **Rot:** Fehler, etwa eine fehlende Abhängigkeit, eine doppelt vorhandene DLL oder ein Workshop-Ordner, der bei RMM und der Workshop Bridge nicht derselbe ist. Ein rotes Feld blockiert das Speichern.

Bei Paketen, die es erlauben, liegt hier auch der Schalter „Dateien nur lokal“.

---

## 🛤️ Wie ein Plugin ins Spiel kommt

Der TesmioLoader allein lädt nur DLLs aus `tesmioloader\build\plugins`. Für Workshop-Pakete gibt es drei Wege, und RMM erkennt von selbst, welcher bei dir gilt:

1. **Workshop Bridge** (bei RMM dabei): Das Plugin `workshop_bridge` lädt die DLLs der eingeschalteten Pakete direkt aus dem Steam-Workshop-Ordner. Steam hält sie aktuell, nichts wird kopiert. RMM stellt nur die INI bereit.
2. **Soviet Mod Loader:** Ist SML installiert und eingeschaltet, lädt er alle abonnierten Pakete. RMM schreibt dann nur INI-Dateien und nie eine DLL. Liegt von einem Paket noch eine DLL in `plugins\`, weigert sich RMM zu speichern, weil das Plugin sonst doppelt laden würde. Die Bridge hält sich unter SML zurück.
3. **TesmioLoader klassisch:** Ohne Bridge und SML kopiert RMM DLL und INI nach `plugins\` und schaltet das Plugin in `tesmioloader.ini` ein.

Die Workshop Bridge erscheint selbst als Plugin in der Liste. Ihre Karte hat den Schalter „Bridge aktiv“, den Workshop-Ordner (normalerweise „auto“ = der Steam-Workshop-Ordner deines Spiels), die Regel, welche Pakete geladen werden, und einen Knopf „Jetzt aufräumen“, der Einträge von Paketen entfernt, die du nicht mehr hast. Die Paketliste selbst pflegst du nie von Hand; das macht der Schalter „Plugin aktiv“ der Pakete.

---

## 📁 Dateien nur lokal

Manche Pakete erlauben, ihre Dateien komplett in den plugins-Ordner zu kopieren, etwa weil sie Texturen mitbringen. Dann steht in der Karte „Hinweise“ der Schalter „Dateien nur lokal“. Einschalten und Speichern zeigt zuerst jede Datei, die kopiert wird, samt Prüfsumme der DLL; nach „OK“ kopiert RMM DLL, INI und den Zubehörordner nach `plugins\`. Die Bridge überspringt das Paket danach von selbst, und der Schalter „Plugin aktiv“ arbeitet über `tesmioloader.ini`.

Ausschalten und Speichern entfernt genau diese Dateien wieder, nach einer Rückfrage mit Liste. Fremde Dateien in `plugins\` fasst RMM nie an. Steam-Updates wirken bei lokalen Dateien erst, wenn du erneut speicherst; das gelbe „Update“ in der Liste erinnert dich daran.

---

## 📜 Das Protokollfenster

Das Protokoll-Symbol in der Seitenleiste öffnet ein Fenster mit drei Arten von Quellen: dem Journal dieser RMM-Sitzung, `tesmioloader.log` aus dem Loader-Ordner und jedem Plugin-Protokoll `tesmioloader.<plugin>.log`, egal ob es im Unterordner `logs\` oder direkt im Loader-Ordner liegt. Die Dateien lassen sich auch lesen, während das Spiel läuft; „Aktualisieren“ liest neu, „Ordner öffnen“ zeigt den Loader-Ordner im Explorer.

„Suchen“ filtert nach Text, „Absender“ nach dem ersten Wort einer Zeile (`plugin`, `bridge`, `hook`, ein Plugin-Name), „Nur Probleme und Warnungen“ blendet alles Unauffällige aus. Fehlerzeilen stehen rot, Warnungen gelb. Die Fußzeile nennt Zeilen, Treffer und die letzte Abschlusszeile des Loaders.

---

## 🗂️ Profile und Wiederherstellung

Das Archiv-Symbol in der Seitenleiste öffnet das Fenster „Profile und Wiederherstellung“ mit zwei Reitern.

**Profile** sichern den kompletten Einstellungsstand unter einem Namen: `tesmioloader.ini`, alle INIs in `plugins\` und `user_config\` und die gesicherten Originale. DLLs gehören nie dazu. „Aktuellen Stand sichern…“ fragt Name und Notiz ab. Rechts siehst du je Datei, ob sie zum gespeicherten Stand passt. „Profil anwenden“ schreibt alles zurück; INIs von Plugins, die das Profil nicht kennt, bleiben stehen. Profile liegen unter `user_config\.autoload\profiles\`.

**Wiederherstellungspunkte** entstehen bei jedem Speichern: der Stand aller berührten Dateien von vor dem Speichern, je Plugin genau einer. „Auf diesen Punkt zurücksetzen“ schreibt diese Dateien zurück, auch eine damals kopierte DLL; der jetzige Stand wird dabei zum neuen Punkt, du kannst also wieder zurück. Punkte, deren Dateien fehlen, werden nur angezeigt.

Beide Aktionen brauchen ein geschlossenes Spiel und einen geschlossenen TesmioLauncher.

---

## 📝 Listen-Editoren

Einige Plugins verwalten Listen statt einzelner Werte. RMM zeigt sie als Liste links und Felder rechts. Das gilt für die drei Plugins, die mit dem TesmioLoader kommen, und für Workshop-Pakete, die so eine Seite mitbringen (etwa Deposits Plus, Research Expansion, Technical Service Storage, Vanilla Buildings, UI Layout Fixes).

**Gemeinsam für alle:**

- Einträge aus dem Plugin oder Paket tragen ein Schloss. Du kannst sie ändern, aber nicht löschen; über den roten Knopf lassen sie sich ausblenden und unter „Ausgeblendete Originaleinträge“ wieder anzeigen.
- Eigene Einträge legst du mit „+“ an. Der Dialog fragt nur das Nötige ab und schlägt vor, was er kann; alles Weitere stellst du danach rechts ein. Eigene Einträge lassen sich löschen.
- Kennungen, die etwas im Spiel benennen müssen (eine Ressource, eine Forschung, eine Gebäudedatei, eine Text-ID), prüft RMM vor dem Speichern. Stimmt eine nicht, wird die Fußzeile rot und nennt Eintrag und Feld.
- Eine gelbe Warnung erinnert dich, wenn eine Änderung den Spielstand betrifft, etwa neue Ressourcen oder Vorkommen, die ein laufendes Spiel dann braucht.

**Resources:** Liste aller Ressourcen des Plugins. Vorbildressource, Transportklasse und Materialfamilie sind Auswahllisten mit den Namen aus dem Spiel. Es gibt keinen Aus-Schalter, weil das Plugin keinen sicheren kennt.

**Needs:** Liste der Bedürfnisse mit Spender, Faktor, Ladenkategorie, Wahrscheinlichkeit und Unzufriedenheit. Höchstens acht Einträge. Der Reiter „Allgemein“ trägt die Schalter des Plugins.

**Deposits:** Reiter „Allgemein“ mit den Plugin-Schaltern und Reiter „Vorkommen“ mit einem Eintrag je Vorkommen: Kennung, Typnummer, Karte, Platz auf der Karte, Symbol und mehr. Die Typnummer schlägt der Dialog als nächste freie vor; eine doppelte wird abgewiesen.

**Paket-Editoren** bringen ihre eigenen Reiter, Listen und Hilfen mit. Manche haben Knöpfe, die Anleitungen des Pakets öffnen, Auswahlfenster für Gebäude, Forschungen oder Spieltexte, Bildvorschauen oder einen Reiter für Übersetzungen.

---

## 🎮 Spielversion und rmm.ini

Beim Start liest RMM die Kennung von `SOVIET64.exe`. Gehört sie zu keiner Spielversion, für die die Plugins gemacht sind, erscheint eine Warnung, und „Speichern + Starten“ fragt vor dem Start nach. Bekannt ist 1.1.1.9. Eine neue Kennung trägst du in `settings_schemas\game_versions.ini` unter `[supported]` ein.

`rmm.ini` neben `rmm.exe` hat wenige Schalter, alle mit Erklärung in der Datei:

| Schlüssel | Bedeutung |
|---|---|
| `[paths] workshop_root` | Ordner mit den Paketen. Leer = Steam-Workshop-Ordner deines Spiels. |
| `[settings] language` | `auto`, `de` oder `en`. |
| `[settings] version_check` | 0 schaltet die Warnung zur Spielversion ab. |
| `[settings] tesmiolauncher_window` | 1 zeigt das Fenster des TesmioLaunchers bei „Speichern + Starten“. |

Was du im Fenster einstellst, hat Vorrang vor der Datei.

---

## 📦 Wo deine Dateien liegen

```
SovietRepublic\tesmioloader\build\
├── rmm.exe, rmm.ini                    RMM und seine Grundeinstellungen
├── settings_schemas\                   Einstellungsseiten für die Loader-Plugins
├── plugins\                            DLLs und wirksame INIs der Plugins
│   └── workshop_bridge.dll, .ini       die Workshop Bridge
├── user_config\
│   ├── <name>.ini                      deine persönlichen Werte je Plugin
│   ├── workshop_bridge.ini             Paketliste der Bridge
│   └── .autoload\
│       ├── <name>.upstream.ini         gesicherte Originale
│       ├── backups\<paket>\previous\   Wiederherstellungspunkte
│       └── profiles\                   deine Profile
└── logs\                               Protokolle der Plugins
```

Fenstergröße, Sprache und Auswahl merkt sich RMM unter `%LOCALAPPDATA%\RepublicModManager`.

---

## 🛠️ Wenn etwas nicht klappt

| Was du siehst | Was dahintersteckt | Was hilft |
|---|---|---|
| Rotes Feld „Abhängigkeit fehlt“ | Ein Paket, das dieses Plugin braucht, ist nicht abonniert oder nicht eingeschaltet | Paket abonnieren und mit „Plugin aktiv“ einschalten |
| Rotes Feld „Doppelte DLL“ | Die DLL liegt in `plugins\` und wird zusätzlich von SML oder der Bridge geladen | Die Kopie in `plugins\` entfernen oder „Dateien nur lokal“ nutzen |
| Rotes Feld zum Workshop-Ordner | RMM und Bridge lesen verschiedene Ordner | In der Karte der Workshop Bridge den Ordner auf „auto“ stellen |
| „Beim letzten Spielstart nicht geladen“ | Das Plugin war beim letzten Start aus, oder das Spiel lief seit dem Einschalten nicht | Speichern und das Spiel einmal starten |
| Speichern geht nicht | Spiel oder TesmioLauncher läuft, oder ein Wert liegt außerhalb des Bereichs | Programme beenden, roten Hinweis in der Fußzeile lesen |
| Ein Wert steht nach dem Speichern wieder anders | Das Plugin las eine andere INI als erwartet | Statuszeile prüfen: Wo liegen die Dateien? Protokollfenster öffnen |

Für einen Fehlerbericht: Rechtsklick auf die Meldung, „Text kopieren“, dazu das Protokollfenster mit „Nur Probleme und Warnungen“.

---

## 🧩 Für Plugin-Autoren

Wie ein Workshop-Paket aufgebaut sein muss, damit RMM es zeigt, und wie eine Einstellungsseite mit Reitern, Karten, Feldern, Listen und Übersetzungen beschrieben wird, steht in `SCHEMA_DE.md` (englisch: `SCHEMA_EN.md`).

---

**Genosse, Achtung:** Dieses Programm wurde mit Hilfe einer künstlichen Intelligenz geschrieben. Die Fünfjahrespläne dazu hat trotzdem ein Mensch aufgestellt, getestet und beim Abstürzen des Spiels geflucht. Wer keine KI im Code möchte, bleibt einfach beim Grundspiel. Kein Hass, keine Umerziehung.

**GNU GPL v3.** Quelltext: https://github.com/Kespri/workers-and-resources-mods/tree/main/tools/republic_mod_manager
