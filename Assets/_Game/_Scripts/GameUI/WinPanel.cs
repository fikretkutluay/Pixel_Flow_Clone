using UnityEngine;
using MobileCore;

namespace Game
{
    public class WinPanel : BasePanel
    {
        public void OnNextButtonClicked()
        {
            GameEvents.TriggerPlayRequested();
        }
    }
}