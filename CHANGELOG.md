# Changelog

## 0.2.0 – Etappe 2: Welt & Charaktere
- Landschaft sitzt höher: Bei ausgeblendeter Taskleiste bleibt ein DPI-abhängiger Abstand (`GameConfig.AutohideGapDips`),
  damit sie wieder einfahren kann; unterste Bodenzeile als saubere Kante
- Boden unter dem Gras deutlich flacher (`GroundDepth` 20 → 13), dadurch mehr Platz nach oben für Gebäude
- Sprite-System (`Sprite`, `PixelBuffer.Draw` mit Spiegelung)
- Charaktere komplett datengetrieben: Template (Körper, Animationen, Farb-Slots, Ebenen) + Palette + Ebenen per JSON,
  Marker-Farben für beliebige Umfärbung, Fehlermeldungen statt Absturz – siehe `docs/CHARAKTERE.md`
- Mitgeliefert: Templates `humanoid` und `cat`; Charaktere Thunk, Merle, Pip, Mochi, Nox
- Figuren wandern zufällig, stehen herum, folgen dem Gelände und reagieren auf Klick
- `--all-characters` zeigt alle Charaktere sofort; `tools/make_sprites.py` erzeugt die Basis-Sprites

## 0.1.0 – Etappe 1: Fundament
- Neustart des Projekts mit neuer Architektur (alter Lagerfeuer-Prototyp entfällt)
- Randloses, transparentes Fenster über die volle Breite des Hauptbildschirms, direkt über der Taskleiste
  (feste und automatisch ausgeblendete Taskleiste, Auflösungs- und DPI-Wechsel)
- Software-Pixelrenderer (`PixelBuffer`) mit ganzzahliger Skalierung: 1 Kunst-Pixel = 2 DIPs
  (statt zuvor deutlich gröberem Raster), pixelscharf auch bei 125 % / 150 % Skalierung
- Prozedurale Landschaft: Hügel, Gras, Erdschichten, Steine, Pilze, Büsche, Blumen; Gras und Blumen wiegen sich im Wind
- Klickdurchlässig außerhalb der Landschaft, kein Fokusklau, nicht in Alt+Tab, nur eine Instanz
