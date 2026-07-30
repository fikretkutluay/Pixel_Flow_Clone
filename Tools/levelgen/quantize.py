"""Turn a painting into a board of cubes.

The source is a normal high-resolution photograph of a public-domain painting,
not pixel art: downscaling with area averaging is what turns it into pixels, and
mapping to the game palette in CIELAB is what keeps the result recognisable.
Nearest-in-RGB looks fine on paper and falls apart on dark tones, which is most
of what these paintings are made of.

Crate is never a target colour. A crate cannot be broken, so a stray brown pixel
landing on it would quietly make a level unsolvable.

Run:  python quantize.py src/Mondrian.jpeg --height 12
"""
import argparse

import levelio
import colorsys
import glob
import math
import os
import re

from PIL import Image, ImageEnhance

PALETTE_ASSET = "../../Assets/_Game/Data/Config/ColorPalette.asset"

# Sandık (1) ve boş (0) hedef renk DEĞİL.
EXCLUDED = {0, 1}


def load_palette():
    text = open(os.path.join(os.path.dirname(__file__), PALETTE_ASSET),
                encoding="utf8").read()
    out = {}
    for m in re.finditer(r"- id: (\d+)\s+color: \{r: ([\d.]+), g: ([\d.]+), b: ([\d.]+)",
                         text):
        cid = int(m.group(1))
        if cid in EXCLUDED:
            continue
        out[cid] = tuple(float(m.group(i)) for i in (2, 3, 4))
    return out


def to_lab(c):
    def f(u):
        return u / 12.92 if u <= 0.04045 else ((u + 0.055) / 1.055) ** 2.4
    r, g, b = [f(x) for x in c]
    X = (0.4124 * r + 0.3576 * g + 0.1805 * b) / 0.95047
    Y = 0.2126 * r + 0.7152 * g + 0.0722 * b
    Z = (0.0193 * r + 0.1192 * g + 0.9505 * b) / 1.08883

    def h(t):
        return t ** (1 / 3) if t > 0.008856 else 7.787 * t + 16 / 116
    X, Y, Z = h(X), h(Y), h(Z)
    return (116 * Y - 16, 500 * (X - Y), 200 * (Y - Z))


def board_size_for_cells(img, cells):
    """Hedef küp sayısını en-boy oranını bozmadan bir board boyutuna çevirir."""
    ar = img.width / img.height
    h = max(4, round((cells / ar) ** 0.5))
    return max(4, round(h * ar)), h


def board_size(img, height=None, width=None):
    """Tablonun en-boy oranını koru — kareye zorlamak resmi eziyor."""
    ar = img.width / img.height
    if height and not width:
        return max(4, round(height * ar)), height
    if width and not height:
        return width, max(4, round(width / ar))
    return width, height


def quantize(path, w, h, palette, saturation=1.0, colours=None,
             max_share=None, ramp_ids=None):
    img = Image.open(path).convert("RGB")
    # BOX = alan ortalaması. Downscale'de doğru filtre bu; LANCZOS keskinlik
    # için halka (ringing) üretir ve tek piksellik hücrelerde gürültüye döner.
    # Once hucre basina birkac ornek alacak kadar kucult, SONRA her pikseli
    # kuantala ve hucrede EN COK GECEN rengi sec. Duz alan ortalamasi sert
    # kenarlari yok ediyordu: Mondrian'in ince siyah cizgileri beyazla
    # ortalanip griye donuyordu, oysa tabloda hic gri yok.
    ss = max(2, min(6, img.width // w, img.height // h))
    small = img.resize((w * ss, h * ss), Image.BOX)

    # Tablolar mat, palet doygun. Doygunluk artirilmazsa mat boya tonlari en
    # yakin komsu olarak notr grileri buluyor ve resim gri lapaya donuyor —
    # olculdu: Kanagawa %70 LightGray, Kupeli Kiz %69 Black.
    if saturation != 1.0:
        small = ImageEnhance.Color(small).enhance(saturation)

    lab_pal = {cid: to_lab(c) for cid, c in palette.items()}
    cache = {}

    def nearest(rgb):
        if rgb not in cache:
            lab = to_lab(tuple(v / 255 for v in rgb))
            cache[rgb] = min(lab_pal, key=lambda cid: math.dist(lab, lab_pal[cid]))
        return cache[rgb]

    px = small.load()
    pixels = []
    lums = []            # hucrenin KAYNAKTAKI parlakligi — max_share bunu kullanir
    for y in range(h):
        for x in range(w):
            votes = {}
            total = 0.0
            for dy in range(ss):
                for dx in range(ss):
                    rgb = px[x * ss + dx, y * ss + dy]
                    cid = nearest(rgb)
                    votes[cid] = votes.get(cid, 0) + 1
                    total += 0.299 * rgb[0] + 0.587 * rgb[1] + 0.114 * rgb[2]
            pixels.append(max(votes, key=votes.get))
            lums.append(total / (ss * ss))

    if max_share:
        pixels = spread(pixels, lums, max_share, lab_pal, palette, ramp_ids)

    if colours:
        pixels = restrict(pixels, colours, lab_pal)
    return pixels


def spread(pixels, lums, max_share, lab_pal, palette, ramp_ids=None):
    """Bir rengin board'u ele gecirmesini engeller.

    Vermeer'in zemini olculdu: 418 hucrenin 304'u (%73) Black. Tek renge bagimli
    bir level hem sikici hem de o rengin aticisi disinda hicbir sey yaptirmiyor.

    Rastgele serpmek yerine hucrelerin KAYNAKTAKI parlakligi kullaniliyor: gercek
    tabloda o zemin duz degil, tonlamasi var. En koyu hucreler rengi korur,
    acilanlar en yakin komsu tonlara tasinir — sonuc gurultu degil, tablonun
    kendi golgelendirmesi.
    """
    n = len(pixels)
    counts = {}
    for p in pixels:
        counts[p] = counts.get(p, 0) + 1

    out = list(pixels)
    for colour, count in sorted(counts.items(), key=lambda kv: -kv[1]):
        if count <= max_share * n:
            continue

        # Rampa ACIKCA verilir. Otomatik "en yakin komsu" secimi Vermeer'de
        # zemini Navy'ye tasidi ve turbanla ayni renk oldu — ozne zeminden
        # ayirt edilemez hale geliyordu.
        if ramp_ids:
            neighbours = [c for c in ramp_ids if c != colour]
        else:
            neighbours = sorted((cid for cid in lab_pal if cid != colour),
                                key=lambda cid: math.dist(lab_pal[colour],
                                                          lab_pal[cid]))[:3]
        ramp = [colour] + sorted(neighbours, key=lambda cid: lab_pal[cid][0])

        idx = sorted((i for i, p in enumerate(pixels) if p == colour),
                     key=lambda i: lums[i])
        keep = int(max_share * n)
        rest = idx[keep:]                       # en acik hucreler tasinir
        for j, i in enumerate(rest):
            out[i] = ramp[1 + j * (len(ramp) - 1) // max(1, len(rest))]
    return out


def restrict(pixels, keep_n, lab_pal):
    """Paleti en cok kullanilan keep_n renge indirger, kalanlari en yakina tasir.

    Bir level'in her rengi ayri bir atici rengi demek: 9 renkli bir board hem
    okunmaz hem de kuyrugu yonetilemez hale getirir.
    """
    counts = {}
    for p in pixels:
        counts[p] = counts.get(p, 0) + 1
    keep = sorted(counts, key=lambda c: -counts[c])[:keep_n]

    remap = {}
    for cid in counts:
        if cid in keep:
            remap[cid] = cid
        else:
            remap[cid] = min(keep, key=lambda k: math.dist(lab_pal[cid], lab_pal[k]))
    return [remap[p] for p in pixels]


def report(name, pixels, w, h):
    counts = {}
    for p in pixels:
        counts[p] = counts.get(p, 0) + 1

    print("=== %s   %dx%d = %d kup" % (name, w, h, w * h))
    for y in range(h - 1, -1, -1):
        print("   " + "".join(levelio.GLYPH[pixels[y * w + x]] for x in range(w)))
    print("   renkler (%d farkli):" % len(counts))
    for cid in sorted(counts, key=lambda c: -counts[c]):
        print("     %-11s %4d  %%%.0f"
              % (levelio.COLORS[cid], counts[cid], 100 * counts[cid] / len(pixels)))


def main():
    ap = argparse.ArgumentParser()
    ap.add_argument("images", nargs="+")
    ap.add_argument("--height", type=int, default=18)
    ap.add_argument("--width", type=int)
    ap.add_argument("--saturation", type=float, default=1.0)
    ap.add_argument("--colors", type=int, help="board'u N renge indirge")
    ap.add_argument("--max-share", type=float,
                    help="tek bir renk board'un en fazla bu oranini kaplasin")
    ap.add_argument("--bg-ramp", default="Black,DarkGray,LightGray",
                    help="max-share asilinca tasinacak tonlar")
    ap.add_argument("--quiet", action="store_true",
                    help="board'u cizme, sadece renk dagilimini yaz")
    args = ap.parse_args()

    palette = load_palette()
    for pattern in args.images:
        for path in sorted(glob.glob(pattern)):
            img = Image.open(path)
            w, h = board_size(img, args.height, args.width)
            pixels = quantize(path, w, h, palette, args.saturation, args.colors,
                              args.max_share,
                              [levelio.ID_OF[n] for n in args.bg_ramp.split(",")]
                              if args.bg_ramp else None)
            name = os.path.basename(path)
            if args.quiet:
                counts = {}
                for p in pixels:
                    counts[p] = counts.get(p, 0) + 1
                top = sorted(counts, key=lambda c: -counts[c])
                print("%-46s %2dx%-2d  %2d renk  baskin: %s"
                      % (name, w, h, len(counts),
                         ", ".join("%s %%%.0f" % (levelio.COLORS[c],
                                                  100 * counts[c] / len(pixels))
                                   for c in top[:3])))
            else:
                report(name, pixels, w, h)
                print()


if __name__ == "__main__":
    main()
