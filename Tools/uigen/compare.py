"""Side-by-side: generated texture (9-sliced to the reference's own pixel size)
against the reference crop it was derived from."""
import glob

import numpy as np
from PIL import Image

from preview import nine_slice

REFDIR = 'C:/MyGameProjects/Pixel_Flow_Clone/Assets/_Game/Art/References/'
SCALE = 1179 / 923


def ref_crop(sub, frag, box):
    p = [f for f in glob.glob(f'{REFDIR}{sub}/*') if frag in f and f.lower().endswith(('.png',))][0]
    im = Image.open(p).convert('RGBA')
    x0, y0, x1, y1 = [int(v * SCALE) for v in box]
    return im.crop((x0, y0, x1, y1))


def hexc(s):
    s = s.lstrip('#')
    return tuple(int(s[i:i + 2], 16) for i in (0, 2, 4))


def duo(gen_path, border, ref_img, bg='#4ABBFF', label_gap=18):
    """Scale the generated sprite to the reference crop's exact size."""
    g = nine_slice(Image.open(gen_path), border, ref_img.width, ref_img.height)
    W = ref_img.width * 2 + label_gap * 3
    H = ref_img.height + label_gap * 2
    c = Image.new('RGBA', (W, H), hexc(bg) + (255,))
    c.alpha_composite(ref_img, (label_gap, label_gap))
    c.alpha_composite(g, (label_gap * 2 + ref_img.width, label_gap))
    return c


def stack(imgs, bg='#2A2D48', pad=14):
    W = max(i.width for i in imgs) + pad * 2
    H = sum(i.height for i in imgs) + pad * (len(imgs) + 1)
    c = Image.new('RGBA', (W, H), hexc(bg) + (255,))
    y = pad
    for i in imgs:
        c.alpha_composite(i, (pad, y))
        y += i.height + pad
    return c
