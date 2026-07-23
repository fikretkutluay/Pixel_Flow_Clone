using UnityEngine;
using TMPro;
using MobileCore;
using DG.Tweening;
namespace Game
{
    public class HudPanel : BasePanel
    {
        [SerializeField] private TMP_Text remainingCubesText;
        [SerializeField] private TMP_Text trackOccupancyText;
        [SerializeField] private TMP_Text parkOccupancyText;

        private void Start()
        {
            CanvasGroup.alpha = 0f;
            CanvasGroup.blocksRaycasts = false;
        }

        private void OnEnable()
        {
            GameEvents.OnRemainingCubesChanged += UpdateRemainingCubes;
            GameEvents.OnTrackOccupancyChanged += UpdateTrackOccupancy;
            GameEvents.OnParkOccupancyChanged += UpdateParkOccupancy;
            GameEvents.OnPlayRequested += Show;
            GameEvents.OnRetryRequested += Show;
            GameEvents.OnLevelCompleted += Hide;
            GameEvents.OnLevelFailed += Hide;
        }

        private void OnDisable()
        {
            GameEvents.OnRemainingCubesChanged -= UpdateRemainingCubes;
            GameEvents.OnTrackOccupancyChanged -= UpdateTrackOccupancy;
            GameEvents.OnParkOccupancyChanged -= UpdateParkOccupancy;
            GameEvents.OnPlayRequested -= Show;
            GameEvents.OnRetryRequested -= Show;
            GameEvents.OnLevelCompleted -= Hide;
            GameEvents.OnLevelFailed -= Hide;
        }

        public override void Hide()
        {
            CanvasGroup.blocksRaycasts = false;
            CanvasGroup.DOFade(0, fadeDuration);
        }

        private void UpdateRemainingCubes(int count) => remainingCubesText.text = count.ToString();
        private void UpdateTrackOccupancy(int c, int cap) => trackOccupancyText.text = $"{c}/{cap}";
        private void UpdateParkOccupancy(int c, int cap) => parkOccupancyText.text = $"{c}/{cap}";
    }
}