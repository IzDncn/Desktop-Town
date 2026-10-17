# Architektur

```
src/DesktopAdventures/
  Shell/      StripWindow (Streifen + Game-Loop), BookWindow (Buch-Popup), StripLayout (Rastermaße)
  Platform/   Win32: Taskleisten-/Monitor-Erkennung (ScreenDock), P/Invoke
  Engine/     PixelBuffer (Software-Zeichenfläche), Sprite, Palette, Noise, ColorUtil
  Characters/ Katalog (lädt Templates + Charakter-JSON, färbt Sprites ein), Definitionen, Marker-Farben
  World/      GameWorld (Welt-Zustand), Landscape (Gelände), CharacterActor (Figuren-Verhalten), Wind
  Ui/         Buch: BookScreen, Reiter (BookTab), UiContext (Immediate-Mode), BitmapFont, PixelIcon, BookTheme
  GameState   Spielstand (freigeschaltete Charaktere), von Welt und Buch genutzt
  Content/    Fonts/, Templates/ und Characters/ (JSON + PNG, neben der .exe, ohne Code änderbar)
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

## Charaktere
`CharacterCatalog` lädt beim Start alle `Content/Characters/*.json`. Pro Charakter wird das Template-Sheet anhand der Palette
umgefärbt (Marker-Farben → Palette) und die gewählten Ebenen werden daraufgelegt; das Ergebnis ist ein fertiges
`CharacterSheet`. `CharacterActor` steuert Verhalten (stehen → laufen → stehen, Umkehr am Rand, Reaktion auf Klick) und
zeichnet das passende Bild; gespiegelt wird zur Laufzeit. Details zum Format: `docs/CHARAKTERE.md`.

Klicks: transparente Pixel des Fensters gehen an Windows durch, daher erreichen das Spiel nur Klicks auf Gras und Figuren.

## Buch
`BookWindow` ist ein eigenes transparentes Fenster; `BookScreen` zeichnet Einband, Seiten, Lesezeichen und den aktiven `BookTab` in einen `PixelBuffer`
(Größe `BookTheme.Width × Height` Kunst-Pixel), derselbe Renderer und dasselbe ganzzahlige Pixelraster wie der Streifen. Das UI ist Immediate-Mode:
jeder Frame wird komplett neu gezeichnet, klickbare Flächen werden dabei über `UiContext.Button` angemeldet und beim Klick ausgelöst. Text kommt aus
`BitmapFont` (Atlas aus `tools/make_font.py`). Geöffnet wird das Buch über `BookProp` im Gras (Klick → `WorldClick.Book`) oder das Kontextmenü.
