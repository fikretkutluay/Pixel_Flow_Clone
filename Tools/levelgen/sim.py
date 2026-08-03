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
from levelio import ID_OF

NONE = ID_OF["None"]
CRATE = ID_OF["Crate"]

UP, DOWN, LEFT, RIGHT = 0, 1, 2, 3


def lane_sequence(w, h):
    """(lane, direction) çiftleri — bir turun tam sırası."""
    seq = [(x, UP) for x in range(w)]
    seq += [(y, LEFT) for y in range(h)]
    seq += [(x, DOWN) for x in range(w - 1, -1, -1)]
    seq += [(y, RIGHT) for y in range(h - 1, -1, -1)]
    return seq


class Board:
    """Küp ızgarası + her lane'in "peel front" indeksi.

    Naif hâlde her atış lane'i kenardan taramak zorunda ve 1000+ küplük
    board'da arama pratikte durma noktasına geliyordu. Bir lane'in ilk dolu
    hücresi ancak o satır/sütunda bir küp kırılınca değişir, dolayısıyla
    önbelleğe alınıp kırılmada geçersiz kılınabilir: atış O(1)'e iniyor.
    """

    __slots__ = ("cells", "w", "h", "fronts", "remaining", "version")

    def __init__(self, cells, w, h):
        self.cells = list(cells)
        self.w, self.h = w, h
        self.fronts = {}
        self.remaining = sum(1 for c in cells if c != NONE and c != CRATE)
        # Durum kimliği: preview_lap sonuçları buna göre önbelleğe alınır.
        self.version = 0

    def _scan(self, lane, direction):
        w, h, cells = self.w, self.h, self.cells
        if direction == UP:
            for y in range(h):
                if cells[y * w + lane] != NONE:
                    return y * w + lane
        elif direction == DOWN:
            for y in range(h - 1, -1, -1):
                if cells[y * w + lane] != NONE:
                    return y * w + lane
        elif direction == RIGHT:
            base = lane * w
            for x in range(w):
                if cells[base + x] != NONE:
                    return base + x
        else:
            base = lane * w
            for x in range(w - 1, -1, -1):
                if cells[base + x] != NONE:
                    return base + x
        return -1

    def front(self, lane, direction):
        key = (direction, lane)
        idx = self.fronts.get(key)
        if idx is None:
            idx = self._scan(lane, direction)
            self.fronts[key] = idx
        return idx

    def _invalidate(self, idx):
        x, y = idx % self.w, idx // self.w
        f = self.fronts
        f.pop((UP, x), None)
        f.pop((DOWN, x), None)
        f.pop((RIGHT, y), None)
        f.pop((LEFT, y), None)

    def shoot(self, lane, direction, colour):
        """LaneRaycaster.TryBreak. Kırdıysa hücre indeksi, yoksa -1."""
        idx = self.front(lane, direction)
        # Sandık da renk tutmayan küp de aynı sonucu verir: atış boşa gider.
        if idx < 0 or self.cells[idx] != colour:
            return -1
        self.cells[idx] = NONE
        self.remaining -= 1
        self.version += 1
        self._invalidate(idx)
        return idx

    def restore(self, idxs, colour):
        for idx in idxs:
            self.cells[idx] = colour
            self.remaining += 1
            self.version -= 1
            self._invalidate(idx)

    def lift_crates(self):
        """BoardController.ClearCrates() birebir. Bu olmadan endgame'e giren bir
        level'da sandıkların arkasında kalan küpler simülatörde SONSUZA DEK
        erişilemez kalıyordu — gerçek oyunda ise tam bu anda kalkıyorlar.
        Bunu unutmak Level_8 gibi sandık-ağırlıklı level'ları OLDUĞUNDAN ÇOK
        daha zor/çözülemez gösteriyordu.
        """
        changed = False
        for idx, c in enumerate(self.cells):
            if c == CRATE:
                self.cells[idx] = NONE
                changed = True
        if changed:
            self.fronts.clear()
            self.version += 1


def preview_lap(board, seq, colour, ammo):
    """Bu atıcı şu anki board'da turunu yaparsa kaç isabet alır?

    Board'u KOPYALAMAZ — kırdıklarını geri koyar. Kopyalamak 1000 hücrede her
    çağrıda 1000 işlem demekti ve bu fonksiyon tur başına düzinelerce çağrılıyor.
    """
    broken = []
    for lane, direction in seq:
        if len(broken) >= ammo:
            break
        idx = board.shoot(lane, direction, colour)
        if idx >= 0:
            broken.append(idx)
    board.restore(broken, colour)
    return len(broken)


class Result:
    def __init__(self):
        self.outcome = "?"        # won / lost / stuck
        self.peak_park = 0
        self.park_cap = 5
        self.wasted_laps = 0      # mermisi bitmeden parka düşen tur
        self.laps = 0
        self.leftover_ammo = 0
        self.remaining_cubes = 0
        self.ticks = 0

    def __str__(self):
        return ("%-6s zirve park %d/%d  bosa tur %2d/%-3d kalan kup %4d  "
                "artan mermi %4d" % (self.outcome, self.peak_park, self.park_cap,
                                     self.wasted_laps, self.laps,
                                     self.remaining_cubes, self.leftover_ammo))


def play(level, policy="cautious", endgame_threshold=5):
    w, h = level.w, level.h
    seq = lane_sequence(w, h)
    lap_len = len(seq)

    board = Board(level.pixels, w, h)
    # Kuyruk sütunlara ayrılır; oyuncu yalnızca her sütunun ÖNÜNÜ alabilir.
    columns = [[] for _ in range(level.column_count)]
    for s in level.queue:
        columns[s["column"]].append({"colour": s["color"], "ammo": s["ammo"]})

    track, park = [], []
    res = Result()
    res.park_cap = level.park_capacity

    def total_shooters():
        return len(track) + len(park) + sum(len(c) for c in columns)

    def finish(outcome, tick):
        res.outcome = outcome
        res.ticks = tick
        res.remaining_cubes = board.remaining
        res.leftover_ammo = (sum(x["ammo"] for x in track + park)
                             + sum(x["ammo"] for c in columns for x in c))
        return res

    memo = {}
    tick = 0
    idle = 0
    endgame_started = False
    max_ticks = lap_len * (len(level.queue) + 8) * 3

    while tick < max_ticks:
        tick += 1
        endgame = total_shooters() <= endgame_threshold

        # GameManager.UpdateEndgame() -> BoardController.ClearCrates(), tek sefer.
        if endgame and not endgame_started:
            endgame_started = True
            board.lift_crates()
            memo.clear()   # kalkan sandiklar onceki hit-onizlemelerini gecersiz kilar

        for s in list(track):
            lane, direction = seq[s["progress"] % lap_len]
            if board.shoot(lane, direction, s["colour"]) >= 0:
                s["ammo"] -= 1
                if s["ammo"] <= 0:
                    track.remove(s)
                    continue
            s["progress"] += 1

            if s["progress"] >= lap_len:
                res.laps += 1
                track.remove(s)
                if endgame:
                    s["progress"] = 0     # bitiş koşusunda park yok
                    track.append(s)
                    continue
                res.wasted_laps += 1
                if len(park) >= level.park_capacity:
                    return finish("lost", tick)
                park.append({"colour": s["colour"], "ammo": s["ammo"]})
                res.peak_park = max(res.peak_park, len(park))

        if board.remaining == 0:
            return finish("won", tick)

        launched = False
        while len(track) < level.track_capacity:
            opts = [("queue", i, c[0]) for i, c in enumerate(columns) if c]
            opts += [("park", i, s) for i, s in enumerate(park)]
            if not opts:
                break

            # Board değişmediyse aynı atıcı aynı sonucu verir. Aday puanlaması
            # bu döngünün en pahalı kısmı ve tur başına onlarca kez çağrılıyordu.
            scored = []
            for src, idx, s in opts:
                key = (board.version, s["colour"], s["ammo"])
                hits = memo.get(key)
                if hits is None:
                    hits = preview_lap(board, seq, s["colour"], s["ammo"])
                    memo[key] = hits
                scored.append((hits >= s["ammo"], hits, -s["ammo"], src, idx))
            scored.sort(reverse=True)
            best = scored[0]

            # Ray boşken beklemek oyunu kilitler, hiçbir politika bunu yapmaz.
            if not best[0] and not endgame and track:
                if policy == "patient":
                    break
                if policy == "cautious" and len(park) >= level.park_capacity - 1:
                    break

            src, idx = best[3], best[4]
            s = columns[idx].pop(0) if src == "queue" else park.pop(idx)
            track.append({"colour": s["colour"], "ammo": s["ammo"], "progress": 0})
            launched = True

        idle = 0 if (launched or track) else idle + 1
        if idle > lap_len:
            break

    return finish("stuck", tick)


POLICIES = ("patient", "cautious", "flood")


def _rank(r):
    """İyiden kötüye: kazanmak, sonra parkı boş tutmak, sonra boşa tur harcamamak."""
    return ({"won": 0, "stuck": 1, "lost": 2}[r.outcome], r.peak_park, r.wasted_laps)


def play_best(level, endgame_threshold=5):
    """Üç politikanın en iyisi — iyi oynayan bir oyuncunun alt sınırı.

    Tek politika yanıltıcı: zayıf bir bot her level'ı zor gösterir.
    """
    runs = [(p, play(level, p, endgame_threshold)) for p in POLICIES]
    best = min(runs, key=lambda kv: _rank(kv[1]))
    return best[1], best[0], runs


if __name__ == "__main__":
    import sys
    import levelio
    for path in sys.argv[1:]:
        lvl = levelio.read(path)
        best, name, runs = play_best(lvl)
        print("%-10s EN IYI (%-8s) %s" % (lvl.name, name, best))
        for pol, r in runs:
            print("             %-9s %s" % (pol, r))
