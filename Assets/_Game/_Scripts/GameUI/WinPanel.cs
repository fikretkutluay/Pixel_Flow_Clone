using DG.Tweening;
using MobileCore;
using UnityEngine;

namespace Game
{
    public class WinPanel : BasePanel
    {
        [Header("Kazanma sallanması")]
        [Tooltip("Açılışta gövdenin salınacağı açı.")]
        [SerializeField] private float wobbleAngle = 9f;
        [SerializeField] private int wobbleVibrato = 7;
        [SerializeField] private float wobbleSeconds = 0.55f;

        /// <summary>
        /// Kayıp paneliyle aynı sakin açılış olmasın: kazanma paneli yaylanarak
        /// gelir ve bir de sallanır. Dönme, ölçek pop'unun ÜSTÜNE biner — ikisi
        /// farklı özelliği sürdüğü için çakışmazlar.
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
