# Charaktere anpassen

Alles zu einem Charakter liegt in **einem Ordner**: `Characters/<id>/`

```
Characters/
  wanderer/
    character.json            <- Name, Sitzplatz, Texte, Outfits
    outfits/
      default/idle.png        <- Sprite-Sheets (Frames nebeneinander)
      default/react.png
      winter/idle.png
      winter/react.png
```

Der Ordnername ist die ID des Charakters. Beim Bauen werden alle Dateien neben die `.exe` kopiert.
Fehler in einer JSON werden beim Start als Meldung angezeigt; der betroffene Charakter wird dann uebersprungen.

## Texte aendern
In `character.json` unter `"lines"`:
- `"click"`: Antworten beim Anklicken (eine Zeile wird zufaellig gewaehlt)
- `"idle"`: Selbstgespraech am Feuer (alle 15-35 s, wenn der Charakter ruhig sitzt)

Zeilen einfach hinzufuegen oder aendern. Kommentare mit `//` sind in der JSON erlaubt. Umlaute gehen.

## Neues Outfit
1. Neuen Ordner `outfits/<name>/` mit `idle.png` und `react.png` anlegen (gleiche Frame-Groesse wie die anderen).
2. In `character.json` unter `"outfits"` eintragen:
   `"<name>": { "idle": "outfits/<name>/idle.png", "react": "outfits/<name>/react.png" }`
3. `"outfit": "<name>"` setzen, um es zu tragen.

(Spaeter koennen Freischaltungen per Code `character.SetOutfit("<name>")` aufrufen.)

## Neuer Charakter
1. Einen vorhandenen Ordner kopieren und umbenennen (z. B. `Characters/mara/`).
2. `name`, `seat`, Texte und Sprites anpassen.
3. `seat` ist der Sitzplatz am Feuer (`0` = links, `1` = rechts). Jeder Platz darf nur einmal vergeben sein.
   Weitere Plaetze: `SceneLayout.Seats` in `SceneLayout.cs`.

## Sprite-Format
- PNG mit Transparenz, **alle Frames einer Animation nebeneinander** in einer Datei.
- `frameWidth` / `frameHeight` = Groesse **eines** Frames in Sprite-Pixeln (Standard 16x24).
- Anzahl der Frames ergibt sich aus der Dateibreite (Breite / frameWidth).
- Figuren schauen im Sprite **nach rechts**; Charaktere auf der rechten Seite werden automatisch gespiegelt.
- `idle` laeuft in Schleife, `react` spielt einmal ab (beim Anklicken).
- Platzhalter-Sprites erzeugt `python tools/make_sprites.py` (Pillow noetig).
