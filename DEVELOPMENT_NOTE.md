# Development Note — Pixel Flow Clone

Unity 6000.3.9f1 · URP 17.3 · Android portrait · 79 commits

This note records how the project was approached, what was built, which problems
were genuinely difficult, and which things were deliberately *not* built. Where a
decision departs from the design document, the deviation is stated along with the
evidence behind it.

---

## 1. Approach

The brief set five binding constraints: level data must not be hardcoded in
scripts, systems must not be over-engineered, the main gameplay scene must be
clearly named and easy to find, the project must run without console errors, and
ten levels must ship.

Two of those shaped almost every decision.

**Data-driven, taken literally.** No level information lives in code. A level is a
`LevelData` ScriptableObject; a value that belongs to the whole game lives in
`GameConfig`. There is no third place, and no magic number in a script. This was
not merely satisfied but used as a working rule: whenever a number appeared in a
script during development, the question asked was "is this level-specific or
game-wide?" — and it moved to whichever asset the answer named.

**"Don't over-engineer" read as a design constraint, not an excuse.** The
interpretation used throughout: it forbids unnecessary *abstraction layers*, not
completeness. So there is no DI container, no service locator, no Addressables, no
abstract factory hierarchy and no async/await flow architecture. A new abstraction
was only introduced once a second concrete use existed ("rule of two"). At the same
time, the project deliberately went *wider* on visual polish, because a submission
that works but looks half-finished reads worse than one that feels complete.

**Layering enforced by the compiler, not by convention.** Four assemblies:

```
MobileCore  <-  Game  <-  Game.Editor
                  ^
                Tests
```

`MobileCore` is game-agnostic and cannot reference `Game`. Editor code sits in an
Editor-only assembly, so nothing editor-side can reach a build. Within that,
the rule that mattered most day to day: **gameplay never knows the UI exists.**
Communication is one-way through a static `GameEvents` class — gameplay raises,
UI listens. No gameplay class holds a reference to a panel.

**Measure, don't eyeball.** Colour, proportion, glyph coverage and spacing were all
measured programmatically from the reference screenshots or from the asset files
themselves, rather than judged by eye. Several real bugs were only caught this way
— see section 3.

But measurement gives a *floor*, not a target. The measured face gradient was 12%;
0.6 looked considerably better. Measurement tells you where to look, not how far
to go.

---

## 2. Implementation

### Core loop

A level is a pixel-art picture of coloured cubes. Shooters wait in queued columns
below it. Tapping the front shooter of a column commits it to the **rail** — a
fixed rectangle running around the board. As a shooter travels, it fires sideways
into each lane it passes, breaking the nearest cube in that lane if the colours
match.

`TrackController` drives the rail, `BoardController` owns the grid,
`QueueController` and `ParkController` own the two holding areas, and
`GameManager` holds the only two decisions that require reading the whole board at
once: win/lose, and the two global states below.

The single most important mechanical detail:

> **Ammo is spent only on a hit — misses are free.**

So the difficulty is never "not enough ammo". It is that a shooter unable to empty
itself in a *single lap* drops into the park, and if a lap ends while the park is
already full, the level is lost immediately. The counter-intuitive consequence,
which drives the whole level design: **a high-ammo shooter is harder, not easier.**

### Three systems layered on the loop

**Pressure ramp.** Once rail + park reaches 7 shooters, the rail speeds up 1.25x,
eased in over 0.35s rather than snapped. Deliberately never randomised: the design
depends on the player predicting when a shooter will land, and a random change of
pace destroys that prediction and makes a loss feel unfair.

**Loss warning.** Park slots flash red twice — once when the park fills, and again
each time a shooter enters the final 22% of its lap while the park is still full.
The second is the one that matters; that is exactly the window in which the player
can still free a slot. A continuously blinking border was tried first and failed:
the eye adapts within seconds and stops seeing it.

**Endgame run.** Once too few shooters remain to ever fill the park, the level
cannot be lost. Holding the player there is just waiting, so shooters stop parking,
the rail speeds up 1.6x, and crates lift away — a crate exists to block a lane, and
blocking has stopped meaning anything.

The first version of the endgame also auto-released everything from the park and
queue. That was wrong: it took the player's last aiming decisions away and finished
the level by itself. Now only the shooters already on the rail keep circling; the
park and queue stay in the player's hands.

### Presentation

All juice is DOTween, concentrated in `ShooterAnimator`, `CubeView` and `Tracer`:
a hop from rail into park with a landing squash, a wave-like shuffle when the queue
advances, cube breaks that swell then collapse with a twist, a spin-out for spent
shooters, and a tracer whose flight delays the cube's break animation so the cube
reacts to being hit rather than ahead of it. The cube is removed from the grid
*immediately* — the win check never waits on presentation.

Shading is a hand-written shader (`ToonCube.shader`) with three passes: an
inverted-hull outline, the lit body, and depth. Cube colour is driven per-instance
through a `MaterialPropertyBlock`, so one material serves nineteen palette colours.

### Level authoring

Ten levels, all hand-authored, all verified against a Python simulator
(`Tools/levelgen/sim.py`) that reproduces the game's rules — lane peeling, laps,
parking, capacities, crate removal in the endgame — and plays each level under
three different player policies. This is the automated form of the GDD's "every
level proven by at least three complete playthroughs" requirement.

An in-editor **Level Designer** window paints boards directly, edits the queue per
column, and shows a live per-colour ammo budget.

The authoring rule that the simulator surfaced: **per colour, total queue ammo must
equal the board's cube count exactly.** Too little and the level is unsolvable; too
much and the shooter carrying the surplus never empties and stays stuck on the rail
or in the park forever. `LevelData.OnValidate` and the Level Designer both catch
each case.

| Level | Board | Cubes | Colours | Shooters | Crates | Note |
|---|---|---|---|---|---|---|
| 1 | 18x10 | 120 | 2 | 8 | — | tutorial |
| 2 | 16x15 | 240 | 4 | 12 | — | |
| 3 | 20x19 | 380 | 6 | 22 | — | 11 hidden "?" |
| 4 | 20x20 | 400 | 3 | 16 | — | 3 hidden |
| 5 | 24x24 | 560 | 6 | 21 | 16 | 3 hidden |
| 6 | 30x30 | 890 | 6 | 34 | 8 | |
| 7 | 16x16 | 200 | 6 | 12 | — | |
| 8 | 20x20 | 370 | 7 | 17 | 12 | deliberate corner lock |
| 9 | 35x41 | 700 | 6 | 30 | — | pixel-art portrait |
| 10 | 40x35 | 1400 | 7 | 44 | — | finale, densest board |

---

## 3. Problems solved

### The rail was speed-dependent on board shape

Distance along the rail was originally counted in *lanes* (`2*(width+height)`),
while the rail itself is a fixed rectangle in world space. On a 39x27 board the
bottom edge spread 39 lanes across 7.2 world units and the right edge spread 27
lanes across 8.8 — so a constant lane speed ran **1.77x faster down the sides than
along the bottom**, and larger boards ran visually slower overall. Level_2 had been
silently compensating with a hand-tuned double speed.

The fix was to express speed as a **lap duration** and convert once at init:

```
baseSpeed = path.Perimeter / trackLapSeconds
```

The perimeter cancels, so every level runs at the same visual pace regardless of
board size. Because the value stopped being level-specific, it moved from
`LevelData` to `GameConfig` and `LevelData.trackSpeed` was deleted.

### The lose screen never appeared

Two independent bugs stacked into one symptom. The rail kept turning after the
win/lose decision, so shooters completed further laps and raised the loss a second
time; and `UIManager.SwitchPanel` calls `Hide` before reopening the same panel, so
the second event's `Hide` completed *on top of* the freshly opened panel and closed
it. Fixed by making `GameManager.Finish` announce exactly once and stop the rail —
both halves were necessary.

### Levels bled into each other

The four teardown `Clear()` calls were duplicated by each caller, and
`HandlePlayRequested` — the actual Play button — never made them at all. A player
who reached the menu with shooters still parked and pressed Play got the next
level's shooters landing on those exact slots, overlapping them. All teardown was
consolidated into `LevelManager.LoadLevel`, with every `Clear()` guarding its own
uninitialised state.

### The seam between cubes disappeared on dense boards

`cubeGap` was a pure percentage of cell size. As cells shrank, the absolute gap
fell below one pixel and dissolved into the anti-aliasing — 0.76px vertically on
Level_10. Thickening the outline does **not** fix this: when two cubes actually
touch, each one's outline loses the depth test against its neighbour's front face.

Measuring the reference showed the seam is neither constant nor proportional:

```
cube 20.0px -> seam 3.0px (15.0%)
cube 50.8px -> seam 5.0px ( 9.9%)
```

The cube grows 2.5x while the seam grows 1.67x. The affine model solved from those
two points — a world-space base plus a percentage of the cell — reproduces both
measurements and holds at either extreme. Both terms are in world units, and since
the camera is locked to a fixed world width, that is resolution-independent.

### The palette was too saturated and too flat

Two separate differences were measured against the reference. Shooter bodies sat at
S≈0.52 in the reference versus 0.57–0.92 (mean 0.72) in ours. More importantly,
reference board cubes spread across V 0.42–1.00 with more than half the area being
a dark mass, while ours were bunched into 0.71–0.83. **The reference gets its calm
not by lowering brightness but by widening the range.**

Contrast extremes were appended to `ColorId` — `Navy` (#233361) and `Black`
(#18181F) as dark mass, `White` (#E9EDF2) as bright accent, plus mid neutrals.
Appending at the *end* was essential: no existing id shifted, so no authored level
broke. Deleting a colour must never happen for the same reason — every id after it
would shift and every level would be corrupted.

Distinguishability was then checked rather than assumed: across the 13 playable
colours the closest pair is dE 26.1 in CIELAB (White/LightGray). The real risk is
within the subset a given level selects, so that subset's dE is what needs checking
when adding a colour or authoring a level.

The palette had also been duplicated in three places, and after recalibration two
were stale: the Level Designer was showing the designer colours that did not appear
in the game — wrong information in the one place level design has to trust. It now
reads the `ColorPalette` asset directly.

### Flat cube faces read as garish

Every pixel of our cube was identical (`[196 97 91]`); in the reference the face
opens up about 12% downward (brown cube 139→156, purple 77→87). Rows of perfectly
flat saturated colour were the main reason the board read as harsh. A
`_FaceGradient` term was added, enabled on board pieces only.

### The font could not spell Turkish

The GDD specified **Lilita One**. Reading its cmap table showed 225 glyphs and no
**Ğ ğ İ Ş ş** — unusable for a Turkish interface. Replaced with **Baloo 2
ExtraBold** (856 glyphs, full coverage, SIL OFL, licence in repo). This is exactly
the kind of defect that eyeballing a sample string would have missed.

### Shader Graph could not produce the outline

The outline needs an inverted hull, which is a second pass, and Shader Graph cannot
emit two passes from one graph. The alternative was a second material slot on every
renderer — more pieces, more prefab surgery. `ToonCube_SG.shadergraph` was replaced
with a hand-written `ToonCube.shader` (hand-editing the graph's JSON was not an
option; it is not reviewable).

Two subtleties in that shader were worth the time. The depth push is written in
clip space against `UNITY_REVERSED_Z`, because the render state's `Offset` pushes
the wrong way under reversed-Z and left black speckles across the bodies. And on
hard-normal meshes (cubes, crates) the hull is inflated from position rather than
normal, or the corners tear apart.

Calibrating the shading against the reference also corrected a misconception: the
shadow does not go **grey, it goes more saturated** (a neutral multiply cannot
produce this), the light band shifts toward **yellow rather than white** and is
additive, and edges **darken rather than glow**. The old graph was *adding* a warm
fresnel at the edges — that was the source of the washed-out look.

### Reference measurement has its own error

Frames pulled from reference video gave results varying 17–33% between rows of the
same image, purely from JPEG compression. That spread is the measurement's own
error, not signal. Uncompressed phone screenshots (1179px) were used as the
reliable basis; video frames only ever as corroboration. Knowing the limit of the
instrument mattered as much as taking the measurement.

---

## 4. Scope decisions

These are decisions, not omissions.

| Decision | Reasoning |
|---|---|
| **No DI container / service locator** | Inspector references plus `GameEvents` cover every need in a project this size. The brief's "clarity first" outweighs a framework. |
| **No Addressables** | Ten levels load by direct reference. Addressables would add build complexity and solve nothing. |
| **No abstract factory / strategy hierarchies** | Applied the "rule of two": no abstraction without a second concrete use. |
| **No async/await flow architecture** | Coroutines and events are sufficient and easier to follow. |
| **No board solver** | Deadlock detection is an O(1) buffer check. A solver is disproportionate for a prototype. |
| **Tests limited to `LaneRaycaster` and `BoundedBuffer`** | Both are pure C# with no Unity dependency, which is what makes them worth unit-testing. The rest is behaviour better verified by playing. |
| **Linked shooters (`linkedCount`) not implemented** | The field exists in the data schema; the behaviour does not. The brief's core mechanic set is already met, and the time went to the critical path — build, levels, video. |
| **Meta UI is visual only** | The store, economy and lives screens carry no functional depth. The aim was visual completeness, and no Core or Gameplay logic was touched to get it. |
| **Rescue window removed** | GDD 1.6 specified a `rescueWindowSeconds` grace period. The reference game has no such thing — a shooter landing on a full park is an immediate loss. Replaced with the warning pulse described in section 2, which puts the signal *before* the moment of failure rather than after it. |
| **UI sprites generated in Python, not Figma** | The available Figma MCP account allows six calls per month, exhausted in a single session. `Tools/uigen/` generates all 55 sprites parametrically and reproducibly instead. |
| **Panel buttons pre-coloured rather than tinted** | GDD 4.3 called for neutral grey plus runtime tint. Measuring the reference showed button tones darken by **increasing saturation** (yellow #F9D160→#F7B92A, green #67EF77→#16E651). Unity's multiplicative tint scales all channels equally — it lowers brightness and cannot touch saturation, the exact opposite operation. HUD elements stayed neutral and tinted, because the reference recolours those per level theme. |
| **TrackPath corner acceleration not fixed** | Measured at ~57%; not noticeable at the current corner radius. Touching gameplay maths this close to delivery carries regression risk that outweighs the gain. |

---

## 5. Known issues

Recorded in full in [`KNOWN_ISSUES.md`](KNOWN_ISSUES.md). The two that matter most:

**GPU instancing is not enabled, and it is not just a checkbox.** `M_ToonCube` has
`m_EnableInstancingVariants: 0`, but `ToonCube.shader` also declares no
`#pragma multi_compile_instancing` and no per-instance properties — `_BaseColor`
lives in the `UnityPerMaterial` CBUFFER. Since cube colour is driven through a
`MaterialPropertyBlock`, every cube also falls out of the SRP Batcher. On Level_10
that is roughly 1400 cubes, doubled by the outline pass. A device test showed a
mild framerate dip, but that test predates the boards growing to 1400 cubes, so a
fresh measurement is owed.

**The simulator is a lower bound, not ground truth.** `sim.py` reports Level_5 as
lost under all three policies, yet it is comfortable to win by hand. The bot can
miss an ordering. It is useful for catching *unwinnable* levels, not for
certifying difficulty.

---

## 6. What I'd do next

In priority order:

1. **Shader instancing.** Add `#pragma multi_compile_instancing` and move
   `_BaseColor` into a per-instance buffer, then switch `CubeView` to instanced
   properties. This is the one open item with a measurable effect on the product,
   and the largest boards are the ones that show it.

2. **Re-measure on device.** The last device test predates both the 1400-cube
   boards and the post-processing volume. Frame timing should be captured again
   before any performance claim is made.

3. **Linked shooters.** The data field exists and the mechanic is a genuine
   addition to the puzzle vocabulary — it is the first thing I would build with
   more time, ahead of any new visual work.

4. **Fix TrackPath corner acceleration.** The ~57% speed-up through corners is
   measured and understood; it needs arc-length reparameterisation of the corner
   arcs. Deferred purely on regression risk near delivery, not on difficulty.

5. **Background music and a park warning sound.** Both hooks were removed rather
   than left as empty inspector slots pretending to be features. The music volume
   slider in Settings is wired to a real API but currently controls nothing.

6. **More levels, and a difficulty curve derived from data.** With the simulator in
   place, level difficulty could be estimated rather than guessed — for example by
   measuring how close each policy comes to filling the park.
