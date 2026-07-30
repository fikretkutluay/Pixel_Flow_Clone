using UnityEngine;
using MobileCore;

namespace Game
{
    public class ParkController : MonoBehaviour
    {
        [SerializeField] private GameConfig config;
        [SerializeField] private GameObject slotViewPrefab;
        [SerializeField] private TrackController trackController;
        [SerializeField] private PointerRouter inputRouter;

        private BoundedBuffer<Shooter> parkBuffer;
        private ParkSlotView[] slotViews;
        private Camera mainCam;

        public bool HasFreeSlot => parkBuffer != null && parkBuffer.HasFreeSlot;
        public int Count => parkBuffer != null ? parkBuffer.Count : 0;

        private void OnEnable()
        {
            if (inputRouter != null)
                inputRouter.OnTap += HandleTap;
        }

        private void OnDisable()
        {
            if (inputRouter != null)
                inputRouter.OnTap -= HandleTap;
        }

        // Same pattern as QueueController.HandleTap: screen point → raycast → Shooter.
        // Difference: only shooters in the PARK buffer are relevant here.
        private void HandleTap(Vector2 screenPos)
        {
            if (mainCam == null || parkBuffer == null) return;

            Ray ray = mainCam.ScreenPointToRay(screenPos);
            if (!Physics.Raycast(ray, out RaycastHit hit)) return;

            Shooter s = hit.collider.GetComponent<Shooter>();
            if (s == null) return;
            if (!parkBuffer.Contains(s)) return;   // Shooters in the queue/rail aren't ours to handle

            TryLaunch(s);   // Silently rejected if the rail is full — see TryLaunch below
        }

        public void Init(int parkCapacity)
        {
            mainCam = Camera.main;
            parkBuffer = new BoundedBuffer<Shooter>(parkCapacity);
            parkBuffer.OnChanged += () => GameEvents.TriggerParkOccupancyChanged(parkBuffer.Count, parkBuffer.Capacity);

            slotViews = new ParkSlotView[parkCapacity];
            float usableWidth = GameLayout.VisibleWidth(mainCam) * config.contentWidthFactor;
            float spacing = usableWidth / parkCapacity;
            float scale = spacing * (1f - config.parkSlotGap);   // BoardController.SpawnCubeView ile aynı desen

            for (int i = 0; i < parkCapacity; i++)
            {
                GameObject obj = Instantiate(slotViewPrefab, SlotPosition(i), Quaternion.identity, transform);
                obj.transform.localScale = new Vector3(scale, scale, scale);
                slotViews[i] = obj.GetComponentInChildren<ParkSlotView>();
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

            shooter.ResetFacing();   // rayda kalan dönüşü sıfırla — park'ta dik dursun
            RefreshSlotPositions();
            return true;
        }

        // Sends a shooter back from park to the rail. Rejected if the rail is full
        // (this is half of the lose condition).
        public bool TryLaunch(Shooter shooter)
        {
            if (trackController == null) return false;
            if (!trackController.HasFreeTrackSlot) return false;
            if (!parkBuffer.TryRemove(shooter)) return false;

            shooter.ResetLap();                      // KRİTİK — bkz. Shooter.ResetLap
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
            float scale = spacing * (1f - config.parkSlotGap);
            Vector3 slotSize = new Vector3(scale, scale, 0.01f);

            Gizmos.color = Color.cyan;
            for (int i = 0; i < parkBuffer.Capacity; i++)
                Gizmos.DrawWireCube(SlotPosition(i), slotSize);
        }
    }
}