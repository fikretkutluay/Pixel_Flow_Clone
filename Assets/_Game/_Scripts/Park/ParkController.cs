using UnityEngine;
using MobileCore;

namespace Game
{
    public class ParkController : MonoBehaviour
    {
        [SerializeField] private GameConfig config;
        [SerializeField] private GameObject slotViewPrefab;

        private BoundedBuffer<Shooter> parkBuffer;

        public bool HasFreeSlot => parkBuffer != null && parkBuffer.HasFreeSlot;
        private ParkSlotView[] slotViews;

        public void Init(int parkCapacity)
        {
            parkBuffer = new BoundedBuffer<Shooter>(parkCapacity);
            parkBuffer.OnChanged += () => GameEvents.TriggerParkOccupancyChanged(parkBuffer.Count, parkBuffer.Capacity);

            slotViews = new ParkSlotView[parkCapacity];
            for (int i = 0; i < parkCapacity; i++)
            {
                GameObject obj = Instantiate(slotViewPrefab, SlotPosition(i), Quaternion.identity, transform);
                slotViews[i] = obj.GetComponent<ParkSlotView>();
            }
        }

        public void Clear()
        {
            if (parkBuffer == null) return;
            foreach (Shooter s in parkBuffer)
                ObjectPooler.Instance.ReturnToPool("Shooter", s.gameObject);

            if (slotViews != null)
                foreach (var view in slotViews)
                    if (view != null) Destroy(view.gameObject);
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

        public void SetRescueAlert(bool active)
        {
            if (slotViews == null) return;
            foreach (var view in slotViews)
                view.SetAlert(active);
        }
    }
}