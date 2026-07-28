"""Sample the reference screenshots for real colour values.

Per row we take the modal (most common) pixel, which rejects text glyphs and
icons sitting on top of the button body.
"""
import glob
from collections import Counter

import numpy as np
from PIL import Image

REF = 'C:/MyGameProjects/Pixel_Flow_Clone/Assets/_Game/Art/References/uiResources/'
SCALE = 1179 / 923  # screenshots are 1179 wide, my coords came off a 923-wide view


def load(frag):
    p = [f for f in glob.glob(REF + '*.PNG') if frag in f][0]
    return np.asarray(Image.open(p).convert('RGB')).astype(int)


def row_modes(img, box, steps=22):
    x0, y0, x1, y1 = [int(v * SCALE) for v in box]
    out = []
    for i in range(steps):
        y = y0 + round((y1 - y0 - 1) * i / (steps - 1))
        row = img[y, x0:x1]
        c = Counter(map(tuple, row)).most_common(1)[0][0]
        out.append((round(100 * i / (steps - 1)), c))
    return out


def report(name, img, box, steps=22):
    print(f'--- {name} ---')
    prev = None
    for pct, c in row_modes(img, box, steps):
        mark = '' if c == prev else '  <'
        print(f'  {pct:3d}%  #{c[0]:02X}{c[1]:02X}{c[2]:02X}  {c}{mark}')
        prev = c


if __name__ == '__main__':
    st = load('ayarlarpaneli')
    report('settings: orange button', st, (158, 948, 450, 1090))
    report('settings: green button', st, (482, 948, 770, 1090))
    report('settings: purple button', st, (158, 1122, 450, 1270))
    report('settings: blue button', st, (482, 1122, 770, 1270))
    report('settings: panel body', st, (110, 580, 812, 1540), steps=12)
    report('settings: title strip', st, (110, 458, 812, 560), steps=8)
    report('settings: close X', st, (728, 480, 794, 546), steps=12)
    report('settings: toggle ON', st, (556, 712, 648, 774), steps=10)
