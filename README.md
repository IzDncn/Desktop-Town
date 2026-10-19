# Desktop Adventures

Ein Pixel-Art-Spiel, das als schmaler Streifen direkt auf der Windows-Taskleiste sitzt: eine Landschaft
über die volle Bildschirmbreite, die mit der Spielzeit wächst. Transparente Bereiche sind klickdurchlässig,
das Spiel stört die normale Arbeit am Rechner nicht.

## Starten

Voraussetzung: Windows 10/11, .NET 8 SDK.

```
dotnet run --project src/DesktopAdventures
```

oder `DesktopAdventures.sln` in Rider / Visual Studio öffnen. Das Buch öffnet per Klick auf das Buch im Gras oder per Rechtsklick → *Buch öffnen*. Beenden: Rechtsklick → *Beenden*.

## Etappen

- [x] 1. Fundament: Streifen auf der Taskleiste, Pixel-Engine, prozedurale Landschaft
- [x] 2. Welt & Charaktere: Sprite-System, datengetriebene Charaktere, Figuren laufen durch die Landschaft
- [x] 3. Buch-Popup: Pixel-UI im Stil des Referenzbilds, Reiter *Charaktere* (komplett) und *Gebäude* (Leerzustand)
- [x] 4. Spielzustand: Speichern, Statistiken, Challenges, Freischaltungen
- [ ] 5. Bauen & Inhalte: Häuser, weitere Charaktere, Tageszeit, Tray-Icon, Release-Workflow

## Spielstand und Challenges
Fortschritt wird automatisch gespeichert (`%APPDATA%\DesktopAdventures\save.json`). Challenges schalten Charaktere frei: `docs/CHALLENGES.md`.
Neu beginnen: `dotnet run --project src/DesktopAdventures -- --reset-save`.

## Charaktere
Eigene Charaktere entstehen per JSON (Farben, Hüte, Specials), ohne Code: `docs/CHARAKTERE.md`.
Alle Charaktere ansehen: `dotnet run --project src/DesktopAdventures -- --all-characters`.

Details: `CHANGELOG.md`, Technik: `docs/ARCHITEKTUR.md`.
