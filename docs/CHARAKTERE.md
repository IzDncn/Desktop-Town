# Charaktere erstellen und anpassen

Alles liegt in `src/DesktopAdventures/Content/` und wird neben die `.exe` kopiert. Es ist **kein Code nötig**.

```
Content/
  Templates/<name>/template.json + sheet.png (+ layers/*.png)   Körperbau, Animationen, Farb-Slots, Ebenen
  Characters/<id>.json                                          ein konkreter Charakter
```

Ein Charakter ist ein **Template** (Körper) + **Palette** (Farben) + optionale **Ebenen** (Hut, Bart ...).
Damit entstehen viele Figuren allein durch eine kleine JSON-Datei.

## Neuen Charakter anlegen (nur JSON)
Datei `Content/Characters/<id>.json` anlegen, z. B. `lena.json`:

```json
{
  "name": "Lena",
  "template": "humanoid",
  "palette": { "skin": "#c89070", "hair": "#d23c46", "shirt": "#2a9d8f", "accent": "#f4a261" },
  "layers": ["cap", "scarf"],
  "speed": 13,
  "idleSeconds": [2, 6],
  "walkSeconds": [2, 7],
  "startUnlocked": false
}
```

| Feld | Bedeutung |
|---|---|
| `template` | Körperbau, z. B. `humanoid` oder `cat` |
| `palette` | Farben je Slot des Templates. Nicht genannte Slots nutzen die Standardfarbe. Wert: `"#rrggbb"` (Licht- und Schattenton werden abgeleitet) oder `["#hell", "#mittel", "#dunkel"]` |
| `layers` | Ebenen des Templates, in Zeichenreihenfolge (die letzte liegt oben) |
| `speed` | Lauftempo in Kunst-Pixeln/s (0 = bleibt stehen) |
| `idleSeconds` / `walkSeconds` | `[min, max]` Sekunden Stehen bzw. Laufen |
| `startUnlocked` | von Anfang an da (später: Freischaltung durch Challenges) |

Die Datei-ID (`lena`) ist die Charakter-ID. Fehler (Tippfehler, unbekannter Slot/Ebene) erscheinen beim Start als Meldung,
der Rest des Spiels läuft weiter.

**Ansehen:** `dotnet run --project src/DesktopAdventures -- --all-characters` lässt alle Charaktere sofort erscheinen.
Klick auf eine Figur löst ihre Reaktions-Animation aus.

## Mitgelieferte Templates
| Template | Slots | Ebenen |
|---|---|---|
| `humanoid` (20×30) | `skin`, `hair`, `shirt`, `pants`, `accent`, `boots` | `cap`, `wizard_hat`, `beard`, `scarf` |
| `cat` (20×17) | `fur`, `belly`, `nose`, `eyes` | – |

## Neue Ebene (Hut, Brille, Umhang ...) oder neues Template ("Special")
Sprites sind normale PNGs (kein Halbtransparent, nur deckend oder durchsichtig). Statt fester Farben benutzt man
**Marker-Farben**, die im Spiel durch die Palette ersetzt werden. Jede Familie hat drei Töne:

| Marker | hell | mittel | dunkel |
|---|---|---|---|
| `red` | `#ff9999` | `#ff5555` | `#aa2222` |
| `green` | `#99ff99` | `#55ff55` | `#22aa22` |
| `blue` | `#9999ff` | `#5555ff` | `#2222aa` |
| `yellow` | `#ffff99` | `#ffff55` | `#aaaa22` |
| `magenta` | `#ff99ff` | `#ff55ff` | `#aa22aa` |
| `cyan` | `#99ffff` | `#55ffff` | `#22aaaa` |

Alle anderen Farben (Umriss, Augen, Gürtelschnalle ...) bleiben unverändert. Welcher Slot-Name zu welchem Marker gehört,
steht in der `template.json` unter `slots`.

- **Neue Ebene:** PNG mit exakt der Größe von `sheet.png` und gleichem Raster zeichnen, unter `layers/` ablegen und in der
  `template.json` bei `layers` eintragen. Danach in jedem Charakter per `"layers": [...]` nutzbar.
- **Neues Template / Special** (Drache, Roboter, Geist ...): Ordner `Content/Templates/<name>/` mit `sheet.png` und
  `template.json` (Vorlage: `humanoid` oder `cat`) anlegen. Bildgröße, Animationen (Zeile/Frames/fps) und Slots sind frei.
  Pflicht ist nur die Animation `idle`; `walk` und `react` sind optional. Figuren schauen im Sprite **nach rechts**.
- Die Fußsohle muss in der untersten Bildzeile liegen; so stehen alle Figuren automatisch auf dem Gras.

## Basis-Sprites neu erzeugen
`python tools/make_sprites.py` (Pillow) erzeugt die mitgelieferten Templates neu. Die PNGs können stattdessen auch
direkt in einem Pixel-Editor überarbeitet werden – dann das Skript nicht mehr ausführen.
