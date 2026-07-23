using UnityEngine;
namespace MobileCore
{
public class UIManager : MonoBehaviour
{
    [SerializeField] private BasePanel mainMenuPanel;
    [SerializeField] private BasePanel settingsPanel;
    [SerializeField] private BasePanel winPanel;
    [SerializeField] private BasePanel losePanel;

    private BasePanel currentPanel;
    
    private void Start()
    {
        GameEvents.OnGameStarted += ShowMainMenu;
        GameEvents.OnLevelCompleted += ShowWinPanel;
        GameEvents.OnLevelFailed += ShowLosePanel;
        GameEvents.OnPlayRequested += HideCurrentPanel;
        GameEvents.OnRetryRequested += HideCurrentPanel;

        SwitchPanel(mainMenuPanel);
    }

    private void SwitchPanel(BasePanel newPanel)
    {
        if (currentPanel != null)
        {
            currentPanel.Hide();
        }

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

    private void ShowMainMenu() { SwitchPanel(mainMenuPanel); }
    private void ShowWinPanel() { SwitchPanel(winPanel); }
    private void ShowLosePanel() { SwitchPanel(losePanel); }

    private void OnDestroy()
    {
        GameEvents.OnGameStarted -= ShowMainMenu;
        GameEvents.OnLevelCompleted -= ShowWinPanel;
        GameEvents.OnLevelFailed -= ShowLosePanel;
        GameEvents.OnPlayRequested -= HideCurrentPanel;
        GameEvents.OnRetryRequested -= HideCurrentPanel;
    }
}
}