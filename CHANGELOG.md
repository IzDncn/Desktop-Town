# Changelog

## 0.1.0 – Etappe 1: Fundament
- Neustart des Projekts mit neuer Architektur (alter Lagerfeuer-Prototyp entfällt)
- Randloses, transparentes Fenster über die volle Breite des Hauptbildschirms, direkt über der Taskleiste
  (feste und automatisch ausgeblendete Taskleiste, Auflösungs- und DPI-Wechsel)
- Software-Pixelrenderer (`PixelBuffer`) mit ganzzahliger Skalierung: 1 Kunst-Pixel = 2 DIPs
  (statt zuvor deutlich gröberem Raster), pixelscharf auch bei 125 % / 150 % Skalierung
- Prozedurale Landschaft: Hügel, Gras, Erdschichten, Steine, Pilze, Büsche, Blumen; Gras und Blumen wiegen sich im Wind
- Klickdurchlässig außerhalb der Landschaft, kein Fokusklau, nicht in Alt+Tab, nur eine Instanz
