"""
Pixel Flow Clone — UI texture generator.

Two families, because measuring the reference showed it uses two:

  candy   Panel buttons. Heavy black keyline, white rim highlight along the top
          edge, a two-tone body (light cap over a deeper base), a light lip and
          a dark inner shadow at the bottom. The light->deep step in the
          reference is a SATURATION increase at near-constant value, which a
          grey texture under Unity's multiplicative tint physically cannot
          reproduce, so these ship pre-coloured per style.

  soft    HUD chrome (level pill, counters, gear). A plain vertical gradient,
          thin or no keyline. The reference re-tints these per level theme, so
          these ship neutral grey and are tinted at runtime.

Everything is authored as a function of y inside the shape, so the centre band
of a 9-slice tiles exactly. Rim and shadow follow the silhouette via edge masks,
which stay correct in the fixed corner regions.
"""

import os

import numpy as np
from PIL import Image, ImageDraw, ImageFilter

SS = 4
OUT = "out"


# ----------------------------------------------------------------- utilities

def hexc(s):
    """'#rrggbb' or a plain grey level -> float RGB triple."""
    if not isinstance(s, str):
        return np.array([s, s, s], dtype=np.float64)
    s = s.lstrip("#")
    return np.array([int(s[i:i + 2], 16) for i in (0, 2, 4)], dtype=np.float64)


def mask_rrect(w, h, box, r):
    img = Image.new("L", (w, h), 0)
    ImageDraw.Draw(img).rounded_rectangle(box, radius=r, fill=255)
    return np.asarray(img, dtype=np.float64) / 255.0


def blur(m, r):
    if r <= 0:
        return m
    im = Image.fromarray((np.clip(m, 0, 1) * 255).astype(np.uint8))
    return np.asarray(im.filter(ImageFilter.GaussianBlur(r)), dtype=np.float64) / 255.0


def erode(m, px):
    """Shrink a mask by `px`, so (m - erode(m, ow)) is an inside keyline band."""
    im = Image.fromarray((np.clip(m, 0, 1) * 255).astype(np.uint8))
    step, left = 4, int(px)
    while left > 0:
        k = min(step, left)
        im = im.filter(ImageFilter.MinFilter(2 * k + 1))
        left -= k
    return np.asarray(im, dtype=np.float64) / 255.0


def top_band(mask, px):
    """Inside `mask`, within px of its upper boundary — follows the curve."""
    s = np.zeros_like(mask)
    s[px:] = mask[:-px]
    return np.clip(mask - s, 0, 1)


def bottom_band(mask, px):
    s = np.zeros_like(mask)
    s[:-px] = mask[px:]
    return np.clip(mask - s, 0, 1)


def ramp(stops, n):
    """stops = [(t, '#rrggbb' | grey0-255)] -> (n,3) colour column."""
    ts = np.array([t for t, _ in stops], dtype=np.float64)
    cs = np.array([hexc(c) if isinstance(c, str) else np.array([c, c, c], float)
                   for _, c in stops])
    t = np.linspace(0.0, 1.0, n)
    return np.stack([np.interp(t, ts, cs[:, k]) for k in range(3)], axis=1)


def save(rgb, alpha, w, h, name):
    rgb = np.clip(rgb, 0, 255)
    a = np.clip(alpha, 0, 1) * 255.0
    arr = np.dstack([rgb, a]).astype(np.uint8)
    img = Image.fromarray(arr, "RGBA").resize((w, h), Image.LANCZOS)
    os.makedirs(OUT, exist_ok=True)
    img.save(f"{OUT}/{name}.png")
    return img


# -------------------------------------------------------------------- candy

# Measured off the reference screenshots: (light cap, deep base, bottom shadow)
STYLES = {
    "yellow": ("#F9D160", "#F7B92A", "#D17E33"),
    "green":  ("#67EF77", "#16E651", "#1FA464"),
    "purple": ("#C483F3", "#A154EB", "#5A2594"),
    "blue":   ("#5AD2FB", "#21BAFA", "#1381E8"),
    "red":    ("#E04A5E", "#D62D40", "#B00D27"),
    "grey":   ("#EDEDED", "#C6C6C6", "#8E8E8E"),
}

KEYLINE = "#0A0A0A"


def candy(name, W, H, radius, style, outline_frac=0.016, split=0.50, dot=True):
    light, deep, shadow = STYLES[style]
    w, h = W * SS, H * SS
    ow = max(int(round(outline_frac * h)), 2)
    rad = int(radius * SS)

    outer = mask_rrect(w, h, (0, 0, w - 1, h - 1), rad)
    inner = mask_rrect(w, h, (ow, ow, w - ow - 1, h - ow - 1), max(rad - ow, 1))

    ih = h - 2 * ow                      # inner height drives every proportion
    col = ramp([
        (0.000, deep),
        (0.050, deep),
        (0.080, light),                  # cap starts just below the rim
        (split - 0.03, light),
        (split + 0.03, deep),            # the light->deep step
        (0.900, deep),
        (0.935, light),                  # lip catching light above the shadow
        (0.960, deep),
        (1.000, deep),
    ], ih)

    body = np.zeros((h, w, 3))
    body[ow:ow + ih] = col[:, None, :]
    body[:ow] = col[0]
    body[ow + ih:] = col[-1]

    rgb = hexc(KEYLINE)[None, None, :] * (1 - inner[..., None]) + body * inner[..., None]

    # white rim hugging the top edge
    rim = blur(top_band(inner, max(int(0.030 * ih), 2)), 1.2 * SS) * 0.78
    rgb = rgb * (1 - rim[..., None]) + 255.0 * rim[..., None]

    # dark inner shadow hugging the bottom edge
    sh = blur(bottom_band(inner, max(int(0.058 * ih), 2)), 1.0 * SS)
    rgb = rgb * (1 - sh[..., None]) + hexc(shadow)[None, None, :] * sh[..., None]

    # specular dot in the upper-left. Kept inside the fixed corner region so it
    # survives 9-slicing untouched.
    if dot:
        cx = cy = int(rad * 0.60)
        r = max(int(rad * 0.13), 2)
        d = blur(mask_rrect(w, h, (cx - r, cy - r, cx + r, cy + r), r), 0.18 * r)
        d = d * 0.95 * inner
        rgb = rgb * (1 - d[..., None]) + 255.0 * d[..., None]

    return save(rgb, outer, W, H, name)


# --------------------------------------------------------------------- soft

def soft(name, W, H, radius, stops, outline_frac=0.0, gloss=0.0):
    w, h = W * SS, H * SS
    ow = int(round(outline_frac * h))
    rad = int(radius * SS)

    outer = mask_rrect(w, h, (0, 0, w - 1, h - 1), rad)
    inner = (mask_rrect(w, h, (ow, ow, w - ow - 1, h - ow - 1), max(rad - ow, 1))
             if ow else outer)

    ih = h - 2 * ow
    col = ramp(stops, ih)
    body = np.zeros((h, w, 3))
    body[ow:ow + ih] = col[:, None, :]
    if ow:
        body[:ow], body[ow + ih:] = col[0], col[-1]

    rgb = hexc(KEYLINE)[None, None, :] * (1 - inner[..., None]) + body * inner[..., None]

    if gloss > 0:
        g = blur(top_band(inner, int(0.34 * ih)), 0.08 * h) * gloss
        rgb = rgb * (1 - g[..., None]) + 255.0 * g[..., None]

    return save(rgb, outer, W, H, name)
