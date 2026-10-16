# Architektur

```
src/DesktopAdventures/
  Shell/      StripWindow (Fenster + Game-Loop), StripLayout (Rastermaße)
  Platform/   Win32: Taskleisten-/Monitor-Erkennung (ScreenDock), P/Invoke
  Engine/     PixelBuffer (Software-Zeichenfläche), Palette, Noise
  World/      GameWorld (Welt-Zustand), Landscape (prozedurales Gelände), Wind
  GameConfig  zentrale Konstanten
```

## Rendering
Die Welt wird in **Kunst-Pixeln** in einen `PixelBuffer` (0xAARRGGBB) gezeichnet und per `WriteableBitmap`
mit Nearest-Neighbor auf den Bildschirm skaliert. `StripLayout` rundet die Kunst-Pixel-Größe auf ganze
physische Pixel, so bleibt das Raster bei jeder DPI-Skalierung exakt.

- Feiner/gröber: `GameConfig.ArtPixelDips` (Standard 2.0).
- Höhe des Streifens: `GameConfig.StripHeightArt`.

## Platzierung
`ScreenDock` liefert linke/rechte Kante und die Oberkante der Taskleiste des Hauptbildschirms (Arbeitsbereich);
bei Autohide wird das aktuelle Taskleisten-Rechteck ausgewertet, `StripWindow` folgt weich.
Positioniert wird per `SetWindowPos` in physischen Pixeln (keine DIP-Rundungsfehler).

## Landschaft
`Landscape` ist deterministisch (Seed). Statisches (Gelände, Steine, Büsche, Pilze) wird einmal in einen Puffer
gemalt, Gras und Blumen werden pro Frame mit `Wind.At(x, t)` darübergelegt. `Landscape.SurfaceY(x)` liefert die
Standhöhe für Gebäude und Figuren.
