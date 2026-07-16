using UnityEngine;

namespace Game
{
    public class Shooter : MonoBehaviour
    {
        [SerializeField] private ColorId color;
        [SerializeField] private int ammo;

        public ColorId Color => color;
        public int Ammo => ammo;
        public bool IsSpent => ammo <= 0;

        public float Distance { get; set; }
        public TrackEdge LastFiredEdge { get; set; }
        public int LastFiredLane { get; set; }

        public void Init(ColorId color, int ammo)
        {
            this.color = color;
            this.ammo = ammo;
            Distance = 0f;
            LastFiredLane = -1;
            LastFiredEdge = TrackEdge.Bottom;
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
    }
}