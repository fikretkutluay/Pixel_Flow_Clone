"""Play a level the way the game does, and report how hard it was.

Why this exists: difficulty in this game is NOT about running out of ammo.
TrackController only calls ConsumeAmmo() on a hit, so misses are free. A shooter
is dangerous because it parks with ammo left over, and a full park plus one
completed lap is an instant loss. So the number that matters is how close the
park gets to full, and no static check on the asset can tell you that — you have
to play it.

The rules mirrored here, all read off the source rather than assumed:

  LaneRaycaster.TryBreak  a shot walks the lane from the board edge, skips empty
                          cells, and stops at the first solid one: it breaks only
                          if the colour matches, and a crate always blocks.
  TrackPath.Evaluate      one lap visits Bottom 0..W-1, Right 0..H-1,
                          Top W-1..0, Left H-1..0 — 2(W+H) lanes, fixed order.
  TrackController         fires once per lane, ammo spent on hits only, a spent
                          shooter retires mid-lap.
  GameManager             lap end -> park; park full at lap end -> loss;
                          once track+park+queue <= endgame threshold nobody
                          parks any more and the level cannot be lost.
"""
import copy

import levelio
from levelio import ID_OF

NONE = ID_OF["None"]
CRATE = ID_OF["Crate"]

UP, DOWN, LEFT, RIGHT = "up", "down", "left", "right"


def lane_sequence(w, h):
    """(lane, direction) çiftleri — bir turun tam sırası."""
    seq = [(x, UP) for x in range(w)]
    seq += [(y, LEFT) for y in range(h)]
    seq += [(x, DOWN) for x in range(w - 1, -1, -1)]
    seq += [(y, RIGHT) for y in range(h - 1, -1, -1)]
    return seq


def _walk(w, h, lane, direction):
    if direction == UP:
        return [(lane, y) for y in range(h)]
    if direction == DOWN:
        return [(lane, y) for y in range(h - 1, -1, -1)]
    if direction == RIGHT:
        return [(x, lane) for x in range(w)]
    return [(x, lane) for x in range(w - 1, -1, -1)]


def try_break(board, w, h, lane, direction, colour):
    """LaneRaycaster.TryBreak birebir. Kırdıysa hücreyi döndürür."""
    for x, y in _walk(w, h, lane, direction):
        cell = board[y * w + x]
        if cell == CRATE:
            return None
        if cell == NONE:
            continue
        if cell == colour:
            board[y * w + x] = NONE
            return (x, y)
        return None
    return None


def simulate_lap(board, w, h, seq, colour, ammo):
    """Bir atıcıyı tek başına turlatır. (isabet, kırılanlar) döndürür.

    Board KOPYALANMAZ — çağıran karar verir. Bakış açısı fonksiyonu bunu
    kopya üstünde çağırıp atıcının mermisini bitirip bitiremeyeceğine bakar.
    """
    hits = 0
    broken = []
    for lane, direction in seq:
        if hits >= ammo:
            break
        cell = try_break(board, w, h, lane, direction, colour)
        if cell:
            hits += 1
            broken.append(cell)
    return hits, broken


class Result:
    def __init__(self):
        self.outcome = "?"        # won / lost / stuck
        self.peak_park = 0
        self.wasted_laps = 0      # mermisi bitmeden parka düşen tur
        self.laps = 0
        self.leftover_ammo = 0
        self.remaining_cubes = 0
        self.ticks = 0

    def __str__(self):
        return ("%-6s  zirve park %d/%d  bosa tur %2d/%-2d  kalan kup %3d  "
                "artan mermi %3d" % (self.outcome, self.peak_park, self.park_cap,
                                     self.wasted_laps, self.laps,
                                     self.remaining_cubes, self.leftover_ammo))


def play(level, policy="cautious", endgame_threshold=5):
    w, h = level.w, level.h
    seq = lane_sequence(w, h)
    lap_len = len(seq)

    board = list(level.pixels)
    # Kuyruk sütunlara ayrılır; oyuncu yalnızca her sütunun ÖNÜNÜ alabilir.
    columns = [[] for _ in range(level.column_count)]
    for s in level.queue:
        columns[s["column"]].append({"colour": s["color"], "ammo": s["ammo"],
                                     "is_hidden": s["is_hidden"]})

    track = []      # {colour, ammo, progress}
    park = []       # {colour, ammo}

    res = Result()
    res.park_cap = level.park_capacity

    def remaining():
        return sum(1 for c in board if c not in (NONE, CRATE))

    def total_shooters():
        return len(track) + len(park) + sum(len(c) for c in columns)

    def candidates():
        out = []
        for ci, col in enumerate(columns):
            if col:
                out.append(("queue", ci, col[0]))
        for pi, s in enumerate(park):
            out.append(("park", pi, s))
        return out

    def evaluate(shooter):
        """Bu atıcı şu anki board'da turunu yaparsa kaç isabet alır?"""
        hits, _ = simulate_lap(list(board), w, h, seq,
                               shooter["colour"], shooter["ammo"])
        return hits

    tick = 0
    idle = 0
    max_ticks = lap_len * (len(level.queue) + 8) * 4

    while tick < max_ticks:
        tick += 1
        endgame = total_shooters() <= endgame_threshold

        # --- rayda ilerleme: her atıcı bir lane ilerler ve ateş eder ---
        for s in list(track):
            lane, direction = seq[s["progress"] % lap_len]
            if try_break(board, w, h, lane, direction, s["colour"]):
                s["ammo"] -= 1
                if s["ammo"] <= 0:
                    track.remove(s)     # mermisi bitti, sahneden çıkar
                    continue
            s["progress"] += 1

            if s["progress"] >= lap_len:
                res.laps += 1
                track.remove(s)
                if endgame:
                    s["progress"] = 0   # bitiş koşusunda park yok, turlamaya devam
                    track.append(s)
                    continue
                res.wasted_laps += 1
                if len(park) >= level.park_capacity:
                    res.outcome = "lost"
                    res.ticks = tick
                    res.remaining_cubes = remaining()
                    res.leftover_ammo = (sum(x["ammo"] for x in track + park)
                                         + sum(x["ammo"] for c in columns for x in c))
                    return res
                park.append({"colour": s["colour"], "ammo": s["ammo"]})
                res.peak_park = max(res.peak_park, len(park))

        if remaining() == 0:
            res.outcome = "won"
            break

        # --- oyuncu: boş slot varsa en iyi adayı yolla ---
        launched = False
        while len(track) < level.track_capacity:
            opts = candidates()
            if not opts:
                break

            scored = []
            for src, idx, s in opts:
                hits = evaluate(s)
                # Mermisini bitirebilen atıcı parka düşmez: en değerli hamle o.
                empties = hits >= s["ammo"]
                scored.append((empties, hits, -s["ammo"], src, idx, s))
            scored.sort(reverse=True)
            best = scored[0]

            # Politika, atıcının mermisini bitiremediği durumda ne yapacağını
            # belirler. Tek bir politika yanıltıcı: zayıf bir bot her level'ı zor
            # gösterir. play_best() üçünü de deneyip EN İYİ sonucu alır, böylece
            # rapor "iyi oynayan biri için ne kadar zor" sorusuna yaklaşır.
            # Ray boşken beklemek oyunu kilitler, hiçbir politika bunu yapmaz:
            # yapacak başka şey yoksa en iyi adayı yolla.
            if not best[0] and not endgame and track:
                if policy == "patient":
                    break        # boşalamayan atıcıyı hiç yollama
                if policy == "cautious" and len(park) >= level.park_capacity - 1:
                    break        # sadece park dolmak üzereyken çekin

            src, idx = best[3], best[4]
            shooter = columns[idx].pop(0) if src == "queue" else park.pop(idx)
            track.append({"colour": shooter["colour"], "ammo": shooter["ammo"],
                          "progress": 0})
            launched = True

        idle = 0 if (launched or track) else idle + 1
        if idle > lap_len:
            res.outcome = "stuck"
            break

    if res.outcome == "?":
        res.outcome = "stuck"

    res.ticks = tick
    res.remaining_cubes = remaining()
    res.leftover_ammo = (sum(x["ammo"] for x in track + park)
                         + sum(x["ammo"] for c in columns for x in c))
    return res


POLICIES = ("patient", "cautious", "flood")

# Sonucu iyiden kötüye sıralar: kazanmak her şeyden önce gelir, sonra parkı boş
# tutmak, sonra boşa tur harcamamak.
def _rank(r):
    return ({"won": 0, "stuck": 1, "lost": 2}[r.outcome], r.peak_park, r.wasted_laps)


def play_best(level, endgame_threshold=5):
    """Üç politikanın en iyisi — iyi oynayan bir oyuncunun alt sınırı."""
    runs = [(p, play(level, p, endgame_threshold)) for p in POLICIES]
    best = min(runs, key=lambda kv: _rank(kv[1]))
    return best[1], best[0], runs


if __name__ == "__main__":
    import sys
    for path in sys.argv[1:]:
        lvl = levelio.read(path)
        best, name, runs = play_best(lvl)
        print("%-10s EN IYI (%s)  %s" % (lvl.name, name, best))
        for pol, r in runs:
            print("             %-9s %s" % (pol, r))
