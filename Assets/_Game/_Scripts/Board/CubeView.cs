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

        public void SetColor(ColorId color)
        {
            // MaterialPropertyBlock: materyali klonlamadan (draw call / instancing dostu)
            // instance başına renk verir. Tek materyal (M_ToonCube), N renk.
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
            breakSeq.Join(transform.DORotate(new Vector3(0f, spinDegrees, spinDegrees * 0.5f),
                                             popDuration + collapseDuration, RotateMode.LocalAxisAdd)
                                   .SetEase(Ease.InQuad));
            breakSeq.OnComplete(Despawn);
        }

        private void Despawn()
        {
            breakSeq = null;
            transform.localScale = liveScale;
            transform.localRotation = Quaternion.identity;
            ObjectPooler.Instance.ReturnToPool("Cube", gameObject);
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
