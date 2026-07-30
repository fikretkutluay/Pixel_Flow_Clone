"""Build all ten levels: painting -> board -> queue tuned to a difficulty target.

The board comes straight out of quantize.py. The queue is searched for, because
difficulty in this game lives almost entirely in the queue:

  * ammo per shooter — a shooter must spend its ammo inside ONE lap or it parks,
    so fewer shooters carrying more ammo is harder, not easier
  * column order — columns are FIFO and only their fronts are tappable, so
    burying a needed colour costs the player track slots

Ammo only ever comes in 10 / 20 / 40, matching the reference game. That makes the
denomination itself the main difficulty dial: a colour split into 40s parks far
more often than the same colour split into 20s.

Every candidate queue is played by sim.play_best() and kept only if it lands in
the level's target park band. That is the automated form of the GDD's "prove each
level is winnable over at least three playthroughs".
"""
import hashlib
import os
import random

from PIL import Image

import levelio
import quantize
import sim
from levelio import ID_OF

LEVELS_DIR = "../../Assets/_Game/Data/Levels"

# Board 1000+ küpte tutulduğu için ilk iki level'ın tek işi mekaniği öğretmek;
# o iş az renkli, tek özneli sade bir resimle çok daha iyi oluyor.
FIRST_LEVEL = 1
SCRIPT_GUID = "271bf4ec02b1c314db3e1e139e1a6603"

AMMO_STEPS = (10, 20, 40)

SATURATION = 1.8
MAX_SHARE = 0.34
BG_RAMP = [ID_OF["Black"], ID_OF["DarkGray"], ID_OF["LightGray"]]

# (dosya, hedef küp, renk, sütun, hedef zirve park, sandık, gizli atıcı, max_share)
#
# max_share=None olan level'larda zemin tek renk kalır. Level 1-2 için bu
# ISTENEN durum: baskın rengin atıcısı turunda her zaman eşleşme bulur, mermisini
# bitirir ve parka hiç düşmez.
#
# Renk sayısı erken level'ların ASIL kolaylık kolu. 1000+ küpte 6 renkle bir
# atıcı turunda kendi renginden yeterince küp bulamıyor ve parka düşüyor —
# ölçüldü: Level_1 hedefi 0-1 iken 6 renkle 3/5'e çıkıyordu. Az renk, aynı
# board'da daha çok eşleşme demek.
SPECS = [
    # max_share yalnızca baskın rengin gerçekten ARKA PLAN olduğu tablolarda
    # açık. Ayçiçekleri'nde sarı öznenin kendisiydi ve kırpma board'un %63'ünü
    # gri/siyaha çevirip tabloyu tanınmaz hale getirmişti; o yüzden tablo başına
    # veriliyor, tek bir genel değer olarak değil.
    ("apple(level1).jpeg",                              1000, 3, 2, (0, 0),  0, 0, None),
    ("lemon(level2).jpeg",                              1000, 3, 2, (0, 1),  0, 0, None),
    ("Mondrian.jpeg",                                   1024, 4, 3, (0, 1),  0, 0, None),
    ("Klimt.jpeg",                                      1050, 4, 3, (1, 2),  0, 0, None),
    # Kanagawa'da baskın Khaki, dalganın rengi değil kağıdın sararmışlığı —
    # kırpmak dalgayı ortaya çıkarıyor.
    ("Great_Wave_off_Kanagawa2.jpg",                    1100, 5, 4, (2, 3),  0, 0, 0.42),
    ("The_Scream.jpg",                                  1150, 5, 4, (3, 3), 10, 0, None),
    ("Hiroshige.jpeg",                                  1200, 5, 4, (4, 4), 14, 0, None),
    ("Caspar_David_Friedrich_-_Wanderer_above_the_sea_of_fog.jpg",
                                                        1250, 5, 4, (3, 3),  0, 3, 0.40),
    ("Van_Gogh_-_Starry_Night_-_Google_Art_Project.jpg", 1300, 6, 4, (5, 5),  0, 5, None),
    ("Vermeer_-_The_Milkmaid.jpg",                       1350, 6, 4, (4, 4), 12, 4, MAX_SHARE),
    ("Girl_with_a_Pearl_Earring.jpg",                    1450, 6, 4, (4, 4), 12, 4, MAX_SHARE),
    ("mona_lisa_.jpg",                                   1500, 6, 4, (4, 5), 16, 5, MAX_SHARE),
]



def guid_for(name):
    return hashlib.md5(("pixelflow::level::" + name).encode()).hexdigest()


def split_ammo(n, step):
    """n küpü 10/20/40'lık atıcılara böler; toplam her zaman n'e YETER.

    Büyük adım (40) daha az ama daha dolu atıcı demek — biri turunda rengini
    bulamazsa doğrudan parka düşer. Zorluğun asıl kaynağı bu.
    """
    out = []
    while n >= step:
        out.append(step)
        n -= step
    for smaller in (20, 10):
        if smaller >= step:
            continue
        while n >= smaller:
            out.append(smaller)
            n -= smaller
    if n > 0:
        out.append(10)      # artan: küçük bir bolluk payı bırakır
    return out


def place_crates(pixels, w, h, count, rng):
    """Sandıkları board'un ALT kenarına yakın, tek sıra hâlinde koyar.

    Rastgele serpmek tehlikeli: bir sandık arkasındaki küpleri dört yönden de
    kapatırsa level çözülemez hâle gelir. Alt kenarda tek sıra yalnızca yukarı
    yönü kapatır; o küpler hâlâ sağdan, soldan ve yukarıdan vurulabilir.
    """
    if count <= 0:
        return pixels
    row = 1
    xs = [x for x in range(w) if pixels[row * w + x] != ID_OF["None"]]
    rng.shuffle(xs)
    out = list(pixels)
    for x in xs[:count]:
        out[row * w + x] = ID_OF["Crate"]
    return out


def make_queue(counts, column_count, rng, steps, hidden_count):
    shooters = []
    for colour, total in counts.items():
        for ammo in split_ammo(total, steps[colour]):
            shooters.append({"color": colour, "ammo": ammo, "is_hidden": False})

    rng.shuffle(shooters)
    for i, s in enumerate(shooters):
        s["column"] = i % column_count

    # Gizli atıcılar kuyruğun ÖNÜNDE olmasın: oyuncu ilk hamlesini kör yapamaz.
    for s in shooters[column_count:][:hidden_count]:
        s["is_hidden"] = True
    return shooters


def make_board(spec, rng):
    fname, cells, colours, column_count, target, crates, hidden, share = spec
    path = os.path.join("src", fname)
    palette_rgb = quantize.load_palette()
    w, h = quantize.board_size_for_cells(Image.open(path), cells)
    pixels = quantize.quantize(path, w, h, palette_rgb, SATURATION, colours,
                               share, BG_RAMP)
    return place_crates(pixels, w, h, crates, rng), w, h


def build(spec, rng, attempts=90):
    fname, cells, colours, column_count, target, crates, hidden, share = spec
    pixels, w, h = make_board(spec, random.Random(4242))

    counts = {}
    for p in pixels:
        if p != ID_OF["None"] and p != ID_OF["Crate"]:
            counts[p] = counts.get(p, 0) + 1
    board_palette = sorted(counts, key=lambda c: -counts[c])

    lo, hi = target
    # Kolay bantlarda 20'lik atıcılar, zor bantlarda 40'lık. Arama ikisini de dener.
    if hi <= 2:
        # 10'luk atıcılar level'ı kolaylaştırır ama kuyruğu şişirir: limonun
        # %79'luk zemini 10'a bölününce 80 atıcı, yani 80 dokunuş çıkıyordu.
        # Baskın renk zaten her turda eşleşme bulduğu için 20/40 da güvenli.
        pool = [20, 40]
    elif hi <= 3:
        pool = [20, 40]
    else:
        pool = [40, 20]

    best = None
    for _ in range(attempts):
        steps = {c: rng.choice(pool) for c in counts}
        queue = make_queue(counts, column_count, rng, steps, hidden)

        lvl = levelio.Level("tmp", w, h, pixels, board_palette, queue,
                            column_count, 5, 5)
        result, policy, runs = sim.play_best(lvl)
        if result.outcome != "won":
            continue

        losses = sum(1 for _, r in runs if r.outcome != "won")
        miss = max(0, lo - result.peak_park) + max(0, result.peak_park - hi)

        # Kaybeden politika sayısı ikinci zorluk göstergesi, ama yönü hedefe
        # bağlı: zor level'da ÇOK olmalı, kolay level'da HİÇ. Tek yönlü tercih
        # kolay bantları da gereksiz riskli yapıyordu.
        want_pressure = hi >= 4
        score = (miss, -losses if want_pressure else losses, len(queue))
        if best is None or score < best[0]:
            best = (score, queue, result, policy, losses)
            enough = losses >= (2 if hi >= 5 else 1) if want_pressure else losses == 0
            if miss == 0 and enough:
                break       # bant tam tutturuldu, aramayı uzatma

    if best is None:
        return None
    return {"queue": best[1], "result": best[2], "policy": best[3],
            "losses": best[4], "pixels": pixels, "w": w, "h": h,
            "palette": board_palette, "columns": column_count}


def main():
    import sys
    only = {int(x) for x in sys.argv[1].split(",")} if len(sys.argv) > 1 else None
    os.makedirs(LEVELS_DIR, exist_ok=True)

    for i, spec in enumerate(SPECS, start=FIRST_LEVEL):
        if only and i not in only:
            continue
        # Level başına ayrı tohum: bir level'ı yeniden üretmek diğerlerinin
        # sonucunu değiştirmesin.
        out = build(spec, random.Random(4242 + i))
        name = "Level_%d" % i
        if out is None:
            print("%-8s URETILEMEDI  %s" % (name, spec[0]))
            continue

        lvl = levelio.Level(name, out["w"], out["h"], out["pixels"],
                            out["palette"], out["queue"], out["columns"], 5, 5)
        path = os.path.join(LEVELS_DIR, name + ".asset")
        levelio.write(path, lvl, SCRIPT_GUID, i)

        meta = path + ".meta"
        if not os.path.exists(meta):
            open(meta, "w", newline="\n").write(
                "fileFormatVersion: 2\nguid: %s\nNativeFormatImporter:\n"
                "  externalObjects: {}\n  mainObjectFileID: 11400000\n"
                "  userData:\n  assetBundleName:\n  assetBundleVariant:\n"
                % guid_for(name))

        r = out["result"]
        ammo_mix = {}
        for s in out["queue"]:
            ammo_mix[s["ammo"]] = ammo_mix.get(s["ammo"], 0) + 1
        mix = " ".join("%dx%d" % (ammo_mix[a], a) for a in sorted(ammo_mix))
        print("%-8s %2dx%-2d %4d kup  %d renk  %2d atici (%s)  "
              "zirve park %d/5  kaybeden %d/3  sandik %2d  gizli %d"
              % (name, out["w"], out["h"], out["w"] * out["h"],
                 len(out["palette"]), len(out["queue"]), mix,
                 r.peak_park, out["losses"], spec[5], spec[6]))


if __name__ == "__main__":
    main()
