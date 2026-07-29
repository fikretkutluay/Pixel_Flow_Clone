"""
Avatar set for the profile panel.

Each avatar is one baked sprite: a candy rounded-square in its own colour with a
white emblem on top. Baking rather than layering keeps the profile grid to a
single Image per slot, and the emblems are the same shapes as the icon set so the
whole UI reads as one family.

The reference game's own animal characters are deliberately not copied — those are
its brand art. These are our own marks (GDD 5.8: "kendi üretimimiz veya CC0").
"""
import numpy as np
from PIL import Image, ImageDraw

from build import SS, blur, erode, hexc, mask_rrect, ramp, save, top_band
import icons

SIZE = 256
KEYLINE = "#0A0A0A"

# Same mascot throughout, varied by colour and expression — the reference panel
# works the same way, one species in nine outfits.
# (emblem, deep base, light cap)
AVATARS = [
    ("dog_open",  "#E8453C", "#F0776F"),
    ("dog_wink",  "#F06292", "#F598B7"),
    ("dog_happy", "#F9A825", "#FBC15A"),
    ("dog_wink",  "#4ADE58", "#88E993"),
    ("dog_open",  "#26C6DA", "#6EDCE9"),
    ("dog_happy", "#3B82F6", "#7FA9F9"),
    ("dog_open",  "#A855F7", "#C58EFA"),
    ("dog_happy", "#8D6E63", "#B49A92"),
    ("dog_wink",  "#78909C", "#A5B5BE"),
]


for _variant in ("open", "wink", "happy"):
    icons.ICONS[f"dog_{_variant}"] = \
        (lambda v: lambda d, n: icons.dog(d, n, v))(_variant)


def emblem_mask(name, n, scale=0.60):
    """Render an icon shape, then shrink it and centre it on an n x n canvas."""
    full = Image.new("L", (n, n), 0)
    icons.ICONS[name](ImageDraw.Draw(full), n)

    inner = max(int(n * scale), 8)
    small = full.resize((inner, inner), Image.LANCZOS)
    out = Image.new("L", (n, n), 0)
    out.paste(small, ((n - inner) // 2, (n - inner) // 2))
    return np.asarray(out, dtype=np.float64) / 255.0


def avatar(name, emblem, deep, light, size=SIZE):
    n = size * SS
    ow = int(0.026 * n)
    rad = int(0.21 * n)

    outer = mask_rrect(n, n, (0, 0, n - 1, n - 1), rad)
    inner = mask_rrect(n, n, (ow, ow, n - ow - 1, n - ow - 1), max(rad - ow, 1))

    ih = n - 2 * ow
    col = ramp([(0.00, light), (0.46, light), (0.54, deep), (1.00, deep)], ih)
    body = np.zeros((n, n, 3))
    body[ow:ow + ih] = col[:, None, :]
    body[:ow], body[ow + ih:] = col[0], col[-1]

    rgb = hexc(KEYLINE)[None, None, :] * (1 - inner[..., None]) + body * inner[..., None]

    rim = blur(top_band(inner, max(int(0.030 * ih), 2)), 1.2 * SS) * 0.70
    rgb = rgb * (1 - rim[..., None]) + 255.0 * rim[..., None]

    # emblem: white fill inside its own keyline, so it reads on any base colour
    glyph = emblem_mask(emblem, n)
    halo = erode(glyph, 0) if ow == 0 else np.clip(
        blur(glyph, 0.9 * ow) * 3.0, 0, 1)
    rgb = rgb * (1 - halo[..., None]) + hexc(KEYLINE)[None, None, :] * halo[..., None]
    rgb = rgb * (1 - glyph[..., None]) + 252.0 * glyph[..., None]

    return save(rgb, outer, size, size, name)


if __name__ == "__main__":
    for i, (emblem, deep, light) in enumerate(AVATARS, start=1):
        avatar(f"pixelflow_avatar_{i}", emblem, deep, light)
    print(f"{len(AVATARS)} avatars written")
