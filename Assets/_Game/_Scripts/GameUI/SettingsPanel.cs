using MobileCore;
using UnityEngine;
using UnityEngine.UI;

namespace Game
{
    /// <summary>
    /// Overlay panel. The sound slider is wired for real; the remaining rows are
    /// visual only and stay that way on purpose (GDD 5.6).
    ///
    /// Set the GameObject inactive in the scene — UIManager activates it on
    /// request. Nothing here deactivates itself, because Awake/Start first run at
    /// that moment and would close the panel as it opened.
    /// </summary>
    public class SettingsPanel : BasePanel
    {
        [SerializeField] private Slider soundSlider;

        private void Awake()
        {
            if (soundSlider == null) return;

            if (AudioManager.Instance != null)
                soundSlider.SetValueWithoutNotify(AudioManager.Instance.SfxVolume);

            soundSlider.onValueChanged.AddListener(HandleVolumeChanged);
        }

        private void OnDestroy()
        {
            if (soundSlider != null) soundSlider.onValueChanged.RemoveListener(HandleVolumeChanged);
        }

        private static void HandleVolumeChanged(float value)
        {
            if (AudioManager.Instance == null) return;
            AudioManager.Instance.SetSfxVolume(value);
            AudioManager.Instance.SetMusicVolume(value * 0.5f);   // music sits under sfx
        }

        /// <summary>Hook this to the close button's OnClick.</summary>
        public void OnCloseButtonClicked() => Hide();
    }
}
