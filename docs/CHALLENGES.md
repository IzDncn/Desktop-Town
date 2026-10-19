# Challenges, Statistiken und Spielstand (Entwickler-Doku)

Challenges sind Ziele, die der Spieler erreichen soll. Sie schalten Charaktere frei (später auch Gebäude). Alles ist per JSON änderbar,
nur neue *Statistiken* brauchen eine Zeile Code.

## Challenge anlegen
Datei `src/DesktopAdventures/Content/Challenges/<id>.json` (die Dateiname ohne `.json` ist die ID):

```json
{
  "name": "Streichelzoo",
  "description": "Klicke 15 Mal auf Charaktere. Sie freuen sich über jede Aufmerksamkeit.",
  "order": 20,
  "requires": ["hallo"],
  "goal": { "stat": "character_clicks", "target": 15, "unit": "Klicks" },
  "rewards": [ { "type": "unlockCharacter", "id": "merle" } ]
}
```

| Feld | Bedeutung |
|---|---|
| `name` | Titel (bis ca. 14 Zeichen, sonst gekürzt) |
| `description` | Beschreibung im Buch (bis ca. 80 Zeichen, 3 Zeilen) |
| `order` | Sortierung in der Liste, kleiner = weiter oben |
| `requires` | IDs von Challenges, die zuerst abgeschlossen sein müssen. Solange das fehlt, ist die Challenge nicht sichtbar. |
| `goal.stat` | Statistik, die zählt (siehe unten) |
| `goal.target` | Zielwert (> 0) |
| `goal.unit` | optional, Einheit in der Fortschrittsanzeige („3 / 5 Minuten“) |
| `rewards` | Liste von Belohnungen; leere Liste = nur Ehre. Typ aktuell: `unlockCharacter` mit der `id` des Charakters (= Dateiname in `Content/Characters`) |

Beim Start werden alle Challenges geprüft. Unbekannte Statistik, unbekannter Charakter, fehlende Voraussetzung usw. erscheinen als Meldung;
die betroffene Challenge wird übersprungen, der Rest läuft.

**Lebenszyklus:** unsichtbar (Voraussetzungen fehlen) → *aktiv* (Buch → Challenges → Aktiv) → *abgeschlossen*. Beim Abschluss werden die Belohnungen vergeben, eine
Meldung erscheint oben im Streifen, und neue Charaktere tauchen in der Landschaft auf und jubeln. Gesperrte Charaktere zeigen im Buch automatisch die Challenge
samt Fortschritt an, die sie freischaltet (sonst den `info.unlockHint`).

## Statistiken
Eine Statistik ist ein benannter Zähler im Spielstand. Verfügbar sind (`GameState.KnownStats`):

| Name | Zählt |
|---|---|
| `playtime_minutes` / `playtime_seconds` | Spielzeit |
| `character_clicks` | Klicks auf Charaktere in der Landschaft |
| `book_opened` | Öffnen des Buchs |
| `characters_unlocked` | freigeschaltete Charaktere (abgeleitet) |
| `challenges_completed` | abgeschlossene Challenges (abgeleitet) |

**Neue Statistik hinzufügen (Code):**
1. Namen in `GameState.KnownStats` eintragen (damit Tippfehler in Challenges auffallen).
2. An der Stelle, an der das Ereignis passiert, `_state.AddStat("mein_zaehler");` aufrufen (optional mit Betrag, z. B. `AddStat("gold", 5)`).
   Beispiele: `StripWindow.View_MouseLeftButtonDown` (Klicks), `StripWindow.ToggleBook` (Buch).
3. Danach ist `"stat": "mein_zaehler"` in jeder Challenge nutzbar.
Abgeleitete Werte (aus anderen Daten berechnet) ergänzt man in `GameState.GetStat`.

## Spielstand
- Datei: `%APPDATA%\DesktopAdventures\save.json`. Gespeichert werden freigeschaltete Charaktere, abgeschlossene Challenges, Statistiken und die Buch-Position.
- Gespeichert wird alle 20 Sekunden (wenn sich etwas geändert hat), bei jedem Challenge-Abschluss, beim Beenden und beim Herunterfahren von Windows.
  Das Schreiben ist atomar (temporäre Datei, dann ersetzen). Eine beschädigte Datei wird als `save.defekt-<datum>.json` beiseitegelegt, danach beginnt ein neuer Spielstand
  (mit Meldung).
- Charaktere mit `"startUnlocked": true` sind immer freigeschaltet.

## Testen
| Start-Option | Wirkung |
|---|---|
| `--reset-save` | löscht den Spielstand beim Start |
| `--all-characters` | alle Charaktere freigeschaltet, **nichts wird gespeichert** (Spielstand bleibt unberührt) |

Beispiel: `dotnet run --project src/DesktopAdventures -- --reset-save`.
Zum schnellen Testen kann man in einer Challenge `goal.target` klein setzen (z. B. `playtime_minutes` auf 0.2).
