using UnityEngine;
using MobileCore;

namespace Game
{
    public class MenuPanel : BasePanel
    {
        public void OnPlayButtonClicked()
        {
            GameEvents.TriggerPlayRequested();
        }
    }
}