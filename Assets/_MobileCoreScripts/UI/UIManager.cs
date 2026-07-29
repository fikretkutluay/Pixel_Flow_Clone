using UnityEngine;

namespace MobileCore
{
    public class UIManager : MonoBehaviour
    {
        [Header("Screens")]
        [SerializeField] private BasePanel mainMenuPanel;
        [SerializeField] private BasePanel winPanel;
        [SerializeField] private BasePanel losePanel;

        [Header("Overlays")]
        [SerializeField] private BasePanel settingsPanel;
        [SerializeField] private BasePanel storePanel;
        [SerializeField] private BasePanel profilePanel;

        private BasePanel currentPanel;

        private void Start()
        {
            GameEvents.OnGameStarted += ShowMainMenu;
            GameEvents.OnLevelCompleted += ShowWinPanel;
            GameEvents.OnLevelFailed += ShowLosePanel;
            GameEvents.OnPlayRequested += HideCurrentPanel;
            GameEvents.OnRetryRequested += HideCurrentPanel;

            GameEvents.OnMainMenuRequested += ShowMainMenu;
            GameEvents.OnSettingsRequested += ShowSettings;
            GameEvents.OnStoreRequested += ShowStore;
            GameEvents.OnProfileRequested += ShowProfile;

            SwitchPanel(mainMenuPanel);
        }

        /// <summary>Replaces whatever screen is up. Overlays do not go through here.</summary>
        private void SwitchPanel(BasePanel newPanel)
        {
            if (currentPanel != null)
            {
                currentPanel.Hide();
            }

            if (newPanel == null) return;

            newPanel.Show();
            currentPanel = newPanel;
        }

        private void HideCurrentPanel()
        {
            if (currentPanel != null)
            {
                currentPanel.Hide();
                currentPanel = null;
            }
        }

        /// <summary>
        /// Opens a panel on top without disturbing the screen underneath, so the
        /// menu stays visible behind the settings dialog. Each overlay closes
        /// itself from its own close button.
        /// </summary>
        private static void OpenOverlay(BasePanel panel)
        {
            if (panel != null) panel.Show();
        }

        private void ShowMainMenu() { SwitchPanel(mainMenuPanel); }
        private void ShowWinPanel() { SwitchPanel(winPanel); }
        private void ShowLosePanel() { SwitchPanel(losePanel); }

        private void ShowSettings() { OpenOverlay(settingsPanel); }
        private void ShowStore() { OpenOverlay(storePanel); }
        private void ShowProfile() { OpenOverlay(profilePanel); }

        private void OnDestroy()
        {
            GameEvents.OnGameStarted -= ShowMainMenu;
            GameEvents.OnLevelCompleted -= ShowWinPanel;
            GameEvents.OnLevelFailed -= ShowLosePanel;
            GameEvents.OnPlayRequested -= HideCurrentPanel;
            GameEvents.OnRetryRequested -= HideCurrentPanel;

            GameEvents.OnMainMenuRequested -= ShowMainMenu;
            GameEvents.OnSettingsRequested -= ShowSettings;
            GameEvents.OnStoreRequested -= ShowStore;
            GameEvents.OnProfileRequested -= ShowProfile;
        }
    }
}
