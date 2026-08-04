# Development Note — Pixel Flow Clone

Unity 6000.3.9f1 · URP 17.3 · Android portrait · 80+ commits

Structured against the assignment brief's development note template
(§8). Where a decision departs from the brief or the internal design
document, the deviation is stated along with the evidence behind it.

---

## 1. Reference Understanding

The reference (*Pixel Flow*, Google Play) reads as a **rhythm-of-pressure**
puzzle rather than a matching puzzle in the usual sense. Its core loop:
coloured shooters travel a fixed circular rail around a pixel-art board and
fire sideways into it as they pass, breaking the nearest matching-colour cube
in each lane. The player's only input is *when* to release the next shooter
from a queue onto that rail.

The mechanic that actually produces the puzzle tension is easy to misread
from a screenshot, and getting it right was the first real analysis problem:

> **A shooter only spends ammo on a hit — a miss costs nothing.** So running
> out of ammo is never the failure mode. The failure mode is a shooter that
> cannot empty itself within a single lap of the rail: it then drops into a
> small parking area, and if that area is already full when a shooter lands,
> the level is lost immediately.

That single rule is what makes the reference feel tense rather than
mechanical — the player is not managing a resource, they are managing
*traffic*. A shooter with a lot of ammo is not an advantage; it is a shooter
that is statistically less likely to empty itself in one lap, and therefore
more dangerous to release carelessly. Confirming this by reading the
reference's behaviour frame-by-frame, rather than assuming "more ammo helps",
was the single most important piece of reference analysis, because every
level-design and pacing decision downstream depends on it being right.

Two supporting systems complete the player experience: the rail visibly
speeds up under pressure (so the player can feel a level tightening), and the
board itself functions as the "win" surface — it is simply the picture that
must be fully cleared. Colour readability and camera framing (the whole board
visible on a single portrait screen, cubes scaled to fit) turned out to
matter as much as the mechanic itself, because a puzzle the player can't read
at a glance is not a mobile casual puzzle regardless of how sound its rules
are.

---

## 2. Your Implementation

### What was built

The full loop specified in the brief is implemented: main menu → level select
by progression → play → win/lose → retry or continue, with progress
persisted to disk between sessions.

- **Rail and board.** A fixed-rectangle rail (`TrackController`, `TrackPath`)
  carries shooters around a pixel-art board (`BoardController`, a grid of
  `CubeCell`/`CubeView`). A shot only breaks the nearest cube in its lane if
  the colour matches.
- **The core rule above, faithfully reproduced.** Ammo is spent only on a
  hit; a shooter that fails to empty within one lap parks; a lap completing
  against a full park is an immediate loss.
- **Three systems layered on the loop:** a pressure ramp that speeds the rail
  up once too many shooters are in play, a two-stage loss warning (park
  fills, then again as a shooter enters its final stretch home while the
  park is still full), and an endgame run once too few shooters remain to
  ever fill the park, at which point the level becomes unlosable and
  obstacle crates lift away.
- **Ten hand-authored levels**, each verified against a Python simulator
  that plays the level under the game's own rules with three different
  strategies (`Tools/levelgen/sim.py`) — the practical form of "levels should
  gradually introduce difficulty" and "at least three complete playthroughs".
- **Full UI**: main menu, settings, profile, store front (visual only — see
  below), HUD, win panel, lose panel, all built on a shared `BasePanel` with
  consistent open/close animation.
- **Juice throughout**: DOTween-driven hops, slides, squash-and-stretch cube
  breaks, a tracer projectile, queue ripple movement, and a hand-written toon
  shader with an outline pass.
- **Progress saving**: current level index persists to
  `Application.persistentDataPath` via a small `ISerializer` abstraction
  (`JsonSaveSystem`), satisfying the brief's "saving unlocked level or
  current level is sufficient" bar directly.

### What was simplified

The brief invites the intern to decide scope, and several things were
deliberately left out rather than half-built:

- **Linked/chained shooters.** The data schema has a field for it
  (`linkedCount`); the behaviour does not exist. The six core mechanics the
  brief asks for are already covered without it, and the time went to the
  critical delivery path instead — levels, build, video, documentation.
- **Store, economy and lives.** These panels are visual only. They have no
  functional depth and never touch gameplay logic — the goal was visual
  completeness of the shell, not a second game system.
- **No level solver.** Deadlock detection is a simple ammo-vs-cube-count
  check per colour (§3), not a search over board states. A solver would be
  disproportionate for a ten-level prototype.
- **No rescue grace period.** An earlier design had a timed window after the
  park filled before a loss triggered. The reference does not have this —
  landing on a full park is instant failure — so it was removed in favour of
  a warning *before* the moment of failure rather than a grace period after
  it.

---

## 3. Architecture

### Assembly boundaries, enforced by the compiler

```
MobileCore  <-  Game  <-  Game.Editor
                  ^
                Tests
```

`MobileCore` holds anything game-agnostic — pooling, save/load, the
generic UI base class, input routing — and cannot reference `Game`. This
is deliberate reuse scaffolding, not premature abstraction: everything in
it is used by more than one system already.

`Game` holds everything specific to this puzzle. `Game.Editor` (the Level
Designer, scene gizmo handles) is Editor-only and cannot ship in a build.

### Communication: gameplay never knows the UI exists

All cross-layer communication runs one way through a static `GameEvents`
class. Gameplay code raises events (`OnLevelCompleted`, `OnRemainingCubesChanged`,
`OnShooterLaunched`, …); UI code listens. No gameplay class holds a
reference to a panel, and no panel reaches into gameplay state directly.
This was the rule most worth keeping strict, because it is the one that
makes "add a new HUD element" or "change what triggers the lose screen"
a one-sided change instead of a two-sided one.

### Data-driven levels

No level information lives in a script. A level is a `LevelData`
ScriptableObject: board size, the pixel array, the palette subset, the
shooter queue, and the three capacities (rail, park, queue columns).
Anything that applies to the whole game — rail speed, pressure thresholds,
layout proportions — lives in a single `GameConfig` asset instead. Colour
itself has one source of truth, `ColorPalette`, read identically by cubes,
shooters and the level editor.

Adding an eleventh level requires zero script changes: create a
`LevelData` asset, paint it in the Level Designer, add it to
`LevelManager`'s list.

### The authoring rule the architecture enforces

Per colour, total queue ammo must equal the board's cube count of that
colour exactly. Too little and the level is mathematically unsolvable;
too much and the shooter carrying the surplus can never empty and stays
stuck forever. `LevelData.OnValidate` and the Level Designer's validation
panel both check this live, in the editor, before a level is ever played.

### Tooling built to support the architecture

- **Level Designer** (`Window > Level Designer`): paints the board, edits
  the queue per column, shows the live ammo budget.
- **`Tools/levelgen/sim.py`**: plays a level under the real game rules with
  three player policies and reports whether it is winnable — the automated
  half of "levels are solvable".
- **`Tools/uigen/`**: generates all 55 UI sprites in Python/Pillow, so the
  UI look can be iterated without touching Figma or hand-painting sprites
  one at a time.

---

## 4. Problems Solved

### The rail was speed-dependent on board shape

Distance along the rail was originally counted in *lanes*
(`2*(width+height)`), while the rail itself is a fixed rectangle in world
space. On a 39x27 board the bottom edge spread 39 lanes across 7.2 world
units and the right edge spread 27 lanes across 8.8 — so a constant lane
speed ran **1.77x faster down the sides than along the bottom**, and larger
boards ran visually slower overall.

The fix was to express speed as a **lap duration** and convert once at init:

```
baseSpeed = path.Perimeter / trackLapSeconds
```

The perimeter cancels, so every level runs at the same visual pace
regardless of board size.

### The lose screen never appeared

Two independent bugs stacked into one symptom. The rail kept turning after
the win/lose decision, so shooters completed further laps and raised the
loss a second time; and the panel switcher calls `Hide` before reopening the
same panel, so the second event's `Hide` completed *on top of* the freshly
opened panel and closed it. Fixed by making the win/lose decision fire
exactly once and stop the rail in the same call — both halves were
necessary; either alone still failed.

### Levels bled into each other

Level teardown (clearing the board, park, queue and rail) was duplicated by
each caller, and the actual menu Play button never made those calls at all.
A player who reached the menu with shooters still parked, then pressed Play
again, got the next level's shooters landing on those exact slots,
overlapping the old ones. Fixed by consolidating every teardown into a
single method that every entry point calls, each step guarding its own
uninitialised state so it is also safe on the very first level load.

### The seam between cubes disappeared on dense boards

Cube spacing was a pure percentage of cell size. As cells shrank on the
denser boards, the absolute gap fell below one pixel and dissolved into the
anti-aliasing. Measuring the reference showed the seam is neither constant
nor proportional — it follows an affine model (a fixed world-space base
plus a percentage of the cell), which was fit from two measured data points
and holds at both extremes of board density.

### The palette read too flat and too saturated

Measured against reference screenshots, our colours were both more uniformly
saturated and more tightly clustered in brightness than the reference, which
gets its visual calm from a *wide* value range (a lot of dark mass, a little
bright accent) rather than from muted colours. New palette entries were
appended — never inserted — so every existing hand-authored level kept its
exact colours; only the available range grew.

### Reference measurement has its own error

Frames pulled from reference video gave results varying 17–33% between rows
of the same image, purely from JPEG compression. That spread is the
measurement's own error, not signal. Uncompressed phone screenshots were
used as the reliable basis, with video frames kept only for corroboration —
knowing the limit of the measuring instrument mattered as much as the
measurement itself.

---

## 5. Known Issues

Full detail in [`KNOWN_ISSUES.md`](KNOWN_ISSUES.md). Summary:

- **GPU instancing is not enabled**, and it is not a one-line fix: the
  shader declares no instancing pragma and no per-instance colour property,
  and cube colour is driven through a `MaterialPropertyBlock`, which pulls
  every cube out of the SRP Batcher as well. On the densest level (1400
  cubes) this is the main open performance item.
- **No current on-device performance measurement.** The last device test
  predates both the largest boards and the post-processing volume.
- **No background music or park-warning sound.** Neither clip was sourced;
  the fields were removed rather than left as empty placeholders. The music
  volume slider in Settings is wired but currently silent.
- **The level simulator is a lower bound, not ground truth.** It reports one
  level as unwinnable under all three of its policies, though that level is
  comfortable to win by hand — useful for catching genuinely broken levels,
  not for grading difficulty.
- Levels 9 and 10 (the largest, finished latest) have had far fewer manual
  playthroughs than the earlier levels, though both pass the simulator.

---

## 6. Next Steps

If this became a production prototype, in priority order:

1. **Shader instancing.** Add the missing instancing pragma, move cube
   colour into a per-instance buffer, and switch the cube renderer to use
   it. This is the one open item with a measurable effect on the product.
2. **Re-measure performance on device** now that the largest boards and the
   post-processing volume are both in place — the last real number predates
   both.
3. **Linked shooters.** The data field already exists; this is the first
   genuinely new mechanic I would add, ahead of any further visual work,
   because it extends the puzzle vocabulary rather than just polishing it.
4. **Background music and a park-warning sound**, to close the one polish
   gap that's a missing asset rather than a missing decision.
5. **More levels, difficulty estimated from data.** With the simulator
   already in place, level difficulty could be scored automatically —
   for example by how close each policy comes to filling the park — instead
   of judged by feel alone.
