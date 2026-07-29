"""
Icon set. Every icon is a white silhouette wrapped in a black keyline on a
transparent ground, matching the reference's chunky cartoon line weight. White
fill means Unity's tint gives the exact colour asked for.

Shapes are described in a normalised 0..1 box so the same source works at any
export size.
"""
import math
import os

import numpy as np
from PIL import Image, ImageDraw, ImageFilter

SS = 4
SIZE = 256
OUT = "out"
KEYLINE = 12.0
FILL = 252.0


# ------------------------------------------------------------------ helpers

def P(n, seq):
    return [(x * n, y * n) for x, y in seq]


def rot(seq, deg, cx=0.5, cy=0.5):
    a = math.radians(deg)
    return [((x - cx) * math.cos(a) - (y - cy) * math.sin(a) + cx,
             (x - cx) * math.sin(a) + (y - cy) * math.cos(a) + cy) for x, y in seq]


def bar(cx, cy, length, thick, deg):
    """Rounded-end bar as a polygon (cheap capsule)."""
    hl, ht = length / 2, thick / 2
    pts = [(cx - hl, cy - ht), (cx + hl, cy - ht), (cx + hl, cy + ht), (cx - hl, cy + ht)]
    return rot(pts, deg, cx, cy)


def dilate(mask, px):
    im = Image.fromarray((np.clip(mask, 0, 1) * 255).astype(np.uint8))
    step, left = 4, int(px)
    while left > 0:
        k = min(step, left)
        im = im.filter(ImageFilter.MaxFilter(2 * k + 1))
        left -= k
    return np.asarray(im, dtype=np.float64) / 255.0


def emit(name, fn, size=SIZE, keyline=KEYLINE):
    n = size * SS
    m = Image.new("L", (n, n), 0)
    fn(ImageDraw.Draw(m), n)
    shape = np.asarray(m, dtype=np.float64) / 255.0

    ow = keyline / 1000.0 * n
    sil = dilate(shape, ow)
    rgb = np.zeros((n, n, 3)) + 10.0                      # keyline
    rgb = rgb * (1 - shape[..., None]) + FILL * shape[..., None]

    os.makedirs(OUT, exist_ok=True)
    arr = np.dstack([np.clip(rgb, 0, 255), np.clip(sil, 0, 1) * 255]).astype(np.uint8)
    Image.fromarray(arr, "RGBA").resize((size, size), Image.LANCZOS) \
        .save(f"{OUT}/pixelflow_icon_{name}.png")


# ------------------------------------------------------------------- shapes

def gear(d, n):
    teeth, ro, ri, cx, cy = 8, 0.47, 0.355, 0.5, 0.5
    pts = []
    for i in range(teeth * 4):
        seg = i % 4
        r = ro if seg in (1, 2) else ri
        a = 2 * math.pi * (i - 0.5) / (teeth * 4)
        pts.append((cx + r * math.cos(a), cy + r * math.sin(a)))
    d.polygon(P(n, pts), fill=255)
    d.ellipse(P(n, [(0.34, 0.34), (0.66, 0.66)]), fill=255)
    d.ellipse(P(n, [(0.405, 0.405), (0.595, 0.595)]), fill=0)


def close_x(d, n):
    for a in (45, -45):
        d.polygon(P(n, bar(0.5, 0.5, 0.62, 0.17, a)), fill=255)


def _heart(scale=1.0, cy=0.47):
    pts = []
    for i in range(180):
        t = 2 * math.pi * i / 180
        x = 16 * math.sin(t) ** 3
        y = 13 * math.cos(t) - 5 * math.cos(2 * t) - 2 * math.cos(3 * t) - math.cos(4 * t)
        pts.append((0.5 + x / 34 * scale, cy - y / 34 * scale))
    return pts


def heart(d, n):
    d.polygon(P(n, _heart()), fill=255)


def heart_broken(d, n):
    d.polygon(P(n, _heart()), fill=255)
    zig = [(0.5, 0.10), (0.44, 0.34), (0.57, 0.46), (0.45, 0.60),
           (0.545, 0.72), (0.50, 0.92), (0.47, 0.92), (0.515, 0.72),
           (0.42, 0.60), (0.54, 0.46), (0.41, 0.34), (0.47, 0.10)]
    d.polygon(P(n, zig), fill=0)


def coin(d, n):
    d.ellipse(P(n, [(0.06, 0.06), (0.94, 0.94)]), fill=255)
    d.ellipse(P(n, [(0.20, 0.20), (0.80, 0.80)]), outline=0, width=int(0.028 * n))


def plus(d, n):
    d.polygon(P(n, bar(0.5, 0.5, 0.66, 0.22, 0)), fill=255)
    d.polygon(P(n, bar(0.5, 0.5, 0.66, 0.22, 90)), fill=255)


def cube(d, n):
    top, half, bot = 0.10, 0.32, 0.90
    mid_u, mid_l = 0.31, 0.69
    hexa = [(0.5, top), (0.5 + half, mid_u), (0.5 + half, mid_l),
            (0.5, bot), (0.5 - half, mid_l), (0.5 - half, mid_u)]
    d.polygon(P(n, hexa), fill=255)
    w = int(0.022 * n)
    d.line(P(n, [(0.5, 0.50), (0.5 + half, mid_u)]), fill=0, width=w)
    d.line(P(n, [(0.5, 0.50), (0.5 - half, mid_u)]), fill=0, width=w)
    d.line(P(n, [(0.5, 0.50), (0.5, bot)]), fill=0, width=w)


def trophy(d, n):
    # handles first, so the bowl draws over where they meet it
    d.arc(P(n, [(0.06, 0.14), (0.40, 0.52)]), start=80, end=280,
          fill=255, width=int(0.060 * n))
    d.arc(P(n, [(0.60, 0.14), (0.94, 0.52)]), start=260, end=100,
          fill=255, width=int(0.060 * n))
    bowl = [(0.22, 0.10), (0.78, 0.10), (0.76, 0.36), (0.70, 0.52),
            (0.60, 0.62), (0.40, 0.62), (0.30, 0.52), (0.24, 0.36)]
    d.polygon(P(n, bowl), fill=255)
    d.rectangle(P(n, [(0.43, 0.60), (0.57, 0.76)]), fill=255)
    d.rounded_rectangle(P(n, [(0.26, 0.76), (0.74, 0.90)]), radius=int(0.045 * n), fill=255)


def skull(d, n):
    d.ellipse(P(n, [(0.13, 0.10), (0.87, 0.72)]), fill=255)
    d.rounded_rectangle(P(n, [(0.28, 0.56), (0.72, 0.88)]), radius=int(0.08 * n), fill=255)
    d.ellipse(P(n, [(0.25, 0.30), (0.45, 0.52)]), fill=0)
    d.ellipse(P(n, [(0.55, 0.30), (0.75, 0.52)]), fill=0)
    d.polygon(P(n, [(0.5, 0.53), (0.565, 0.65), (0.435, 0.65)]), fill=0)
    for x in (0.40, 0.50, 0.60):
        d.line(P(n, [(x, 0.72), (x, 0.88)]), fill=0, width=int(0.020 * n))


def speaker(d, n):
    d.polygon(P(n, [(0.10, 0.36), (0.26, 0.36), (0.46, 0.16), (0.46, 0.84),
                    (0.26, 0.64), (0.10, 0.64)]), fill=255)
    for i, r in enumerate((0.16, 0.27, 0.38)):
        d.arc(P(n, [(0.52 - r + 0.06, 0.5 - r), (0.52 + r + 0.06, 0.5 + r)]),
              start=-58, end=58, fill=255, width=int((0.050 - i * 0.006) * n))


def bell(d, n):
    d.pieslice(P(n, [(0.18, 0.14), (0.82, 0.86)]), start=180, end=360, fill=255)
    d.rectangle(P(n, [(0.18, 0.50), (0.82, 0.70)]), fill=255)
    d.rounded_rectangle(P(n, [(0.10, 0.66), (0.90, 0.78)]), radius=int(0.05 * n), fill=255)
    d.ellipse(P(n, [(0.43, 0.80), (0.57, 0.94)]), fill=255)
    d.ellipse(P(n, [(0.45, 0.06), (0.55, 0.16)]), fill=255)


def phone(d, n):
    d.rounded_rectangle(P(n, [(0.34, 0.10), (0.66, 0.90)]), radius=int(0.07 * n), fill=255)
    d.rounded_rectangle(P(n, [(0.395, 0.20), (0.605, 0.78)]), radius=int(0.025 * n), fill=0)
    for sx in (-1, 1):
        for off, ln in ((0.235, 0.13), (0.325, 0.22)):
            x = 0.5 + sx * off
            d.line(P(n, [(x, 0.5 - ln / 2), (x, 0.5 + ln / 2)]), fill=255,
                   width=int(0.042 * n))


def shield(d, n):
    d.polygon(P(n, [(0.5, 0.06), (0.88, 0.20), (0.88, 0.52),
                    (0.5, 0.94), (0.12, 0.52), (0.12, 0.20)]), fill=255)


def chat(d, n):
    d.rounded_rectangle(P(n, [(0.08, 0.14), (0.92, 0.70)]), radius=int(0.16 * n), fill=255)
    d.polygon(P(n, [(0.30, 0.62), (0.52, 0.62), (0.34, 0.92)]), fill=255)
    for x in (0.30, 0.50, 0.70):
        d.ellipse(P(n, [(x - 0.065, 0.36), (x + 0.065, 0.49)]), fill=0)


def floppy(d, n):
    d.rounded_rectangle(P(n, [(0.10, 0.10), (0.90, 0.90)]), radius=int(0.06 * n), fill=255)
    d.rectangle(P(n, [(0.30, 0.10), (0.70, 0.40)]), fill=0)
    d.rectangle(P(n, [(0.40, 0.14), (0.56, 0.34)]), fill=255)
    d.rounded_rectangle(P(n, [(0.24, 0.54), (0.76, 0.90)]), radius=int(0.03 * n), fill=0)
    for y in (0.63, 0.73):
        d.rectangle(P(n, [(0.32, y), (0.68, y + 0.05)]), fill=255)


def store(d, n):
    d.rounded_rectangle(P(n, [(0.16, 0.44), (0.84, 0.90)]), radius=int(0.05 * n), fill=255)
    d.polygon(P(n, [(0.04, 0.44), (0.14, 0.12), (0.86, 0.12), (0.96, 0.44)]), fill=255)
    for i in range(1, 5):                       # awning stripes
        x = 0.04 + 0.92 * i / 5
        d.line(P(n, [(x - 0.03, 0.14), (x, 0.44)]), fill=0, width=int(0.024 * n))
    d.rounded_rectangle(P(n, [(0.38, 0.62), (0.62, 0.90)]), radius=int(0.03 * n), fill=0)


def lock(d, n):
    d.arc(P(n, [(0.26, 0.08), (0.74, 0.62)]), start=180, end=360,
          fill=255, width=int(0.095 * n))
    d.rounded_rectangle(P(n, [(0.16, 0.42), (0.84, 0.92)]), radius=int(0.10 * n), fill=255)
    d.ellipse(P(n, [(0.43, 0.56), (0.57, 0.70)]), fill=0)
    d.polygon(P(n, [(0.465, 0.64), (0.535, 0.64), (0.515, 0.80), (0.485, 0.80)]), fill=0)


def pencil(d, n):
    body = rot([(0.40, 0.14), (0.62, 0.14), (0.62, 0.74), (0.51, 0.90), (0.40, 0.74)], 35)
    d.polygon(P(n, body), fill=255)
    band = rot([(0.40, 0.26), (0.62, 0.26), (0.62, 0.33), (0.40, 0.33)], 35)
    d.polygon(P(n, band), fill=0)


def refresh(d, n):
    box = P(n, [(0.16, 0.16), (0.84, 0.84)])
    d.arc(box, start=25, end=300, fill=255, width=int(0.115 * n))
    d.polygon(P(n, [(0.72, 0.06), (0.95, 0.30), (0.63, 0.36)]), fill=255)


def play(d, n):
    d.polygon(P(n, [(0.24, 0.10), (0.86, 0.50), (0.24, 0.90)]), fill=255)


def diamond(d, n):
    d.polygon(P(n, rot([(0.28, 0.28), (0.72, 0.28), (0.72, 0.72), (0.28, 0.72)], 45)),
              fill=255)


def star(d, n):
    pts = []
    for i in range(10):
        r = 0.46 if i % 2 == 0 else 0.20
        a = math.pi / 2 * 3 + math.pi * i / 5
        pts.append((0.5 + r * math.cos(a), 0.5 + r * math.sin(a)))
    d.polygon(P(n, pts), fill=255)


def hand(d, n):
    """Pointing hand — the tap/hint power-up."""
    d.rounded_rectangle(P(n, [(0.43, 0.05), (0.60, 0.52)]), radius=int(0.085 * n), fill=255)
    d.rounded_rectangle(P(n, [(0.30, 0.40), (0.79, 0.95)]), radius=int(0.15 * n), fill=255)
    d.rounded_rectangle(P(n, [(0.17, 0.53), (0.42, 0.75)]), radius=int(0.10 * n), fill=255)
    for y in (0.55, 0.68):                      # knuckle creases
        d.line(P(n, [(0.62, y), (0.76, y)]), fill=0, width=int(0.020 * n))


def dog(d, n, eyes="open"):
    """The Cubic_Dog mascot flattened to a 2D mark — game's own character."""
    def R(box, r):
        d.rounded_rectangle([(box[0] * n, box[1] * n), (box[2] * n, box[3] * n)],
                            radius=int(r * n), fill=255)

    def E(cx, cy, rx, ry):
        d.ellipse([((cx - rx) * n, (cy - ry) * n), ((cx + rx) * n, (cy + ry) * n)], fill=0)

    R((0.05, 0.22, 0.30, 0.70), 0.12)          # ears
    R((0.70, 0.22, 0.95, 0.70), 0.12)
    R((0.24, 0.40, 0.42, 0.93), 0.08)          # legs
    R((0.58, 0.40, 0.76, 0.93), 0.08)
    R((0.15, 0.09, 0.85, 0.79), 0.22)          # head

    w = max(int(0.016 * n), 2)

    def open_eye(cx):
        E(cx, 0.40, 0.075, 0.095)

    def closed_eye(cx):
        d.arc([((cx - 0.085) * n, 0.34 * n), ((cx + 0.085) * n, 0.50 * n)],
              180, 360, fill=0, width=int(0.030 * n))

    left, right = {"open": (open_eye, open_eye),
                   "wink": (open_eye, closed_eye),
                   "happy": (closed_eye, closed_eye)}[eyes]
    left(0.355)
    right(0.645)

    R((0.36, 0.50, 0.64, 0.74), 0.10)          # muzzle sits over the head
    d.rounded_rectangle([(0.36 * n, 0.50 * n), (0.64 * n, 0.74 * n)],
                        radius=int(0.10 * n), outline=0, width=w)
    E(0.50, 0.575, 0.055, 0.042)               # nose
    d.line([(0.50 * n, 0.60 * n), (0.50 * n, 0.71 * n)], fill=0, width=w)


ICONS = dict(gear=gear, close=close_x, heart=heart, heart_broken=heart_broken,
             coin=coin, plus=plus, cube=cube, trophy=trophy, skull=skull,
             speaker=speaker, bell=bell, phone=phone, shield=shield, chat=chat,
             floppy=floppy, store=store, lock=lock, pencil=pencil,
             refresh=refresh, play=play, diamond=diamond, star=star, hand=hand,
             dog=dog)

if __name__ == "__main__":
    for name, fn in ICONS.items():
        emit(name, fn)
    print(f"{len(ICONS)} icons written")
