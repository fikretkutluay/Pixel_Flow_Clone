using UnityEngine;
using TMPro;
using MobileCore;
using DG.Tweening;
namespace Game
{
    public class HudPanel : BasePanel
    {
        [SerializeField] private TMP_Text levelText;
        [SerializeField] private TMP_Text remainingCubesText;
        [SerializeField] private TMP_Text trackOccupancyText;
        // No park occupancy readout: the slots themselves show it (GDD 5.3).

        private void Start()
        {
            CanvasGroup.alpha = 0f;
            CanvasGroup.blocksRaycasts = false;
        }

        private void OnEnable()
        {
            GameEvents.OnLevelStarted += UpdateLevel;
            GameEvents.OnRemainingCubesChanged += UpdateRemainingCubes;
            GameEvents.OnTrackOccupancyChanged += UpdateTrackOccupancy;
            GameEvents.OnPlayRequested += Show;
            GameEvents.OnRetryRequested += Show;
            GameEvents.OnLevelCompleted += Hide;
            GameEvents.OnLevelFailed += Hide;
        }

        private void OnDisable()
        {
            GameEvents.OnLevelStarted -= UpdateLevel;
            GameEvents.OnRemainingCubesChanged -= UpdateRemainingCubes;
            GameEvents.OnTrackOccupancyChanged -= UpdateTrackOccupancy;
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

        public void OnSettingsButtonClicked() => GameEvents.TriggerSettingsRequested();

        /// <summary>
        /// The one power-up that does something: restarts the level. Reuses the
        /// retry path, so it costs no new code (GDD 5.3).
        /// </summary>
        public void OnRestartButtonClicked() => GameEvents.TriggerRetryRequested();

        // Each readout is optional — an unassigned field means that element was
        // dropped from the layout, not that something is broken.
        private void UpdateLevel(int level)
        {
            if (levelText != null) levelText.text = $"Seviye {level}";
        }

        private void UpdateRemainingCubes(int count)
        {
            if (remainingCubesText != null) remainingCubesText.text = count.ToString();
        }

        private void UpdateTrackOccupancy(int count, int capacity)
        {
            if (trackOccupancyText != null) trackOccupancyText.text = $"{count}/{capacity}";
        }
    }
}