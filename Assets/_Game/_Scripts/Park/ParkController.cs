using UnityEngine;
using MobileCore;

namespace Game
{
    public class ParkController : MonoBehaviour
    {
        [SerializeField] private GameConfig config;
        [SerializeField] private GameObject slotViewPrefab;

        private BoundedBuffer<Shooter> parkBuffer;
        private ParkSlotView[] slotViews;
        private Camera mainCam;

        public bool HasFreeSlot => parkBuffer != null && parkBuffer.HasFreeSlot;

        public void Init(int parkCapacity)
        {
            mainCam = Camera.main;
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

        // Park yatay dizilir: parkBand merkezinde, ekran genişliğine yayılmış slotlar.
        private Vector3 SlotPosition(int index)
        {
            int capacity = parkBuffer.Capacity;
            float usableWidth = GameLayout.VisibleWidth(mainCam) * config.contentWidthFactor;
            float spacing = usableWidth / capacity;
            float x = -usableWidth * 0.5f + spacing * (index + 0.5f);
            float y = GameLayout.ParkBandCenterY(mainCam, config);
            return new Vector3(x, y, 0f);
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

        // Park slotlarını yatay kutular olarak çizer — placeholder.
        private void OnDrawGizmos()
        {
            if (parkBuffer == null || mainCam == null || config == null) return;

            float usableWidth = GameLayout.VisibleWidth(mainCam) * config.contentWidthFactor;
            float spacing = usableWidth / parkBuffer.Capacity;
            Vector3 slotSize = new Vector3(spacing * 0.85f, spacing * 0.85f, 0.01f);

            Gizmos.color = Color.cyan;
            for (int i = 0; i < parkBuffer.Capacity; i++)
                Gizmos.DrawWireCube(SlotPosition(i), slotSize);
        }
    }
}