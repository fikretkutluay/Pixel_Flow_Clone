# Known Issues

An honest account of what is wrong, incomplete or unverified at delivery. Items
that were *decided* rather than missed are in
[`DEVELOPMENT_NOTE.md`](DEVELOPMENT_NOTE.md) section 4.

---

## Performance

### GPU instancing is not enabled, and enabling it is not a one-line change

**Severity:** the main open technical item.

`M_ToonCube` has `m_EnableInstancingVariants: 0`. But ticking that box alone would
change nothing, for two reasons found in the shader:

- `ToonCube.shader` declares no `#pragma multi_compile_instancing` in any of its
  three passes, and no `UNITY_VERTEX_INPUT_INSTANCE_ID` / `UNITY_INSTANCING_BUFFER`.
- `_BaseColor` sits in the `UnityPerMaterial` CBUFFER, i.e. per-material, not
  per-instance.

Meanwhile cube colour is driven through a `MaterialPropertyBlock`
(`CubeView.SetColor`), which takes each renderer **out of the SRP Batcher**. The
result on the densest board (Level_10, 1400 cubes) is roughly one batch per cube,
doubled by the outline pass.

**Fix:** add the instancing pragma, move `_BaseColor` into a per-instance buffer,
and switch `CubeView` to instanced properties. Not attempted before delivery
because it touches the shader every visible object uses.

**Measured impact:** unknown. A mild framerate dip was observed on device, but that
test predates the boards growing to 1400 cubes and predates the post-processing
volume. No current measurement exists — see below.

### No current on-device performance measurement

An APK was built and played on a physical Android device, and the game works. That
test happened **before** the largest boards and the post-processing volume were
added, so the numbers from it can no longer be quoted. Frame timing should be
recaptured before any performance claim is made.

---

## Content and polish

### No background music, no park warning sound

Neither clip was ever sourced. Rather than leave empty inspector slots pretending
to be features, the `backgroundMusic` and `rescueWarningClip` fields were removed
from `AudioManager`, along with the now-unused `GameEvents.OnRescueStarted`.

**Visible consequence:** the music volume slider in the Settings panel is wired to
a real API (`AudioManager.SetMusicVolume`) but currently controls nothing audible.

Five sound effects are present and working: cube break, shooter launch, UI click,
and the win/lose stingers.

### Level_9 and Level_10 are the least play-tested

Both are large (700 and 1400 cubes) and were finished late. They pass the simulator
and have been completed by hand, but they have had far fewer playthroughs than
Levels 1–6.

### Level_8's corner lock is deliberate but fragile

Level_8 places crates so that one colour is enclosed on all four sides. That colour
only becomes reachable during the endgame run — and the endgame run waits for the
shooter count to drop to 5. The level is winnable and verified, but the pattern is
close to a genuine deadlock and should not be copied into new levels without
simulating it first.

---

## Tooling

### The level simulator is a lower bound, not ground truth

`Tools/levelgen/sim.py` reports **Level_5 as lost under all three player
policies**, yet the level is comfortable to win by hand. The bot can miss a viable
ordering.

**How to read it:** a "lost" verdict is a prompt to check the level by hand, not
proof it is broken. A "won" verdict is trustworthy. The tool is reliable for
catching *unwinnable* levels, not for grading difficulty.

### `build.py` is abandoned

`Tools/levelgen/build.py` generated levels in bulk. All ten shipped levels are
hand-authored; the generated ones were discarded. The script is left in the repo
for reference but is not part of any workflow and is not maintained.

---

## Verification gaps

These are things not confirmed rather than things known to be broken.

| Item | Status |
|---|---|
| **Console zero errors / zero warnings** | Last confirmed before the final cleanup pass. Should be re-checked on a fresh open, since assets were deleted (TMP Examples, `ToonCube_SG.shadergraph`) and serialized audio fields were removed from the scene. |
| **Unit tests (17: LaneRaycaster 7, BoundedBuffer 10)** | Written and previously passing. `BoundedBuffer.Clear()` was added after the last full run, so a re-run is owed. |
| **Gameplay video (>= 3 levels)** | Not recorded. Should follow the instancing work, or it will capture stutter that the fix would have removed. |
| **APK in the delivery package** | A working APK was produced during development but is not included in the handover package. |
| **Aspect ratios beyond the test device** | The camera locks to a fixed world width, so portrait ratios should frame correctly by construction, but only one physical device was tested. |

---

## Minor / cosmetic

- **TrackPath corner acceleration.** Shooters move about 57% faster through the
  rounded corners than along the straights, because the path is parameterised by
  distance rather than arc length. Measured and understood; not noticeable at the
  current corner radius. Deferred on regression risk, not difficulty.

- **`Assets/_Recovery/0.unity`** is a Unity crash-recovery scene backup left on
  disk. It is gitignored and not part of the project, but it is still in the
  working directory.

- **`GameLayout.VisibleHeight`** is retained as a reference value; the band
  calculations no longer use it.
