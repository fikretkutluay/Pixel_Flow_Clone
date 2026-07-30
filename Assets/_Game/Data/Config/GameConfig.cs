using UnityEngine;

namespace Game
{
    [CreateAssetMenu(fileName = "GameConfig", menuName = "Scriptable Objects/GameConfig")]
    public class GameConfig : ScriptableObject
    {
        public int visibleQueueWindow = 3;
        public float boardPhysicalSize = 7f;

        [Header("Layout — ekran yüksekliğinin oranı (toplam 1.0 olmalı)")]
        public float topBarBand = 0.09f;
        public float boardBand  = 0.44f;
        public float parkBand   = 0.11f;
        public float queueBand  = 0.28f;
        public float bottomBand = 0.08f;

        [Header("Yatay yerleşim")]
        [Tooltip("Park/kuyruk satırının ekran genişliğine oranı (kenar boşluğu payı).")]
        public float contentWidthFactor = 0.92f;
        [Tooltip("Kuyrukta derinlik (dikey) yönünde slotlar arası aralık.")]
        public float queueSlotSpacing = 1.4f;

        [Header("Görsel aralıklar")]
        [Tooltip("Küpler arası YATAY boşluk oranı (0 = bitişik, 0.1 = %10 boşluk).")]
        public float cubeGap = 0.16f;
        [Tooltip("Küpler arası DİKEY boşluk. Yataydan küçük tutmak küpü eninden " +
                 "uzun gösterir — referanstaki boncuk oranı buradan geliyor.")]
        public float cubeGapVertical = 0.04f;
        [Tooltip("Park slotu ile komşu slot arası görsel boşluk oranı.")]
        public float parkSlotGap = 0.15f;

        [Header("Ray hızı")]
        [Tooltip("Bir turun kaç saniye sürdüğü. Ray dünyada sabit olduğu için bu " +
                 "değer görsel hızı doğrudan verir ve board boyutundan etkilenmez. " +
                 "Oyun geneli: her level aynı hızda akar.")]
        public float trackLapSeconds = 2.7f;

        [Header("Baskı hızlanması")]
        [Tooltip("Ray + park toplamı bu sayıya ulaşınca ray hızlanır.")]
        public int tensionShooterThreshold = 7;
        [Tooltip("Baskı altındayken hız çarpanı.")]
        public float tensionSpeedMultiplier = 1.25f;
        [Tooltip("Hız değişiminin oturma süresi — ani sıçramayı önler.")]
        public float tensionRampSeconds = 0.35f;

        [Header("Kayıp uyarısı")]
        [Tooltip("Turun son yüzdesi — atıcı buraya girince park doluysa uyarı yanar.")]
        [Range(0.05f, 0.5f)] public float warnLapFraction = 0.22f;
        [Tooltip("Uyarı başına kaç kez yanıp söner.")]
        public int warnPulseCount = 2;
        [Tooltip("Bir yanıp sönmenin süresi.")]
        public float warnPulseSeconds = 0.18f;

        [Header("Bitiş koşusu")]
        [Tooltip("Kuyruk + ray + park toplamı buraya inince kaybetmek imkânsız hâle " +
                 "gelir: atıcılar artık park etmez, ray hızlanır, sandıklar kalkar.")]
        public int endgameShooterThreshold = 5;
        [Tooltip("Bitiş koşusundaki hız çarpanı.")]
        public float endgameSpeedMultiplier = 1.6f;
    }
}