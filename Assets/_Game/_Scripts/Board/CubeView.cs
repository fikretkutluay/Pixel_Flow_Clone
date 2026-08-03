using DG.Tweening;
using MobileCore;
using UnityEngine;

namespace Game
{
    public class CubeView : MonoBehaviour
    {
        [SerializeField] private Renderer cubeRenderer;
        [SerializeField] private ColorPalette palette;

        [Header("Break")]
        [Tooltip("How far it swells before collapsing.")]
        [SerializeField] private float popScale = 1.35f;
        [SerializeField] private float popDuration = 0.07f;
        [SerializeField] private float collapseDuration = 0.19f;
        [SerializeField] private float spinDegrees = 220f;

        private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
        private MaterialPropertyBlock mpb;
        private Sequence breakSeq;
        private Vector3 liveScale;

        /// <summary>
        /// Which pool this came from. Crates have their own model and therefore their
        /// own pool, and must go home to the right one.
        /// </summary>
        public string PoolTag { get; set; } = BoardController.CubePoolTag;

        /// <summary>
        /// The prefab's own scale, captured before anything resizes it. Fitting a
        /// piece to its cell multiplies this rather than replacing it, so a prefab
        /// authored at 1:1:2 keeps that proportion at every board size.
        /// </summary>
        public Vector3 BaseScale { get; private set; } = Vector3.one;

        private void Awake()
        {
            BaseScale = transform.localScale;
        }

        public void SetColor(ColorId color)
        {
            // A MaterialPropertyBlock gives each instance its own colour without
            // cloning the material: one material (M_ToonCube), N colours.
            //
            // Note that this does take the renderer out of the SRP Batcher. Making it
            // batch again would need per-instance properties in the shader, which
            // ToonCube.shader does not declare yet.
            if (mpb == null) mpb = new MaterialPropertyBlock();
            cubeRenderer.GetPropertyBlock(mpb);
            mpb.SetColor(BaseColorId, palette.Of(color));
            cubeRenderer.SetPropertyBlock(mpb);
        }

        /// <summary>
        /// Swells, then collapses with a twist. <paramref name="delay"/> lets the
        /// tracer land first, so the cube reacts to being hit rather than ahead of it.
        /// </summary>
        public void PlayBreakAndReturn(float delay = 0f)
        {
            // BoardController scales cubes to the cell, so the rest size is whatever
            // it happens to be now — capture it rather than assuming one.
            liveScale = transform.localScale;

            breakSeq?.Kill();
            transform.DOKill();

            breakSeq = DOTween.Sequence();
            if (delay > 0f) breakSeq.AppendInterval(delay);
            breakSeq.Append(transform.DOScale(liveScale * popScale, popDuration).SetEase(Ease.OutQuad));
            breakSeq.Append(transform.DOScale(Vector3.zero, collapseDuration).SetEase(Ease.InBack));
            // Spun about Z, the axis the camera looks along — a Y spin would read as
            // the cube squashing rather than turning.
            breakSeq.Join(transform.DORotate(new Vector3(0f, 0f, spinDegrees),
                                             popDuration + collapseDuration, RotateMode.LocalAxisAdd)
                                   .SetEase(Ease.InQuad));
            breakSeq.OnComplete(Despawn);
        }

        /// <summary>
        /// Crates leaving once they stop mattering: rises and fades out rather than
        /// shattering, since nothing broke it — it is simply being taken away.
        /// </summary>
        public void PlayLiftAway()
        {
            liveScale = transform.localScale;

            breakSeq?.Kill();
            transform.DOKill();

            float rise = liveScale.y * 2.5f;

            breakSeq = DOTween.Sequence();
            breakSeq.Append(transform.DOMoveY(transform.position.y + rise, 0.45f)
                                     .SetEase(Ease.InBack));
            breakSeq.Join(transform.DOScale(Vector3.zero, 0.45f).SetEase(Ease.InQuad));
            breakSeq.Join(transform.DORotate(new Vector3(0f, 0f, 160f), 0.45f,
                                             RotateMode.LocalAxisAdd).SetEase(Ease.InOutQuad));
            breakSeq.OnComplete(Despawn);
        }

        private void Despawn()
        {
            breakSeq = null;
            transform.localScale = liveScale;
            transform.localRotation = Quaternion.identity;
            ObjectPooler.Instance.ReturnToPool(PoolTag, gameObject);
        }

        // Board.Clear can recycle a cube mid-break; leave the pool a clean object.
        private void OnDisable()
        {
            breakSeq?.Kill();
            breakSeq = null;
            transform.DOKill();
            if (liveScale != Vector3.zero) transform.localScale = liveScale;
            transform.localRotation = Quaternion.identity;
        }
    }
}
