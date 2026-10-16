# Desktop Adventures

Ein kleines Pixel-Art-Spiel ohne richtiges Fenster: Ein Lagerfeuer mit Charakteren sitzt
direkt auf der Taskleiste. Transparente Bereiche sind klickdurchlaessig.

## Starten (Rider)
1. `Lagerfeuer.csproj` oeffnen (File > Open)
2. Run-Konfiguration "Lagerfeuer" waehlen, Play druecken
3. Beenden: Rechtsklick auf Feuer oder Figur > Beenden

Voraussetzung: .NET 8 SDK, Windows.

## Charaktere & Sprites
Charaktere (Aussehen, Outfits, Texte) liegen als JSON + PNG in `Characters/`, Anleitung: `docs/CHARAKTERE.md`.
`tools/make_sprites.py` (Python + Pillow) erzeugt die Platzhalter-Sprites; eigene Pixel-Art ersetzt sie einfach.

## Etappen
- [x] 1. Transparentes Fenster mit animiertem Lagerfeuer
- [x] 2. Ein Charakter mit Idle-Animation und Klick-Reaktion
- [x] 3. Taskleisten-Verhalten (folgt der Autohide-Taskleiste)
- [x] 4. Charaktere aus JSON (Aussehen, Outfits, Texte austauschbar) - siehe `docs/CHARAKTERE.md`
- [x] 5. Das "Fenster": klarer, gerahmter Spielbereich - siehe `docs/SZENE.md`
- [ ] 6. Erstes Minigame samt Freischaltung
- [ ] 7. Speichern, Tageszeit-Stimmung, Tray-Icon

Siehe `CHANGELOG.md`. Jede Etappe ist als Git-Tag abgelegt (`etappe-2`, `etappe-3`, `etappe-3.1`, `etappe-4`, `etappe-4.1`, `etappe-5`, ...).
Kulisse und Masse der Szene: `docs/SZENE.md`.
Alten Stand ansehen: `git checkout etappe-4` - zurueck zur neuesten Version: `git checkout main`.
