using System.Collections;
using UnityEngine;

namespace Game
{
    /// <summary>
    /// Görsel katman: yuvarlatılmış köşeli koyu panel + kayıp uyarısında
    /// kırmızı yanıp sönen kenarlık.
    ///
    /// Uyarı sürekli değil, sayılı bir atım. Sürekli yanıp sönen bir kenarlık
    /// kısa sürede arka plana karışıyor; asıl mesele oyuncunun tam da bir atıcı
    /// inmek üzereyken uyarılması, o yüzden uyarı her seferinde yeniden atıyor.
    /// </summary>
    public class ParkSlotView : MonoBehaviour
    {
        [SerializeField] private Renderer borderRenderer;
        [SerializeField] private Color idleColor = new Color(0.25f, 0.25f, 0.3f);
        [SerializeField] private Color alertColor = new Color(1f, 0.23f, 0.23f);

        private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
        private MaterialPropertyBlock mpb;
        private Coroutine pulseRoutine;

        private void Awake()
        {
            SetBorderColor(idleColor);
        }

        private void OnDisable()
        {
            pulseRoutine = null;
            SetBorderColor(idleColor);
        }

        /// <summary>Flashes the border a set number of times, then settles.</summary>
        public void Pulse(int count, float pulseSeconds)
        {
            if (!isActiveAndEnabled) return;

            if (pulseRoutine != null) StopCoroutine(pulseRoutine);
            pulseRoutine = StartCoroutine(PulseRoutine(Mathf.Max(count, 1),
                                                       Mathf.Max(pulseSeconds, 0.05f)));
        }

        private IEnumerator PulseRoutine(int count, float pulseSeconds)
        {
            var half = new WaitForSeconds(pulseSeconds * 0.5f);

            for (int i = 0; i < count; i++)
            {
                SetBorderColor(alertColor);
                yield return half;
                SetBorderColor(idleColor);
                yield return half;
            }

            pulseRoutine = null;
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
