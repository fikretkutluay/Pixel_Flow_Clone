using UnityEngine;
using TMPro;

namespace Game
{
    public class Shooter : MonoBehaviour
    {
        [SerializeField] private bool isHidden;
        public bool IsHidden => isHidden;
        [SerializeField] private ColorId color;
        [SerializeField] private int ammo;
        public ColorId Color => color;
        public int Ammo => ammo;
        public bool IsSpent => ammo <= 0;

        [Header("Görsel — CubeView ile AYNI materyal/palet (renk eşleşmesi şart)")]
        [SerializeField] private Renderer shooterRenderer;
        [SerializeField] private ColorPalette palette;

        [Tooltip("\"?\" atıcının materyali. Kuyruğun tepesine gelip açılana kadar " +
                 "gerçek rengi yerine bu görünür.")]
        [SerializeField] private Material hiddenMaterial;

        private Material revealedMaterial;
        private bool materialsCaptured;

        [Header("Gövde — döndürülen görsel (AmmoText bunun DIŞINDA olmalı)")]
        [SerializeField] private Transform bodyTransform;
        [Tooltip("Merminin çıktığı nokta. Boşsa gövdenin merkezi kullanılır.")]
        [SerializeField] private Transform muzzle;
        [Tooltip("Modelin kendi 'ön' yönü +X değilse düzeltme açısı (90 / -90 / 180 dene).")]
        [SerializeField] private float bodyFacingOffsetDeg = 0f;

        [Header("Kuyruk feedback'i — sadece kuyrukta anlamlı (ray/park'ta tam alpha kalır)")]
        [SerializeField] private TMP_Text ammoText;
        [SerializeField, Range(0f, 1f)] private float queueBackAlpha = 0.35f;

        private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
        private MaterialPropertyBlock mpb;

        private Quaternion bodyBaseRotation;
        private bool bodyBaseCaptured;

        public float Distance { get; set; }
        public TrackEdge LastFiredEdge { get; set; }
        public int LastFiredLane { get; set; }

        public bool IsWaitingForPark { get; set; }

        public Transform Body => bodyTransform;
        public Vector3 MuzzlePosition => muzzle != null ? muzzle.position : transform.position;
        public ShooterAnimator Animator { get; private set; }

        /// <summary>Palette colour this shooter shows — used to tint its tracer.</summary>
        public UnityEngine.Color DisplayColor =>
            palette != null ? palette.Of(isHidden ? ColorId.Purple : color) : UnityEngine.Color.white;

        private void Awake()
        {
            Animator = GetComponent<ShooterAnimator>();
            CaptureBodyBase();
        }

        public void Init(ColorId color, int ammo, bool isHidden)
        {
            this.color = color;
            this.ammo = ammo;
            this.isHidden = isHidden;
            ResetLap();
            ResetFacing();
            ApplyVisual();
            ApplyAmmoText();
            SetQueueFront(true);   // varsayılan: tam alpha (yalnızca QueueController arkaya düşürür)
        }

        /// <summary>
        /// Tur durumunu sıfırlar. Park'tan raya geri yollarken ZORUNLU:
        /// Distance perimetreyi aşmış halde kalırsa atıcı raya girer girmez
        /// yeniden "tur bitti" sayılır ve park'a geri düşer.
        /// </summary>
        public void ResetLap()
        {
            Distance = 0f;
            LastFiredLane = -1;
            LastFiredEdge = TrackEdge.Bottom;
            IsWaitingForPark = false;
        }

        public void Reveal()
        {
            if (!isHidden) return;

            isHidden = false;
            ApplyVisual();
            ApplyAmmoText();      // "?" yerini gerçek ammo alır
        }

        public void ConsumeAmmo()
        {
            if (ammo > 0)
                ammo--;
            ApplyAmmoText();
        }

        public bool HasFiredAt(TrackEdge edge, int lane)
        {
            return LastFiredEdge == edge && LastFiredLane == lane;
        }

        public void MarkFired(TrackEdge edge, int lane)
        {
            LastFiredEdge = edge;
            LastFiredLane = lane;
        }

        // QueueController.RefreshColumn her sütunda çağırır: index 0 = tıklanabilir
        // (tam alpha), gerisi soluk. Tek sinyal bu — mekanik zaten ikili.
        public void SetQueueFront(bool isFront)
        {
            if (ammoText == null) return;
            ammoText.alpha = isFront ? 1f : queueBackAlpha;
        }

        /// <summary>
        /// Gövdeyi verilen dünya-Z açısına döndürür. Kök dönmez, dolayısıyla
        /// AmmoText de dönmez (okunur kalır). Modelin kendi baz rotasyonu
        /// (X=180 gibi import düzeltmeleri) korunur.
        /// </summary>
        public void SetFacing(float zAngleDeg)
        {
            if (bodyTransform == null) return;
            CaptureBodyBase();
            bodyTransform.localRotation =
                bodyBaseRotation * Quaternion.Euler(0f, 0f, zAngleDeg + bodyFacingOffsetDeg);
        }

        /// <summary>Pool'dan yeniden çıkan atıcı, kuyrukta baz yönüne dönsün.</summary>
        public void ResetFacing()
        {
            if (bodyTransform == null) return;
            CaptureBodyBase();
            bodyTransform.localRotation = bodyBaseRotation;
        }

        private void CaptureBodyBase()
        {
            if (bodyBaseCaptured || bodyTransform == null) return;
            bodyBaseRotation = bodyTransform.localRotation;
            bodyBaseCaptured = true;
        }

        // Board küpleriyle AYNI materyal (M_ToonCube) + AYNI ColorPalette kullanır —
        // renk eşleşmesi tek doğruluk kaynağından geliyor. "?" atıcı ise kendi
        // materyaliyle çizilir; rengi ancak kuyruğun tepesine gelip açılınca görünür.
        private void ApplyVisual()
        {
            if (shooterRenderer == null) return;
            CaptureMaterials();

            if (isHidden && hiddenMaterial != null)
            {
                shooterRenderer.sharedMaterial = hiddenMaterial;
                // Renk bloğu temizlenmezse gizli materyal de o rengi alır.
                shooterRenderer.SetPropertyBlock(null);
                return;
            }

            if (revealedMaterial != null) shooterRenderer.sharedMaterial = revealedMaterial;
            if (palette == null) return;

            if (mpb == null) mpb = new MaterialPropertyBlock();
            shooterRenderer.GetPropertyBlock(mpb);
            mpb.SetColor(BaseColorId, palette.Of(color));
            shooterRenderer.SetPropertyBlock(mpb);
        }

        private void CaptureMaterials()
        {
            if (materialsCaptured || shooterRenderer == null) return;

            // Prefab'ın kendi materyali "açık" hâl; gizli materyal onun yerine geçer.
            revealedMaterial = shooterRenderer.sharedMaterial;
            materialsCaptured = true;
        }

        private void ApplyAmmoText()
        {
            if (ammoText == null) return;
            ammoText.text = isHidden ? "?" : ammo.ToString();
        }
    }
}