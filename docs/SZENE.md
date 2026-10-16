# Szene & Kulisse

Das Spiel besteht aus einem kleinen Fenster (380 x 240 px) mit Holzrahmen. Alles darin laeuft in einem
Raster aus **Sprite-Pixeln** (1 Sprite-Pixel = 5 Bildschirm-Pixel, `SceneLayout.Scale`).

## Kulisse tauschen
`Assets/backdrop.png` ist 76 x 48 Sprite-Pixel gross (= 380 x 240 bei Scale 5) und enthaelt Rahmen,
Himmel und Boden in einem Bild. Eigene Pixel-Art einfach drueberlegen (gleiche Groesse beibehalten).
Durchsichtige Pixel (z. B. die Rahmenecken) sind **nicht** klickbar - das Spiel laesst Klicks dort durch.

Standard-Kulisse neu erzeugen: `python tools/make_sprites.py`

## Wichtige Masse (`SceneLayout.cs`)
| Wert | Bedeutung |
|---|---|
| `Width` / `Height` | Fenstergroesse, muss `backdrop.png * Scale` entsprechen |
| `FrameThickness` | Staerke des Holzrahmens in Sprite-Pixeln (Sprechblasen bleiben innerhalb) |
| `FloorY` | Hoehe der Standlinie: hier stehen Fuesse der Figuren und der Fuss des Feuers |
| `FireX` | Linke Kante des Feuers |
| `Seats` | Sitzplaetze der Charaktere (`seat` in der character.json) |

Aendert man die Kulissengroesse, muessen `Width`, `Height`, `FloorY` und die Konstanten in
`tools/make_sprites.py` (`BW`, `BH`, `T`, `GROUND_TOP`) zusammenpassen.
