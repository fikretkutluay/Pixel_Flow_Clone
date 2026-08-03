using UnityEngine;

namespace Game
{
    [CreateAssetMenu(fileName = "GameConfig", menuName = "Scriptable Objects/GameConfig")]
    public class GameConfig : ScriptableObject
    {
        public int visibleQueueWindow = 3;

        [Header("Layout — fractions of screen height, must sum to 1.0")]
        public float topBarBand = 0.09f;
        public float boardBand  = 0.44f;
        public float parkBand   = 0.11f;
        public float queueBand  = 0.28f;
        public float bottomBand = 0.08f;

        [Header("Horizontal layout")]
        [Tooltip("Park/queue row width as a fraction of the screen, leaving side margins.")]
        public float contentWidthFactor = 0.92f;
        [Tooltip("Vertical spacing between queue slots, along the depth axis.")]
        public float queueSlotSpacing = 1.4f;

        // --- Cube gap: base + percentage (affine model) ---
        //
        // A pure percentage was wrong: as cells shrink (Level_10 at 40x35) the
        // absolute gap fell below one pixel and dissolved into the anti-aliasing.
        // A pure constant would be wrong too — measured against the reference, the
        // seam does vary with board density, but not proportionally:
        //
        //     cube 20.0px -> seam 3.0px (15.0%)
        //     cube 50.8px -> seam 5.0px ( 9.9%)
        //
        // The cube grows 2.5x while the seam grows 1.67x. The affine model solved
        // from these two points (base 1.7px + 6.5% of the cell) reproduces both
        // measurements and holds at either extreme. Both terms are in world units
        // and therefore resolution independent: the camera is locked to a fixed
        // world width, so a world unit is a fixed fraction of the screen.
        [Header("Visual spacing")]
        [Tooltip("Part of the gap that scales with cell size (6.5%, from the reference).")]
        public float cubeGap = 0.065f;
        [Tooltip("Part of the gap independent of cell size, in world units. This is " +
                 "what stops the seam vanishing on dense boards.")]
        public float cubeGapBaseWorld = 0.0158f;
        [Tooltip("Vertical gap as a fraction of the horizontal one. Below 1 the cube " +
                 "reads taller than it is wide, which is the bead proportion seen " +
                 "in the reference.")]
        [Range(0.1f, 1f)] public float cubeGapVerticalRatio = 0.5f;
        [Tooltip("Safety limit: the gap may never exceed this fraction of the cell.")]
        [Range(0.1f, 0.5f)] public float cubeGapMaxFraction = 0.35f;
        [Tooltip("Visual gap between a park slot and its neighbour, as a fraction.")]
        public float parkSlotGap = 0.15f;

        [Header("Rail speed")]
        [Tooltip("Seconds per lap. The rail is fixed in world space, so this maps " +
                 "directly to visual pace and is unaffected by board size. Global " +
                 "rather than per level: every level runs at the same speed.")]
        public float trackLapSeconds = 2.7f;

        [Header("Pressure ramp")]
        [Tooltip("Rail speeds up once rail + park reaches this many shooters.")]
        public int tensionShooterThreshold = 7;
        [Tooltip("Speed multiplier while under pressure.")]
        public float tensionSpeedMultiplier = 1.25f;
        [Tooltip("Settling time for a speed change, to avoid an abrupt jump.")]
        public float tensionRampSeconds = 0.35f;

        [Header("Loss warning")]
        [Tooltip("Final fraction of a lap. A shooter entering it with the park full raises the warning.")]
        [Range(0.05f, 0.5f)] public float warnLapFraction = 0.22f;
        [Tooltip("Flashes per warning.")]
        public int warnPulseCount = 2;
        [Tooltip("Duration of a single flash.")]
        public float warnPulseSeconds = 0.18f;

        [Header("Endgame run")]
        [Tooltip("Once queue + rail + park drops to this, the level can no longer be " +
                 "lost: shooters stop parking, the rail speeds up and crates lift away.")]
        public int endgameShooterThreshold = 5;
        [Tooltip("Speed multiplier during the endgame run.")]
        public float endgameSpeedMultiplier = 1.6f;
    }
}
