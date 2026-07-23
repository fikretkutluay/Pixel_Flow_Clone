using UnityEngine;
using MobileCore;

namespace Game
{
    public class LosePanel : BasePanel
    {
        public void OnRetryButtonClicked()
        {
            GameEvents.TriggerRetryRequested();
        }
    }
}