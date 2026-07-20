using UnityEngine;
using MobileCore;

namespace Game
{
    public class ParkController : MonoBehaviour
    {
        [SerializeField] private GameConfig config;

        private BoundedBuffer<Shooter> parkBuffer;

        public bool HasFreeSlot => parkBuffer != null && parkBuffer.HasFreeSlot;

        public void Init(int parkCapacity)
        {
            parkBuffer = new BoundedBuffer<Shooter>(parkCapacity);
        }
        private void Start()
        {
            Init(1);   // test amaçlı düşük kapasite, deadlock'u kolay tetiklemek için
        }
        public bool TryPark(Shooter shooter)
        {
            if (!parkBuffer.TryAdd(shooter)) return false;

            RefreshSlotPositions();
            return true;

        }

        public bool TryLaunch(Shooter shooter, TrackController trackController)
        {
            if (!trackController.HasFreeTrackSlot) return false;
            if (!parkBuffer.TryRemove(shooter)) return false;

            trackController.TryAddShooter(shooter);
            RefreshSlotPositions();
            return true;

        }

        private Vector3 SlotPosition(int index)
        {
            float x = config.parkOrigin.x;
            float y = config.parkOrigin.y - index * config.parkSlotSpacing;
            return new Vector3(x, y, 0);
        }

        private void RefreshSlotPositions()
        {
            int index = 0;
            foreach (var shooter in parkBuffer)
            {
                shooter.transform.position = SlotPosition(index);
                index++;
            }
        }
    }
}