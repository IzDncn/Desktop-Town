# Desktop Adventures

Ein Pixel-Art-Spiel, das als schmaler Streifen direkt auf der Windows-Taskleiste sitzt: eine Landschaft
über die volle Bildschirmbreite, die mit der Spielzeit wächst. Transparente Bereiche sind klickdurchlässig,
das Spiel stört die normale Arbeit am Rechner nicht.

## Starten

Voraussetzung: Windows 10/11, .NET 8 SDK.

```
dotnet run --project src/DesktopAdventures
```

oder `DesktopAdventures.sln` in Rider / Visual Studio öffnen. Beenden: Rechtsklick auf die Landschaft → *Beenden*.

## Etappen

- [x] 1. Fundament: Streifen auf der Taskleiste, Pixel-Engine, prozedurale Landschaft
- [ ] 2. Welt: Sprite-System, erste Figur läuft durch die Landschaft, Tageszeit
- [ ] 3. Buch-Popup: Pixel-UI im Stil des Referenzbilds, Tabs
- [ ] 4. Spielzustand: Speichern, Ressourcen, Challenges, Freischaltungen
- [ ] 5. Bauen & Inhalte: Häuser, weitere Charaktere, Tray-Icon, Release-Workflow

Details: `CHANGELOG.md`, Technik: `docs/ARCHITEKTUR.md`.
