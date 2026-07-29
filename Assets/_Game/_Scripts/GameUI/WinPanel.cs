using MobileCore;

namespace Game
{
    public class WinPanel : BasePanel
    {
        /// <summary>
        /// "Devam Et" returns to the main menu rather than jumping straight into
        /// the next level, matching the reference game. LevelManager has already
        /// advanced and saved the index by this point, so the menu's Play button
        /// opens the next level (GDD 5.2).
        /// </summary>
        public void OnContinueButtonClicked()
        {
            GameEvents.TriggerMainMenuRequested();
        }
    }
}
