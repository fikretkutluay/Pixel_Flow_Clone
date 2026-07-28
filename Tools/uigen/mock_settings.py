"""Assemble a Settings panel from the generated textures and put it beside the
reference screenshot at identical scale."""
import glob

from PIL import Image

from preview import nine_slice, tint, hexc

REF = 'C:/MyGameProjects/Pixel_Flow_Clone/Assets/_Game/Art/References/uiResources/'
S = 1179 / 923            # screenshots are 1179 wide; layout coords are in 923-space


def sprite(name):
    return Image.open(f'out/{name}.png')


def place(canvas, img, border, box, color=None):
    x0, y0, x1, y1 = box
    im = tint(img, hexc(color)) if color else img
    im = nine_slice(im, border, x1 - x0, y1 - y0) if border else \
        im.resize((x1 - x0, y1 - y0), Image.LANCZOS)
    canvas.alpha_composite(im, (x0, y0))


W, H = 923, 2000
mock = Image.new('RGBA', (W, H), hexc('#141A33') + (255,))

place(mock, sprite('pixelflow_ui_panel_blue'), (34, 34, 34, 34),
      (100, 452, 820, 1552))
place(mock, sprite('pixelflow_ui_panel_header_blue'), (34, 34, 34, 0),
      (100, 452, 820, 566))

for name, box in [('yellow', (158, 946, 452, 1092)), ('green', (482, 946, 776, 1092)),
                  ('purple', (158, 1122, 452, 1268)), ('blue', (482, 1122, 776, 1268))]:
    place(mock, sprite(f'pixelflow_ui_button_{name}'), (59, 59, 0, 0), box)

place(mock, sprite('pixelflow_ui_buttonsq_red'), None, (724, 478, 798, 552))

# settings rows: iconframe + a pill for the toggle track
for i in range(3):
    y = 610 + i * 108
    place(mock, sprite('pixelflow_ui_iconframe'), None, (152, y, 224, y + 72), '#FFFFFF')
place(mock, sprite('pixelflow_ui_pill'), (60, 60, 0, 0), (556, 706, 780, 782), '#FFF06B')
place(mock, sprite('pixelflow_ui_pill'), (60, 60, 0, 0), (556, 814, 780, 890), '#FFF06B')

ref = Image.open([f for f in glob.glob(REF + '*.PNG') if 'ayarlar' in f][0]) \
    .convert('RGBA').resize((W, H), Image.LANCZOS)

box = (60, 400, 870, 1620)
out = Image.new('RGBA', ((box[2] - box[0]) * 2 + 36, box[3] - box[1] + 24),
                hexc('#2A2D48') + (255,))
out.alpha_composite(ref.crop(box), (12, 12))
out.alpha_composite(mock.crop(box), (box[2] - box[0] + 24, 12))
out.save('out/_cmp_settings.png')
print('ok — left reference, right generated')
