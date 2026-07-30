using UnityEngine;

namespace Game
{
    /// <summary>
    /// Ortografik kamerayı GENİŞLİĞE kilitler: board her cihazda yatayda
    /// aynı dünya-genişliğini kaplar, en-boy farkı dikeyde açılıp kapanır.
    /// Oyuna özgü bir framing kuralı olduğu için Core'da değil Game'de.
    /// </summary>
    [ExecuteAlways]
    [RequireComponent(typeof(Camera))]
    public class CameraFitter : MonoBehaviour
    {
        [SerializeField] private Camera cam;
        [Tooltip("Ekranın yatayda kaplayacağı sabit dünya genişliği. " +
                 "Rayın dış genişliğinden biraz büyük olmalı — board artık " +
                 "rayın içine sığdırıldığı için ölçüyü ray belirliyor.")]
        [SerializeField] private float visibleWorldWidth = 10f;

        private float lastAspect = -1f;

        private void Awake()
        {
            if (cam == null) cam = GetComponent<Camera>();
            cam.orthographic = true;
            Fit();
        }

        // Editor'de Game view aspect'ini değiştirerek test ederken (adım 8)
        // ve olası çözünürlük değişiminde canlı yeniden hesaplasın diye.
        // Her frame'de değil, yalnızca aspect gerçekten değişince iş yapar.
        private void Update()
        {
            if (!Mathf.Approximately(cam.aspect, lastAspect))
                Fit();
        }

        private void Fit()
        {
            cam.orthographicSize = visibleWorldWidth / (2f * cam.aspect);
            lastAspect = cam.aspect;
        }
    }
}