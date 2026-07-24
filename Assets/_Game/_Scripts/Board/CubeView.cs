using UnityEngine;
using MobileCore;
using System.Collections;
namespace Game
{
    public class CubeView : MonoBehaviour
    {
        [SerializeField] private Renderer cubeRenderer;

        public void SetColor(ColorId color)
        {
            cubeRenderer.material.color = ColorToUnityColor(color);
        }

        private Color ColorToUnityColor(ColorId color)
        {
            switch (color)
            {
                case ColorId.Red: return Color.red;
                case ColorId.Blue: return Color.blue;
                case ColorId.Green: return Color.green;
                case ColorId.Yellow: return Color.yellow;
                case ColorId.Purple: return new Color(0.6f, 0.2f, 0.8f);
                case ColorId.Crate: return new Color(0.4f, 0.25f, 0.1f);
                default: return Color.gray;
            }
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

            transform.localScale = startScale;   // pool'a dönünce bir sonraki spawn için sıfırla
            ObjectPooler.Instance.ReturnToPool("Cube", gameObject);
        }
    }
}