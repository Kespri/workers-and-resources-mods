# deposits_plus 1.6-beta — größere Felder, faire Verteilung innerhalb der Landesgrenze

Für WRSR 1.1.1.9 und TesmioLoader. Die Fahrzeugfreigabe aus 1.2-beta bleibt erhalten.

## Verhalten

- Jede geladene Spielwelt: alle Ressourcen der aktuellen INI werden anhand ihrer Tokens mit der Historie dieses Spielstands verglichen. Unbekannte leere sowie bisher nur übersprungene leere Ressourcen werden einmalig verteilt. Vorhandene oder bereits initialisierte Vorkommen bleiben erhalten, auch nach vollständigem Abbau. Keine fest eingebauten Ressourcenlisten, Spielstandnamen oder Weltkennungen.
- Natürliche, längliche und geschwungene Gebiete mit wechselnder Breite, gelegentlichen Verzweigungen, unregelmäßigen Rändern, echten Lücken und auslaufender Ergiebigkeit. Ein Gebiet kann mehrere getrennte Teilflächen enthalten, zählt für die Häufigkeit aber nur einmal. Einzelne isolierte Rasterpunkte werden entfernt.
- Kein Platzieren im Wasser. Geprüft werden die Gelände-Höhen innerhalb jeder Ressourcen-Zelle gegen Wasserstand, Wellenamplitude und Sicherheitsabstand. Dazu kommt der Uferabstand.
- Nur innerhalb der Landesgrenze: Das Plugin liest einen geprüften Schnappschuss des geladenen nativen `BORDER_POLYGON`. Ohne Polygon gelten die nativen rechteckigen Baugrenzen. Grenzzellen werden konservativ ausgeschlossen; nicht nur der Mittelpunkt muss im Land liegen. Die Grenzstruktur wird gegen die installierte Engine geprüft. Eine unbekannte/ungültige Struktur unterdrückt neue Platzierungen, statt außerhalb des Landes zu verteilen.
- Kein Überschneiden mit anderen neu erzeugten Gebieten, vorhandenen Plugin-Vorkommen oder Vanilla-Öl/Eisen/Kohle/Uran/Bauxit/Kies. Bestehende Überschneidungen werden nicht geändert.
- Alle neuen Ressourcen kommen reihum dran: höchstens ein Feld und 32 Versuche pro Zug, dann die nächste Ressource. Die vom gespeicherten Zufallswert abhängige Reihenfolge rotiert; weder INI-Reihenfolge noch alphabetischer Name geben Vorrang. Insgesamt bleiben höchstens 256 Versuche je angefordertem Feld erlaubt.
- Wasser-, Grenz- und Belegungsstücke werden ausgespart. Ein Feld zählt nur, wenn nach Beschneiden und Entfernen winziger Reststücke mindestens 60 % seiner ursprünglichen Fläche übrig bleiben. Sonst wird ein anderer Platz versucht; Größe 3 wird nicht als winziger Restfleck gezählt.
- Ist nicht genügend geeignete Fläche verfügbar, entstehen weniger oder keine Gebiete. Das Log nennt Ziel/Ergebnis, beschnittene Felder, Versuche und getrennte Ablehnungsgründe: Landesgrenze, Wasser/Ufer, Vorkommen/Abstand, Kartenrand und zu wenig verbleibende Feldfläche. Die Ursachen können sich überschneiden. Sind überhaupt keine freien Startzellen vorhanden, entfällt die erfolglose Suche.
- Erstgenerierung läuft einmal beim ersten Terrain-Zeichnen nach dem Laden. Danach kein fortlaufender Generator oder ständiges Nachfüllen. Eine kurze Verzögerung beim ersten Anzeigen der Welt ist möglich.

## Bestehenden Spielstand sicher übernehmen

1. Sicherheitskopie des gesamten Spielstands behalten.
2. Bei der ersten Verwendung dieser Version die bisherigen Ressourcen **nicht gleichzeitig entfernen oder umsortieren**: Alte Spielstände besitzen noch keine Zuordnungsliste. Die bisherige INI-Reihenfolge ist für deren erste Übernahme notwendig.
3. Spielstand laden und unter neuem Namen speichern. Dabei entsteht `tesmio_deposits.bin` im Spielstandsordner.
4. Ab dann sind Ressourcen-Zuordnung und Bearbeitungsstand bekannt. Auch Umsortieren der INI verschiebt keine Vorkommen mehr zwischen Ressourcen; die gespeicherten Kanäle werden den konfigurierten Laufzeitkanälen zugeordnet.

Standard `generate_existing_empty = 1`: Auch beim nachträglichen Umstieg auf deposits_plus erhalten bisher nicht initialisierte leere Plugin-Ressourcen ihre Erstverteilung. Ein vorhandenes Vorkommen wird vollständig übernommen, nicht durch zusätzliche Zufallsgebiete ergänzt. Für alle Karten und INI-Ressourcen gilt dieselbe Schleife.

Die Historie unterscheidet **übernommen**, **generiert**, **noch nicht initialisiert** und **Generierung versucht, aber kein/zu wenig Platz**. Ein leeres Vorkommen mit übernommener oder generierter Historie bleibt erschöpft. Die bloße Existenz eines INI-Eintrags oder eines Metadaten-Eintrags bedeutet dagegen nicht, dass seine Erstverteilung bereits durchgeführt wurde.

Metadaten aus 1.3 bleiben lesbar: `status=2` (damals wegen deaktivierter Generierung oder Altspielstand-Schutz übersprungen) wird bei weiterhin leerem Kanal als ausstehende Erstverteilung behandelt. Dafür sind keine Weltkennung und keine einzelnen Ressourcennamen erforderlich. Sobald Daten in einem solchen Kanal übernommen oder gespeichert werden, wird er als initialisiert markiert; späterer Abbau setzt diese Markierung nicht zurück.

Grenze bei alten Daten: Ohne Metadaten kann das Plugin nicht beweisen, dass ein leerer Kanal vor seiner Verwendung niemals abgebaut wurde. Auch 1.3 hat bei einem übersprungenen Kanal später manuell eingetragenen und wieder abgebauten Vorrat nicht lückenlos protokolliert. Bei solcher unbekannten Vorgeschichte `generate_existing_empty = 0` verwenden: Dann bleiben alte/unvollständig initialisierte leere Einträge geschützt. Diese Option ist **kein Regenerierungsbefehl**; bereits übernommen/generiert/versucht wird auch mit `1` niemals erneut aufgefüllt.

Welten aus `media_soviet/save/...` und `media_soviet/saved_last` gelten ohne Metadaten als Alt-Spielstand, Terrain-Vorlagen als neue Welt. Kein Verändern der originalen Karten-Vorlage beim Laden. Nach der Erstverteilung unter neuem Namen speichern; ohne Speichern besteht beim nächsten Laden weiterhin der alte Bearbeitungsstand.

## Später eine Ressource ergänzen

Den übernommenen Spielstand zuerst speichern. Dann eine neue Ressourcen-Sektion mit eindeutigem, noch nicht verwendetem `token` und `type` in `plugins/deposits_plus.ini` ergänzen, vorzugsweise mit `map = auto`. Das Spiel komplett neu starten und den Spielstand laden. Nur die neue Ressource wird verteilt, bestehende bleiben unverändert. Anschliessend speichern.

Die INI wird beim Plugin-Start gelesen: Hauptmenü-Laden alleine liest eine während des laufenden Spiels veränderte INI nicht neu ein.

Identität ist das Mine-Token; `type` muss erhalten bleiben. Anzeigename/Sektionsreihenfolge sind nicht die Identität. Entfernte Ressourcen behalten eine Historie samt Restvorkommen; erneutes Einfügen desselben bereits initialisierten Tokens erzeugt sie nicht neu. Eine vor ihrer ersten Verteilung entfernte Ressource kann diese nach dem Wiedereinfügen erstmals erhalten. Ein alter Typ darf nicht einer anderen gespeicherten Identität zugeteilt werden.

## Einstellungen

Im Abschnitt `[deposits_plus]`:

```ini
generation = 1
generate_existing_empty = 1
generation_seed = 0
generation_gap_m = 40
generation_shore_m = 40
generation_water_clearance_m = 2
```

`generation_seed = 0` würfelt beim erstmaligen Anlegen der Metadaten. Eine feste positive Zahl liefert bei identischer Karte, Konfiguration und Belegung dieselbe Verteilung. Der Seed gehört danach zum Spielstand; INI-Änderungen würfeln gespeicherte Gebiete nicht neu.

Einfache Einstellungen **in jedem Ressourcenabschnitt**, beispielsweise zusätzlich zu den bestehenden Token-/Typ-/Map-Angaben unter `[sand]`:

```ini
generation = 1
generation_frequency = 3
generation_size = 2
```

`generation = 0` verhindert die automatische Erstverteilung dieser Ressource; vorhandene Vorkommen bleiben bestehen. Der gleichnamige Schalter unter `[deposits_plus]` schaltet die automatische Verteilung insgesamt ein/aus.

| `generation_frequency` | Häufigkeit | Gewünschte Vorkommensgebiete |
|---|---|---:|
| 1 | extrem selten | 1 |
| 2 | selten | 2 |
| 3 | normal | 3 |
| 4 | häufig | 4 |
| 5 | sehr häufig | 5 |
| 6 | extrem häufig | 6 |

| `generation_size` | Größe | Grundmaß/Referenzradius |
|---|---|---|
| 1 | klein | 150–350 m |
| 2 | mittel | 350–550 m |
| 3 | groß | 550–750 m |

Das Grundmaß wird pro Gebiet zufällig gewählt und skaliert dessen gesamten Verlauf, nicht jeden Teilfleck separat. Es beschreibt **keinen Kreis und keine garantierte Außenkante**: Der längliche Verlauf reicht entlang seiner Hauptachse ungefähr über das 3,4- bis 4,6-Fache des Grundmaßes; Ausrichtung, Breite, Verzweigungen, Löcher und Enden variieren. Große Felder benötigen entsprechend mehr freie Fläche und enthalten bei gleicher Ergiebigkeit meist mehr Gesamtvorrat.

Die tatsächliche Anzahl kann wegen Landesgrenze, Wasser, Uferabstand, anderen Vorkommen oder Kartenrand geringer ausfallen. Es bleibt bei der begrenzten Suche und einer genauen Logmeldung; keine erzwungene Überschneidung und keine Verteilung im Wasser. Die Zählung betrifft ganze Gebiete, nicht die Zahl ihrer voneinander getrennten Teilflächen.

Die bisherigen Detailoptionen bleiben gültig. Ohne explizite Werte gelten ab 1.6 die neuen Standardwerte (Häufigkeit 3, Größe 2):

```ini
generation = 1
generation_count = 3
generation_radius_min_m = 350
generation_radius_max_m = 550
generation_richness_min = 0.45
generation_richness_max = 1.00
```

**Vorrang unabhängig von der Zeilenreihenfolge:** Ein gesetztes `generation_frequency` überschreibt `generation_count`; `generation_size` überschreibt beide `generation_radius_*`-Werte. Mischungen werden im Log mit Ressource und überschriebenen Schlüsseln gemeldet. Am einfachsten entweder die Stufen oder die entsprechenden Detailwerte verwenden. Stufen müssen ganze Zahlen in ihren jeweiligen Bereichen sein; ungültige Werte deaktivieren nur die Generierung der betroffenen Ressource mit Warnung, nicht ihren Minentyp oder vorhandene Lagerstätten. Ein anderer gültiger Eintrag später in derselben Sektion hebt einen vorherigen Parsefehler nicht auf.

Grundmaße und Abstände sind Welt-Meter, Ergiebigkeit ist 0..1 und unabhängig von beiden Stufen. `generation_count = 0` unterdrückt die Erstverteilung nur dann, wenn keine Häufigkeitsstufe es überschreibt; für eindeutiges Abschalten `generation = 0` verwenden. Leere, noch nie initialisierte Ressourcen bleiben bei deaktivierter Generierung ausstehend und können nach Freigabe beim nächsten Laden verteilt werden. Landesgrenze und Wasser werden berücksichtigt; Gelände-Neigung, bestehende Gebäude und Straßen sind keine zusätzlichen Filter. Eine Platzierung im Land garantiert deshalb nicht die sofortige Eignung für jedes konkrete Minengebäude.

**Bestandsschutz:** Die natürliche Feldform und alle neuen Einstellungen gelten nur für noch ausstehende Erstverteilungen. Bereits übernommene, generierte oder abgebaute Vorkommen werden weder umgezeichnet noch vergrößert oder aufgefüllt. Das Speicherformat und die Zusammenarbeit mit `depletion` bleiben unverändert. Zum Vergleichen neuer Formen eine neue Karte oder eine noch nicht initialisierte Ressource verwenden, nicht gespeicherte Metadaten löschen.

## Sand

Die mitgelieferte INI behält `map = terrain` / `component = 1` als Herkunftsangabe und ergänzt `independent_map = 1`. Das Plugin weist Sand intern einen freien separaten Kanal **nach** den bisherigen Auto-Kanälen zu. Bei der ersten Übernahme eines alten Spielstands ohne Metadaten wird seine Terrain-Ergiebigkeit übernommen, sofern noch kein eigenes Sandvorkommen vorhanden ist (flächen-gemittelt von der Terrain- auf die Ressourcen-Auflösung). Ist auch diese leer, gilt die allgemeine Erstverteilung. Neue Welten bekommen unabhängig davon zufälligen Sand. Wird Sand erst später einer bereits übernommenen Welt hinzugefügt, wird er wie jede neue Ressource verteilt; die dekorative Terrain-Maske wird dann nicht als historische Lagerstätte übernommen.

Die sichtbare Terrain-Maske wird weder umgefärbt noch beim Sandabbau reduziert. Der Sand-Pinsel liegt jetzt im Ressourcen-Reiter des Editors. `independent_map = 1` danach beibehalten. Ein direktes Zurückschalten auf eine Terrain-Maske ist keine unterstützte Rückmigration.

Andere ausdrücklich geteilte Vanilla-/Terrain-Kanäle werden nicht automatisch bemalt. Dafür gibt es eine genaue Warnung. Für neue unabhängige Ressourcen `map = auto` verwenden.

## Speicher- und Fehlerverhalten

Native DDS-Karten bleiben massgeblich für den aktuellen Abbaustand. `tesmio_deposits.bin` ergänzt versionierte, prüfsummengesicherte Identitäten, Kanal-Zuordnung, Seed und Historie entfernter Ressourcen. Es wird erst nach dem nativen Kartenspeichern aus dessen DDS-Dateien erzeugt und über eine temporäre Datei atomar ersetzt. Beim Laden werden beide benötigt. Nicht löschen oder von einem anderen Spielstand dazukopieren.

Unlesbare/ungültige Metadaten, fehlende ursprüngliche Kartendateien, widersprüchliche Typen, unbekannte DDS-Formate oder fehlende Engine-Funktionen führen zu Warnungen und unterdrücken die neue Generierung bzw. das Überschreiben der Metadaten. Native Karten und Metadaten werden miteinander verglichen: Bei einer unvollständigen Speicherung oder einer externen Kartenänderung wird weder eine alte Momentaufnahme zurückgeschrieben noch eine möglicherweise falsche Kanal-Reihenfolge geraten. Es wird niemals eine kaputte Datei als neue leere Welt interpretiert. Die normale Spielmechanik wird nicht absichtlich angehalten; bei einem solchen Fehler nicht mit umsortierter INI weiterspielen, sondern Log prüfen und Sicherung verwenden.

Für Sicherungen, Kopieren und Save-As immer den gesamten Spielstandsordner mitnehmen. Ohne diese DLL bleiben die zusätzlichen Rohstoff-Minentypen wie bisher nicht verfügbar. Ein vollständiges Rollback auf 1.2-beta benötigt den alten Spielstand **und** die alte INI; blosses Austauschen der DLL migriert die neuen Zuordnungen/Sanddaten nicht zurück.

## Verifikation und erster Spieltest

Offline geprüft: alle 18 Stufenkombinationen, faire reihum erfolgende Verteilung, INI-Reihenfolge-Unabhängigkeit, Clipping/Mindestfläche, konkave Landesgrenzen und Grenzzellen, Layout-Signatur der installierten Engine, Wasser-/Belegungsmasken, keine Überschneidung, begrenzte Suche, Metadaten-Prüfsummen, Neu-Hinzufügen, Umsortieren, Erschöpfung, Entfernen/Wiederaufnehmen, Sandübernahme und unveränderte native Fahrzeug-Gates. Ein zusätzlicher Test verwendet die tatsächlichen Grenz-, Höhen- und Vanilla-Belegungsdaten der Küstenkarten-Vorlage, einschließlich mehrerer Zufallswerte. Die Engine-Anbindung muss trotzdem im Spiel getestet werden; die Offline-Tests ersetzen diesen Test nicht.

Empfehlung: neue Testkarte öffnen, Ressourcen-Minikarte einschalten, danach speichern und sowohl aus dem Menü als auch nach Neustart laden. Anschliessend in einer Spielstandkopie genau eine neue Ressource ergänzen. Logzeilen beginnen mit `generation` in `tesmioloader.log`.

## Quelldateien / Bauen

`deposits_plus.cpp`, `deposit_generation.h`, `deposit_country.h` und `deposit_generation_runtime.inl` gehören zusammen in den Plugin-Quellordner. Die zentrale `build.bat` kompiliert weiterhin `deposits_plus.cpp`; zusätzliche Build-Schritte sind nicht nötig. Zur Laufzeit benötigt das Spiel nur `deposits_plus.dll` und `deposits_plus.ini` (sowie die bisherigen Ressourcen-/Icon-Dateien).
