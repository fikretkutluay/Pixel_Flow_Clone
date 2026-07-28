"""Verification harness: 9-slice + multiplicative tint, exactly like Unity's
Image component, so what we look at here is what the game will show."""

import numpy as np
from PIL import Image


def hexc(s):
    s = s.lstrip("#")
    return tuple(int(s[i:i + 2], 16) for i in (0, 2, 4))


def tint(img, color):
    """Unity Image.color is multiplicative: result = texel * color."""
    a = np.asarray(img.convert("RGBA"), dtype=np.float64)
    for i, c in enumerate(color):
        a[..., i] *= c / 255.0
    return Image.fromarray(np.clip(a, 0, 255).astype(np.uint8), "RGBA")


def nine_slice(img, border, tw, th):
    """border = (left, right, top, bottom) in px. Corners fixed, edges stretched."""
    l, r, t, b = border
    W, H = img.size
    cw, ch = W - l - r, H - t - b          # source centre
    dw, dh = max(tw - l - r, 1), max(th - t - b, 1)   # dest centre

    cols_s = [(0, l), (l, l + cw), (W - r, W)]
    rows_s = [(0, t), (t, t + ch), (H - b, H)]
    cols_d = [(0, l), (l, l + dw), (tw - r, tw)]
    rows_d = [(0, t), (t, t + dh), (th - b, th)]

    out = Image.new("RGBA", (tw, th), (0, 0, 0, 0))
    for (sy0, sy1), (dy0, dy1) in zip(rows_s, rows_d):
        for (sx0, sx1), (dx0, dx1) in zip(cols_s, cols_d):
            if sx1 <= sx0 or sy1 <= sy0 or dx1 <= dx0 or dy1 <= dy0:
                continue
            piece = img.crop((sx0, sy0, sx1, sy1)).resize((dx1 - dx0, dy1 - dy0), Image.LANCZOS)
            out.paste(piece, (dx0, dy0), piece)
    return out


def sheet(bg_hex, items, pad=28, cols=None):
    """items = [(PIL RGBA image, label_or_None)] laid out in a row/grid."""
    imgs = [i for i, _ in items]
    cols = cols or len(imgs)
    rows = (len(imgs) + cols - 1) // cols
    cw = max(i.width for i in imgs) + pad
    chh = max(i.height for i in imgs) + pad
    canvas = Image.new("RGBA", (cw * cols + pad, chh * rows + pad), hexc(bg_hex) + (255,))
    for n, im in enumerate(imgs):
        cx, cy = n % cols, n // cols
        x = pad + cx * cw + (cw - pad - im.width) // 2
        y = pad + cy * chh + (chh - pad - im.height) // 2
        canvas.alpha_composite(im, (x, y))
    return canvas
