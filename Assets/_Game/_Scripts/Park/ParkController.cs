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

        [Tooltip("Beyond this the shooter is arriving from the rail and hops in; " +
                 "below it, it is just shuffling along the row.")]
        [SerializeField] private float arrivalDistance = 1.5f;

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

            // Rejected when the rail is full. Rather than stay silent, the shooter
            // shakes its head.
            if (!TryLaunch(s)) s.Animator?.ShakeDenied();
        }

        public void Init(int parkCapacity)
        {
            mainCam = Camera.main;
            parkBuffer = new BoundedBuffer<Shooter>(parkCapacity);
            parkBuffer.OnChanged += () => GameEvents.TriggerParkOccupancyChanged(parkBuffer.Count, parkBuffer.Capacity);

            slotViews = new ParkSlotView[parkCapacity];
            float usableWidth = GameLayout.VisibleWidth(mainCam) * config.contentWidthFactor;
            float spacing = usableWidth / parkCapacity;
            float scale = spacing * (1f - config.parkSlotGap);   // same pattern as BoardController.SpawnCubeView

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

            shooter.ResetFacing();   // drop the rail's rotation so it stands upright in the park
            RefreshSlotPositions();
            return true;
        }

        /// <summary>Where a shooter sits once parked — used to aim its landing hop.</summary>
        private int IndexOf(Shooter shooter)
        {
            int index = 0;
            foreach (Shooter s in parkBuffer)
            {
                if (s == shooter) return index;
                index++;
            }
            return -1;
        }

        // Sends a shooter back from park to the rail. Rejected if the rail is full
        // (this is half of the lose condition).
        public bool TryLaunch(Shooter shooter)
        {
            if (trackController == null) return false;
            if (!trackController.HasFreeTrackSlot) return false;
            if (!parkBuffer.TryRemove(shooter)) return false;

            shooter.ResetLap();                      // critical, see Shooter.ResetLap
            trackController.TryAddShooter(shooter);

            // A stretch, not a jump: the rail owns this shooter's position from the
            // next frame on, so a positional tween would be fought and lost.
            shooter.Animator?.PunchLaunch();
            GameEvents.TriggerShooterLaunched();

            RefreshSlotPositions();
            return true;
        }

        // The park lays out horizontally: slots centred on the park band and spread
        // across the screen width.
        private Vector3 SlotPosition(int index)
        {
            int capacity = parkBuffer.Capacity;
            float usableWidth = GameLayout.VisibleWidth(mainCam) * config.contentWidthFactor;
            float spacing = usableWidth / capacity;
            float x = -usableWidth * 0.5f + spacing * (index + 0.5f);
            float y = GameLayout.ParkBandCenterY(mainCam, config);
            return new Vector3(x, y, 0f);
        }

        // Everyone already parked slides across; the newcomer arcs in. Telling them
        // apart by distance keeps this from needing extra bookkeeping.
        private void RefreshSlotPositions()
        {
            int index = 0;
            foreach (var shooter in parkBuffer)
            {
                Vector3 target = SlotPosition(index);
                ShooterAnimator anim = shooter.Animator;

                if (anim == null)
                    shooter.transform.position = target;
                else if (Vector3.Distance(shooter.transform.position, target) > arrivalDistance)
                    anim.HopTo(target);
                else
                    anim.SlideTo(target);

                index++;
            }
        }

        /// <summary>Flashes every slot — the only visual cue that a loss is near.</summary>
        public void PulseWarning(int count, float pulseSeconds)
        {
            if (slotViews == null) return;
            foreach (var view in slotViews)
                if (view != null) view.Pulse(count, pulseSeconds);
        }
    }
}