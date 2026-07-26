using UnityEngine;
using MobileCore;
using System.Collections;

namespace Game
{
    public class CubeView : MonoBehaviour
    {
        [SerializeField] private Renderer cubeRenderer;
        [SerializeField] private ColorPalette palette;

        private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
        private MaterialPropertyBlock mpb;

        public void SetColor(ColorId color)
        {
            // MaterialPropertyBlock: materyali klonlamadan (draw call / instancing dostu)
            // instance başına renk verir. Tek materyal (M_ToonCube), N renk.
            if (mpb == null) mpb = new MaterialPropertyBlock();
            cubeRenderer.GetPropertyBlock(mpb);
            mpb.SetColor(BaseColorId, palette.Of(color));
            cubeRenderer.SetPropertyBlock(mpb);
        }

        public void PlayBreakAndReturn()
        {
            StartCoroutine(BreakRoutine());
        }

        private IEnumerator BreakRoutine()
        {
            float duration = 0.15f;
            float elapsed = 0f;
            Vector3 startScale = transform.localScale;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                transform.localScale = Vector3.Lerp(startScale, Vector3.zero, elapsed / duration);
                yield return null;
            }

            transform.localScale = startScale;
            ObjectPooler.Instance.ReturnToPool("Cube", gameObject);
        }
    }
}