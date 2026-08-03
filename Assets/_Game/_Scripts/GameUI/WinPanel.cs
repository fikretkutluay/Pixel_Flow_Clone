using DG.Tweening;
using MobileCore;
using UnityEngine;

namespace Game
{
    public class WinPanel : BasePanel
    {
        [Header("Win wobble")]
        [Tooltip("Angle the body swings through on open.")]
        [SerializeField] private float wobbleAngle = 9f;
        [SerializeField] private int wobbleVibrato = 7;
        [SerializeField] private float wobbleSeconds = 0.55f;

        /// <summary>
        /// Deliberately not the same calm entrance as the lose panel: the win panel
        /// springs in and wobbles. The rotation rides on top of the scale pop —
        /// they drive different properties, so they do not fight.
        /// </summary>
        public override void Show()
        {
            base.Show();

            Body.localRotation = Quaternion.identity;
            Body.DOPunchRotation(new Vector3(0f, 0f, wobbleAngle),
                                 wobbleSeconds, wobbleVibrato, 0.8f);
        }

        public override void Hide()
        {
            base.Hide();
            Body.localRotation = Quaternion.identity;
        }

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
