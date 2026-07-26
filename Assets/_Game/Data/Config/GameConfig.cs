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
        public float boardBand = 0.44f;
        public float parkBand = 0.11f;
        public float queueBand = 0.28f;
        public float bottomBand = 0.08f;

        [Header("Yatay yerleşim")]
        [Tooltip("Park/kuyruk satırının ekran genişliğine oranı (kenar boşluğu payı).")]
        public float contentWidthFactor = 0.92f;
        [Tooltip("Kuyrukta derinlik (dikey) yönünde slotlar arası aralık.")]
        public float queueSlotSpacing = 1.4f;
        [Tooltip("Park slotu ile komşu slot arası görsel boşluk oranı (cubeGap ile aynı mantık).")]
        public float parkSlotGap = 0.15f;

        [Tooltip("Ray ile board kenarı arası sabit dünya mesafesi (hücre boyutundan bağımsız).")]
        public float trackMargin = 0.3f;
        [Tooltip("Küpler arası görsel boşluk oranı (0 = bitişik, 0.1 = %10 boşluk).")]
        public float cubeGap = 0.16f;
    }
}