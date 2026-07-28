# UI texture generator

Parametric source for every sprite in `Assets/_Game/Art/Sprites/UI`. The shapes
are generated from code rather than hand-painted, so a colour or proportion can
be changed by editing one number and re-running.

## Regenerating

```
python generate_all.py     # panels, buttons, pills, ribbon  -> out/
python icons.py            # icon set                        -> out/
python import_to_unity.py  # copies to Assets/ + writes .meta import settings
```

`import_to_unity.py` derives each `.meta` GUID from the asset path, so re-running
it never orphans an existing reference. It also sets the sprite import settings
(Full Rect, Alpha Is Transparency, no mipmaps, Clamp, no compression) and the
9-slice borders, so no manual Inspector work is needed.

## Two texture families, and why

Sampling the reference screenshots (`measure.py`) showed the UI uses two
different constructions, not one:

**`candy`** — panel buttons. Heavy black keyline, a white rim along the top edge,
a two-tone body, a light lip and a dark inner shadow at the bottom. The
light→deep step is a **saturation increase at near-constant value**, e.g. the
yellow button goes `#F9D160` → `#F7B92A` and the green `#67EF77` → `#16E651`.
A neutral-grey texture under Unity's multiplicative `Image.color` can only scale
all three channels equally, which lowers value and leaves saturation alone — the
opposite operation. So these ship **pre-coloured**, one PNG per style, generated
from measured values.

**`soft`** — HUD chrome (level pill, counters, gear). A plain vertical gradient
with a thin or absent keyline. The reference re-tints these per level theme, so
they ship **neutral grey** and are tinted at runtime, as originally planned.

This is a deliberate departure from GDD §4.3, which specified neutral + tint for
everything. The measurement above is the reason; it is recorded here and belongs
in the development note.

## 9-slice rules

Everything is authored as a function of y inside the silhouette, so the centre
band tiles exactly. Rim and shadow follow the outline via edge masks, which stay
correct inside the fixed corner regions.

| Sprite | Border (L,B,R,T) | Usage |
|---|---|---|
| `button_*` 600×290 | 59, 0, 59, 0 | Sliced. Stretch width freely; height scales all proportions. |
| `buttonsq_*` 300×300 | 84, 0, 84, 0 | Simple for square icon buttons. |
| `pill` 400×120 | 60, 0, 60, 0 | Sliced. True pill — use at its authored height, vary width only. |
| `circle` 300×300 | 0 | Simple. |
| `iconframe` 300×300 | 62, 62, 62, 62 | Simple below ~140 px, Sliced above. |
| `panel*` 800×1000 | 34, 34, 34, 34 | Sliced. |
| `panel_header*` 800×130 | 34, 0, 34, 34 | Sliced. Top corners round, bottom square. |
| `ribbon*` 900×250 | 100, 0, 100, 0 | Sliced. Borders cover the tails. |
| `icon_*` 256×256 | 0 | Simple. White fill, so tint gives the exact colour. |

## Verifying

`mock_settings.py` and `mock_win.py` rebuild those two screens out of the
generated sprites — 9-sliced and tinted exactly as Unity's `Image` does — and
write a side-by-side against the reference screenshot into `out/`. Use them
after any change instead of eyeballing the PNGs.
