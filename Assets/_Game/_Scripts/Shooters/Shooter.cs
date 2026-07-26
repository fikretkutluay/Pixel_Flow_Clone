using UnityEngine;

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

        private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
        private MaterialPropertyBlock mpb;

        public float Distance { get; set; }
        public TrackEdge LastFiredEdge { get; set; }
        public int LastFiredLane { get; set; }

        public bool IsWaitingForPark { get; set; }

        public void Init(ColorId color, int ammo, bool isHidden)
        {
            this.color = color;
            this.ammo = ammo;
            this.isHidden = isHidden;
            ResetLap();
            ApplyVisual();
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
            isHidden = false;
            ApplyVisual();   // gizli tondan gerçek renge geçiş
        }

        public void ConsumeAmmo()
        {
            if (ammo > 0)
                ammo--;
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

        // Board küpleriyle AYNI materyal (M_ToonCube) + AYNI ColorPalette kullanır —
        // renk eşleşmesi tek doğruluk kaynağından geliyor. isHidden iken gerçek renk
        // gösterilmez (görsel "?" atıcı gelene kadar geçici olarak Purple'a düşer).
        private void ApplyVisual()
        {
            if (shooterRenderer == null || palette == null) return;

            ColorId shown = isHidden ? ColorId.Purple : color;

            if (mpb == null) mpb = new MaterialPropertyBlock();
            shooterRenderer.GetPropertyBlock(mpb);
            mpb.SetColor(BaseColorId, palette.Of(shown));
            shooterRenderer.SetPropertyBlock(mpb);
        }
    }
}