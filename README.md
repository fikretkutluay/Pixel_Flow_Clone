# Pixel Flow Clone

A mobile puzzle-shooter built in Unity as an internship project: coloured shooters
circle a rail around a pixel-art board and break the cubes they match. Clear the
board before the parking area fills up.

| | |
|---|---|
| **Engine** | Unity 6000.3.9f1 |
| **Render pipeline** | URP 17.3 |
| **Target** | Android, portrait |
| **Input** | Unity Input System 1.18 |
| **Tweening** | DOTween |
| **Levels** | 10, hand-authored |

---

## Running the project

1. Open the project in Unity **6000.3.9f1**. Other versions are untested.
2. Open `Assets/_Game/Scenes/MainScene.unity`. It is the only scene — level
   changes rebuild the board rather than loading a new scene.
3. Press Play. The game opens on the main menu; **Play** starts at the furthest
   level reached, which is persisted to disk.

To jump straight to a specific level while iterating, select the `LevelManager`
object under `MANAGERS`, set **Test Level Index** (one-based), then right-click
the component header and choose **Load Test Level**.

### Android build

`File > Build Settings > Android`, with `MainScene` as the only enabled scene.
Minimum SDK is 25. Nothing else needs configuring — the camera locks to a fixed
world width, so any portrait aspect ratio frames correctly.

---

## How the game works

A level is a pixel-art picture made of coloured cubes. Shooters wait in queued
columns beneath it. Tapping the front shooter of a column commits it to the
**rail**, a fixed rectangle that runs around the board.

While a shooter travels the rail it fires sideways into the board on every lane it
passes. A shot only breaks the nearest cube in that lane if the colours match.

The pressure comes from a detail that is easy to get backwards:

> **Ammo is only spent on a hit — misses are free.** So the difficulty is never
> "not enough ammo". It is that a shooter which cannot empty itself in a *single
> lap* drops into the park. If a lap ends while the park is already full, the
> level is lost immediately.

The consequence is counter-intuitive and drives the level design: **a
high-ammo shooter is harder, not easier.**

Three systems layer on top of that:

- **Pressure ramp** — once rail + park reaches 7 shooters, the rail speeds up by
  1.25x, eased in rather than snapped. Deliberately never randomised: the design
  depends on the player predicting when a shooter lands, so a change of pace has
  to be something they can see coming.
- **Loss warning** — the park slots flash red when the park fills, and again each
  time a shooter enters the last 22% of its lap while the park is still full. The
  second one is the one that matters; that is the window the player has to act in.
- **Endgame run** — once too few shooters remain to ever fill the park, the level
  cannot be lost. Shooters stop parking, the rail speeds up, and crates lift away.
  The park and queue are deliberately *not* auto-emptied: the player keeps their
  remaining aiming decisions.

---

## Project layout

```
Assets/
├── _Game/                    game-specific code and content
│   ├── _Scripts/
│   │   ├── Board/            grid, cube views, lane raycasting, tracers
│   │   ├── Track/            rail path, shooter movement, chevrons
│   │   ├── Queue/  Park/     the two shooter holding areas
│   │   ├── Flow/             GameManager, LevelManager, camera and layout
│   │   ├── GameUI/           panels (menu, HUD, win, lose, settings, profile)
│   │   └── Tests/            edit-mode unit tests
│   ├── Data/                 LevelData, GameConfig, ColorPalette assets
│   ├── Editor/               Level Designer window, scene gizmo handles
│   ├── Art/                  shaders, materials, sprites, fonts, references
│   ├── Prefabs/  Audio/  Scenes/
├── _MobileCoreScripts/       reusable, game-agnostic core (MobileCore asmdef)
│   ├── Pooling/  Events/  Audio/  Input/  Save/  UI/  Grid/  Collections/
└── Plugins/Demigiant/        DOTween

Tools/
├── uigen/                    Python + Pillow UI sprite generation
└── levelgen/                 level read/write and playability simulation
```

### Assembly boundaries

Four assemblies, with the dependency direction enforced by the compiler rather
than by convention:

```
MobileCore  <-  Game  <-  Game.Editor
                  ^
                Tests
```

`MobileCore` cannot reference `Game`. Editor code is confined to an Editor-only
assembly, so nothing editor-side can leak into a build.

The layering rule that matters most in practice: **gameplay never knows about the
UI.** All communication runs one way through the static `GameEvents` class —
gameplay raises, UI listens.

---

## Data-driven design

No level information lives in a script. A level is a `LevelData` ScriptableObject
holding the board size, the pixel array, the palette subset, the shooter queue and
the three capacities. Anything level-specific goes there; anything game-wide goes
in `GameConfig`.

`ColorPalette` is the single source of truth for colour, read by the cubes, the
shooters and the Level Designer alike.

### Adding a level — no code change required

1. `Assets > Create > Scriptable Objects > LevelData`, saved under
   `Assets/_Game/Data/Levels/`.
2. Open `Window > Level Designer` and paint the board, palette and queue.
3. Select the `LevelManager` object in `MainScene` and add the new asset to its
   **Levels** array.

That is the whole process. No script is edited, recompiled or even opened —
levels are pure data.

### The level authoring rule

**Per colour, total queue ammo must equal the board's cube count exactly.**

Too little and the level is unsolvable. Too much and the shooter carrying the
surplus never empties, so it stays stuck on the rail or in the park forever.
Both cases are caught by `LevelData.OnValidate` and shown live in the Level
Designer's validation panel.

---

## Tools

### Level Designer

`Window > Level Designer`. Paint the board directly, edit the queue per column,
and read a live per-colour ammo budget. A **Trim** button shrinks the board to its
occupied region — empty margins distort the aspect ratio and needlessly shrink the
cells when the board is fitted to its area.

### `Tools/levelgen/` (Python)

| File | Purpose |
|---|---|
| `levelio.py` | Read/write `LevelData.asset`, render the board as ASCII |
| `sim.py` | Play a level under the game's own rules and report whether it is winnable |
| `quantize.py` | Quantise an image to the palette |

`sim.py` gives a **lower bound**, not ground truth: it calls Level_5 unwinnable,
but the level is comfortable to win by hand. The bot can miss an ordering.

### `Tools/uigen/` (Python)

Generates the 55 UI sprites in `Assets/_Game/Art/Sprites/UI/` parametrically with
Pillow.

```
python generate_all.py && python icons.py && python avatars.py
python import_to_unity.py     # copies into Assets and writes .meta import settings
```

`import_to_unity.py` leaves existing `.meta` files alone and derives GUIDs from the
asset path, so re-running it never breaks references.

---

## Tests

Edit-mode unit tests under `Assets/_Game/_Scripts/Tests/`, run via
`Window > General > Test Runner`.

Coverage is deliberately narrow — `LaneRaycaster` and `BoundedBuffer` only. Both
are pure C# with no Unity dependency, which is exactly what makes them worth
testing; the rest of the game is behaviour best verified by playing it.

---

## Third-party assets

| Asset | Use | Licence |
|---|---|---|
| **DOTween** (Demigiant) | All tweening and juice | Free for commercial use |
| **TextMeshPro** | All text rendering | Unity package |
| **Baloo 2 ExtraBold** (Ek Type) | The game's only font | SIL OFL 1.1 — `Assets/_Game/Art/Text/OFL.txt` |
| **Sound effects** (Freesound / Pixabay contributors) | Cube break, launch, UI click, win and lose stingers | CC0 / Pixabay licence, free for commercial use |

The GDD specified **Lilita One** as the font. It was replaced: Lilita One's TTF
ships 225 glyphs and is **missing Ğ ğ İ Ş ş**, verified by reading its cmap table.
That is unusable for a Turkish interface. Baloo 2 carries 856 glyphs with full
coverage.

No third-party art was used. The UI sprites are generated by `Tools/uigen/`, and
the mascot and avatars are derived from the project's own `Cubic_Dog` model.

---

## Documentation

Everything below lives in [`Docs/`](Docs/), outside `Assets/` so none of it is
imported into the Unity project as an asset.

- **[`DEVELOPMENT_NOTE.md`](Docs/DEVELOPMENT_NOTE.md)** — decisions, deviations,
  and what was measured to justify them.
- **[`KNOWN_ISSUES.md`](Docs/KNOWN_ISSUES.md)** — current defects and limitations.
- **[`HANDOFF.md`](Docs/HANDOFF.md)** — working state and next steps (Turkish).
- **[`Pixel_Flow_Clone_GDD.docx`](Docs/Pixel_Flow_Clone_GDD.docx)** — the original
  design document. Sections 6 and 7 are superseded by `HANDOFF.md`; the
  architectural contract and palette sections still stand.
- **[`Pixel Flow Assignment.pdf`](<Docs/Pixel Flow Assignment.pdf>)** — the
  original internship brief this project answers.

---

## Delivery

- **Repository:** this page — code, README, and everything in `Docs/`.
- **Android build (APK) and gameplay video:** [Google Drive folder](https://drive.google.com/drive/folders/17JeAlhVoAYYIxj2IxnA1MqlUUv_bbva2?usp=sharing)
