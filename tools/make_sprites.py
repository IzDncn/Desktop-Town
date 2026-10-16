#!/usr/bin/env python3
"""
Erzeugt die mitgelieferten Basis-Sprites (Templates) für Desktop Adventures.

    pip install pillow
    python tools/make_sprites.py

Die PNGs sind normale Pixel-Art und können in jedem Editor (Aseprite, LibreSprite, GIMP ...) weiterbearbeitet
werden. Statt fester Farben benutzen sie MARKER-FARBEN: Jede Farbfamilie hat 3 Töne (hell/mittel/dunkel) und
wird im Spiel pro Charakter durch eine Palette ersetzt. Alle anderen Farben bleiben unverändert.
Siehe docs/CHARAKTERE.md.
"""
from pathlib import Path
from PIL import Image

ROOT = Path(__file__).resolve().parent.parent / "src" / "DesktopAdventures" / "Content" / "Templates"

# Marker-Farben (hell, mittel, dunkel) - muss zu SlotMarkers.cs passen
M = {
    "red":     ((255, 153, 153), (255, 85, 85),  (170, 34, 34)),
    "green":   ((153, 255, 153), (85, 255, 85),  (34, 170, 34)),
    "blue":    ((153, 153, 255), (85, 85, 255),  (34, 34, 170)),
    "yellow":  ((255, 255, 153), (255, 255, 85), (170, 170, 34)),
    "magenta": ((255, 153, 255), (255, 85, 255), (170, 34, 170)),
    "cyan":    ((153, 255, 255), (85, 255, 255), (34, 170, 170)),
}
HI, MID, LO = 0, 1, 2
OUTLINE = (42, 28, 36, 255)


class Canvas:
    def __init__(self, w, h, ox, oy):
        self.img = Image.new("RGBA", (w, h), (0, 0, 0, 0))
        self.ox, self.oy = ox, oy

    def px(self, x, y, color):
        if isinstance(color, tuple) and len(color) == 3:
            color = color + (255,)
        X, Y = x + self.ox, y + self.oy
        if 0 <= X < self.img.width and 0 <= Y < self.img.height:
            self.img.putpixel((X, Y), color)

    def rect(self, x, y, w, h, color):
        for j in range(h):
            for i in range(w):
                self.px(x + i, y + j, color)

    def erase(self, x, y):
        X, Y = x + self.ox, y + self.oy
        if 0 <= X < self.img.width and 0 <= Y < self.img.height:
            self.img.putpixel((X, Y), (0, 0, 0, 0))

    def outline(self):
        src = self.img.copy()
        for y in range(src.height):
            for x in range(src.width):
                if src.getpixel((x, y))[3] != 0:
                    continue
                for dx, dy in ((1, 0), (-1, 0), (0, 1), (0, -1)):
                    nx, ny = x + dx, y + dy
                    if 0 <= nx < src.width and 0 <= ny < src.height and src.getpixel((nx, ny))[3] != 0:
                        self.img.putpixel((x, y), OUTLINE)
                        break
        return self.img


def c(family, shade):
    return M[family][shade]


# ------------------------------------------------------------------ Humanoid
HW, HH = 20, 30
HOX, HOY = 2, 6
SKIN, HAIR, SHIRT, PANTS, ACCENT, BOOTS = "red", "green", "blue", "yellow", "magenta", "cyan"

SHIFT_F = [1, 2, 1, -1, -2, -1]
LIFT_F = [1, 0, 0, 0, 0, 1]
LIFT_B = [0, 0, 1, 1, 0, 0]
ARM_DX = [0, -1, 0, 0, 1, 0]


def pose(anim, k):
    p = dict(hb=0, fx=0, bx=0, lf=0, lb=0, arm=0, wave=False, hand=0)
    if anim == "idle":
        p["hb"] = [0, 0, 1, 1][k]
    elif anim == "walk":
        p.update(hb=-1 if k in (0, 3) else 0, fx=SHIFT_F[k], bx=-SHIFT_F[k],
                 lf=LIFT_F[k], lb=LIFT_B[k], arm=ARM_DX[k])
    elif anim == "react":
        p.update(wave=True, hand=[0, 1, 0, -1][k], hb=0)
    return p


def humanoid_base(p):
    cv = Canvas(HW, HH, HOX, HOY)
    b = p["hb"]
    # Beine (hinteres dunkler)
    for x0, dx, lift, back in ((5, p["bx"], p["lb"], True), (8, p["fx"], p["lf"], False)):
        x = x0 + dx
        cv.rect(x, 16, 3, 5 - lift, c(PANTS, LO if back else MID))
        cv.rect(x, 21 - lift, 4, 1, c(BOOTS, MID))
        cv.rect(x, 22 - lift, 4, 1, c(BOOTS, LO))
    # Torso
    cv.rect(4, 11 + b, 8, 6, c(SHIRT, MID))
    cv.rect(4, 11 + b, 8, 1, c(SHIRT, HI))
    cv.rect(4, 12 + b, 1, 5, c(SHIRT, LO))
    cv.rect(4, 16 + b, 8, 1, c(SHIRT, LO))
    # Kopf
    cv.rect(3, 2 + b, 10, 9, c(SKIN, MID))
    for x, y in ((3, 2), (12, 2), (3, 10), (12, 10)):
        cv.erase(x, y + b)
    cv.rect(4, 10 + b, 8, 1, c(SKIN, LO))
    # Haare
    cv.rect(4, 2 + b, 8, 1, c(HAIR, HI))
    cv.rect(3, 3 + b, 10, 2, c(HAIR, MID))
    cv.rect(3, 5 + b, 3, 5, c(HAIR, MID))
    cv.rect(3, 5 + b, 1, 5, c(HAIR, LO))
    cv.rect(6, 5 + b, 4, 1, c(HAIR, MID))
    cv.erase(3, 3 + b); cv.erase(12, 3 + b)
    # Augen
    for x in (9, 11):
        cv.rect(x, 7 + b, 1, 2, OUTLINE)
    # Arm
    if p["wave"]:
        cv.rect(12, 11 + b, 2, 2, c(SHIRT, LO))
        cv.rect(13, 9 + b, 2, 2, c(SHIRT, LO))
        cv.rect(13 + p["hand"], 7 + b, 2, 2, c(SKIN, MID))
    else:
        ax = 7 + p["arm"]
        cv.rect(ax, 12 + b, 2, 4, c(SHIRT, LO))
        cv.rect(ax, 16 + b, 2, 1, c(SKIN, MID))
    return cv.outline()


def layer_cap(p):
    cv = Canvas(HW, HH, HOX, HOY)
    b = p["hb"]
    cv.rect(4, 1 + b, 8, 1, c(ACCENT, HI))
    cv.rect(3, 2 + b, 10, 2, c(ACCENT, MID))
    cv.rect(7, 4 + b, 8, 1, c(ACCENT, LO))
    return cv.outline()


def layer_wizard(p):
    cv = Canvas(HW, HH, HOX, HOY)
    b = p["hb"]
    starts = [4, 4, 5, 5, 6, 7, 8]
    widths = [8, 8, 6, 6, 4, 2, 2]
    for i in range(7):
        y = 2 - i + b
        for j in range(widths[i]):
            cv.px(starts[i] + j, y, c(ACCENT, LO if j < 2 else MID))
    cv.rect(1, 3 + b, 14, 1, c(ACCENT, MID))
    cv.rect(1, 4 + b, 14, 1, c(ACCENT, LO))
    cv.rect(4, 2 + b, 8, 1, c(ACCENT, HI))
    return cv.outline()


def layer_beard(p):
    cv = Canvas(HW, HH, HOX, HOY)
    b = p["hb"]
    cv.rect(6, 9 + b, 7, 2, c(HAIR, MID))
    cv.rect(7, 11 + b, 5, 1, c(HAIR, MID))
    cv.rect(8, 12 + b, 4, 1, c(HAIR, LO))
    cv.rect(9, 13 + b, 2, 1, c(HAIR, LO))
    cv.rect(6, 9 + b, 1, 2, c(HAIR, LO))
    cv.erase(12, 10 + b)
    cv.rect(12, 9 + b, 1, 1, c(SKIN, MID)) if False else None
    return cv.outline()


def layer_scarf(p):
    cv = Canvas(HW, HH, HOX, HOY)
    b = p["hb"]
    cv.rect(4, 11 + b, 8, 1, c(ACCENT, HI))
    cv.rect(4, 12 + b, 8, 1, c(ACCENT, MID))
    cv.rect(2, 12 + b, 2, 4, c(ACCENT, LO))
    return cv.outline()


HUMANOID_ANIMS = [("idle", 4), ("walk", 6), ("react", 4)]


def build_sheet(frame_w, frame_h, anims, fn, cols):
    sheet = Image.new("RGBA", (frame_w * cols, frame_h * len(anims)), (0, 0, 0, 0))
    for row, (name, n) in enumerate(anims):
        for k in range(n):
            sheet.paste(fn(name, k), (k * frame_w, row * frame_h))
    return sheet


# ------------------------------------------------------------------ Katze
CW, CH = 20, 17
COX, COY = 0, 3
FUR, BELLY, NOSE, COLLAR, EYES = "red", "green", "magenta", "yellow", "blue"


def cat_pose(anim, k):
    p = dict(dy=0, sa=0, la=0, sb=0, lb=0, tail=0, blink=False, up=False)
    if anim == "idle":
        p.update(tail=[0, 1, 0, -1][k], blink=(k == 3))
    elif anim == "walk":
        sa = [-1, 0, 1, 0][k]
        p.update(sa=sa, sb=-sa, la=[0, 1, 0, 0][k], lb=[0, 0, 0, 1][k], dy=-1 if k in (1, 3) else 0,
                 tail=[0, 1, 0, 1][k])
    elif anim == "react":
        p.update(dy=[0, -2, -3, -1][k], up=True, tail=[1, 1, 0, 1][k], la=0)
    return p


def cat_frame(p):
    cv = Canvas(CW, CH, COX, COY)
    dy = p["dy"]
    legs_h = 4
    # Schwanz (hinter dem Körper)
    tx = 3 + p["tail"]
    pts = [(4, 7), (3, 6), (2, 5), (tx, 4), (tx, 3)] if not p["up"] else [(4, 7), (3, 6), (3, 5), (3, 4), (tx, 3), (tx, 2)]
    for x, y in pts:
        cv.rect(x, y + dy, 2, 2, c(FUR, LO))
    # Beine: hinten/vorn, je zwei
    for x0, dx, lift, far in ((5, p["sa"], p["la"], True), (7, p["sb"], p["lb"], False),
                              (10, p["sb"], p["lb"], True), (12, p["sa"], p["la"], False)):
        h = max(1, legs_h - lift - (2 if p["up"] else 0))
        cv.rect(x0 + dx, 9 + dy, 2, h, c(FUR, LO if far else MID))
        cv.rect(x0 + dx, 9 + dy + h - 1, 2, 1, c(FUR, HI) if not far else c(FUR, MID))
    # Körper
    cv.rect(4, 5 + dy, 10, 5, c(FUR, MID))
    cv.rect(5, 5 + dy, 8, 1, c(FUR, HI))
    cv.rect(4, 9 + dy, 10, 1, c(FUR, LO))
    cv.rect(6, 8 + dy, 6, 2, c(BELLY, MID))
    for x, y in ((4, 5), (13, 5), (4, 9), (13, 9)):
        cv.erase(x, y + dy)
    # Kopf
    cv.rect(12, 3 + dy, 6, 5, c(FUR, MID))
    cv.rect(13, 3 + dy, 4, 1, c(FUR, HI))
    cv.rect(12, 7 + dy, 6, 1, c(FUR, LO))
    cv.erase(12, 3 + dy); cv.erase(17, 3 + dy)
    # Ohren
    for x, y in ((13, 2), (13, 1), (14, 2), (17, 2), (17, 1), (16, 2)):
        cv.px(x, y + dy, c(FUR, MID))
    cv.px(14, 1 + dy, c(NOSE, MID)) if False else None
    # Gesicht
    if p["blink"]:
        cv.px(15, 5 + dy, c(FUR, LO)); cv.px(17, 5 + dy, c(FUR, LO))
    else:
        cv.px(15, 5 + dy, c(EYES, MID)); cv.px(17, 5 + dy, c(EYES, MID))
    cv.px(18, 6 + dy, c(NOSE, MID))
    return cv.outline()


CAT_ANIMS = [("idle", 4), ("walk", 4), ("react", 4)]


def main():
    h = ROOT / "humanoid"
    sheet = build_sheet(HW, HH, HUMANOID_ANIMS, lambda a, k: humanoid_base(pose(a, k)), 6)
    sheet.save(h / "sheet.png")
    for name, fn in (("cap", layer_cap), ("wizard_hat", layer_wizard), ("beard", layer_beard), ("scarf", layer_scarf)):
        build_sheet(HW, HH, HUMANOID_ANIMS, lambda a, k, fn=fn: fn(pose(a, k)), 6).save(h / "layers" / f"{name}.png")
    build_sheet(CW, CH, CAT_ANIMS, lambda a, k: cat_frame(cat_pose(a, k)), 4).save(ROOT / "cat" / "sheet.png")
    print("Sprites geschrieben nach", ROOT)


if __name__ == "__main__":
    main()
