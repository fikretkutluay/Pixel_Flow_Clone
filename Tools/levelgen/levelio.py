"""Read and write LevelData ScriptableObject assets.

Unity serialises ColorId[] as a hex blob of 4-byte little-endian ints, which is
awkward to eyeball but trivial to parse. Everything here works on that blob so
the generator and the simulator can share one representation of a level.
"""
import re
import struct

COLORS = {
    0: "None", 1: "Crate", 2: "Red", 3: "Blue", 4: "Green", 5: "Yellow",
    6: "Purple", 7: "Navy", 8: "White", 9: "Khaki", 10: "Maroon",
    11: "DarkPurple", 12: "DarkGray", 13: "LightGray", 14: "Black",
    15: "Pink", 16: "Orange", 17: "Flesh", 18: "Brawn", 19: "LightBrawn",
}
ID_OF = {v: k for k, v in COLORS.items()}

# Tek harfli kısaltmalar — board'u terminalde çizerken kullanılıyor.
GLYPH = {
    0: ".", 1: "#", 2: "R", 3: "B", 4: "G", 5: "Y", 6: "P", 7: "N",
    8: "W", 9: "K", 10: "M", 11: "D", 12: "g", 13: "l", 14: "X",
    15: "I", 16: "O", 17: "F", 18: "A", 19: "L",
}


class Level:
    def __init__(self, name, board_w, board_h, pixels, palette, queue,
                 column_count, track_capacity, park_capacity):
        self.name = name
        self.w = board_w
        self.h = board_h
        self.pixels = pixels            # [ColorId] uzunluk w*h
        self.palette = palette          # [ColorId]
        self.queue = queue              # [{column, color, ammo, is_hidden}]
        self.column_count = column_count
        self.track_capacity = track_capacity
        self.park_capacity = park_capacity

    def at(self, x, y):
        return self.pixels[y * self.w + x]

    def cube_counts(self):
        counts = {}
        for p in self.pixels:
            if p in (ID_OF["None"], ID_OF["Crate"]):
                continue
            counts[p] = counts.get(p, 0) + 1
        return counts

    def ammo_by_colour(self):
        totals = {}
        for s in self.queue:
            totals[s["color"]] = totals.get(s["color"], 0) + s["ammo"]
        return totals

    def render(self):
        """Board'u ASCII olarak döndürür — y=0 ALTTA, oyundaki gibi."""
        rows = []
        for y in range(self.h - 1, -1, -1):
            rows.append("".join(GLYPH.get(self.at(x, y), "?") for x in range(self.w)))
        return "\n".join(rows)


def _unpack_blob(blob):
    raw = bytes.fromhex(blob.strip())
    return list(struct.unpack("<%di" % (len(raw) // 4), raw))


def _pack_blob(values):
    return struct.pack("<%di" % len(values), *values).hex()


def read(path):
    text = open(path, encoding="utf8").read()

    def scalar(key, default=None):
        m = re.search(r"^\s*%s:\s*(\S+)\s*$" % key, text, re.M)
        return int(m.group(1)) if m else default

    size = re.search(r"boardSize:\s*\{x:\s*(\d+),\s*y:\s*(\d+)\}", text)
    w, h = int(size.group(1)), int(size.group(2))

    pixels = _unpack_blob(re.search(r"boardPixels:\s*([0-9a-fA-F]*)", text).group(1))
    palette = _unpack_blob(re.search(r"palette:\s*([0-9a-fA-F]*)", text).group(1))

    queue = []
    block = re.search(r"queue:\n(.*?)\n  columnCount:", text, re.S)
    if block:
        for entry in re.finditer(
                r"- column: (\d+)\s+color: (\d+)\s+ammo: (\d+)\s+"
                r"isHidden: (\d+)\s+linkedCount: (\d+)", block.group(1)):
            queue.append({
                "column": int(entry.group(1)),
                "color": int(entry.group(2)),
                "ammo": int(entry.group(3)),
                "is_hidden": entry.group(4) == "1",
            })

    name = path.replace("\\", "/").rsplit("/", 1)[-1].replace(".asset", "")
    lvl = Level(name, w, h, pixels, palette, queue,
                scalar("columnCount", 4), scalar("trackCapacity", 5),
                scalar("parkCapacity", 5))

    if len(pixels) != w * h:
        raise SystemExit("%s: boardPixels %d != %dx%d" % (name, len(pixels), w, h))
    return lvl


QUEUE_ENTRY = """  - column: {column}
    color: {color}
    ammo: {ammo}
    isHidden: {hidden}
    linkedCount: 1
"""

TEMPLATE = """%YAML 1.1
%TAG !u! tag:unity3d.com,2011:
--- !u!114 &11400000
MonoBehaviour:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {{fileID: 0}}
  m_PrefabInstance: {{fileID: 0}}
  m_PrefabAsset: {{fileID: 0}}
  m_GameObject: {{fileID: 0}}
  m_Enabled: 1
  m_EditorHideFlags: 0
  m_Script: {{fileID: 11500000, guid: {script_guid}, type: 3}}
  m_Name: {name}
  m_EditorClassIdentifier: Game::Game.LevelData
  levelID: {level_id}
  boardSize: {{x: {w}, y: {h}}}
  boardPixels: {pixels}
  palette: {palette}
  queue:
{queue}  columnCount: {column_count}
  trackCapacity: {track_capacity}
  parkCapacity: {park_capacity}
"""


def write(path, level, script_guid, level_id):
    queue = "".join(
        QUEUE_ENTRY.format(column=s["column"], color=s["color"], ammo=s["ammo"],
                           hidden=1 if s["is_hidden"] else 0)
        for s in level.queue)

    open(path, "w", newline="\n", encoding="utf8").write(TEMPLATE.format(
        script_guid=script_guid, name=level.name, level_id=level_id,
        w=level.w, h=level.h,
        pixels=_pack_blob(level.pixels), palette=_pack_blob(level.palette),
        queue=queue, column_count=level.column_count,
        track_capacity=level.track_capacity, park_capacity=level.park_capacity))


if __name__ == "__main__":
    import sys
    for p in sys.argv[1:]:
        lvl = read(p)
        print("=== %s  %dx%d  columns=%d track=%d park=%d"
              % (lvl.name, lvl.w, lvl.h, lvl.column_count,
                 lvl.track_capacity, lvl.park_capacity))
        print(lvl.render())
        cubes = lvl.cube_counts()
        ammo = lvl.ammo_by_colour()
        print("renk        kup  mermi  bolluk")
        for cid in sorted(cubes, key=lambda c: -cubes[c]):
            a = ammo.get(cid, 0)
            print("  %-10s %4d %6d  %5.2f"
                  % (COLORS[cid], cubes[cid], a,
                     a / cubes[cid] if cubes[cid] else 0))
        print("kuyruk: %d atici, %d mermi"
              % (len(lvl.queue), sum(s["ammo"] for s in lvl.queue)))
