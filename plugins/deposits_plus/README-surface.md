# Sandige Wiese — deposits_plus 1.8.0-beta

Optionale Bodenoptik für die unabhängigen Sandvorkommen in WRSR 1.1.1.9.
Die bestätigte Wiesenstruktur mit unregelmäßigen Sandspuren wird an den
Sandvorkommen eingeblendet. Die Ressourcendichte steuert den Übergang:
kräftigere Vorkommen werden deutlicher sichtbar, schwache Ränder laufen aus.

Korrektur 1.7.1: Die normale Shader-ClassLinkage des Spiele-Laders wird
unverändert weitergereicht. Version 1.7 hatte diese irrtümlich ausgeschlossen;
dies war eine zusätzliche, im separaten Test reproduzierte Hürde. Die Korrektur wurde
zusätzlich mit dem tatsächlichen FX-Lader aus der installierten Engine-DLL
in einem separaten WARP-Testprozess geprüft. Texturen und INI bleiben gleich.

Korrektur 1.7.2: Die Einbindung erfolgt erst im tatsächlichen virtuellen
`CreateShaders`-Aufruf. `CreateManagedShaders` legt dagegen nur ein leeres
Objekt an und löst noch keine Pixelshader-Erzeugung aus. Deshalb zeigte der
Spieltest von 1.7.1 weiterhin `PS calls=0`. Geprüft wurde jetzt auch die gesamte
native Abfolge mit Objektanlage, späterem Dateiladen, Cache-Nutzung sowie
Zurücksetzen und erneutem Laden. Die Shaderdateien selbst bleiben unverändert.

Ergänzung 1.7.3: Eine eigene Herbstvariante verbindet die bestätigten Sandspuren
mit der braunen Palette der originalen `grass2_fall.dds`. Sommer und Herbst
verwenden getrennte Farb-/Normaltexturen, auch während der saisonalen Überblendung.
Die native Reihenfolge ist Sommer → Herbst → Schnee; in 1.7.2 waren Herbst
und Schnee bei der Materialauswahl vertauscht. Deshalb konnte die Sandoptik
beim Wechsel zur braunen Wiese verschwinden. Diese Zuordnung ist korrigiert.
`grass2_snow.dds` und die native Schneeberechnung werden nicht ersetzt.
Es sind keine neuen INI-Schlüssel erforderlich.

## Einstellungen

In `plugins/deposits_plus.ini`, im vorhandenen Abschnitt `[deposits_plus]`:

```ini
sand_surface = 1
sand_surface_strength = 1.0
sand_surface_token = $TYPE_MINE_SAND
```

- `sand_surface`: 1 aktiviert die Zusatzdarstellung, 0 deaktiviert sie.
- `sand_surface_strength`: 0.0 bis 1.0; 1.0 ist die volle Stärke der
  vorbereiteten Textur. Die Wiesenanteile sind bereits in der Textur enthalten.
- `sand_surface_token`: Kennung des bestehenden Sandvorkommens. Es muss eine
  unabhängige Ressourcenkarte verwenden (`independent_map = 1` bei Sand).
  Dies erzeugt keine neuen Lagerstätten und verändert keine Mengen.

Nach einer Änderung das Spiel vollständig beenden und neu starten.
Ein vorhandener Spielstand genügt; keine neue Karte und keine Neugenerierung
der Ressourcen nötig. `weather_roads` und `depletion` müssen nicht geändert werden.

## Dateien und Installation

Neben `plugins/deposits_plus.dll` und der INI werden benötigt:

```text
plugins/
  deposits_plus.dll
  deposits_plus.ini
  deposits_plus/
    assets/
      sand_meadow_color.dds
      sand_meadow_normal.dds
      sand_meadow_autumn_color.dds
      sand_meadow_autumn_normal.dds
```

Alle vier DDS sind 1024 × 1024 mit elf Mipmap-Stufen. Farbe: BC1/DXT1;
Normalmap: BC3/DXT5. `build.bat` kopiert die Assets beim Neubauen in diese
Struktur. Beim manuellen Aktualisieren vorhandene INI-Einstellungen behalten
und nur die drei neuen Schlüssel ergänzen.

Originaldateien unter `media_soviet` werden weder ersetzt noch überschrieben.
Zum Abschalten genügt `sand_surface = 0` und ein vollständiger Neustart.

## Was unverändert bleibt

- Lagerstätten, Verteilung, Mengen, Bergbau, Fahrzeug-Skills und Saveformate.
- Ressourcenkarte und eigentliche Terrain-Materialmaske: ausschließlich lesender Zugriff.
- Native Fels-/Bodenschichten sowie Beleuchtung, Nebel und Schnee-Berechnung.
- Schnee-, Wüsten- und andere Grundmaterialien: Nur die erkannte Wiesenbasis
  `grass2.dds` beziehungsweise `grass2_fall.dds` wird eingeblendet.

Es werden keine Bäume entfernt, keine Höhen verändert und keine zusätzlichen
Wüstenflächen angelegt. Grasobjekte bleiben zunächst nativ. Mit aktivem
`depletion` folgt die Oberfläche dem verbleibenden Sand in der Ressourcenkarte;
sie ist kein dauerhaft aufgemalter Terrain-Anstrich.

## Technische Abgrenzung und Absicherung

Die zusätzliche Textur wird beim Laden ausschließlich in 13 identifizierte
native Terrain-Pixelshader eingefügt. Deren bestehende Abfolge für
Terrain-Blending, Licht, Nebel und Schnee bleibt erhalten. Die Texturabtastung
verwendet die nativen UVs, Sampler und Ableitungen. Nur RGB wird eingeblendet,
die ursprünglichen Alpha-Werte bleiben erhalten. Keine Shaderdatei wird geändert.

Die Auswahlmaske ist die aktuelle GPU-Ressourcenkarte. Bei einem Weltwechsel
wird ihre Identität neu geprüft; es gibt keine GPU-Auslese oder vollständige
Ressourcenkopie pro Frame. Die Zusatzabtastungen können trotzdem Grafikleistung
kosten; die tatsächliche Auswirkung muss im Spiel getestet werden.

Unbekannte Engine-/Shaderstände, fehlende Assets, unpassende Ressourcenformate
und von anderen Mods belegte Bindungsplätze deaktivieren nur diese Optik und
erzeugen eine konkrete Warnung. Die bisherigen deposits_plus-Funktionen bleiben
davon unabhängig. Andere Shader-Mods werden nicht zwangsweise überschrieben.

## Teststand und saisonaler Spieltest

Offline geprüft: alle 13 Originalshader mit D3D11/WARP, tatsächlich gerenderte
Masken-/Licht-/Schnee-Fixtures, DDS-Laden, Jahreszeitenflags, Weltkartenwechsel,
temporäre Shader-Hooks und Wiederherstellung der Grafikbindungen. Separat geprüft
sind beide saisonalen Farb-/Normalpaare mit 69.120 GPU-Pixelvergleichen sowie
die Materialzugriffe der installierten Engine. Die bisherigen
Tests für Erzeugung, Abbauzustand, Speichern/Laden und Arbeitsfahrzeuge bestehen.

Die Sommerdarstellung, das Schrumpfen mit `depletion` und Speichern/Laden
wurden zuvor im Spiel bestätigt. Für die neue Herbstvariante ist noch ein
Sicht- und Leistungstest im laufenden Spiel erforderlich:

1. Spiel vollständig neu starten und einen vorhandenen Spielstand laden.
2. Das bekannte Sandvorkommen auf der braunen Herbstwiese ansehen.
3. Sommer → Herbst → Schnee und Schneeschmelze prüfen: Sand bleibt auf der
   Wiese sichtbar und wird von der nativen Schneedecke überlagert.
4. Aus dem Hauptmenü neu laden und prüfen, ob dieselbe Stelle korrekt erscheint.

Erwartete Logmeldungen in `tesmioloader.log`:

```text
sand surface shader preparation: 13/13 verified native programs augmented
sand surface active: token=$TYPE_MINE_SAND ...
sand surface terrain selection: season=2 transition=0 surface=autumn/native ...
```

`prepared` allein bedeutet nur, dass die Hooks eingerichtet sind, nicht dass
bereits sichtbar gerendert wurde. Bei Problemen Screenshot und Log prüfen;
vorhandene Spielstände nicht löschen und Ressourcen nicht neu generieren.

## Fremdcode

`third_party/d3d12TokenizedProgramFormat.hpp` und `third_party/DxilHash.cpp`
stammen unverändert aus Microsofts DirectXShaderCompiler-Repository. Die
Lizenzhinweise stehen vollständig in `third_party/LICENSE.TXT` im Quellordner
beziehungsweise `plugins/deposits_plus/THIRD-PARTY-LICENSE.txt` im Laufzeitordner. Der Hashhelfer
berechnet ausschließlich die DXBC-Formatprüfsumme; er ist keine
Sicherheits-Signaturprüfung.

- https://github.com/microsoft/DirectXShaderCompiler/blob/main/include/dxc/Support/d3d12TokenizedProgramFormat.hpp
- https://github.com/microsoft/DirectXShaderCompiler/blob/main/lib/DxilHash/DxilHash.cpp
- https://github.com/microsoft/DirectXShaderCompiler/blob/main/LICENSE.TXT
