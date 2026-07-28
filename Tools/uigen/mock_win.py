import glob

from PIL import Image

from preview import nine_slice, tint, hexc

REF = 'C:/MyGameProjects/Pixel_Flow_Clone/Assets/_Game/Art/References/uiResources/'


def sprite(n):
    return Image.open(f'out/{n}.png')


def place(c, img, border, box, color=None):
    x0, y0, x1, y1 = box
    im = tint(img, hexc(color)) if color else img
    im = nine_slice(im, border, x1 - x0, y1 - y0) if border else \
        im.resize((x1 - x0, y1 - y0), Image.LANCZOS)
    c.alpha_composite(im, (x0, y0))


W, H = 923, 2000
mock = Image.new('RGBA', (W, H), hexc('#14161F') + (255,))

place(mock, sprite('pixelflow_ui_panel_blue'), (34, 34, 34, 34), (100, 760, 840, 1470))
place(mock, sprite('pixelflow_ui_ribbon'), None, (128, 648, 800, 810), '#2A93FF')
place(mock, sprite('pixelflow_ui_panel'), (34, 34, 34, 34), (155, 925, 782, 1215), '#3FA9FF')
place(mock, sprite('pixelflow_ui_button_green'), (59, 59, 0, 0), (155, 1282, 452, 1402))
place(mock, sprite('pixelflow_ui_button_yellow'), (59, 59, 0, 0), (478, 1282, 775, 1402))

ref = Image.open([f for f in glob.glob(REF + '*.PNG') if 'winpanel' in f][0]) \
    .convert('RGBA').resize((W, H), Image.LANCZOS)

box = (70, 600, 880, 1520)
out = Image.new('RGBA', ((box[2] - box[0]) * 2 + 36, box[3] - box[1] + 24),
                hexc('#2A2D48') + (255,))
out.alpha_composite(ref.crop(box), (12, 12))
out.alpha_composite(mock.crop(box), (box[2] - box[0] + 24, 12))
out.save('out/_cmp_win.png')
print('ok')
