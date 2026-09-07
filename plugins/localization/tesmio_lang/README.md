# Gemeinsame Übersetzungen für Tesmio-Plugins

Namespace: `tesmio_lang`. Weitere Plugins können eigene Schlüssel in diesem Paket verwenden, ohne einen zusätzlichen Paketordner zu benötigen.

## depletion

In `sovietGerman.ini`:

```ini
[strings]
depletion.deposit_remaining = Restvorkommen
```

In `sovietEnglish.ini`:

```ini
[strings]
depletion.deposit_remaining = Deposit remaining
```

Vollständiger Schlüssel: `tesmio_lang.depletion.deposit_remaining`.

Die Caption wird ohne Doppelpunkt oder Zahlen geschrieben. depletion ergänzt unverändert Restbestand, Referenzbestand, t/kt/Mt und Prozent, beispielsweise `Restvorkommen: 11.0 kt / 11.5 kt (95.7 %)`. Die bestehende Zahlenformatierung bleibt erhalten.

## Weitere Übersetzungen

- Neue Schlüssel nach dem Muster `<plugin>.<textname>` unter dem vorhandenen `[strings]` ergänzen; keinen zweiten `[strings]`-Abschnitt anlegen.
- Den Schlüssel mindestens in `sovietEnglish.ini` definieren, da Englisch die Fallback-Sprache ist.
- Andere Sprachen über die passenden `soviet<Sprache>.ini`-Dateien mit eigenem `[strings]` hinzufügen. UTF-8 unterstützt auch Umlaute.
- Für die depletion-Caption höchstens 63 UTF-16-Codeeinheiten, ohne Zeilenumbruch; kurze Titel verhindern ein Überlaufen des Spielfensters.
- Andere Plugins müssen ihre Texte ausdrücklich über diesen Namespace abrufen. Ein INI-Eintrag allein ersetzt keine hartcodierte Beschriftung in fremdem Plugin-Code.

## Optionaler Betrieb

Ist localization aktiv, verwendet depletion ab Version 1.1.2 die Übersetzung der gewählten Spielsprache, gegebenenfalls den englischen Paket-Fallback. Fehlen localization, der Schlüssel oder der native Textabruf, bleibt die bisherige Ausgabe über `[depletion] panel_caption` erhalten (Standard: `Deposit remaining`). Auch eine leere, überlange oder mehrzeilige Caption führt zur bisherigen Ausgabe, nicht zur Abschaltung des Abbaus.

Texte werden niemals als Formatbefehle ausgeführt. Zahlen und Berechnungen stammen weiterhin aus depletion. Änderungen an Sprachdateien werden nach vollständigem Spielneustart übernommen.
