using System.Collections;
using UnityEngine;

namespace Game
{
    /// <summary>
    /// Görsel katman: yuvarlatılmış köşeli koyu panel + rescue sırasında
    /// kırmızı yanıp sönen kenarlık. SetAlert(bool) sözleşmesi LineRenderer
    /// sürümüyle birebir aynı — ParkController'da hiçbir değişiklik gerekmez.
    /// </summary>
    public class ParkSlotView : MonoBehaviour
    {
        [SerializeField] private Renderer borderRenderer;
        [SerializeField] private Color idleColor = new Color(0.25f, 0.25f, 0.3f);
        [SerializeField] private Color alertColor = new Color(0.9f, 0.15f, 0.15f);
        [SerializeField] private float blinkInterval = 0.3f;

        private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
        private MaterialPropertyBlock mpb;
        private Coroutine blinkRoutine;

        private void Awake()
        {
            SetBorderColor(idleColor);
        }

        public void SetAlert(bool active)
        {
            if (active && blinkRoutine == null)
                blinkRoutine = StartCoroutine(BlinkRoutine());
            else if (!active && blinkRoutine != null)
            {
                StopCoroutine(blinkRoutine);
                blinkRoutine = null;
                SetBorderColor(idleColor);
            }
        }
        private IEnumerator BlinkRoutine()
        {
            bool on = false;
            while (true)
            {
                SetBorderColor(on ? alertColor : idleColor);
                on = !on;
                yield return new WaitForSeconds(blinkInterval);
            }
        }

        private void SetBorderColor(Color c)
        {
            if (borderRenderer == null) return;
            if (mpb == null) mpb = new MaterialPropertyBlock();
            borderRenderer.GetPropertyBlock(mpb);
            mpb.SetColor(BaseColorId, c);
            borderRenderer.SetPropertyBlock(mpb);
        }
    }
}