using UnityEngine;

namespace Game
{
    [CreateAssetMenu(fileName = "GameConfig", menuName = "Scriptable Objects/GameConfig")]
    public class GameConfig : ScriptableObject
    {
        public int visibleQueueWindow = 3;

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

        // --- Küpler arası boşluk: TABAN + YÜZDE (afin model) ---
        //
        // Saf yüzde kullanmak hatalıydı: hücre küçüldükçe (Level_10, 40x35)
        // mutlak boşluk 1px'in altına düşüp anti-aliasing'de tamamen eriyordu.
        // Saf sabit de yanlış olurdu — referansta ölçtük, çizgi board yoğunluğuna
        // göre DEĞİŞİYOR ama orantılı değil:
        //
        //     küp 20.0px -> dikiş 3.0px (%15.0)
        //     küp 50.8px -> dikiş 5.0px (%9.9)
        //
        // Küp 2.5 kat büyürken dikiş 1.67 kat büyümüş. Bu iki noktadan çözülen
        // afin model (taban 1.7px + hücrenin %6.5'i) her iki ölçümü de tutturuyor
        // ve iki uçta da bozulmuyor. Her iki terim de dünya birimi olduğu için
        // çözünürlükten bağımsız: kamera sabit dünya genişliğine oturtulduğundan
        // dünya birimi = sabit ekran oranı.
        [Header("Görsel aralıklar")]
        [Tooltip("Boşluğun hücre boyutuyla ölçeklenen kısmı (referanstan: %6.5).")]
        public float cubeGap = 0.065f;
        [Tooltip("Boşluğun hücre boyutundan BAĞIMSIZ taban kısmı (dünya birimi). " +
                 "Yoğun board'larda çizginin kaybolmasını bu engelliyor.")]
        public float cubeGapBaseWorld = 0.0158f;
        [Tooltip("Dikey boşluk çarpanı: yatayın bu oranı. 1'den küçük tutmak küpü " +
                 "eninden uzun gösterir — referanstaki boncuk oranı buradan geliyor.")]
        [Range(0.1f, 1f)] public float cubeGapVerticalRatio = 0.5f;
        [Tooltip("Güvenlik sınırı: boşluk hücrenin bu oranını asla geçemez.")]
        [Range(0.1f, 0.5f)] public float cubeGapMaxFraction = 0.35f;
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