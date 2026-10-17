#!/usr/bin/env python3
"""
Rastert eine Pixel-Schrift (TTF) in einen Bitmap-Font-Atlas für das Buch-UI.

    pip install pillow
    python tools/make_font.py

Ergebnis: src/DesktopAdventures/Content/Fonts/jersey10/font.png + font.json
Schrift: Jersey 10 (SIL Open Font License, siehe tools/fonts/OFL.txt). Andere Schrift/Größe:
FONT und SIZE unten ändern. Zeichensatz in CHARS.
"""
import json
from pathlib import Path
from PIL import Image, ImageDraw, ImageFont

ROOT = Path(__file__).resolve().parent
FONT = ROOT / "fonts" / "Jersey10-Regular.ttf"
SIZE = 16
OUT = ROOT.parent / "src" / "DesktopAdventures" / "Content" / "Fonts" / "jersey10"
CHARS = ("ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789"
         "!\"#$%&'()*+,-./:;<=>?@[\\]^_`{|}~ÄÖÜäöüß–—…„“”‚‘’·•×°€←→")


def main():
    font = ImageFont.truetype(str(FONT), SIZE)
    ascent, descent = font.getmetrics()
    line_height = ascent + descent
    def render(ch, adv):
        cell = Image.new("L", (adv, line_height), 0)
        d = ImageDraw.Draw(cell)
        d.fontmode = "1"
        d.text((0, 0), ch, font=font, fill=255)
        return cell

    notdef = render("\uffff", max(1, round(font.getlength("\uffff"))))
    glyphs, images = {}, []
    x = 0
    for ch in CHARS:
        adv = max(1, round(font.getlength(ch)))
        cell = render(ch, adv)
        # Glyph fehlt in der Schrift (leer oder das "Kästchen" für unbekannte Zeichen) -> weglassen
        if cell.getbbox() is None or (cell.size == notdef.size and cell.tobytes() == notdef.tobytes()):
            continue
        glyphs[ch] = [x, adv]
        images.append((x, cell))
        x += adv
    atlas = Image.new("RGBA", (x, line_height), (0, 0, 0, 0))
    for gx, cell in images:
        mask = cell.point(lambda v: 255 if v > 127 else 0)
        atlas.paste((255, 255, 255, 255), (gx, 0), mask)
    OUT.mkdir(parents=True, exist_ok=True)
    atlas.save(OUT / "font.png")
    space = max(2, round(font.getlength(" ")))
    (OUT / "font.json").write_text(json.dumps(
        {"lineHeight": line_height, "spacing": 0, "spaceWidth": space, "glyphs": glyphs},
        ensure_ascii=False, indent=1), encoding="utf-8")
    missing = [c for c in CHARS if c not in glyphs]
    print(f"{len(glyphs)} Zeichen, Zeilenhöhe {line_height}px, fehlend: {''.join(missing) or '-'}")


if __name__ == "__main__":
    main()
