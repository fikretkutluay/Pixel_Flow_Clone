using MobileCore;

namespace Game
{
    public class MenuPanel : BasePanel
    {
        public void OnPlayButtonClicked()
        {
            GameEvents.TriggerPlayRequested();
        }

        public void OnSettingsButtonClicked()
        {
            GameEvents.TriggerSettingsRequested();
        }

        public void OnStoreButtonClicked()
        {
            GameEvents.TriggerStoreRequested();
        }

        public void OnProfileButtonClicked()
        {
            GameEvents.TriggerProfileRequested();
        }
    }
}
