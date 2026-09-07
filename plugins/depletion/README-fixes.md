# depletion 1.1.2 — optionale Übersetzung und Lade-Korrekturen

## Neu in 1.1.2: optionale Localization

Die Zeile wird weiterhin in `DrawReserveRow()` erzeugt. Ihre Beschriftung wird jetzt über `PanelCaption()` und den Schlüssel `tesmio_lang.depletion.deposit_remaining` aus dem gemeinsamen Paket `plugins/localization/tesmio_lang` gelesen. Deutsch: `Restvorkommen`, Englisch: `Deposit remaining`.

Ohne localization oder bei fehlendem Schlüssel bleibt die bestehende `panel_caption`-Ausgabe erhalten. Fehler im optionalen Textabruf deaktivieren weder Panel noch Abbau. Zahlen, t/kt/Mt, Prozentrechnung und Zeilenposition bleiben unverändert. Weitere Sprachdateien können im gemeinsamen Paket ergänzt werden; dessen README beschreibt das Schlüssel-Muster für zukünftige Plugins.

Die folgenden Lade-Korrekturen aus 1.1.1 sind unverändert enthalten.

Für WRSR 1.1.1.9. Keine neuen Einstellungen und keine Änderung des Speicherformats.

## Behobene Fehler

- Der bisherige Minen-Cache blieb beim Laden aus dem Hauptmenü erhalten. Alte Gebäudeadressen konnten dadurch vor der Neuinitialisierung noch in die Anzeige, Fortschrittsmeldungen oder Kartenbearbeitung gelangen. Beim Beginn von `C3D_TERRAIN::InitializeFromFolder` wird dieser Cache jetzt vollständig verworfen, einschließlich ausstehender Abbuchungen.
- Die Erfassung wird erst beim ersten Render-Aufruf des neu initialisierten aktiven Terrains freigegeben. Ein zuvor begonnener Verarbeitungsschritt darf nach einem Weltwechsel nicht mehr auf dessen Zustand zugreifen.
- Minen-Cache und Weltwechsel sind gegen gleichzeitige Tick-/Render-Zugriffe geschützt. Native Tick-, Panel-, Terrain-Init- und Render-Aufrufe erfolgen außerhalb dieser Sperre.
- Beim Schreiben und Anzeigen wird geprüft, ob eine gespeicherte Adresse noch zur erwarteten Minenart gehört. Freigegebene oder anderweitig wiederverwendete Gebäudeadressen werden nicht weiterverarbeitet.
- Bereits geöffnete Texturen werden auch bei einer Ausnahme in der Verarbeitung wieder geschlossen.
- Falls nach dem ersten installierten Hook ein weiterer Hook fehlschlägt, wird depletion deaktiviert, die DLL aber nicht entladen. Bereits eingehängte Aufrufe bleiben dadurch gültig.

## Bewusst unverändert

- Produktionsabhängiger Verbrauch, Förderqualität, Reichweite, Tonnen pro Texel und Schreiben der Ressourcenkarte.
- Die zusätzliche Ressourcenverteilung durch deposits; insbesondere die noch offene Erstausstattung alter Spielstände.
- Die INI-Dateien und vorhandenen Spielstände.
- Der zweite Wert in `Rest / Referenz`: Er wird nach dem Laden weiterhin aus dem aktuellen Kartenbestand neu gebildet. Eine dauerhafte historische Anfangsmenge ist eine separate, noch nicht implementierte Speicherfunktion.
- Das Speicherraster: Mit `tonnes_per_texel = 1200` entspricht ein Byte-Werteschritt ca. 4,71 t. Bruchteile eines Schrittes liegen weiterhin nur im Arbeitsspeicher.

## Prüfung

Offline-Tests führen den tatsächlichen Plugin-Quelltext mit nachgebildeten Engine-Objekten aus: Menü-Laden mit gleichen Adressen, unterschiedliche Welten, bereits freigegebene Gebäude, laufender alter Tick während des Ladens, normaler Verbrauch, Produktionsstopp, Erschöpfung, erneutes Laden, Betrieb ohne deposits, Schreibintervall und Hook-/Textur-Fehlerpfade.

Diese Tests ersetzen nicht den Spieltest. Nächste Kontrolle: Sand abbauen, speichern, aus dem Hauptmenü laden und weiter abbauen; dabei muss `deplete world load` vor einer neuen Minenerfassung erscheinen. Der bisher beobachtete alte Mineneintrag mit `quality 10.500` darf nicht übernommen werden.
