# Changelog

## 0.4.0 – Etappe 4: Spielstand, Challenges, Freischaltungen
- Spielstand (`%APPDATA%\DesktopAdventures\save.json`): Freischaltungen, Challenges, Statistiken, Buch-Position; Autosave, Speichern beim Beenden/Herunterfahren,
  atomares Schreiben, beschädigte Dateien werden gesichert
- Challenge-System per JSON (`Content/Challenges`): Ziel auf eine Statistik, Voraussetzungen (`requires`), Belohnungen (`unlockCharacter`), Prüfung beim Start
- Statistiken: Spielzeit, Klicks auf Charaktere, Buch geöffnet (erweiterbar)
- Reiter *Challenges* funktional (Aktiv/Abgeschlossen, Liste mit Fortschrittsbalken, Details mit Belohnung); gesperrte Charaktere zeigen ihre Freischalt-Challenge
  mit Fortschritt
- Meldung oben im Streifen beim Abschluss; neue Charaktere erscheinen sofort in der Landschaft und jubeln
- Mitgeliefert: 5 Challenges (Hallo!, Streichelzoo, Geduld, Buchwurm, Nachtschwärmer); außer Thunk sind alle Charaktere jetzt gesperrt
- Start-Optionen `--reset-save`, `--all-characters` (speichert nichts)
- Doku: `docs/CHALLENGES.md`

## 0.3.1 – Feinschliff Buch & Taskleiste
- Taskleiste: Erkennung der ausgeblendeten Taskleiste über drei Signale (Fensterrechteck, Abtasten, Maus-Vorhersage) statt nur über das Rechteck;
  Autohide wird zusätzlich aus dem Arbeitsbereich erschlossen. Diagnose-Log neben der .exe (`taskbar-debug.log`), in Debug-Builds per Kontextmenü
  *[Dev] Taskleisten-Log starten*; Notlösung `GameConfig.AutohideFollowsTaskbar`
- Buch ist per Maus verschiebbar (an jeder freien Stelle ziehen); die Position wird für die laufende Sitzung gemerkt
- Neue Schrift Jersey 20 in nativer Größe: offene Rundungen bei a/e, eindeutige Ziffern, richtiges ß; Buch dadurch etwas größer (504×298)
- Untermenüs im Buch (`SubTabBar`): Charaktere → Freigeschaltet/Gesperrt, Gebäude → Gebaut/Noch offen, neuer Reiter Challenges → Aktiv/Abgeschlossen (noch leer)

## 0.3.0 – Etappe 3: Buch-Popup
- Buch im Gras (funkelt gelegentlich); Klick oder Rechtsklick → *Buch öffnen* zeigt das Popup im Pixel-Stil mit Lesezeichen-Reitern
  (Esc schließt, Pfeiltasten wechseln den Reiter)
- Reiter *Charaktere*: Übersichtsraster mit Blättern, gesperrte Charaktere als Silhouette, Detailseite mit animierter Vorschau
  (Klick spielt eine Klick-Aktion), Titel, Beschreibung, Fakten und Freischalt-Hinweis aus der Charakter-JSON (`info`)
- Reiter *Gebäude*: Leerzustand, bis es Gebäude gibt
- Bitmap-Schrift (Jersey 10, OFL) mit Umlauten, `tools/make_font.py`
- `GameState` (freigeschaltete Charaktere), `PixelBuffer` mit Clip-Rechteck, Skalierung und Tint
- Doku: Infos im Buch und Reiter erweitern in `docs/CHARAKTERE.md`

## 0.2.1 – Taskleiste & Animationssystem
- Fix: Beim Einfahren der Autohide-Taskleiste geht der Streifen sofort mit nach oben, die Taskleiste liegt nie mehr über dem Spiel.
  Neben dem Fensterrechteck wird die Taskleiste zusätzlich über Abtastung vom unteren Rand erkannt (Windows 11);
  `--debug-taskbar` schreibt die Messwerte nach `%TEMP%\desktop-adventures-taskbar.log`
- Verhalten per JSON: `behavior.onClick`, `idleActions`, `idleActionChance` (gewichtet, mit Wiederholungen); Template-Standard,
  pro Charakter überschreibbar
- `extra`: Zusatz-Animationen nur für einen einzelnen Charakter (eigenes Sheet)
- Neue Animation `cheer` (Jubelsprung) für Humanoide; Pip jubelt bei Klick (Beispiel für Überschreiben)
- Doku: Entwickler-Anleitung für Animationen und Verhalten in `docs/CHARAKTERE.md`

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
