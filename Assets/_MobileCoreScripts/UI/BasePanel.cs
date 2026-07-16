using UnityEngine;
using DG.Tweening;
namespace MobileCore
{
    public abstract class BasePanel : MonoBehaviour
    {
        private CanvasGroup canvasGroup;

        protected float fadeDuration = 0.5f;

        protected CanvasGroup CanvasGroup
        {
            get
            {
                if (canvasGroup == null)
                    canvasGroup = GetComponent<CanvasGroup>();
                return canvasGroup;
            }
        }

        public virtual void Show()
        {
            gameObject.SetActive(true);
            CanvasGroup.blocksRaycasts = true;
            CanvasGroup.alpha = 0f;
            CanvasGroup.DOFade(1, fadeDuration);
        }

        public virtual void Hide()
        {
            CanvasGroup.blocksRaycasts = false;
            CanvasGroup.DOFade(0, fadeDuration).OnComplete(() => gameObject.SetActive(false));
        }
    }
}