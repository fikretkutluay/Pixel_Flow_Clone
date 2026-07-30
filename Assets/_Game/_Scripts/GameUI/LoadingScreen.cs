using System.Collections;
using DG.Tweening;
using MobileCore;
using UnityEngine;

namespace Game
{
    /// <summary>
    /// Full-screen cover shown at boot and over level construction (GDD 5.1).
    ///
    /// Must be the LAST child of the Canvas so it draws over every panel, and it
    /// never deactivates itself — it has to stay alive to keep listening for the
    /// next transition.
    /// </summary>
    public class LoadingScreen : BasePanel
    {
        [Tooltip("How long the boot screen holds before revealing the menu.")]
        [SerializeField] private float bootSeconds = 1.2f;

        [Tooltip("How long the cover holds while a level is being built.")]
        [SerializeField] private float coverSeconds = 0.5f;

        private Coroutine routine;
        private Tween fade;

        private void Awake()
        {
            gameObject.SetActive(true);
            CanvasGroup.alpha = 1f;
            CanvasGroup.blocksRaycasts = true;
        }

        private void OnEnable()
        {
            GameEvents.OnPlayRequested += CoverTransition;
            GameEvents.OnRetryRequested += CoverTransition;
        }

        private void OnDisable()
        {
            GameEvents.OnPlayRequested -= CoverTransition;
            GameEvents.OnRetryRequested -= CoverTransition;
        }

        private void Start() => Cover(bootSeconds);

        private void CoverTransition() => Cover(coverSeconds);

        private void Cover(float seconds)
        {
            if (routine != null) StopCoroutine(routine);
            routine = StartCoroutine(CoverRoutine(seconds));
        }

        private IEnumerator CoverRoutine(float seconds)
        {
            Show();
            yield return new WaitForSeconds(seconds);
            Hide();
            routine = null;
        }

        /// <summary>Appears instantly — a cover that faded in would show the seam.</summary>
        public override void Show()
        {
            gameObject.SetActive(true);
            fade?.Kill();
            CanvasGroup.alpha = 1f;
            CanvasGroup.blocksRaycasts = true;
        }

        /// <summary>Fades out but stays active, so it can cover the next transition.</summary>
        public override void Hide()
        {
            CanvasGroup.blocksRaycasts = false;
            fade?.Kill();
            fade = CanvasGroup.DOFade(0f, fadeDuration);
        }

        private void OnDestroy() => fade?.Kill();
    }
}
