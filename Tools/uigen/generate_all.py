"""Emit the whole base-texture set."""
import numpy as np
from PIL import Image, ImageDraw

from build import (SS, KEYLINE, STYLES, blur, bottom_band, candy, erode, hexc,
                   mask_rrect, ramp, save, soft, top_band)

# ---------------------------------------------------------------- buttons
BTN_W, BTN_H = 600, 290
BTN_R = int(0.205 * BTN_H)          # 59 — matches the reference corner arc
for s in STYLES:
    candy(f"pixelflow_ui_button_{s}", BTN_W, BTN_H, BTN_R, s)
    # square sibling for the close X and other icon-only buttons
    candy(f"pixelflow_ui_buttonsq_{s}", 300, 300, 84, s)

# ------------------------------------------------------- HUD pill (tintable)
# Reference re-tints these per level theme, so they stay neutral grey.
# True pill: use at its authored height and vary width only.
soft("pixelflow_ui_pill", 400, 120, 60,
     [(0.00, 253), (0.35, 240), (1.00, 213)])

# ------------------------------------------------------------------ circle
soft("pixelflow_ui_circle", 300, 300, 150,
     [(0.00, 253), (0.35, 240), (1.00, 210)],
     outline_frac=0.030, gloss=0.30)

# --------------------------------------------------------------- iconframe
soft("pixelflow_ui_iconframe", 300, 300, 62,
     [(0.00, 250), (0.40, 236), (1.00, 208)],
     outline_frac=0.030, gloss=0.22)

# ------------------------------------------------------------------- panel
# Neutral for tinting, plus a pre-coloured blue matching the reference exactly —
# the reference gradient gains saturation downward, which a grey texture under a
# multiplicative tint cannot express, and on an area this large it shows.
soft("pixelflow_ui_panel", 800, 1000, 34,
     [(0.00, 253), (0.30, 243), (0.70, 224), (1.00, 208)])
soft("pixelflow_ui_panel_blue", 800, 1000, 34,
     [(0.00, "#55C6FF"), (0.36, "#4ABAFE"), (0.55, "#45B5FE"),
      (0.82, "#3DACFE"), (1.00, "#3BA6FF")])


# ------------------------------------------------------------ panel header
def header(name, W, H, radius, fill=236):
    """Top corners rounded, bottom edge square — sits on the panel's top edge."""
    w, h = W * SS, H * SS
    rad = radius * SS
    m = mask_rrect(w, h, (0, 0, w - 1, h - 1), rad)
    m[h // 2:] = mask_rrect(w, h, (0, 0, w - 1, h - 1), 0)[h // 2:]
    rgb = np.zeros((h, w, 3)) + (hexc(fill) if isinstance(fill, str) else fill)
    lip = blur(bottom_band(m, int(0.06 * h)), 0.01 * h)
    rgb = rgb * (1 - lip[..., None] * 0.18)
    return save(rgb, m, W, H, name)


header("pixelflow_ui_panel_header", 800, 130, 34)
header("pixelflow_ui_panel_header_blue", 800, 130, 34, "#1F86F3")


# ------------------------------------------------------------------ ribbon
def ribbon(name, W, H, face="#2F9BF6", tail="#1668C4", keyline=KEYLINE):
    """Banner whose tails emerge from behind the face and fall away outward,
    each finished with a V notch — the Win panel header."""
    w, h = W * SS, H * SS
    ow = int(0.022 * h)
    tw = int(0.082 * w)             # how far a tail reaches inward, behind the face
    stick = int(0.042 * w)          # how far it sticks out past the face
    face_bot = int(0.78 * h)

    def poly(pts):
        im = Image.new("L", (w, h), 0)
        ImageDraw.Draw(im).polygon(pts, fill=255)
        return np.asarray(im, dtype=np.float64) / 255.0

    left = poly([(tw, 0.12 * h), (0, 0.24 * h), (0, 0.95 * h),
                 (tw * 0.46, 0.74 * h), (tw, 0.95 * h)])
    right = poly([(w - tw, 0.12 * h), (w - 1, 0.24 * h), (w - 1, 0.95 * h),
                  (w - 1 - tw * 0.46, 0.74 * h), (w - 1 - tw, 0.95 * h)])
    tails = np.clip(left + right, 0, 1)
    face_m = mask_rrect(w, h, (stick, 0, w - 1 - stick, face_bot), int(0.16 * h))

    tails_in, face_in = erode(tails, ow), erode(face_m, ow)

    rgb = np.zeros((h, w, 3)) + hexc(keyline)
    for m, c in ((tails_in, hexc(tail)), (face_in, hexc(face))):
        rgb = rgb * (1 - m[..., None]) + c[None, None, :] * m[..., None]

    rim = blur(top_band(face_in, int(0.07 * h)), 0.012 * h) * 0.34
    rgb = rgb * (1 - rim[..., None]) + 255.0 * rim[..., None]

    return save(rgb, np.clip(tails + face_m, 0, 1), W, H, name)


ribbon("pixelflow_ui_ribbon_blue", 900, 250)
ribbon("pixelflow_ui_ribbon", 900, 250, face=238, tail=170, keyline=KEYLINE)

print("done")
