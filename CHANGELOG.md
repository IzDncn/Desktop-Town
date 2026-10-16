# Changelog

Jede Etappe ist ein Git-Tag. Alte Staende ansehen: `git checkout etappe-2` (zurueck: `git checkout main`).

## Etappe 5 (`etappe-5`)
- Gerahmter Spielbereich: Kulisse `Assets/backdrop.png` (Holzrahmen, Nachthimmel, Mond, Tannen, Boden, Steinkranz)
- Fenster ist jetzt 380x240; Figuren und Feuer stehen auf einer festen Standlinie (`SceneLayout.FloorY`)
- Sprechblasen bleiben innerhalb des Rahmens und sind etwas schmaler (zwei Blasen passen nebeneinander)
- Hinweis: Das Fenster ist jetzt sichtbar und faengt Klicks ab (nur die abgerundeten Ecken sind durchsichtig)
- Doku: `docs/SZENE.md`

## Etappe 4.1 (`etappe-4.1`)
- Fix: Sprechblasen ragten ueber den Fensterrand (Blase wurde im unsichtbaren Zustand gemessen -> Breite 0)

## Etappe 4 (`etappe-4`)
- Charaktere kommen aus `Characters/<id>/character.json` (Name, Sitzplatz, Texte, Outfits, Frame-Groesse/-Timing)
- Outfits pro Charakter (Wanderer: `default` + `winter`), Wechsel per `Character.SetOutfit`
- Zwei Charaktere am Feuer (Wanderer, Pip der Barde); eigene Sprechblase pro Charakter, gelegentliches Selbstgespraech
- Neue Struktur: `CampScene` (Aufbau + Takt), `CharacterView` (UI), `Character` (Logik), `Data/` (JSON-Modelle + Loader), `SceneLayout`
- Fehlerhafte Charaktere werden uebersprungen und beim Start gemeldet
- Doku: `docs/CHARAKTERE.md`; Sprite-Generator liegt jetzt in `tools/`

## Etappe 3.1 (`etappe-3.1`)
- Fix: 6 px Abstand zum unteren Bildschirmrand, damit die Autohide-Taskleiste einfahren kann (`WindowDocker.BottomGap`)

## Etappe 3 (`etappe-3`)
- Projekt heisst jetzt **Desktop Adventures**
- Neu: `Platform/Taskbar.cs` liest die sichtbare Taskleisten-Hoehe aus (auch bei Autohide)
- Neu: `Platform/WindowDocker.cs` schiebt das Fenster sanft mit der Taskleiste mit
- `MainWindow` nutzt den Docker statt fester Positionierung

## Etappe 1+2 (`etappe-2`)
- Transparentes Fenster, animiertes Feuer, Charakter mit Idle und Klick-Reaktion
