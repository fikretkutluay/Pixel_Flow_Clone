using DG.Tweening;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace MobileCore
{
    /// <summary>
    /// Press feedback for UI buttons: squash on press, spring back on release, and
    /// the click sound. Drop it next to a Button; no per-button setup needed.
    /// </summary>
    [RequireComponent(typeof(Button))]
    [DisallowMultipleComponent]
    public class ButtonPunch : MonoBehaviour, IPointerDownHandler, IPointerUpHandler
    {
        [SerializeField] private float pressedScale = 0.92f;
        [SerializeField] private float pressDuration = 0.07f;
        [SerializeField] private float releaseDuration = 0.28f;

        [Tooltip("Turn off for buttons that already make their own noise.")]
        [SerializeField] private bool playClickSound = true;

        private Button button;
        private Vector3 restScale;
        private Tween tween;

        private void Awake()
        {
            button = GetComponent<Button>();
            restScale = transform.localScale;
        }

        private void OnDisable()
        {
            tween?.Kill();
            transform.localScale = restScale;
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            if (button == null || !button.IsInteractable()) return;

            if (playClickSound && AudioManager.Instance != null)
                AudioManager.Instance.PlayUiClick();

            tween?.Kill();
            tween = transform.DOScale(restScale * pressedScale, pressDuration)
                             .SetEase(Ease.OutQuad)
                             .SetUpdate(true);          // keeps working if the game is paused
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            if (button == null || !button.IsInteractable()) return;

            tween?.Kill();
            tween = transform.DOScale(restScale, releaseDuration)
                             .SetEase(Ease.OutBack)
                             .SetUpdate(true);
        }
    }
}
