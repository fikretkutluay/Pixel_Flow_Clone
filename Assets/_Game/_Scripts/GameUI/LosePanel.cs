using MobileCore;

namespace Game
{
    public class LosePanel : BasePanel
    {
        public void OnRetryButtonClicked()
        {
            GameEvents.TriggerRetryRequested();
        }

        /// <summary>Closing a lost level drops back to the main menu.</summary>
        public void OnCloseButtonClicked()
        {
            GameEvents.TriggerMainMenuRequested();
        }
    }
}
