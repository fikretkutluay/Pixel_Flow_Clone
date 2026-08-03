using UnityEngine;
using DG.Tweening;

namespace MobileCore
{
    /// <summary>
    /// Fade + pop shared by every panel.
    ///
    /// Both entry points kill their own tweens first. Without that, asking a panel
    /// to open while it was still closing left a fade-out and a fade-in running on
    /// the same CanvasGroup, and the fade-out's OnComplete deactivated the object a
    /// moment after it had been shown — the panel simply never appeared.
    ///
    /// The pop scales <see cref="Body"/> rather than the panel root, so a
    /// full-screen dimmer behind the dialog does not grow with it. Resolves to a
    /// child called "Body" when there is one, otherwise the root, so no panel needs
    /// wiring by hand.
    /// </summary>
    [RequireComponent(typeof(CanvasGroup))]
    public abstract class BasePanel : MonoBehaviour
    {
        [Header("Panel transition")]
        [SerializeField] protected float fadeDuration = 0.28f;
        [Tooltip("Springs from this scale to 1 on open. 1 disables the pop.")]
        [SerializeField] private float popFrom = 0.86f;
        [SerializeField] private Ease popEase = Ease.OutBack;

        private CanvasGroup canvasGroup;
        private Transform body;
        private bool bodyResolved;

        protected CanvasGroup CanvasGroup
        {
            get
            {
                if (canvasGroup == null)
                    canvasGroup = GetComponent<CanvasGroup>();
                return canvasGroup;
            }
        }

        /// <summary>The body that scales; the full-screen dim is deliberately outside it.</summary>
        protected Transform Body
        {
            get
            {
                if (bodyResolved) return body;

                bodyResolved = true;
                Transform found = transform.Find("Body");
                body = found != null ? found : transform;
                return body;
            }
        }

        public virtual void Show()
        {
            KillTweens();

            gameObject.SetActive(true);
            CanvasGroup.blocksRaycasts = true;
            CanvasGroup.alpha = 0f;
            CanvasGroup.DOFade(1f, fadeDuration);

            if (popFrom > 0f && !Mathf.Approximately(popFrom, 1f))
            {
                Body.localScale = Vector3.one * popFrom;
                Body.DOScale(Vector3.one, fadeDuration).SetEase(popEase);
            }
        }

        public virtual void Hide()
        {
            KillTweens();

            CanvasGroup.blocksRaycasts = false;
            CanvasGroup.DOFade(0f, fadeDuration * 0.7f).OnComplete(() =>
            {
                Body.localScale = Vector3.one;
                gameObject.SetActive(false);
            });
        }

        /// <summary>Cancels any half-finished transition so open and close cannot overlap.</summary>
        protected void KillTweens()
        {
            CanvasGroup.DOKill();
            Body.DOKill();
        }
    }
}
