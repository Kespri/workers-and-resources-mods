# deposits_plus 1.2-beta: Arbeitsfahrzeuge fuer eigene Lagerstaetten

Zielversion: WRSR / SOVIET64.exe 1.1.1.9, TesmioLoader.

## Konfiguration

Im bestehenden Lagerstaettenabschnitt in `deposits_plus.ini` ergaenzen:

```ini
[sand]
token = $TYPE_MINE_SAND
working_vehicle_skill = gravelmining
; alle bisherigen type/map/component/radius-Einstellungen beibehalten
```

Dies ist ein Ausschnitt, kein vollstaendiger Ersatz fuer den Abschnitt.
Die mitgelieferte INI aktiviert die Zuordnung bereits fuer Sand. Andere
Lagerstaetten bleiben unveraendert. Unterstuetzt werden vorerst `none`
(auch das Verhalten ohne Eintrag) und `gravelmining`, jeweils unabhaengig
von Gross-/Kleinschreibung. Unbekannte Werte melden eine genaue Warnung
und deaktivieren nur die Fahrzeugerweiterung dieses Abschnitts.

Das Gebaeude bleibt `$TYPE_MINE_SAND` (building_type 7). Es braucht weiterhin
`$WORKING_VEHICLES_NEEDED`, geeignete `$VEHICLE_PARKING`-Plaetze und die
normalen Betriebsvoraussetzungen. Fahrzeuge benutzen ihre vorhandene
Faehigkeit `$SKILL_GRAVELMINING`; es gibt in dieser Version keinen neuen
Fahrzeugbefehl `$SKILL_SANDMINING`.

Die Lagerstaettenkarte und die mit `$PRODUCTION` konfigurierte Ressource
werden nicht veraendert. Insbesondere stellt diese Erweiterung Sand NICHT
von `map=terrain` auf eine andere Karte um. Treibstoff-, Zustands-,
Kapazitaets- und Produktionsregeln des Spiels bleiben erhalten.

## Technik und Kompatibilitaet

Vier native Typpruefungen werden um die explizit konfigurierten Minentypen
erweitert: Fahrzeugzulassung fuer Kauf/Zuweisung, zwei Arbeitsleistungs-
Berechnungswege und der Ankunfts-/Arbeitszustand von Grubenfahrzeugen.
Dabei werden keine Gebaeude- oder Fahrzeugtypen im Speicher umgeschrieben.
Die Originalzweige fuer Kies und Bauxit bleiben zuerst erhalten. Eine
zusaetzliche Pruefung beschraenkt neue Zuordnungen auf Gebaeudetyp 7.

Installation nur beim Pluginstart. Alle vier Eingriffsstellen samt
umgebenden Identifikationsbytes werden vorab gegen 1.1.1.9 geprueft.
Bei abweichenden Bytes oder einem Vorbereitungsfehler bleiben alle vier
Fahrzeugpruefungen unveraendert. Ohne aktive Zuordnung wird kein neuer
Fahrzeugpatch installiert. `code_patch=0` schaltet auch diese Erweiterung aus.
Andere Plugins, die dieselben Stellen bereits umschreiben, fuehren zu
einer Warnung und zur Ablehnung dieser Erweiterung.

Kein neues Speicherformat, keine periodischen Fahrzeugscans und keine
Aenderung des Spiel-EXE auf der Festplatte. Bestehende Sandminen koennen
die Zuordnung nutzen, sofern sie bereits geeignete Arbeitsfahrzeugplaetze
besitzen; fehlende Gebaeudeplaetze werden nicht automatisch nachgeruestet.

Logpraefix: `vehicles`, im normalen TesmioLoader-Log. Erfolgreicher Start:
`4 native gates installed` sowie eine Zuordnungszeile pro freigegebener Mine.

## Verifikation und noch notwendiger Spieltest

Offline verifiziert gegen die lokal installierte EXE mit SHA256:
`296644A9F207D609031FC2AE73FED2DCB34619A1D55A35D1C7B51965CE6841B8`.

- DLL mit denselben MSVC-Schaltern wie das zentrale `build.bat` gebaut.
- 24.960 echte x64-Testaufrufe der Original-/Erweiterungszweige:
  Register und definierte Rechenflags, Vanilla-Verhalten, unkonfigurierte
  Typen, Gebaeudetypabgrenzung, Grenzwerte und bis zu 32 Lagerstaetten.
- INI-Parser, Standardwerte, ungueltige Werte, bestehende Zusatzschluessel.
- Vier Stellen und Vorbedingungen direkt gegen EXE-Bytes geprueft.
- Abweichende Bytes einzeln sowie Speicher-/Schutz-/Cachefehler vor der
  Aktivierung getestet; keine teilweise installierten Fahrzeugzweige.
- Erfolgreiche Installation veraendert nur die vier vorgesehenen Bereiche
  einer privaten EXE-Kopie im Speicher. Kein Spielprozess wurde verwendet.

Diese Tests ersetzen keinen Spieltest. Bitte in einem Testspiel pruefen:

1. Sandmine mit Arbeitsfahrzeugplaetzen ueber einem Sandvorkommen oeffnen;
   vorhandenen Bagger kaufen beziehungsweise von einem Depot zuweisen.
2. Ohne Arbeiter pruefen, dass der arbeitende, versorgte Bagger Sand foerdert.
3. Fehlenden Treibstoff, volle Lager und Wiederaufnahme nach Versorgung
   pruefen; die originalen Betriebsregeln sollen weiter greifen.
4. Speichern/laden, Fahrzeugwechsel und eine normale Kies-/Bauxitmine
   gegenpruefen. Keine unkonfigurierte Mine darf Bagger neu akzeptieren.

Die Testscripts und Ergebnisprotokolle liegen im zugehoerigen Arbeitsordner
`_deposits_vehicle_work`. Quelltext und INI lassen sich weiterhin mit dem
zentralen `TesmioLoader-master/build.bat` bauen; dafuer ist kein Zusatzheader
dieser Erweiterung notwendig.
