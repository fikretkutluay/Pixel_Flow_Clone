using DG.Tweening;
using UnityEngine;
using TMPro;

namespace Game
{
    public class Shooter : MonoBehaviour
    {
        [SerializeField] private bool isHidden;
        public bool IsHidden => isHidden;
        [SerializeField] private ColorId color;
        [SerializeField] private int ammo;
        public ColorId Color => color;
        public int Ammo => ammo;
        public bool IsSpent => ammo <= 0;

        [Header("Visuals — must share CubeView's material and palette")]
        [SerializeField] private Renderer shooterRenderer;
        [SerializeField] private ColorPalette palette;

        [Tooltip("Material for a \"?\" shooter. Shown instead of the real colour " +
                 "until the shooter reaches the front of the queue and reveals.")]
        [SerializeField] private Material hiddenMaterial;

        [Tooltip("How long the \"?\" pattern takes to lift off the body. 0 = instant.")]
        [SerializeField] private float revealSeconds = 0.45f;
        [Tooltip("Fraction the pattern tiling drops to during the reveal. Lower " +
                 "values enlarge the glyphs more.")]
        [SerializeField, Range(0.1f, 1f)] private float revealSpread = 0.55f;

        private Material revealedMaterial;
        private bool materialsCaptured;
        private Tween revealTween;

        [Header("Body — the rotated visual. AmmoText must sit OUTSIDE this transform")]
        [SerializeField] private Transform bodyTransform;
        [Tooltip("Where the projectile leaves from. Falls back to the body centre.")]
        [SerializeField] private Transform muzzle;
        [Tooltip("Correction angle if the model's own forward is not +X (try 90 / -90 / 180).")]
        [SerializeField] private float bodyFacingOffsetDeg = 0f;

        [Header("Queue feedback — only meaningful in the queue; full alpha on rail and park")]
        [SerializeField] private TMP_Text ammoText;
        [SerializeField, Range(0f, 1f)] private float queueBackAlpha = 0.35f;

        private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
        private static readonly int PatternStrengthId = Shader.PropertyToID("_PatternStrength");
        private static readonly int PatternTilingId = Shader.PropertyToID("_PatternTiling");
        private MaterialPropertyBlock mpb;

        private Quaternion bodyBaseRotation;
        private bool bodyBaseCaptured;

        public float Distance { get; set; }
        public TrackEdge LastFiredEdge { get; set; }
        public int LastFiredLane { get; set; }

        public bool IsWaitingForPark { get; set; }

        public Transform Body => bodyTransform;
        public Vector3 MuzzlePosition => muzzle != null ? muzzle.position : transform.position;
        public ShooterAnimator Animator { get; private set; }

        /// <summary>
        /// Palette colour this shooter shows, used to tint its tracer. While hidden
        /// the real colour must not leak, so the "?" material's own tone is
        /// returned instead. This previously returned ColorId.Purple, but Purple is
        /// a playable colour and GDD 4.1 requires the "?" shooter not to collide
        /// with one.
        /// </summary>
        public UnityEngine.Color DisplayColor
        {
            get
            {
                if (isHidden)
                    return hiddenMaterial != null
                        ? hiddenMaterial.GetColor(BaseColorId)
                        : UnityEngine.Color.gray;

                return palette != null ? palette.Of(color) : UnityEngine.Color.white;
            }
        }

        private void Awake()
        {
            Animator = GetComponent<ShooterAnimator>();
            CaptureBodyBase();
        }

        public void Init(ColorId color, int ammo, bool isHidden)
        {
            // A shooter re-issued from the pool must not carry a half-finished
            // reveal, or the tween would overwrite the colour ApplyVisual sets.
            revealTween?.Kill();
            revealTween = null;

            this.color = color;
            this.ammo = ammo;
            this.isHidden = isHidden;
            ResetLap();
            ResetFacing();
            ApplyVisual();
            ApplyAmmoText();
            SetQueueFront(true);   // default to full alpha; only QueueController dims it
        }

        /// <summary>
        /// Resets lap state. Mandatory when sending a shooter from the park back to
        /// the rail: if Distance is left past the perimeter, the shooter counts as
        /// having finished a lap the moment it rejoins and drops straight back into
        /// the park.
        /// </summary>
        public void ResetLap()
        {
            Distance = 0f;
            LastFiredLane = -1;
            LastFiredEdge = TrackEdge.Bottom;
            IsWaitingForPark = false;
        }

        /// <summary>
        /// Reveals a "?" shooter once it reaches the front of the queue. The
        /// material does not swap in a single frame: the pattern grows as it fades
        /// while the body crosses to its real colour, so the "?" reads as lifting
        /// off the body. The normal material is restored at the end so the next
        /// shooter out of the pool starts clean.
        /// </summary>
        public void Reveal()
        {
            if (!isHidden) return;

            isHidden = false;
            ApplyAmmoText();      // the real ammo count replaces the "?"
            Animator?.PunchReveal();

            revealTween?.Kill();
            revealTween = null;

            bool canAnimate = shooterRenderer != null && hiddenMaterial != null
                              && palette != null && revealSeconds > 0f;
            if (!canAnimate)
            {
                ApplyVisual();
                return;
            }

            CaptureMaterials();

            UnityEngine.Color from = hiddenMaterial.GetColor(BaseColorId);
            UnityEngine.Color to = palette.Of(color);
            float baseTiling = hiddenMaterial.GetFloat(PatternTilingId);

            if (mpb == null) mpb = new MaterialPropertyBlock();

            // The hidden material stays on the renderer for the whole animation;
            // only the property block is driven. Lowering the tiling enlarges the
            // glyphs, and combined with the fade the pattern reads as scattering
            // away from the body.
            revealTween = DOVirtual.Float(1f, 0f, revealSeconds, t =>
                {
                    if (shooterRenderer == null) return;

                    shooterRenderer.GetPropertyBlock(mpb);
                    mpb.SetFloat(PatternStrengthId, t);
                    mpb.SetFloat(PatternTilingId,
                                 Mathf.Lerp(baseTiling * revealSpread, baseTiling, t));
                    mpb.SetColor(BaseColorId, UnityEngine.Color.Lerp(to, from, t));
                    shooterRenderer.SetPropertyBlock(mpb);
                })
                .SetEase(Ease.OutQuad)
                .OnComplete(() =>
                {
                    revealTween = null;
                    ApplyVisual();
                });
        }

        // A shooter can be returned to the pool mid-animation.
        private void OnDisable()
        {
            revealTween?.Kill();
            revealTween = null;
        }

        public void ConsumeAmmo()
        {
            if (ammo > 0)
                ammo--;
            ApplyAmmoText();
        }

        public bool HasFiredAt(TrackEdge edge, int lane)
        {
            return LastFiredEdge == edge && LastFiredLane == lane;
        }

        public void MarkFired(TrackEdge edge, int lane)
        {
            LastFiredEdge = edge;
            LastFiredLane = lane;
        }

        // Called by QueueController.RefreshColumn for every column: index 0 is the
        // tappable one (full alpha), the rest are dimmed. One signal is enough, the
        // mechanic itself is binary.
        public void SetQueueFront(bool isFront)
        {
            if (ammoText == null) return;
            ammoText.alpha = isFront ? 1f : queueBackAlpha;
        }

        /// <summary>
        /// Rotates the body to the given world-Z angle. The root does not rotate, so
        /// AmmoText stays upright and readable. The model's own base rotation
        /// (import fixes such as X=180) is preserved.
        /// </summary>
        public void SetFacing(float zAngleDeg)
        {
            if (bodyTransform == null) return;
            CaptureBodyBase();
            bodyTransform.localRotation =
                bodyBaseRotation * Quaternion.Euler(0f, 0f, zAngleDeg + bodyFacingOffsetDeg);
        }

        /// <summary>Returns a pooled shooter to its base facing for the queue.</summary>
        public void ResetFacing()
        {
            if (bodyTransform == null) return;
            CaptureBodyBase();
            bodyTransform.localRotation = bodyBaseRotation;
        }

        private void CaptureBodyBase()
        {
            if (bodyBaseCaptured || bodyTransform == null) return;
            bodyBaseRotation = bodyTransform.localRotation;
            bodyBaseCaptured = true;
        }

        // Uses the same material (M_ToonCube) and the same ColorPalette as the board
        // cubes, so colour matching comes from a single source of truth. A hidden
        // shooter is drawn with its own material and only shows its colour once it
        // reaches the front of the queue and reveals.
        private void ApplyVisual()
        {
            if (shooterRenderer == null) return;
            CaptureMaterials();

            if (isHidden && hiddenMaterial != null)
            {
                shooterRenderer.sharedMaterial = hiddenMaterial;
                // Without clearing the block the hidden material inherits that colour.
                shooterRenderer.SetPropertyBlock(null);
                return;
            }

            if (revealedMaterial != null) shooterRenderer.sharedMaterial = revealedMaterial;
            if (palette == null) return;

            if (mpb == null) mpb = new MaterialPropertyBlock();
            shooterRenderer.GetPropertyBlock(mpb);
            mpb.SetColor(BaseColorId, palette.Of(color));
            shooterRenderer.SetPropertyBlock(mpb);
        }

        private void CaptureMaterials()
        {
            if (materialsCaptured || shooterRenderer == null) return;

            // The prefab's own material is the revealed state; the hidden material
            // temporarily takes its place.
            revealedMaterial = shooterRenderer.sharedMaterial;
            materialsCaptured = true;
        }

        private void ApplyAmmoText()
        {
            if (ammoText == null) return;
            ammoText.text = isHidden ? "?" : ammo.ToString();
        }
    }
}