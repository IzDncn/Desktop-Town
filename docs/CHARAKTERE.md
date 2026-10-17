# Charaktere erstellen und anpassen (Entwickler-Doku)

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

---

# Animationen und Verhalten (Entwickler)

## Grundlagen
- Ein Sprite-Sheet ist ein Raster aus Bildern der Größe `frame` (z. B. 20×30). **Jede Zeile = eine Animation**, jede Spalte = ein Bild.
- Eine Animation wird in der `template.json` unter `animations` beschrieben: `row` (Zeile, ab 0), `frames` (Anzahl), `fps`, optional `loop` (Standard `true`).
- Die Namen sind frei wählbar. Nur zwei haben feste Bedeutung: **`idle`** (Stehen, Pflicht) und **`walk`** (Laufen, optional; ohne `walk` bleibt die Figur stehen). Alles andere (`react`, `cheer`, `sleep` ...) sind „Aktionen“, die per Verhalten ausgelöst werden.
- Ob eine Aktion abgespielt wird, entscheidet allein das **Verhalten** (`behavior`), nicht der Name.

## Verhalten (`behavior`)
Steht in der `template.json` (Standard für alle Charaktere des Templates) und/oder in der Charakter-JSON (überschreibt feldweise).

```json
"behavior": {
  "onClick": [
    { "animation": "react", "weight": 2, "repeat": 2 },
    { "animation": "cheer", "weight": 1, "repeat": 2 }
  ],
  "idleActions": [ { "animation": "yawn" }, "scratch" ],
  "idleActionChance": 0.3
}
```

| Feld | Bedeutung |
|---|---|
| `onClick` | Liste von Aktionen. Beim Anklicken wird **eine** gewichtet zufällig gewählt. Leere Liste `[]` = Klick bewirkt nichts. Ohne Angabe: `react`, falls vorhanden. |
| `idleActions` | Aktionen, die die Figur von selbst macht, wenn sie lange genug gestanden hat (statt loszulaufen). |
| `idleActionChance` | Wahrscheinlichkeit 0–1 für eine Idle-Aktion (Standard 0.35). Ohne `idleActions` wirkungslos. |

Eine Aktion ist entweder nur der Animationsname (`"scratch"`) oder ein Objekt mit `animation`, `weight` (relative Wahrscheinlichkeit, Standard 1) und `repeat` (wie oft hintereinander, Standard 1). Während einer Aktion steht die Figur; Klicks währenddessen werden ignoriert.

**Vererbung:** Was die Charakter-JSON nicht nennt, kommt vom Template. Setzt ein Charakter `onClick`, ersetzt das die *gesamte* `onClick`-Liste des Templates (Listen werden nicht gemischt); `idleActions` und `idleActionChance` bleiben davon unberührt.

## A) Neue Animation für ALLE Charaktere eines Templates
Beispiel: `humanoid` bekommt `sit` (4 Bilder).

1. **Sheet erweitern:** In `Templates/humanoid/sheet.png` eine neue Zeile unten anfügen (Höhe += `frame.height`). Bilder links beginnend, gleiche Größe, Fußsohle in der untersten Bildzeile.
2. **Ebenen erweitern:** Dieselbe Höhe auch in **jeder** Datei unter `layers/` herstellen (Hut, Bart ...). Ebenen werden Zeile für Zeile über das Körper-Sheet gelegt – eine leere Zeile heißt „Hut fehlt in dieser Animation“. Alle Ebenen müssen exakt die Größe des Sheets haben, sonst gibt es beim Start eine Fehlermeldung.
3. **Eintragen** in der `template.json`:
   ```json
   "sit": { "row": 4, "frames": 4, "fps": 4 }
   ```
4. **Auslösen** über `behavior` (Template oder einzelne Charaktere), z. B. als Idle-Aktion: `"idleActions": [ { "animation": "sit", "repeat": 3 } ]`.
5. Mit `dotnet run --project src/DesktopAdventures -- --all-characters` prüfen. Klick-Aktionen kann man mit `"onClick": [ "sit" ]` testen.

## B) Neue Animation nur für EINEN Charakter
Dafür gibt es `extra`: ein eigenes Sheet, das unter das Template-Sheet gehängt wird.

1. PNG in `Content/Characters/` anlegen, z. B. `pip_extra.png`. **Gleiche Bildgröße wie das Template**, Zeilen ab 0, wieder mit Marker-Farben (wird mit der Palette des Charakters eingefärbt).
2. In `pip.json`:
   ```json
   "extra": {
     "sheet": "pip_extra.png",
     "animations": { "lute": { "row": 0, "frames": 6, "fps": 5 } }
   },
   "behavior": { "idleActions": [ { "animation": "lute", "repeat": 4 } ], "idleActionChance": 0.5 }
   ```
3. Wichtig: Ebenen (Hut, Schal ...) werden **nicht** automatisch über `extra`-Bilder gelegt – im Extra-Sheet müssen Hut & Co. fertig mitgezeichnet sein. Der Name darf nicht schon im Template existieren.

Faustregel: Kommt eine Animation bei **mehreren** Charakteren vor → Variante A. Ist sie eine Besonderheit **eines** Charakters → Variante B. Soll ein Charakter einen komplett eigenen Körper haben → eigenes Template (siehe oben).

## C) Animationen per Skript erzeugen (`tools/make_sprites.py`)
Die mitgelieferten Templates werden prozedural gezeichnet. Eine neue Animation entsteht dort so:
1. In `pose(anim, k)` einen neuen Zweig mit den Parametern der Pose ergänzen (z. B. `hb` = Kopf/Oberkörper-Versatz, `fx`/`bx` = Beinposition, `lf`/`lb` = Bein angehoben). Als Vorbild dient `"cheer"`.
2. Neue Körperteile (z. B. eine zweite Armhaltung) in `humanoid_base` zeichnen und, wenn nötig, Ebenen (`layer_*`) nachziehen.
3. `("sit", 4)` in `HUMANOID_ANIMS` anhängen – die Reihenfolge bestimmt die Zeilennummer.
4. `python tools/make_sprites.py` ausführen (überschreibt `sheet.png` und alle `layers/*.png`) und die Animation in der `template.json` eintragen.
Handbearbeitete PNGs werden dabei überschrieben – wer von Hand weiterzeichnet, führt das Skript nicht mehr aus.

## Fehlersuche
- Beim Start erscheint eine Meldung mit Datei und Grund (unbekannte Animation, falsche Größe, Tippfehler bei Slots, ...). Betroffene Charaktere werden übersprungen, der Rest läuft.
- „liegt außerhalb des Sheets“: `row`/`frames` passen nicht zur PNG-Größe.
- Figur reagiert nicht auf Klick: `onClick` leer oder Animation fehlt; nur deckende Pixel der Figur nehmen Klicks an (durchsichtige gehen an Windows durch).
- JSON erlaubt `//`-Kommentare und Kommas am Ende.

---

# Infos im Buch (Beschreibung, Fakten, Freischalt-Hinweis)

Das Buch (Klick auf das Buch im Gras oder Rechtsklick → *Buch öffnen*) zeigt links alle Charaktere als Karten (4 pro Reihe, 12 pro Seite, Blättern
mit den Pfeilen; neue Charaktere erscheinen automatisch) und rechts die Details des gewählten. Alle Texte stehen in der Charakter-JSON im Block `info`:

```json
"info": {
  "title": "Barbar",
  "description": "Thunk hat noch nie einen Kampf verloren, aber schon oft den Weg nach Hause.\nEr sammelt gern Beeren.",
  "facts": ["Mag: Beeren", "Hasst: Kleine Türen"],
  "unlockHint": "Wird später durch eine Challenge freigeschaltet."
}
```

| Feld | Wird angezeigt | Richtwert |
|---|---|---|
| `name` (Hauptfeld) | Überschrift links oben auf der rechten Seite | bis ca. 12 Zeichen |
| `info.title` | rechts neben dem Namen (z. B. Klasse/Beruf) | bis ca. 14 Zeichen |
| `info.description` | Fließtext unter dem Vorschaubild; `\n` = neue Zeile | bis ca. 160 Zeichen (6 Zeilen), Überlänge wird mit „…“ abgeschnitten |
| `info.facts` | kurze Zeilen rechts neben dem Vorschaubild, eine pro Eintrag | höchstens 3 Zeilen (ca. 16 Zeichen je Zeile, längere Einträge werden umgebrochen) |
| `info.unlockHint` | statt Beschreibung, solange der Charakter gesperrt ist | wie `description` |

- Fehlt `info` ganz oder teilweise, erscheinen neutrale Platzhaltertexte; das Buch funktioniert immer.
- **Gesperrte Charaktere** werden als Silhouette mit „???“ gezeigt, ohne Name, Titel, Beschreibung und Fakten – nur `unlockHint` ist sichtbar. Welche Charaktere freigeschaltet sind, steuert aktuell `startUnlocked` (ab Etappe 4: Challenges).
- **Vorschau:** Im Buch spielt der Charakter seine Idle-Animation; ein Klick auf das Vorschaubild spielt eine Klick-Aktion aus seinem `behavior.onClick` ab (siehe oben). Mit `--all-characters` sind alle freigeschaltet, ohne diese Option sieht man auch die gesperrte Darstellung.
- **Zeichensatz:** A–Z, a–z, Ziffern, übliche Satzzeichen sowie ä ö ü Ä Ö Ü ß – — … „ “ ” ‚ ‘ ’ · • × ° €. Andere Zeichen erscheinen als „?“. Pfeile und Emojis gibt es nicht.
- **Vorschaubild:** Die Karte und die Vorschau zeigen das Idle-Bild des Charakters. Sprites bis ca. 36×40 Pixel passen in die Karte, größere werden abgeschnitten.
- Texte sind in der JSON direkt änderbar; nach einem Neustart (bzw. `dotnet run`) sind sie im Buch. Anführungszeichen im Text als `\"` schreiben.

## Reiter im Buch erweitern (Entwickler)
Jeder Reiter ist eine Klasse, die von `BookTab` erbt (`src/DesktopAdventures/Ui/`). Sie liefert Titel, Lesezeichen-Farbe, Symbol und zeichnet beide Seiten
(`Draw(ui, leftPage, rightPage)`); klickbare Flächen meldet sie mit `ui.Button(rect, () => ...)` an (Immediate-Mode). Eintragen: in `StripWindow.ToggleBook()`
zur Reiter-Liste hinzufügen. Vorbilder: `CharactersTab` (voll ausgebaut) und `BuildingsTab` (Leerzustand).
Gebäude-Infos werden später genauso aufgebaut wie die der Charaktere (eigener `info`-Block je Gebäude-JSON); bis es Gebäude gibt, zeigt der Reiter „Gebäude“ einen Leerzustand.

## Schrift
Das Buch nutzt eine Bitmap-Schrift (Jersey 10, SIL Open Font License, Lizenztext in `Content/Fonts/jersey10/OFL.txt`), erzeugt mit `python tools/make_font.py`.
Anderer Zeichensatz oder andere Schrift: `CHARS`, `FONT`, `SIZE` im Skript anpassen und neu ausführen.
